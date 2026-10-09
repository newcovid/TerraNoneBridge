using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.Chat;
using Terraria.ID;

namespace TerraNoneBridge.Core.Utils
{
    /// <summary>
    /// 消息格式化工具类。
    /// 统一管理发往游戏内的消息颜色和格式。
    /// </summary>
    public static class FormatHelper
    {
        public static void Broadcast(string message, Color color)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), color);
                System.Console.WriteLine(message);
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(message, color);
            }
        }

        public static void BroadcastRemoteChat(string user, string message)
        {
            Broadcast($"[QQ] {user}: {message}", Color.CornflowerBlue);
        }
    }
}