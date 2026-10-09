using System;

namespace TerraNoneBridge.Core.Commands
{
    /// <summary>
    /// [架构核心] 指令元数据特性。
    /// 用于标记一个类为可执行指令，并定义其触发关键词、帮助信息以及指令类型。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class ConsoleCommandAttribute : Attribute
    {
        public string Name { get; }
        public string Usage { get; }
        public string Description { get; }

        /// <summary>
        /// [新增] 标记是否为修改性指令。
        /// true: 修改性指令 (需要安全环境检查，必须在主线程执行)
        /// false: 查询性指令 (只读，可立即在后台线程响应)
        /// </summary>
        public bool IsModify { get; }

        public ConsoleCommandAttribute(string name, string usage = "", string description = "", bool isModify = false)
        {
            Name = name.ToLower();
            Usage = usage;
            Description = description;
            IsModify = isModify;
        }
    }
}