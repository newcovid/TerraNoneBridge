using System.Collections.Generic;

namespace TerraNoneBridge.Core.Commands
{
    /// <summary>
    /// [架构核心] 所有 WebSocket 指令的通用接口。
    /// 实现此接口的类将由 CommandManager 自动扫描并加载。
    /// </summary>
    public interface IConsoleCommand
    {
        /// <summary>
        /// 执行指令逻辑。
        /// </summary>
        /// <param name="args">去除指令名后的参数列表</param>
        /// <param name="caller">调用者 (可能是 RCON 或 游戏内玩家)</param>
        void Execute(List<string> args, ICmdCaller caller);
    }
}