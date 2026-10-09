using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("butcher", "butcher", "Mods.TerraNoneBridge.Commands.Butcher.Desc", isModify: true)]
    public class ButcherCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            int killCount = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && !Main.npc[i].friendly && !Main.npc[i].dontTakeDamage)
                {
                    Main.npc[i].StrikeNPC(new NPC.HitInfo { Damage = 99999, Crit = true });
                    if (Main.npc[i].life <= 0) killCount++;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                }
            }

            string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Butcher.Success", killCount);
            // 统一为 camelCase: killedCount
            caller.Reply(new { killedCount = killCount }, msg, true);
        }
    }
}