using System.Collections.Generic;
using System.Linq;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("help", "help [command]", "Mods.TerraNoneBridge.Commands.Help.Desc", isModify: false)]
    public class HelpCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            var commands = CommandManager.GetCommands();

            var helpList = commands.Select(c => new
            {
                name = c.Name,
                usage = c.Usage,
                // [修复] 将 DescriptionKey 改为 Description
                description = Language.GetTextValue(c.Description),
                permission = c.IsModify ? "Admin" : "User"
            }).ToList();

            if (args.Count > 0)
            {
                string target = args[0].ToLower();
                var cmd = commands.FirstOrDefault(c => c.Name == target);
                if (cmd != null)
                {
                    // [修复] 将 DescriptionKey 改为 Description
                    string desc = Language.GetTextValue(cmd.Description);
                    string msg = $"{cmd.Usage}\n{desc}";

                    var specificDto = new
                    {
                        name = cmd.Name,
                        usage = cmd.Usage,
                        description = desc,
                        permission = cmd.IsModify ? "Admin" : "User"
                    };

                    caller.Reply(specificDto, msg, true);
                }
                else
                {
                    caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Help.NotFound", target), false);
                }
            }
            else
            {
                string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Help.Header") + "\n";
                foreach (var c in commands)
                {
                    // [修复] 将 DescriptionKey 改为 Description
                    msg += $"{c.Name}: {Language.GetTextValue(c.Description)}\n";
                }

                caller.Reply(helpList, msg, true);
            }
        }
    }
}