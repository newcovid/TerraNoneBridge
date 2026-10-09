using System.Collections.Generic;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerraNoneBridge.Core.Commands
{
    /// <summary>
    /// [架构核心] 指令基类。
    /// 同时兼容 tModLoader 的 ModCommand (游戏内) 和本模组的 IConsoleCommand (WebSocket)。
    /// 自动从 ConsoleCommandAttribute 读取元数据。
    /// </summary>
    public abstract class BaseCommand : ModCommand, IConsoleCommand
    {
        private ConsoleCommandAttribute _attr;

        protected BaseCommand()
        {
            // 反射获取特性信息
            _attr = GetType().GetCustomAttribute<ConsoleCommandAttribute>();
        }

        // --- ModCommand 实现 (游戏内聊天支持) ---

        public override string Command => _attr?.Name ?? GetType().Name;

        public override CommandType Type => CommandType.Chat | CommandType.Server | CommandType.Console;

        // [修复] 在这里获取本地化文本，确保 /help 显示正确的双语说明
        public override string Description => _attr != null ? Language.GetTextValue(_attr.Description) : "";

        public override string Usage => _attr?.Usage ?? "";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            // 将原生 caller 包装为通用接口，然后执行逻辑
            Execute(new List<string>(args), new InGameCaller(caller));
        }

        // --- IConsoleCommand 实现 (WebSocket 支持) ---

        public abstract void Execute(List<string> args, ICmdCaller caller);
    }
}