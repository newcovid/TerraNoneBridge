using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Utils
{
    /// <summary>
    /// 发往 Nonebot 的消息分类。
    /// 分类值会写入 chat/event 数据包的 category 字段，供模组配置与 Nonebot 插件按类过滤。
    /// </summary>
    public static class BroadcastCategory
    {
        public const string Chat = "chat";             // 玩家聊天
        public const string JoinLeave = "join_leave";  // 玩家加入/离开
        public const string Death = "death";           // 玩家死亡
        public const string Boss = "boss";             // Boss 苏醒/击败及其预警
        public const string WorldEvent = "event";      // 入侵、天象、世界进度、传送等原版事件
        public const string TownNpc = "npc";           // 城镇 NPC 到达/死亡/离开
        public const string OtherMod = "mod";          // 其他模组发出的播报
        public const string Server = "server";         // 服务器启停、控制台 say

        // NetworkText 的文本与模式均为私有字段，只能通过反射读取本地化键
        private static readonly FieldInfo TextField = typeof(NetworkText).GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo ModeField = typeof(NetworkText).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);

        // 原版通过 ChatHelper.BroadcastChatMessage 播报时使用的本地化键
        private static readonly Dictionary<string, string> KeyCategories = new Dictionary<string, string>
        {
            ["LegacyMultiplayer.19"] = JoinLeave,  // {0} 已加入。
            ["LegacyMultiplayer.20"] = JoinLeave,  // {0} 已离开。

            ["LegacyMultiplayer.23"] = Death,      // /alldeath 等死亡统计
            ["LegacyMultiplayer.24"] = Death,
            ["LegacyMultiplayer.25"] = Death,
            ["LegacyMultiplayer.26"] = Death,

            ["Announcement.HasAwoken"] = Boss,
            ["Announcement.HasBeenDefeated_Single"] = Boss,
            ["Announcement.HasBeenDefeated_Plural"] = Boss,
            ["LegacyMisc.48"] = Boss,   // 双子魔眼已经醒来！
            ["LegacyMisc.107"] = Boss,  // 美杜莎已苏醒！
            ["LegacyMisc.9"] = Boss,    // 你感到有个邪恶的东西在看着你... (克苏鲁之眼)
            ["LegacyMisc.28"] = Boss,   // 你感受到地下深处的震动... (毁灭者)
            ["LegacyMisc.29"] = Boss,   // 这将是一个可怕的夜晚... (双子魔眼)
            ["LegacyMisc.30"] = Boss,   // 周围的空气越来越冷... (机械骷髅王)
            ["LegacyMisc.52"] = Boss,   // 月亮末日慢慢逼近...

            ["Announcement.HasArrived"] = TownNpc,
            ["LegacyMisc.19"] = TownNpc,  // {0}被杀死了...
            ["LegacyMisc.35"] = TownNpc,  // 旅商离开
            ["LegacyMisc.36"] = TownNpc,  // {0}已离开！
        };

        /// <summary>
        /// 对系统广播 (messageAuthor == 255) 进行分类。
        /// 玩家死亡消息由调用方根据 Player.KillMe 上下文单独判定。
        /// </summary>
        public static string ClassifySystemMessage(NetworkText text)
        {
            string key = GetLocalizationKey(text);

            // 字面量/格式化文本几乎都来自其他模组
            if (key == null) return OtherMod;

            if (KeyCategories.TryGetValue(key, out string category)) return category;
            if (key.StartsWith("DeathText", StringComparison.Ordinal) || key.StartsWith("DeathSource.", StringComparison.Ordinal)) return Death;
            if (key.StartsWith("CLI.", StringComparison.Ordinal)) return Server;
            if (key.StartsWith("Mods.", StringComparison.Ordinal)) return OtherMod;

            // 其余原版键：入侵、血月/日食、天降陨石、肉山/祭坛等世界进度、传送、PvP 切换等
            return WorldEvent;
        }

        private static string GetLocalizationKey(NetworkText text)
        {
            if (text == null || TextField == null || ModeField == null) return null;
            try
            {
                if (ModeField.GetValue(text)?.ToString() != "LocalizationKey") return null;
                return TextField.GetValue(text) as string;
            }
            catch
            {
                return null;
            }
        }
    }
}
