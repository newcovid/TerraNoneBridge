using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("give", "give <player> <item> [amount]", "Mods.TerraNoneBridge.Commands.Give.Desc", isModify: true)]
    public class GiveCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count < 2)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Give.Usage"), false);
                return;
            }

            var player = PlayerService.FindPlayerByName(args[0]);
            if (player == null)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.PlayerNotOnline"), false);
                return;
            }

            string itemName = args[1];
            int stack = args.Count > 2 && int.TryParse(args[2], out int s) ? s : 1;

            var results = ItemService.SearchItem(itemName);

            if (results.Count == 0)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Give.NoItem"), false);
                return;
            }

            // 更新引用: MatchQuality -> matchQuality
            var exactMatch = results.FirstOrDefault(r => r.name.ToLower() == itemName.ToLower() || r.matchQuality == 0);
            int itemId = -1;
            string finalName = "";

            if (exactMatch != null)
            {
                itemId = exactMatch.id;     // ID -> id
                finalName = exactMatch.name; // Name -> name
            }
            else if (results.Count == 1)
            {
                itemId = results[0].id;
                finalName = results[0].name;
            }
            else
            {
                string hints = string.Join(", ", results.ConvertAll(i => i.name));
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Give.Multiple", hints), false);
                return;
            }

            player.QuickSpawnItem(null, itemId, stack);

            string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Give.Success", player.name, stack, finalName);
            // 统一 JSON 字段为 camelCase: itemId
            caller.Reply(new { player = player.name, item = finalName, itemId = itemId, amount = stack }, msg, true);
        }
    }
}