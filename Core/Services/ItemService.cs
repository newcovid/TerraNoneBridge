using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.GameContent.ItemDropRules;
using TerraNoneBridge.Core.Utils;
using Terraria.UI.Chat;
using Microsoft.Xna.Framework;
using Terraria.GameContent;

namespace TerraNoneBridge.Core.Services
{
    public static class ItemService
    {
        // --- DTOs ---
        public class ItemSearchResult
        {
            public int id;              // ID -> id
            public string name;         // Name -> name
            public string modName;      // ModName -> modName
            public int matchQuality;    // MatchQuality -> matchQuality
            public string imagePath;    // ImagePath -> imagePath
            public int frameCount;      // FrameCount -> frameCount
        }

        public class ItemDetailDto
        {
            public int id;
            public string name;
            public string mod;
            public string type = "Item";
            public string imagePath;
            public int frameCount;
            public ItemStatsDto stats;
            public string description;
            public List<SourceDto> droppedBy;
            public List<SourceDto> soldBy;
            public List<RecipeDto> recipes;
        }

        public class ItemStatsDto
        {
            public int damage;
            public int defense;
            public int crit;
            public int useTime;
            public float knockBack;
            public int value;
            public bool autoReuse;
            public bool consumable;
            public int maxStack;
        }

        public class SourceDto
        {
            public string name;
            public string imagePath;
        }

        public class RecipeDto
        {
            public string resultName;
            public int resultCount;
            public List<IngredientDto> stations;
            public List<IngredientDto> ingredients;
        }

        public class IngredientDto
        {
            public string name;
            public int stack;
            public string imagePath;
            public int id;
            public int frameCount;
        }

        // --- Logic ---

        public static List<ItemSearchResult> SearchItem(string keyword, int limit = 10)
        {
            var candidates = new List<ItemSearchResult>();
            if (string.IsNullOrWhiteSpace(keyword)) return candidates;

            keyword = keyword.ToLower();

            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                var item = ContentSamples.ItemsByType[i];
                if (item == null || item.IsAir) continue;

                string name = Lang.GetItemNameValue(i);
                string lowerName = name.ToLower();

                int matchQ = -1;

                if (lowerName == keyword) matchQ = 0;
                else if (lowerName.Contains(keyword)) matchQ = 1;
                else if (StringUtils.IsFuzzyMatch(lowerName, keyword)) matchQ = 2;

                if (matchQ != -1)
                {
                    candidates.Add(new ItemSearchResult
                    {
                        id = i,
                        name = name,
                        modName = item.ModItem?.Mod.Name ?? "Terraria",
                        matchQuality = matchQ,
                        imagePath = AssetPathHelper.GetItemImagePath(i),
                        frameCount = GetItemFrameCount(i)
                    });
                }
            }

            return candidates
                .OrderBy(r => r.matchQuality)
                .ThenBy(r => r.name.Length)
                .Take(limit)
                .ToList();
        }

