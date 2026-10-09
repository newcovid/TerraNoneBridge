using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("search", "search <name>", "Mods.TerraNoneBridge.Commands.Search.Desc", isModify: false)]
    public class SearchCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count < 1)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Search.Usage"), false);
                return;
            }

            string keyword = string.Join(" ", args);
            var results = ItemService.SearchItem(keyword, 20); // Get up to 20 results

            var data = new
            {
                query = keyword,
                count = results.Count,
                results = results // This now includes ImagePath from ItemService update
            };

            if (results.Count == 0)
            {
                caller.Reply(data, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.ItemNotFound", keyword), false);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Search.Found", results.Count));
                foreach (var r in results.Take(10)) // Limit text output
                {
                    // 更新引用: ID -> id, Name -> name, ModName -> modName
                    sb.AppendLine($"[{r.id}] {r.name} ({r.modName})");
                }
                if (results.Count > 10) sb.AppendLine("...");

                caller.Reply(data, sb.ToString(), true);
            }
        }
    }
}