using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    // 标记为 isModify: false 以允许玩家查询时间
    // 修改时间的权限控制在 Execute 内部处理
    [ConsoleCommand("time", "time [set <morning/noon/evening/midnight>]", "Mods.TerraNoneBridge.Commands.Time.Desc", isModify: false)]
    public class TimeCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            bool isSet = false;
            if (args.Count >= 2 && args[0].ToLower() == "set")
            {
                // [权限检查] 如果是游戏内玩家尝试修改时间，直接拒绝
                if (caller is InGameCaller)
                {
                    caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.NoPermission"), false);
                    return;
                }

                string timeKey = args[1].ToLower();
                switch (timeKey)
                {
                    case "morning": Main.dayTime = true; Main.time = 0; break;
                    case "noon": Main.dayTime = true; Main.time = 27000; break;
                    case "evening": Main.dayTime = false; Main.time = 0; break;
                    case "midnight": Main.dayTime = false; Main.time = 16200; break;
                    default:
                        if (double.TryParse(timeKey, out double t)) { Main.time = t; }
                        break;
                }
                NetMessage.SendData(Terraria.ID.MessageID.WorldData);
                isSet = true;
            }

            // Calculate formatted time
            double time = Main.time;
            if (!Main.dayTime) time += 54000.0;
            time = time / 86400.0 * 24.0;
            time = time - 7.5; // Offset to start at 4:30 AM
            if (time < 0.0) time += 24.0;
            if (time >= 24.0) time -= 24.0;

            int hours = (int)time;
            int minutes = (int)((time - hours) * 60.0);
            string timeString = $"{hours:D2}:{minutes:D2}";
            string phase = GetMoonPhaseName(Main.moonPhase); // 本地化在 Helper 中处理

            // 统一 JSON 字段为 camelCase
            var data = new
            {
                timeString = timeString,  // time_string -> timeString
                isDay = Main.dayTime,     // is_day -> isDay
                moonPhase = phase,        // moon_phase -> moonPhase
                moonPhaseId = Main.moonPhase, // moon_phase_id -> moonPhaseId
                rawTime = Main.time,      // raw_time -> rawTime
                action = isSet ? "set" : "query"
            };

            string msg = isSet
                ? Language.GetTextValue("Mods.TerraNoneBridge.Commands.Time.Set", timeString)
                : Language.GetTextValue("Mods.TerraNoneBridge.Commands.Time.Query", timeString, (Main.dayTime ? Language.GetTextValue("Mods.TerraNoneBridge.Common.Day") : Language.GetTextValue("Mods.TerraNoneBridge.Common.Night")), phase);

            caller.Reply(data, msg, true);
        }

        private string GetMoonPhaseName(int phase)
        {
            return Language.GetTextValue($"Mods.TerraNoneBridge.MoonPhase.{phase}");
        }
    }
}