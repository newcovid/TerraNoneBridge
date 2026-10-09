using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using System.Threading.Tasks;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("save", "save", "Mods.TerraNoneBridge.Commands.Save.Desc", isModify: true)]
    public class SaveCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            // Execute on main thread to be safe, though WorldGen.saveAndPlay handles it.
            // We use Task.Run to not block the socket loop, but WorldGen might block game loop.
            // For TML, calling it directly is usually fine as it schedules it.

            WorldGen.saveAndPlay();

            string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Save.Success");
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            caller.Reply(new { success = true, timestamp = timestamp }, msg, true);
        }
    }
}