        public static ItemDetailDto GetItemDto(int itemId)
        {
            if (itemId <= 0 || itemId >= ItemLoader.ItemCount) return null;

            Item item = new Item();
            item.SetDefaults(itemId);

            var dto = new ItemDetailDto
            {
                id = itemId,
                name = item.Name,
                mod = item.ModItem?.Mod.Name ?? "Terraria",
                imagePath = AssetPathHelper.GetItemImagePath(itemId),
                frameCount = GetItemFrameCount(itemId),
                stats = new ItemStatsDto
                {
                    damage = item.damage,
                    defense = item.defense,
                    crit = item.crit + 4,
                    useTime = item.useTime,
                    knockBack = item.knockBack,
                    value = item.value,
                    autoReuse = item.autoReuse,
                    consumable = item.consumable,
                    maxStack = item.maxStack
                },
                droppedBy = new List<SourceDto>(),
                soldBy = new List<SourceDto>(),
                recipes = new List<RecipeDto>()
            };

            // Description
            var tooltips = Lang.GetTooltip(itemId);
            if (tooltips.Lines > 0)
            {
                StringBuilder descSb = new StringBuilder();
                for (int i = 0; i < tooltips.Lines; i++)
                {
                    string line = CleanTooltipLine(tooltips.GetLine(i));
                    if (!string.IsNullOrWhiteSpace(line)) descSb.AppendLine(line);
                }
                dto.description = descSb.ToString().Trim();
            }

            // Drops
            var drops = GetNpcsDroppingItem(itemId);
            foreach (var drop in drops)
            {
                dto.droppedBy.Add(new SourceDto
                {
                    name = Lang.GetNPCNameValue(drop),
                    imagePath = AssetPathHelper.GetNpcImagePath(drop)
                });
            }

            // Sold By
            var sellers = GetNpcsSellingItem(itemId);
            foreach (var seller in sellers)
            {
                dto.soldBy.Add(new SourceDto
                {
                    name = Lang.GetNPCNameValue(seller),
                    imagePath = AssetPathHelper.GetNpcImagePath(seller)
                });
            }

            // Recipes
            var recipes = Main.recipe.Where(r => r.createItem.type == itemId).ToList();
            foreach (var r in recipes)
            {
                var recipeDto = new RecipeDto
                {
                    resultName = item.Name,
                    resultCount = r.createItem.stack,
                    stations = new List<IngredientDto>(),
                    ingredients = new List<IngredientDto>()
                };

                foreach (var tileId in r.requiredTile)
                {
                    int stationItemId = GetFirstItemForTile(tileId);
                    if (stationItemId > 0)
                    {
                        recipeDto.stations.Add(new IngredientDto
                        {
                            name = Lang.GetItemNameValue(stationItemId),
                            stack = 1,
                            id = stationItemId,
                            imagePath = AssetPathHelper.GetItemImagePath(stationItemId),
                            frameCount = GetItemFrameCount(stationItemId)
                        });
                    }
                    else
                    {
                        string tileName = Lang.GetMapObjectName(tileId);
                        if (string.IsNullOrEmpty(tileName)) tileName = "Tile " + tileId;

                        recipeDto.stations.Add(new IngredientDto
                        {
                            name = tileName,
                            stack = 1,
                            id = -1,
                            imagePath = "",
                            frameCount = 1
                        });
                    }
                }

                if (recipeDto.stations.Count == 0)
                {
                    recipeDto.stations.Add(new IngredientDto
                    {
                        name = Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.TileHand"),
                        stack = 0,
                        id = 0,
                        imagePath = "",
                        frameCount = 1
                    });
                }

                foreach (var ing in r.requiredItem)
                {
                    if (!ing.IsAir)
                    {
                        recipeDto.ingredients.Add(new IngredientDto
                        {
                            name = ing.Name,
                            stack = ing.stack,
                            id = ing.type,
                            imagePath = AssetPathHelper.GetItemImagePath(ing.type),
                            frameCount = GetItemFrameCount(ing.type)
                        });
                    }
                }
                dto.recipes.Add(recipeDto);
            }

            return dto;
        }

