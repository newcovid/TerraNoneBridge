using System.Collections.Generic;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("query", "query <name/id>", "Mods.TerraNoneBridge.Commands.Query.Desc", isModify: false)]
    public class QueryCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count < 1)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Query.Usage"), false);
                return;
            }

            string input = string.Join(" ", args);
            int targetId = -1;

            if (int.TryParse(input, out int id))
            {
                targetId = id;
            }
            else
            {
                var results = ItemService.SearchItem(input, 5);
                if (results.Count == 0)
                {
                    caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.ItemNotFound", input), false);
                    return;
                }
                else if (results.Count > 1)
                {
                    // 更新引用: Name -> name, ID -> id
                    if (results[0].name == input)
                    {
                        targetId = results[0].id;
                    }
                    else
                    {
                        string hints = "";
                        foreach (var r in results) hints += $"{r.name}({r.id}), ";
                        caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Query.Multiple", hints.TrimEnd(',', ' ')), false);
                        return;
                    }
                }
                else
                {
                    targetId = results[0].id;
                }
            }

            if (caller.CommandType == Terraria.ModLoader.CommandType.Console)
            {
                var dto = ItemService.GetItemDto(targetId);
                caller.Reply(dto, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Query.Success"), true);
            }
            else
            {
                string detail = ItemService.GetItemDetailText(targetId);
                caller.Reply(null, detail, true);
            }
        }
    }
}