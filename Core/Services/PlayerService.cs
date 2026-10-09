using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using TerraNoneBridge.Core.Utils;

namespace TerraNoneBridge.Core.Services
{
    public static class PlayerService
    {
        public class InventoryItemDto
        {
            public int slot;
            public int id;
            public string name;
            public int stack;
            public string prefix; // e.g., "Legendary", "Warding"
            public string imagePath;
            public int frameCount; // [新增] 用于前端裁剪精灵图
        }

        public class PlayerInventoryDto
        {
            public string playerName;
            public List<InventoryItemDto> inventory;
            public List<InventoryItemDto> armor;
            public List<InventoryItemDto> misc; // Dyes, misc equip
        }

        public static Player FindPlayerByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.ToLower();

            // Try exact match first
            var exact = Main.player.FirstOrDefault(p => p.active && p.name.ToLower() == name);
            if (exact != null) return exact;

            // Try contains
            return Main.player.FirstOrDefault(p => p.active && p.name.ToLower().Contains(name));
        }

        public static PlayerInventoryDto GetPlayerInventoryDto(string playerName)
        {
            Player p = FindPlayerByName(playerName);
            if (p == null) return null;

            var dto = new PlayerInventoryDto
            {
                playerName = p.name,
                inventory = new List<InventoryItemDto>(),
                armor = new List<InventoryItemDto>(),
                misc = new List<InventoryItemDto>()
            };

            // Main Inventory (58 slots usually)
            for (int i = 0; i < 58; i++)
            {
                if (!p.inventory[i].IsAir)
                {
                    dto.inventory.Add(CreateItemDto(i, p.inventory[i]));
                }
            }

            // Armor & Accessories (0-19)
            for (int i = 0; i < p.armor.Length; i++)
            {
                if (!p.armor[i].IsAir)
                {
                    dto.armor.Add(CreateItemDto(i, p.armor[i]));
                }
            }

            // Misc Equips (hooks, mounts, etc) and Dyes
            for (int i = 0; i < p.miscEquips.Length; i++)
            {
                if (!p.miscEquips[i].IsAir)
                {
                    dto.misc.Add(CreateItemDto(i, p.miscEquips[i]));
                }
            }
            for (int i = 0; i < p.miscDyes.Length; i++)
            {
                if (!p.miscDyes[i].IsAir)
                {
                    dto.misc.Add(CreateItemDto(100 + i, p.miscDyes[i]));
                }
            }

            return dto;
        }

        private static InventoryItemDto CreateItemDto(int slot, Item item)
        {
            string prefixName = "";
            if (item.prefix > 0)
            {
                prefixName = Lang.prefix[item.prefix].Value;
            }

            return new InventoryItemDto
            {
                slot = slot,
                id = item.type,
                name = item.Name,
                stack = item.stack,
                prefix = prefixName,
                imagePath = AssetPathHelper.GetItemImagePath(item.type),
                frameCount = ItemService.GetItemFrameCount(item.type) // [新增] 获取物品帧数
            };
        }
    }
}