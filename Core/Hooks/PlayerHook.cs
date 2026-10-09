using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Config;

namespace TerraNoneBridge.Core.Hooks
{
    /// <summary>
    /// 负责处理玩家连接状态的监控。
    /// 采用轮询机制 (Polling) 而非底层 Hook，以确保最大稳定性。
    /// 未来用于：白名单检查、保存离线玩家背包等。
    /// </summary>
    public class PlayerHookSystem : ModSystem
    {
        private HashSet<int> _activePlayers = new HashSet<int>();

        public override void PostUpdateEverything()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.Server) return;

            HashSet<int> currentFramePlayers = new HashSet<int>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active)
                {
                    currentFramePlayers.Add(i);
                    if (!_activePlayers.Contains(i)) OnPlayerJoin(p);
                }
            }

            foreach (int id in _activePlayers)
            {
                if (!currentFramePlayers.Contains(id)) OnPlayerLeave(Main.player[id]);
            }

            _activePlayers = currentFramePlayers;
        }

        private void OnPlayerJoin(Player player)
        {
            // TODO: 未来在此处添加 "背包查看器" 的数据缓存逻辑
            // 目前 ChatHook 会捕获加入广播，无需重复发送消息
        }

        private void OnPlayerLeave(Player player)
        {
            // TODO: 未来在此处添加 "保存最后背包数据" 逻辑
        }
    }
}