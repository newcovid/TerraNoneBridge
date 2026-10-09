using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Utils;

namespace TerraNoneBridge.Core.Hooks
{
    /// <summary>
    /// 负责处理聊天相关的事件钩子。
    /// 1. 拦截游戏内聊天 -> 发送给 Nonebot
    /// 2. 接收 Nonebot 消息 -> 广播到游戏内
    /// </summary>
    public class ChatHook : ModSystem
    {
        public static bool IsSyncing = false;

        public override void Load()
        {
            Terraria.Chat.On_ChatHelper.BroadcastChatMessage += Hook_BroadcastChatMessage;
            Terraria.Chat.On_ChatHelper.BroadcastChatMessageAs += Hook_BroadcastChatMessageAs;
        }

        public override void Unload()
        {
            Terraria.Chat.On_ChatHelper.BroadcastChatMessage -= Hook_BroadcastChatMessage;
            Terraria.Chat.On_ChatHelper.BroadcastChatMessageAs -= Hook_BroadcastChatMessageAs;
        }

        // 处理系统/插件广播 (ID 255)
        private void Hook_BroadcastChatMessage(Terraria.Chat.On_ChatHelper.orig_BroadcastChatMessage orig, NetworkText text, Color color, int excludedPlayer)
        {
            if (IsSyncing) { orig(text, color, excludedPlayer); return; }
            orig(text, color, excludedPlayer);
            // "Server" 这里的 ID 标识通常由接收端解析，或者可以复用 Common.ServerName
            SendChatToSocket("Server", text.ToString(), color);
        }

        // 处理玩家说话 (ID < 255)
        private void Hook_BroadcastChatMessageAs(Terraria.Chat.On_ChatHelper.orig_BroadcastChatMessageAs orig, byte messageAuthor, NetworkText text, Color color, int excludedPlayer)
        {
            orig(messageAuthor, text, color, excludedPlayer);
            if (IsSyncing || messageAuthor == 255) return;

            string senderName = Language.GetTextValue("Mods.TerraNoneBridge.Common.Unknown");
            if (messageAuthor < 255 && Main.player[messageAuthor].active)
            {
                senderName = Main.player[messageAuthor].name;
                SendChatToSocket(senderName, text.ToString(), color);
            }
        }

        private void SendChatToSocket(string user, string message, Color color)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            try
            {
                ModContent.GetInstance<SocketClient>().Send(new ChatPacket
                {
                    UserName = user,
                    Message = message,
                    ColorHex = color.Hex3()
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