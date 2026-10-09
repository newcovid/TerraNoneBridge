using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("list", "list", "Mods.TerraNoneBridge.Commands.List.Desc", isModify: false)]
    public class ListCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            var activePlayers = Main.player.Where(p => p.active).Select(p => p.name).ToList();

            var data = new
            {
                count = activePlayers.Count,
                max = Main.maxPlayers,
                players = activePlayers
            };

            if (activePlayers.Count == 0)
            {
                caller.Reply(data, Language.GetTextValue("Mods.TerraNoneBridge.Commands.List.Empty"), true);
            }
            else
            {
                string names = string.Join(", ", activePlayers);
                string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.List.Header", activePlayers.Count, Main.maxPlayers, names);
                caller.Reply(data, msg, true);
            }
        }
    }
}