        public static string GetItemDetailText(int itemId)
        {
            if (itemId <= 0 || itemId >= ItemLoader.ItemCount) return Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.ItemNotFound", itemId);

            Item item = new Item();
            item.SetDefaults(itemId);
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Header", item.Name, itemId));
            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Source", item.ModItem?.Mod.Name ?? "Terraria"));

            List<string> stats = new List<string>();
            if (item.damage > 0) stats.Add(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Stats.Damage", item.damage));
            if (item.defense > 0) stats.Add(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Stats.Defense", item.defense));
            if (item.damage > 0 && item.crit >= 0) stats.Add(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Stats.Crit", item.crit + 4));
            if (item.useTime > 0) stats.Add(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Stats.Speed", item.useTime));
            if (item.value > 0) stats.Add(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.Stats.Value", item.value / 100));

            if (stats.Count > 0)
            {
                sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.StatsHeader", string.Join(" | ", stats)));
                sb.AppendLine();
            }

            var tooltips = Lang.GetTooltip(itemId);
            if (tooltips.Lines > 0)
            {
                for (int i = 0; i < tooltips.Lines; i++)
                {
                    string line = CleanTooltipLine(tooltips.GetLine(i));
                    if (!string.IsNullOrWhiteSpace(line)) sb.AppendLine(line);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public static int GetItemFrameCount(int itemId)
        {
            if (itemId <= 0) return 1;
            if (Main.itemAnimations[itemId] != null)
            {
                return Main.itemAnimations[itemId].FrameCount;
            }
            return 1;
        }

        private static string CleanTooltipLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            try
            {
                var snippets = ChatManager.ParseMessage(text, Color.White);
                text = string.Join("", snippets.Select(s => s.Text));
            }
            catch
            {
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\[[cig]/[^:]+:(.+?)\]", "$1");
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\[i:[^\]]+\]", "");
            }
            return text.TrimStart('$', '%', '&', '^', ' ').Trim();
        }

        private static int GetFirstItemForTile(int tileId)
        {
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                var item = ContentSamples.ItemsByType[i];
                if (item != null && !item.IsAir && item.createTile == tileId)
                {
                    return i;
                }
            }
            return -1;
        }

        private static List<int> GetNpcsDroppingItem(int itemId)
        {
            HashSet<int> npcs = new HashSet<int>();
            for (int i = -65; i < NPCLoader.NPCCount; i++)
            {
                if (i == 0) continue;
                try
                {
                    var rules = Main.ItemDropsDB.GetRulesForNPCID(i, false);
                    if (rules != null)
                    {
                        foreach (var rule in rules)
                        {
                            if (RecursiveCheckRule(rule, itemId)) { npcs.Add(i); break; }
                        }
                    }
                }
                catch { }
            }
            return npcs.ToList();
        }

        private static List<int> GetNpcsSellingItem(int itemId)
        {
            HashSet<int> npcs = new HashSet<int>();
            foreach (var abstractShop in NPCShopDatabase.AllShops)
            {
                if (abstractShop is NPCShop npcShop)
                {
                    foreach (var entry in npcShop.Entries)
                    {
                        Item entryItem = GetItemFromShopEntry(entry);
                        if (entryItem != null && entryItem.type == itemId)
                        {
                            npcs.Add(npcShop.NpcType);
                            break;
                        }
                    }
                }
            }
            return npcs.ToList();
        }

        private static Item GetItemFromShopEntry(object entry)
        {
            if (entry == null) return null;
            try
            {
                var prop = entry.GetType().GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (prop != null) return prop.GetValue(entry) as Item;
                var field = entry.GetType().GetField("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) return field.GetValue(entry) as Item;
            }
            catch { }
            return null;
        }

        private static bool RecursiveCheckRule(IItemDropRule rule, int itemId)
        {
            if (rule == null) return false;
            if (rule is CommonDrop c && c.itemId == itemId) return true;
            if (rule is CommonDropNotScalingWithLuck cnl && cnl.itemId == itemId) return true;
            if (rule is ItemDropWithConditionRule ic && ic.itemId == itemId) return true;
            if (rule is OneFromOptionsDropRule opt && opt.dropIds != null && opt.dropIds.Contains(itemId)) return true;
            if (rule is OneFromOptionsNotScaledWithLuckDropRule optnl && optnl.dropIds != null && optnl.dropIds.Contains(itemId)) return true;
            if (rule is OneFromRulesRule rulesOpt && rulesOpt.options != null && rulesOpt.options.Any(r => RecursiveCheckRule(r, itemId))) return true;
            if (rule.ChainedRules != null)
            {
                foreach (var chain in rule.ChainedRules)
                    if (RecursiveCheckRule(chain.RuleToChain, itemId)) return true;
            }
            if (rule is DropBasedOnExpertMode de)
                if (RecursiveCheckRule(de.ruleForNormalMode, itemId) || RecursiveCheckRule(de.ruleForExpertMode, itemId)) return true;
            if (rule is DropBasedOnMasterMode dm)
                if (RecursiveCheckRule(dm.ruleForDefault, itemId) || RecursiveCheckRule(dm.ruleForMasterMode, itemId)) return true;
            return false;
        }
    }
}