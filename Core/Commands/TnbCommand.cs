using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.Localization;
using Microsoft.Xna.Framework;

namespace TerraNoneBridge.Core.Commands
{
    /// <summary>
    /// [游戏内入口] 统一的主指令 /tnb
    /// 解决与原版 /help 的冲突，并提供统一的命名空间。
    /// </summary>
    public class TnbCommand : ModCommand
    {
        public override string Command => "tnb";

        public override CommandType Type => CommandType.Chat | CommandType.Server | CommandType.Console;

        // 简短描述，详细的都在 /tnb help 里
        public override string Description => Language.GetTextValue("Mods.TerraNoneBridge.Commands.Tnb.Desc");

        public override string Usage => "/tnb <command> [args...]";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // 1. 检查参数
            if (args.Length == 0)
            {
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Tnb.UsageHint"));
                return;
            }

            // 2. 解析子指令
            string subCommand = args[0].ToLower();
            List<string> subArgs = args.Skip(1).ToList();

            // 3. 权限检查
            // 获取指令元数据，检查是否为管理(Modify)指令
            var cmdInfo = CommandManager.GetCommandInfo(subCommand);
            if (cmdInfo != null && cmdInfo.IsModify)
            {
                // 如果是管理指令，且是在游戏内通过 /tnb 调用的，则拒绝执行
                // 仅允许 WebSocket (Nonebot) 端执行此类指令
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.NoPermission"), Color.Red);
                return;
            }

            // 4. 使用适配器调用
            CommandManager.HandleCommand(subCommand, subArgs, new InGameCaller(caller));
        }
    }
}