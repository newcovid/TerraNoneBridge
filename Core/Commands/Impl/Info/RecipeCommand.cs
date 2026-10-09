using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Utils;
using TerraNoneBridge.Core.Services;

namespace TerraNoneBridge.Core.Commands.Impl.Info
{
    [ConsoleCommand("recipe", "recipe <name/id>", "Mods.TerraNoneBridge.Commands.Recipe.Desc")]
    public class RecipeCommand : IConsoleCommand
    {
        // ================= 数据传输对象 (DTOs) =================
        // 修改为 Public Fields 以匹配 camelCase 风格

        public class RecipeDataDto
        {
            public int targetId;
            public Dictionary<int, ItemNodeDto> nodes = new Dictionary<int, ItemNodeDto>();
            public List<RecipeDto> craftRecipes = new List<RecipeDto>();
            public List<RecipeDto> usageRecipes = new List<RecipeDto>();
        }

        public class ItemNodeDto
        {
            public int id;
            public string name;
            public string imagePath;
            public string mod;
            public int frameCount;
        }

        public class RecipeDto
        {
            public int recipeId;
            public int resultId;
            public int resultCount;
            public List<StationDto> stations = new List<StationDto>();
            public List<string> conditions = new List<string>();
            public List<IngredientDto> ingredients = new List<IngredientDto>();
        }

        public class IngredientDto
        {
            public int itemId;
            public int count;
            public string groupName;
            public List<int> groupIds;
        }

        public class StationDto
        {
            public int tileId; // -1 for Hand/None
            public string name;
            public string imagePath;
        }

        // ================= 指令逻辑 =================

