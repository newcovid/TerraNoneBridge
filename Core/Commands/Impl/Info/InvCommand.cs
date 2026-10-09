using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("inv", "inv <player>", "Mods.TerraNoneBridge.Commands.Inv.Desc", isModify: false)]
    public class InvCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count < 1)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Inv.Usage"), false);
                return;
            }

            string targetName = string.Join(" ", args);
            var dto = PlayerService.GetPlayerInventoryDto(targetName);

            if (dto == null)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.PlayerNotFound", targetName), false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Inv.Header", dto.playerName));

            // 修复：本地化 "(Empty)"
            if (dto.inventory.Count == 0) sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Common.Empty"));

            int count = 0;
            foreach (var item in dto.inventory)
            {
                if (count++ > 15) { sb.AppendLine("..."); break; }
                string prefix = string.IsNullOrEmpty(item.prefix) ? "" : $"[{item.prefix}] ";
                sb.AppendLine($"- {prefix}{item.name} x{item.stack}");
            }

            caller.Reply(dto, sb.ToString(), true);
        }
    }
}