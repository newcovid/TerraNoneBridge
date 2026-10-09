using System;
using Terraria.ModLoader;

namespace TerraNoneBridge.Core.Systems
{
    /// <summary>
    /// [新增] 性能监控系统。
    /// 负责实时计算服务器的真实 TPS (Ticks Per Second)。
    /// </summary>
    public class PerformanceMonitor : ModSystem
    {
        // 静态属性供后台线程安全读取 (float 读取是原子的)
        public static float RealTps { get; private set; } = 60f;

        private int _tickCounter = 0;
        private DateTime _lastMeasureTime = DateTime.Now;

        public override void PostUpdateEverything()
        {
            _tickCounter++;

            var now = DateTime.Now;
            var span = now - _lastMeasureTime;

            // 每秒更新一次 TPS 数据
            if (span.TotalSeconds >= 1.0)
            {
                RealTps = (float)(_tickCounter / span.TotalSeconds);
                _tickCounter = 0;
                _lastMeasureTime = now;
            }
        }
    }
}