using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("settle", "settle", "Mods.TerraNoneBridge.Commands.Settle.Desc", isModify: true)]
    public class SettleCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            // 修复：1.4 中 Liquid.SettleLiquids() 已移除
            // 使用 QuickWater 和 WaterCheck 模拟液体沉降
            Liquid.QuickWater(3);
            WorldGen.WaterCheck();

            string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Settle.Success");
            caller.Reply(new { success = true }, msg, true);
        }
    }
}