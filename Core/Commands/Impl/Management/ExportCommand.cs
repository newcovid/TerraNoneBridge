using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;
using TerraNoneBridge.Core.Systems;
using TerraNoneBridge.Core.Config;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    // 该指令通过 /tnb exportassets [all] 调用
    [ConsoleCommand("exportassets", "exportassets [all]", "Mods.TerraNoneBridge.Commands.Export.Desc", isModify: false)]
    public class ExportCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (Main.dedServ)
            {
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.ErrorServer"));
                return;
            }

            if (AssetExportSystem.IsExporting)
            {
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Export.Busy"));
                return;
            }

            // 检查是否包含可选参数 "all"
            bool exportAll = false;
            if (args.Count > 0 && args[0].Trim().ToLower() == "all")
            {
                exportAll = true;
            }

            var config = ModContent.GetInstance<ServerConfig>();
            string exportRoot;

            if (string.IsNullOrWhiteSpace(config.CustomExportPath))
            {
                exportRoot = Path.Combine(Main.SavePath, "TerraNoneBridge_Exports", System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            }
            else
            {
                exportRoot = Path.Combine(Main.SavePath, config.CustomExportPath);
            }

            AssetExportSystem.StartExport(exportRoot, caller, exportAll);
        }
    }
}