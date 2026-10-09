using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Config;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Utils;

namespace TerraNoneBridge.Core.Hooks
{
    /// <summary>
    /// 负责处理聊天相关的事件钩子。
    /// 1. 拦截游戏内聊天与系统广播 -> 分类、过滤后发送给 Nonebot
    /// 2. 接收 Nonebot 消息 -> 广播到游戏内
    /// </summary>
    public class ChatHook : ModSystem
    {
        public static bool IsSyncing = false;

        // 大于 0 表示当前处于 Player.KillMe 调用栈中，期间的系统广播即为死亡消息
        private static int _playerDeathDepth = 0;

        public override void Load()
        {
            // ChatHelper.BroadcastChatMessage 内部会转调 BroadcastChatMessageAs(255, ...)，只需拦截这一处
            Terraria.Chat.On_ChatHelper.BroadcastChatMessageAs += Hook_BroadcastChatMessageAs;
            Terraria.On_Player.KillMe += Hook_KillMe;
        }

        public override void Unload()
        {
            Terraria.Chat.On_ChatHelper.BroadcastChatMessageAs -= Hook_BroadcastChatMessageAs;
            Terraria.On_Player.KillMe -= Hook_KillMe;
            _playerDeathDepth = 0;
        }

        private void Hook_KillMe(Terraria.On_Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
        {
            _playerDeathDepth++;
            try
            {
                orig(self, damageSource, dmg, hitDirection, pvp);
            }
            finally
            {
                _playerDeathDepth--;
            }
        }

        private void Hook_BroadcastChatMessageAs(Terraria.Chat.On_ChatHelper.orig_BroadcastChatMessageAs orig, byte messageAuthor, NetworkText text, Color color, int excludedPlayer)
        {
            orig(messageAuthor, text, color, excludedPlayer);
            if (IsSyncing || !ModContent.GetInstance<SocketClient>().IsConnected) return;

            // 系统/插件广播 (ID 255)
            if (messageAuthor == 255)
            {
                string category = _playerDeathDepth > 0 ? BroadcastCategory.Death : BroadcastCategory.ClassifySystemMessage(text);
                // "Server" 这里的 ID 标识由接收端解析为系统消息
                SendChatToSocket("Server", text, color, category);
                return;
            }

            // 玩家说话 (ID < 255)
            if (messageAuthor < Main.maxPlayers && Main.player[messageAuthor].active)
            {
                SendChatToSocket(Main.player[messageAuthor].name, text, color, BroadcastCategory.Chat);
            }
        }

        private static void SendChatToSocket(string user, NetworkText text, Color color, string category)
        {
            try
            {
                var config = ModContent.GetInstance<ServerConfig>();
                if (!config.IsCategoryEnabled(category)) return;

                string message = ChatTextHelper.ToPlainText(text.ToString());
                if (string.IsNullOrWhiteSpace(message) || config.ContainsBlockedKeyword(message)) return;

                ModContent.GetInstance<SocketClient>().Send(new ChatPacket
                {
                    UserName = user,
                    Message = message,
                    ColorHex = color.Hex3(),
                    Category = category
                });
            }
            catch { }
        }

        public static void OnReceiveRemoteChat(ChatPacket packet)
        {
            if (packet == null || string.IsNullOrEmpty(packet.Message)) return;
            IsSyncing = true;
            try
            {
                FormatHelper.BroadcastRemoteChat(packet.UserName, packet.Message);
            }
            finally
            {
                IsSyncing = false;
            }
        }
    }
}
