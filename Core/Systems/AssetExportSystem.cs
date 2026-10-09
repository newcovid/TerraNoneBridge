using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria; // 确保引用 Main
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;
using TerraNoneBridge.Core.Commands;

namespace TerraNoneBridge.Core.Systems
{
    public class AssetExportSystem : ModSystem
    {
        // ================= 参数配置区域 =================

        private const float ExportTimeBudget = 0.012f;
        private const int ProgressReportStep = 4;
        private const int PercentBase = 100;
        private const int FileWriteBufferSize = 65536;

        // ==============================================

        public static volatile bool IsExporting = false;
        private static readonly object _syncLock = new object();

        private class AssetTask
        {
            public string Category;
            public int Index;
            // 存储 Asset 名称而不是引用，因为引用可能会在重载时失效
            public string AssetName;
            public Asset<Texture2D> OriginalRef;
        }

        private static Queue<AssetTask> _pendingAssets = new Queue<AssetTask>();
        private static HashSet<string> _processedPaths = new HashSet<string>();
        private static HashSet<string> _createdDirectories = new HashSet<string>();

        private static int _totalCount = 0;
        private static int _processedCount = 0;
        private static int _lastReportedPercent = -1;
        private static string _currentExportRoot = "";
        private static ICmdCaller _caller;

        private static readonly HashSet<string> _coreCategories = new HashSet<string>
        {
            "Item",
            "Npc",
            "Buff"
        };

        public override void Unload()
        {
            lock (_syncLock)
            {
                IsExporting = false;
                _caller = null;
                _currentExportRoot = "";
                _pendingAssets.Clear();
                _processedPaths.Clear();
                _createdDirectories.Clear();
            }
        }

        public static void StartExport(string savePath, ICmdCaller caller, bool exportAll)
        {
            if (IsExporting)
            {
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.Busy"));
                return;
            }

            lock (_syncLock)
            {
                if (IsExporting) return;

                _currentExportRoot = savePath;
                _caller = caller;

                _pendingAssets.Clear();
                _processedPaths.Clear();
                _createdDirectories.Clear();
                _processedCount = 0;
                _lastReportedPercent = -1;

                try
                {
                    if (!Directory.Exists(_currentExportRoot))
                        Directory.CreateDirectory(_currentExportRoot);

                    string copyrightText = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.CopyrightNotice");
                    File.WriteAllText(Path.Combine(_currentExportRoot, "COPYRIGHT_WARNING.txt"), copyrightText);
                }
                catch (Exception ex)
                {
                    caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.ErrorCreateDir", ex.Message));
                    return;
                }

                CollectAssetsReflectively(exportAll);
                _totalCount = _pendingAssets.Count;

                IsExporting = true;
            }

            string modeText = exportAll
                ? Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.ModeAll")
                : Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.ModeCore");

            caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.Start", _totalCount, $"{_currentExportRoot} {modeText}"));
        }

