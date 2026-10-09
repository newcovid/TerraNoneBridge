using System.IO;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Terraria.GameContent; // [新增] 用于访问 TextureAssets

namespace TerraNoneBridge.Core.Utils
{
    /// <summary>
    /// Helper to generate consistent image paths for the Nonebot frontend.
    /// Matches the structure used by AssetExportSystem.
    /// </summary>
    public static class AssetPathHelper
    {
        public static string GetItemImagePath(int itemId)
        {
            if (itemId <= 0) return "";
            try
            {
                // [修复] 原版物品：优先使用 TextureAssets 中的真实名称，以匹配 AssetExportSystem 的导出逻辑
                if (itemId < ItemID.Count)
                {
                    if (itemId < TextureAssets.Item.Length && TextureAssets.Item[itemId] != null)
                    {
                        return CleanPath("Item/" + TextureAssets.Item[itemId].Name + ".png");
                    }
                    // Fallback (仅当资源未加载时使用)
                    return CleanPath($"Item/Images/Item_{itemId}.png");
                }

                // 模组物品
                ModItem modItem = ItemLoader.GetItem(itemId);
                if (modItem != null && !string.IsNullOrEmpty(modItem.Texture))
                {
                    return CleanPath("Item/" + modItem.Texture + ".png");
                }
            }
            catch { }
            return "";
        }

        public static string GetNpcImagePath(int npcId)
        {
            if (npcId == 0) return "";
            try
            {
                // [修复] 原版 NPC
                if (npcId < NPCID.Count)
                {
                    if (npcId < TextureAssets.Npc.Length && TextureAssets.Npc[npcId] != null)
                    {
                        return CleanPath("Npc/" + TextureAssets.Npc[npcId].Name + ".png");
                    }
                    return CleanPath($"Npc/Images/NPC_{npcId}.png");
                }

                // 模组 NPC
                ModNPC modNpc = NPCLoader.GetNPC(npcId);
                if (modNpc != null && !string.IsNullOrEmpty(modNpc.Texture))
                {
                    return CleanPath("Npc/" + modNpc.Texture + ".png");
                }
            }
            catch { }
            return "";
        }

        public static string GetBuffImagePath(int buffId)
        {
            if (buffId <= 0) return "";
            try
            {
                // [修复] 原版 Buff
                if (buffId < BuffID.Count)
                {
                    if (buffId < TextureAssets.Buff.Length && TextureAssets.Buff[buffId] != null)
                    {
                        return CleanPath("Buff/" + TextureAssets.Buff[buffId].Name + ".png");
                    }
                    return CleanPath($"Buff/Images/Buff_{buffId}.png");
                }

                // 模组 Buff
                ModBuff modBuff = BuffLoader.GetBuff(buffId);
                if (modBuff != null && !string.IsNullOrEmpty(modBuff.Texture))
                {
                    return CleanPath("Buff/" + modBuff.Texture + ".png");
                }
            }
            catch { }
            return "";
        }

        public static string GetTileImagePath(int tileId)
        {
            // Tiles are complex because they are spritesheets, but we can return the sheet path
            try
            {
                // [修复] 原版 Tile
                if (tileId < TileID.Count)
                {
                    // 注意：Tile 纹理数组通常很大，且部分 ID 可能为空，需谨慎处理
                    // 这里我们暂时保留硬编码作为 Fallback，因为 Tile 纹理管理较复杂
                    // 但尝试获取真实名称更稳妥
                    // TextureAssets.Tile 是 Asset<Texture2D>[] 吗？是的。
                    // 但需要注意索引越界，因为 TileID.Count 可能大于数组长度
                    if (tileId < TextureAssets.Tile.Length && TextureAssets.Tile[tileId] != null)
                    {
                        return CleanPath("Tile/" + TextureAssets.Tile[tileId].Name + ".png");
                    }
                    return CleanPath($"Tile/Images/Tiles_{tileId}.png");
                }

                // 模组 Tile
                ModTile modTile = TileLoader.GetTile(tileId);
                if (modTile != null && !string.IsNullOrEmpty(modTile.Texture))
                {
                    return CleanPath("Tile/" + modTile.Texture + ".png");
                }
            }
            catch { }
            return "";
        }

        private static string CleanPath(string path)
        {
            // Remove "Terraria/" prefix if present (Common in 1.4+ asset names)
            if (path.StartsWith("Terraria/") || path.StartsWith("Terraria\\"))
                path = path.Substring(9);

            // Sanitize
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (c != '/' && c != '\\') path = path.Replace(c, '_');
            }
            return path;
        }
    }
}