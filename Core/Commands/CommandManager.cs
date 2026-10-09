using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerraNoneBridge.Core.Commands
{
    public static class CommandManager
    {
        private static readonly Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();

        public static void Initialize()
        {
            _commands.Clear();
            var type = typeof(IConsoleCommand);
            // 扫描当前程序集中实现了 IConsoleCommand 接口的非抽象类
            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var t in types)
            {
                var attr = t.GetCustomAttribute<ConsoleCommandAttribute>();
                if (attr != null)
                {
                    var cmd = (IConsoleCommand)Activator.CreateInstance(t);
                    _commands[attr.Name.ToLower()] = cmd;
                    // 可选：记录注册日志
                    // TerraNoneBridge.Instance.Logger.Info($"Registered command: {attr.Name}");
                }
            }
        }

        public static void Unload()
        {
            _commands.Clear();
        }

        public static void HandleCommand(string commandName, List<string> args, ICmdCaller caller)
        {
            if (_commands.TryGetValue(commandName.ToLower(), out var cmd))
            {
                try
                {
                    cmd.Execute(args, caller);
                }
                catch (Exception ex)
                {
                    // 使用本地化键：Mods.TerraNoneBridge.Log.CommandExecError
                    string errorMsg = Language.GetTextValue("Mods.TerraNoneBridge.Log.CommandExecError", commandName, ex.Message);
                    caller.Reply(null, errorMsg, false);
                }
            }
            else
            {
                // 使用本地化键：Mods.TerraNoneBridge.Commands.Common.UnknownCommand
                string unknownMsg = Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.UnknownCommand", commandName);
                caller.Reply(null, unknownMsg, false);
            }
        }

        public static List<ConsoleCommandAttribute> GetCommands()
        {
            return _commands.Values
                .Select(c => c.GetType().GetCustomAttribute<ConsoleCommandAttribute>())
                .Where(a => a != null)
                .OrderBy(a => a.Name)
                .ToList();
        }

        public static ConsoleCommandAttribute GetCommandInfo(string commandName)
        {
            if (_commands.TryGetValue(commandName.ToLower(), out var cmd))
            {
                return cmd.GetType().GetCustomAttribute<ConsoleCommandAttribute>();
            }
            return null;
        }
    }
}