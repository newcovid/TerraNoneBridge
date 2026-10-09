using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.Localization;
using TerraNoneBridge.Core.Systems;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("tps", "tps", "Mods.TerraNoneBridge.Commands.Tps.Desc", isModify: false)]
    public class TpsCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            int activeNPCs = 0;
            int activeItems = 0;
            int activePlayers = 0;

            try
            {
                for (int i = 0; i < Main.maxNPCs; i++) if (Main.npc[i].active) activeNPCs++;
                for (int i = 0; i < Main.maxItems; i++) if (Main.item[i].active) activeItems++;
                for (int i = 0; i < Main.maxPlayers; i++) if (Main.player[i].active) activePlayers++;
            }
            catch { }

            long usedMemory = 0;
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    usedMemory = process.WorkingSet64 / 1024 / 1024;
                }
            }
            catch { }

            long gcMemory = global::System.GC.GetTotalMemory(false) / 1024 / 1024;
            float tps = PerformanceMonitor.RealTps;

            string worldName = Main.worldName ?? Language.GetTextValue("Mods.TerraNoneBridge.Common.Unknown");

            // 统一 JSON 字段为 camelCase
            var stats = new
            {
                version = Main.versionNumber,
                world = worldName,
                tps = tps,
                onlineCount = activePlayers, // online_count -> onlineCount
                npcCount = activeNPCs,       // npc_count -> npcCount
                itemCount = activeItems,     // item_count -> itemCount
                memoryMb = usedMemory,       // memory_mb -> memoryMb
                gcMb = gcMemory              // gc_mb -> gcMb
            };

            string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Tps.OutputFormat",
                stats.version, stats.world, stats.tps.ToString("F1"), stats.onlineCount,
                stats.npcCount, stats.itemCount, stats.memoryMb, stats.gcMb
            );

            caller.Reply(stats, msg, true);
        }
    }
}