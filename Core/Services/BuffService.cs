using System.Collections.Generic;
using System.Linq; // 引入 Linq
using Terraria;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Utils;

namespace TerraNoneBridge.Core.Services
{
    public static class BuffService
    {
        public class BuffSearchResult
        {
            public int id;          // ID -> id
            public string name;     // Name -> name
            public string modName;  // ModName -> modName
        }

        public static List<BuffSearchResult> SearchBuff(string keyword, int limit = 10)
        {
            var results = new List<BuffSearchResult>();
            if (string.IsNullOrWhiteSpace(keyword)) return results;

            keyword = keyword.ToLower();

            // 第一次遍历：精确包含
            for (int i = 0; i < BuffLoader.BuffCount; i++)
            {
                string name = Lang.GetBuffName(i);
                if (string.IsNullOrEmpty(name)) continue;

                if (name.ToLower().Contains(keyword))
                {
                    results.Add(new BuffSearchResult { id = i, name = name, modName = BuffLoader.GetBuff(i)?.Mod.Name ?? "Terraria" });
                }
                if (results.Count >= limit) return results;
            }

            // 第二次遍历：模糊匹配
            if (results.Count < limit)
            {
                for (int i = 0; i < BuffLoader.BuffCount; i++)
                {
                    string name = Lang.GetBuffName(i);
                    if (string.IsNullOrEmpty(name)) continue;
                    if (results.Any(r => r.id == i)) continue;

                    if (StringUtils.IsFuzzyMatch(name.ToLower(), keyword))
                    {
                        results.Add(new BuffSearchResult { id = i, name = name, modName = BuffLoader.GetBuff(i)?.Mod.Name ?? "Terraria" });
                    }
                    if (results.Count >= limit) break;
                }
            }
            return results;
        }
    }
}