using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Utils
{
    public static class StringUtils
    {
        // ================== 原有逻辑保留 ==================

        public static int CalcLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[a.Length, b.Length];
        }

        public static bool IsFuzzyMatch(string source, string input)
        {
            // 1. 包含匹配 (绝对优先)
            // 忽略大小写检查，提升用户体验
            if (source.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // 2. 编辑距离匹配
            int len = input.Length;
            // 同样转为小写计算距离，提高容错率
            int dist = CalcLevenshteinDistance(source.ToLower(), input.ToLower());

            // 动态容错阈值：
            // 2个字以内: 必须精确 (否则 "星" 会匹配所有带星的)
            // 3-4个字: 允许1个错字 ("星云稿" -> "星云镐")
            // 5+个字: 允许2个错字
            int limit = len < 3 ? 0 : (len < 5 ? 1 : 2);

            return dist <= limit;
        }

        // ================== 新增方法 ==================

        /// <summary>
        /// 根据查询字符串查找物品 ID 列表。
        /// 结合了 ID 精确查找和名称模糊匹配。
        /// </summary>
        public static List<int> FindItemIds(string query)
        {
            List<int> result = new List<int>();
            if (string.IsNullOrWhiteSpace(query)) return result;

            // 1. 尝试直接解析 ID
            if (int.TryParse(query, out int id))
            {
                if (id > 0 && id < ItemLoader.ItemCount)
                {
                    result.Add(id);
                    // 如果输入的是纯数字 ID，通常直接返回该 ID 即可
                    return result;
                }
            }

            // 2. 遍历所有物品进行名称匹配
            // 复用 IsFuzzyMatch 以保证判定逻辑一致
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                var item = ContentSamples.ItemsByType[i];
                // 必须检查 item 是否为空
                if (item != null && !item.IsAir)
                {
                    bool matched = false;

                    // A. 匹配内部英文名
                    if (!string.IsNullOrEmpty(item.Name) && IsFuzzyMatch(item.Name, query))
                    {
                        matched = true;
                    }
                    // B. 匹配本地化名称 (如中文名)
                    else
                    {
                        string localizedName = Lang.GetItemNameValue(i);
                        if (!string.IsNullOrEmpty(localizedName) && IsFuzzyMatch(localizedName, query))
                        {
                            matched = true;
                        }
                    }

                    if (matched)
                    {
                        result.Add(i);
                    }
                }
            }

            return result;
        }
    }
}