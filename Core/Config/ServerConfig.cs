using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Utils;

namespace TerraNoneBridge.Core.Config
{
    public class ServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("ConnectionSettings")]

        [DefaultValue("127.0.0.1")]
        public string IpAddress { get; set; }

        [Range(1, 65535)]
        [DefaultValue(7778)]
        public int Port { get; set; }

        [DefaultValue("")]
        public string AccessToken { get; set; }

        [Range(0, 3600)]
        [DefaultValue(5)]
        public int ReconnectInterval { get; set; }

        [Header("FeatureSwitches")]

        // 玩家聊天双向互通总开关 (游戏 <-> QQ)
        [DefaultValue(true)]
        public bool EnableChatSync { get; set; }

        // 系统播报总开关：关闭后下方所有分类播报均不发送
        [DefaultValue(true)]
        public bool EnableEventBroadcast { get; set; }

        [Header("BroadcastFilters")]

        [DefaultValue(true)]
        public bool BroadcastJoinLeave { get; set; }

        [DefaultValue(true)]
        public bool BroadcastPlayerDeath { get; set; }

        [DefaultValue(true)]
        public bool BroadcastBoss { get; set; }

        [DefaultValue(true)]
        public bool BroadcastWorldEvent { get; set; }

        [DefaultValue(true)]
        public bool BroadcastTownNpc { get; set; }

        [DefaultValue(true)]
        public bool BroadcastOtherMods { get; set; }

        [DefaultValue(true)]
        public bool BroadcastServerStatus { get; set; }

        // 包含任一关键词的消息不会转发到 QQ (不区分大小写)
        public List<string> BlockedKeywords { get; set; } = new List<string>();

        [Header("ExportSettings")]

        // [新增] 自定义导出路径
        // 留空则默认使用 "TerraNoneBridge_Exports/yyyyMMdd_HHmmss"
        [DefaultValue("")]
        public string CustomExportPath { get; set; }

        // 上一次应用的连接参数，用于避免修改播报开关时触发重连
        private static string _appliedConnectionKey;

        /// <summary>
        /// 判断某一分类的消息是否允许发往 Nonebot。
        /// </summary>
        public bool IsCategoryEnabled(string category)
        {
            if (category == BroadcastCategory.Chat) return EnableChatSync;
            if (!EnableEventBroadcast) return false;

            switch (category)
            {
                case BroadcastCategory.JoinLeave: return BroadcastJoinLeave;
                case BroadcastCategory.Death: return BroadcastPlayerDeath;
                case BroadcastCategory.Boss: return BroadcastBoss;
                case BroadcastCategory.WorldEvent: return BroadcastWorldEvent;
                case BroadcastCategory.TownNpc: return BroadcastTownNpc;
                case BroadcastCategory.OtherMod: return BroadcastOtherMods;
                case BroadcastCategory.Server: return BroadcastServerStatus;
                default: return true;
            }
        }

        public bool ContainsBlockedKeyword(string message)
        {
            if (BlockedKeywords == null || string.IsNullOrEmpty(message)) return false;
            foreach (string keyword in BlockedKeywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword) && message.IndexOf(keyword.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnChanged()
        {
            base.OnChanged();

            string connectionKey = $"{IpAddress}|{Port}|{AccessToken}|{ReconnectInterval}";
            bool connectionChanged = _appliedConnectionKey != null && _appliedConnectionKey != connectionKey;
            _appliedConnectionKey = connectionKey;
            if (!connectionChanged) return;

            var socket = ModContent.GetInstance<SocketClient>();
            if (socket != null)
            {
                System.Threading.Tasks.Task.Run(() => socket.Reconnect());
            }
        }
    }
}
