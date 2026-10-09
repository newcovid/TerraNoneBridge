using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("buff", "buff <player/all> <buff> [sec]", "Mods.TerraNoneBridge.Commands.Buff.Desc", isModify: true)]
    public class BuffCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count < 2)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Buff.Usage"), false);
                return;
            }

            string targetName = args[0];
            string buffInput = args[1];
            int durationSeconds = 60;
            if (args.Count > 2 && int.TryParse(args[2], out int t)) durationSeconds = t;

            int buffId = -1;
            string buffName = "";

            if (int.TryParse(buffInput, out int id))
            {
                buffId = id;
                buffName = Lang.GetBuffName(buffId);
            }
            else
            {
                var results = BuffService.SearchBuff(buffInput, 1);
                if (results.Count == 0)
                {
                    caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.BuffNotFound", buffInput), false);
                    return;
                }
                // 更新引用: ID -> id, Name -> name
                buffId = results[0].id;
                buffName = results[0].name;
            }

            if (buffId <= 0)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.BuffNotFound", buffInput), false);
                return;
            }

            List<string> affected = new List<string>();

            if (targetName.ToLower() == "all")
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active)
                    {
                        ApplyBuff(Main.player[i], buffId, durationSeconds);
                        affected.Add(Main.player[i].name);
                    }
                }
                string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Buff.Success", "All", buffName, durationSeconds);
                caller.Reply(new { targets = affected, buff = buffName, duration = durationSeconds }, msg, true);
            }
            else
            {
                var player = PlayerService.FindPlayerByName(targetName);
                if (player != null)
                {
                    ApplyBuff(player, buffId, durationSeconds);
                    string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Buff.Success", player.name, buffName, durationSeconds);
                    caller.Reply(new { targets = new[] { player.name }, buff = buffName, duration = durationSeconds }, msg, true);
                }
                else
                {
                    caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.PlayerNotFound", targetName), false);
                }
            }
        }

        private void ApplyBuff(Player p, int buffId, int durationSeconds)
        {
            p.AddBuff(buffId, durationSeconds * 60);
            NetMessage.SendData(MessageID.AddPlayerBuff, -1, -1, null, p.whoAmI, buffId, durationSeconds * 60);
        }
    }
}