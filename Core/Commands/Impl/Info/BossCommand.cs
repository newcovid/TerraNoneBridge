using System.Collections.Generic;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("boss", "boss", "Mods.TerraNoneBridge.Commands.Boss.Desc", isModify: false)]
    public class BossCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            var dto = WorldService.GetBossProgressDto();
            string text = WorldService.GetBossProgressText();
            caller.Reply(dto, text, true);
        }
    }
}