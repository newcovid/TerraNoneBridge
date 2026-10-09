using System.Collections.Generic;
using System.Linq; // 添加 Linq 引用
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Management
{
    [ConsoleCommand("kick", "kick <player> [reason]", "Mods.TerraNoneBridge.Commands.Kick.Desc", isModify: true)]
    public class KickCommand : IConsoleCommand
    {
        public void Execute(List<string> args, ICmdCaller caller)
        {
            // 安全检查：防止 args 为 null 或空
            if (args == null || args.Count < 1)
            {
                caller.Reply(null, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Kick.Usage"), false);
                return;
            }

            string targetName = args[0];

            // 优化参数解析逻辑：使用 Skip 获取后续所有参数作为理由
            // 如果没有提供理由，则使用默认理由
            string reason = args.Count > 1
                ? string.Join(" ", args.Skip(1))
                : Language.GetTextValue("Mods.TerraNoneBridge.Commands.Kick.DefaultReason");

            var player = PlayerService.FindPlayerByName(targetName);
            if (player != null)
            {
                // [关键修复] NetMessage.SendData 的第二个参数是 remoteClient (接收者)
                // 踢人指令必须发送给特定玩家 (player.whoAmI)，而不是广播 (-1)
                // 之前的写法可能导致了底层处理异常或逻辑错误
                NetMessage.SendData(MessageID.Kick, player.whoAmI, -1, NetworkText.FromLiteral(reason));

                string msg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Kick.Success", player.name, reason);
                caller.Reply(new { target = player.name, reason = reason, success = true }, msg, true);
            }
            else
            {
                caller.Reply(new { target = targetName, success = false }, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.PlayerNotFound", targetName), false);
            }
        }
    }
}