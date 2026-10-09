using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria.UI.Chat;

namespace TerraNoneBridge.Core.Utils
{
    /// <summary>
    /// 将游戏内聊天标签转换为 QQ 可读的纯文本。
    /// 例如 [i/d...:ModName/ItemName] -> [物品名]，[c/FF0000:文字] -> 文字。
    /// </summary>
    public static class ChatTextHelper
    {
        // 与 ChatManager 使用的标签格式一致: [tag/options:text]
        private static readonly Regex TagRegex = new Regex(@"(?<!\\)\[(?<tag>[a-zA-Z]{1,10})(\/(?<options>[^:]+))?:(?<text>.+?)(?<!\\)\]", RegexOptions.Compiled);

        public static string ToPlainText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('[') < 0) return text;
            return TagRegex.Replace(text, RenderTag);
        }

        private static string RenderTag(Match match)
        {
            // 逐个标签交给原版/模组注册的 TagHandler 解析，单个标签失败不影响整条消息
            try
            {
                var builder = new StringBuilder();
                foreach (TextSnippet snippet in ChatManager.ParseMessage(match.Value, Color.White))
                {
                    builder.Append(snippet.Text);
                }
                return builder.ToString();
            }
            catch
            {
                string tag = match.Groups["tag"].Value.ToLowerInvariant();
                string inner = match.Groups["text"].Value;
                switch (tag)
                {
                    case "i":
                    case "item":
                        return $"[{inner}]";
                    case "g":
                    case "glyph":
                        return string.Empty;
                    default:
                        return inner;
                }
            }
        }
    }
}