        public void Execute(List<string> args, ICmdCaller caller)
        {
            if (args.Count == 0)
            {
                caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.InvalidArgs"));
                return;
            }

            string query = string.Join(" ", args);
            int targetId = -1;

            if (int.TryParse(query, out int parsedId))
            {
                if (parsedId > 0 && parsedId < ItemLoader.ItemCount)
                {
                    targetId = parsedId;
                }
            }

            if (targetId == -1)
            {
                var searchResults = ItemService.SearchItem(query, 20);

                if (searchResults.Count == 0)
                {
                    caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.ItemNotFound", query));
                    return;
                }

                // 更新引用: Name -> name, ID -> id
                var exactMatch = searchResults.FirstOrDefault(r => r.name.ToLower() == query.ToLower() || r.name == query);
                if (exactMatch != null)
                {
                    targetId = exactMatch.id;
                }
                else if (searchResults.Count == 1)
                {
                    targetId = searchResults[0].id;
                }
                else
                {
                    string idList = string.Join(", ", searchResults.Take(5).Select(r => $"{r.name}({r.id})"));
                    if (searchResults.Count > 5) idList += "...";
                    caller.Reply(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Common.MultiResult", idList));
                    return;
                }
            }

            var resultData = BuildRecipeData(targetId);
            caller.Reply(resultData, Language.GetTextValue("Mods.TerraNoneBridge.Commands.Recipe.Success"));
        }

        private RecipeDataDto BuildRecipeData(int targetId)
        {
            var data = new RecipeDataDto { targetId = targetId };

            HashSet<int> processedItems = new HashSet<int>();
            Queue<int> itemQueue = new Queue<int>();

            itemQueue.Enqueue(targetId);
            AddItemToNodes(targetId, data.nodes);

            int recipeCountLimit = 500;
            int currentRecipeCount = 0;

            while (itemQueue.Count > 0 && currentRecipeCount < recipeCountLimit)
            {
                int currentItemId = itemQueue.Dequeue();

                if (processedItems.Contains(currentItemId)) continue;
                processedItems.Add(currentItemId);

                foreach (var r in Main.recipe)
                {
                    if (r.createItem.type == currentItemId && !r.Disabled)
                    {
                        var recipeDto = ConvertToDto(r, data.nodes);
                        data.craftRecipes.Add(recipeDto);
                        currentRecipeCount++;

                        foreach (var ing in recipeDto.ingredients)
                        {
                            if (ing.itemId > 0 && !processedItems.Contains(ing.itemId))
                            {
                                itemQueue.Enqueue(ing.itemId);
                            }
                            if (ing.groupIds != null && ing.groupIds.Count > 0)
                            {
                                int repId = ing.groupIds[0];
                                if (!processedItems.Contains(repId))
                                {
                                    itemQueue.Enqueue(repId);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var r in Main.recipe)
            {
                if (r.Disabled) continue;

                bool usesTarget = false;
                if (r.requiredItem.Any(i => i.type == targetId))
                {
                    usesTarget = true;
                }
                else
                {
                    foreach (var groupIndex in r.acceptedGroups)
                    {
                        var group = RecipeGroup.recipeGroups[groupIndex];
                        if (group.ValidItems.Contains(targetId))
                        {
                            usesTarget = true;
                            break;
                        }
                    }
                }

                if (usesTarget)
                {
                    var recipeDto = ConvertToDto(r, data.nodes);
                    data.usageRecipes.Add(recipeDto);
                }
            }

            return data;
        }

        private RecipeDto ConvertToDto(Recipe r, Dictionary<int, ItemNodeDto> nodes)
        {
            var dto = new RecipeDto
            {
                recipeId = GetRecipeHash(r),
                resultId = r.createItem.type,
                resultCount = r.createItem.stack
            };

            AddItemToNodes(r.createItem.type, nodes);

            if (r.requiredTile != null && r.requiredTile.Count > 0)
            {
                foreach (int tileId in r.requiredTile)
                {
                    string imgPath = "";
                    string name = "";

                    int itemStyleId = GetItemFromTile(tileId);
                    if (itemStyleId > 0)
                    {
                        imgPath = AssetPathHelper.GetItemImagePath(itemStyleId);
                        name = Lang.GetItemNameValue(itemStyleId);
                    }
                    else
                    {
                        name = Lang.GetMapObjectName(ModContent.GetModTile(tileId)?.Type ?? tileId);
                        if (string.IsNullOrEmpty(name))
                            name = Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.TileDefault", tileId);
                        imgPath = $"Tile/Images/Tiles_{tileId}";
                    }

                    dto.stations.Add(new StationDto
                    {
                        tileId = tileId,
                        name = name,
                        imagePath = imgPath
                    });
                }
            }
            else
            {
                dto.stations.Add(new StationDto { tileId = -1, name = Language.GetTextValue("Mods.TerraNoneBridge.Services.Item.TileHand"), imagePath = "" });
            }

            foreach (var condition in r.Conditions)
            {
                dto.conditions.Add(condition.Description.Value);
            }

            foreach (var item in r.requiredItem)
            {
                var ingDto = new IngredientDto
                {
                    itemId = item.type,
                    count = item.stack
                };

                AddItemToNodes(item.type, nodes);

                int groupIndex = -1;
                foreach (var id in r.acceptedGroups)
                {
                    var group = RecipeGroup.recipeGroups[id];
                    if (group.ValidItems.Contains(item.type))
                    {
                        groupIndex = id;
                        break;
                    }
                }

                if (groupIndex != -1)
                {
                    var group = RecipeGroup.recipeGroups[groupIndex];
                    ingDto.groupName = group.GetText();
                    ingDto.groupIds = group.ValidItems.ToList();

                    foreach (var gid in ingDto.groupIds.Take(10))
                    {
                        AddItemToNodes(gid, nodes);
                    }
                }

                dto.ingredients.Add(ingDto);
            }

            return dto;
        }

        private void AddItemToNodes(int itemId, Dictionary<int, ItemNodeDto> nodes)
        {
            if (nodes.ContainsKey(itemId)) return;

            var item = ContentSamples.ItemsByType[itemId];
            if (item == null) return;

            string localName = Lang.GetItemNameValue(itemId);

            nodes[itemId] = new ItemNodeDto
            {
                id = itemId,
                name = string.IsNullOrEmpty(localName) ? item.Name : localName,
                imagePath = AssetPathHelper.GetItemImagePath(itemId),
                mod = item.ModItem?.Mod.Name ?? "Terraria",
                frameCount = ItemService.GetItemFrameCount(itemId)
            };
        }

        private int GetItemFromTile(int tileId)
        {
            foreach (var pair in ContentSamples.ItemsByType)
            {
                if (pair.Value.createTile == tileId)
                {
                    return pair.Key;
                }
            }
            return -1;
        }

        private int GetRecipeHash(Recipe r)
        {
            int hash = r.createItem.type;
            foreach (var i in r.requiredItem) hash = hash * 31 + i.type;
            foreach (var t in r.requiredTile) hash = hash * 17 + t;
            return hash;
        }
    }
}