        private static void CollectAssetsReflectively(bool exportAll)
        {
            Type type = typeof(TextureAssets);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Asset<Texture2D>[]))
                {
                    string category = field.Name;
                    if (!exportAll && !_coreCategories.Contains(category)) continue;

                    try
                    {
                        var assets = (Asset<Texture2D>[])field.GetValue(null);
                        if (assets != null)
                        {
                            for (int i = 0; i < assets.Length; i++)
                            {
                                // 必须检查 assets[i] 是否为 null
                                if (assets[i] != null)
                                {
                                    _pendingAssets.Enqueue(new AssetTask
                                    {
                                        Category = category,
                                        Index = i,
                                        OriginalRef = assets[i],
                                        AssetName = assets[i].Name // 预先保存 Name，防止后续访问出错
                                    });
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        public override void PostUpdateEverything()
        {
            if (!IsExporting) return;

            bool isEmpty;
            lock (_syncLock) isEmpty = _pendingAssets.Count == 0;

            if (isEmpty)
            {
                FinishExport();
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            long maxTicks = (long)(Stopwatch.Frequency * ExportTimeBudget);

            // 批量处理循环
            while (sw.ElapsedTicks < maxTicks)
            {
                AssetTask task = null;
                lock (_syncLock)
                {
                    if (_pendingAssets.Count > 0)
                        task = _pendingAssets.Dequeue();
                }

                if (task == null) break;

                // 这里的异常捕获至关重要，防止单次失败导致整个 Update 循环崩溃
                try
                {
                    ProcessSingleAsset(task);
                }
                catch (Exception ex)
                {
                    // 仅记录日志，不抛出
                    ModContent.GetInstance<TerraNoneBridge>().Logger.Warn($"[Export Error] {task.Category} index {task.Index}: {ex.Message}");
                }
            }
            sw.Stop();

            ReportProgress();
        }

        private static void ProcessSingleAsset(AssetTask task)
        {
            // 1. 基础空值防御
            if (task == null) return;
            if (task.OriginalRef == null && string.IsNullOrEmpty(task.AssetName)) return;

            _processedCount++;

            Asset<Texture2D> finalAsset = task.OriginalRef;

            // 2. 强制加载逻辑 (针对懒加载资源)
            try
            {
                // 如果原始引用未加载，或者已经被 Dispose
                if (finalAsset == null || finalAsset.State == AssetState.NotLoaded || finalAsset.State == AssetState.Loading)
                {
                    string loadPath = task.AssetName;

                    // 如果名称为空，尝试根据 ID 推断原版路径 (兜底策略)
                    if (string.IsNullOrEmpty(loadPath))
                    {
                        if (task.Category == "Item" && task.Index < ItemID.Count) loadPath = $"Images/Item_{task.Index}";
                        else if (task.Category == "Npc" && task.Index < NPCID.Count) loadPath = $"Images/NPC_{task.Index}";
                        else if (task.Category == "Buff" && task.Index < BuffID.Count) loadPath = $"Images/Buff_{task.Index}";
                    }

                    if (!string.IsNullOrEmpty(loadPath))
                    {
                        // 使用 ImmediateLoad 强制主线程加载
                        finalAsset = Main.Assets.Request<Texture2D>(loadPath, AssetRequestMode.ImmediateLoad);
                    }
                }

                // 再次等待，确保万无一失
                if (finalAsset != null && finalAsset.State != AssetState.Loaded)
                {
                    finalAsset.Wait();
                }
            }
            catch
            {
                // 如果加载过程报错（例如文件不存在），则跳过此文件
                return;
            }

            // 3. 获取纹理并检查有效性
            if (finalAsset == null || finalAsset.State != AssetState.Loaded) return;

            Texture2D tex = null;
            try { tex = finalAsset.Value; } catch { return; }

            if (tex == null || tex.IsDisposed) return;
            if (tex.GraphicsDevice == null || tex.GraphicsDevice.IsDisposed) return;
            if (tex.Width <= 0 || tex.Height <= 0) return;

            // 4. 路径计算
            string canonicalName = null;
            try { canonicalName = GetCanonicalPath(task); } catch { }

            if (string.IsNullOrEmpty(canonicalName)) canonicalName = task.AssetName;
            if (string.IsNullOrEmpty(canonicalName)) canonicalName = finalAsset.Name; // 双重保险

            // 如果仍然没有名字，无法导出，跳过
            if (string.IsNullOrEmpty(canonicalName)) return;

            lock (_syncLock)
            {
                if (_processedPaths.Contains(canonicalName)) return;
                _processedPaths.Add(canonicalName);
            }

            // 5. 文件名清洗
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (c != '/' && c != '\\') canonicalName = canonicalName.Replace(c, '_');
            }

            string relativePath = canonicalName;
            if (relativePath.StartsWith("Terraria/") || relativePath.StartsWith("Terraria\\"))
                relativePath = relativePath.Substring(9);

            string safeCategory = string.IsNullOrEmpty(task.Category) ? "Unknown" : task.Category;
            string root = _currentExportRoot;
            if (string.IsNullOrEmpty(root)) return;

            string fullPath = Path.Combine(root, safeCategory, relativePath + ".png");
            string dir = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(dir)) return;

            // 6. 写入磁盘
            try
            {
                lock (_syncLock)
                {
                    if (!_createdDirectories.Contains(dir))
                    {
                        Directory.CreateDirectory(dir);
                        _createdDirectories.Add(dir);
                    }
                }

                using (FileStream stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, FileWriteBufferSize))
                {
                    tex.SaveAsPng(stream, tex.Width, tex.Height);
                    stream.Flush();
                }
            }
            catch (Exception writeEx)
            {
                ModContent.GetInstance<TerraNoneBridge>().Logger.Warn($"[Export Write Fail] {fullPath}: {writeEx.Message}");
            }
        }

        private static string GetCanonicalPath(AssetTask task)
        {
            try
            {
                string path = null;
                // 只有当 Index 有效时才调用 ItemLoader
                if (task.Index < 0) return null;

                switch (task.Category)
                {
                    case "Item":
                        if (task.Index < ItemLoader.ItemCount)
                        {
                            // 对于原版物品，ItemLoader.GetItem 返回 null，这是正常的，不应报错
                            var item = ItemLoader.GetItem(task.Index);
                            if (item != null) path = item.Texture;
                        }
                        break;
                    case "Npc":
                        if (task.Index < NPCLoader.NPCCount)
                        {
                            var npc = NPCLoader.GetNPC(task.Index);
                            if (npc != null) path = npc.Texture;
                        }
                        break;
                    case "Projectile":
                        if (task.Index < ProjectileLoader.ProjectileCount)
                        {
                            var proj = ProjectileLoader.GetProjectile(task.Index);
                            if (proj != null) path = proj.Texture;
                        }
                        break;
                    case "Buff":
                        if (task.Index < BuffLoader.BuffCount)
                        {
                            var buff = BuffLoader.GetBuff(task.Index);
                            if (buff != null) path = buff.Texture;
                        }
                        break;
                    case "Tile":
                        if (task.Index < TileLoader.TileCount)
                        {
                            var tile = TileLoader.GetTile(task.Index);
                            if (tile != null) path = tile.Texture;
                        }
                        break;
                    case "Wall":
                        if (task.Index < WallLoader.WallCount)
                        {
                            var wall = WallLoader.GetWall(task.Index);
                            if (wall != null) path = wall.Texture;
                        }
                        break;
                }
                return path;
            }
            catch
            {
                return null;
            }
        }

        private static void FinishExport()
        {
            lock (_syncLock)
            {
                IsExporting = false;
                _processedPaths.Clear();
                _createdDirectories.Clear();
            }

            ReportProgress();

            if (_caller != null)
                _caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.Complete", _processedCount, _currentExportRoot));

            _caller = null;
        }

        private static void ReportProgress()
        {
            if (_totalCount <= 0) return;

            int percent = (int)((float)_processedCount / _totalCount * PercentBase);

            if (percent > _lastReportedPercent + ProgressReportStep || percent == PercentBase || _processedCount == _totalCount)
            {
                _lastReportedPercent = percent;
                if (_caller != null)
                {
                    _caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.Progress", percent, _processedCount, _totalCount));
                }
            }
        }
    }
}