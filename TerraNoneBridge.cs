using System;
using System.Text;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Commands;

namespace TerraNoneBridge
{
	/// <summary>
	/// 模组入口类。
	/// 维护全局单例 Instance，供其他系统访问 Mod 实例成员 (如 Logger)。
	/// </summary>
	public class TerraNoneBridge : Mod
	{
		public static TerraNoneBridge Instance { get; private set; }

		/// <summary>
		/// [新增] 主线程最后一次更新的时间戳。
		/// 用于检测服务器是否卡死或正在进行繁重操作。
		/// </summary>
		public static DateTime LastGameUpdate { get; set; } = DateTime.Now;

		public override void Load()
		{
			Instance = this;
			LastGameUpdate = DateTime.Now;

			// [修复] 初始化指令管理器，扫描并注册所有指令
			CommandManager.Initialize();

			// [修复] 设置控制台编码为 UTF-8，解决本地服务器后台中文乱码问题
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
			{
				try
				{
					Console.OutputEncoding = Encoding.UTF8;
					// 某些情况下输入编码也需要设置，但在 tModLoader 环境下主要关注输出
				}
				catch { /* 忽略无法设置编码的情况 (如某些嵌入式环境) */ }
			}
		}

		public override void Unload()
		{
			// [修复] 卸载指令管理器，清理引用
			CommandManager.Unload();
			Instance = null;
		}
	}
}