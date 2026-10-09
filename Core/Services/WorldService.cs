using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerraNoneBridge.Core.Services
{
    public static class WorldService
    {
        public class BossEntryDto
        {
            public string name;
            public bool isDowned;
            public string type;
        }

        public class BossProgressDto
        {
            public string worldName;
            public string difficulty;
            public List<BossEntryDto> defeated;
            public List<BossEntryDto> undefeated;
        }

        public static BossProgressDto GetBossProgressDto()
        {
            // 修复：本地化 "Unknown" 和难度等级
            string difficultyKey = Main.masterMode ? "Master" : (Main.expertMode ? "Expert" : (Main.hardMode ? "Hardmode" : "Normal"));
            string difficultyText = Language.GetTextValue($"Mods.TerraNoneBridge.Common.Difficulty.{difficultyKey}");

            var dto = new BossProgressDto
            {
                worldName = Main.worldName ?? Language.GetTextValue("Mods.TerraNoneBridge.Common.Unknown"),
                difficulty = difficultyText,
                defeated = new List<BossEntryDto>(),
                undefeated = new List<BossEntryDto>()
            };

            List<BossEntryDto> all = new List<BossEntryDto>();
            bool usedChecklist = false;

            if (ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist))
            {
                try
                {
                    object response = bossChecklist.Call("GetBossInfoDictionary", TerraNoneBridge.Instance, "1.0");
                    if (response is Dictionary<string, Dictionary<string, object>> data)
                    {
                        usedChecklist = true;
                        foreach (var kvp in data)
                        {
                            var info = kvp.Value;
                            bool isDowned = false;
                            if (info.TryGetValue("downed", out object downedObj))
                            {
                                if (downedObj is Func<bool> downedFunc) isDowned = downedFunc();
                                else if (downedObj is bool downedBool) isDowned = downedBool;
                            }

                            // 默认使用 Checklist 提供的名称 (通常是英文或注册时的名称)
                            string displayName = kvp.Key;
                            if (info.TryGetValue("displayName", out object nameObj) && nameObj is string nameStr)
                                displayName = nameStr;

                            // [修复] 尝试通过 NPC ID 获取本地化名称
                            // BossChecklist 返回的 info 包含 "npcIDs" (List<int>)
                            if (info.TryGetValue("npcIDs", out object npcIdsObj) && npcIdsObj is List<int> npcIds && npcIds.Count > 0)
                            {
                                // 获取第一个关联 NPC 的本地化名称
                                string localizedName = Lang.GetNPCNameValue(npcIds[0]);
                                if (!string.IsNullOrEmpty(localizedName) && localizedName != "Unknown")
                                {
                                    displayName = localizedName;
                                }
                            }

                            all.Add(new BossEntryDto { name = displayName, isDowned = isDowned, type = "Mod/Checklist" });
                        }
                    }
                }
                catch { }
            }

            if (!usedChecklist)
            {
                AddVanilla(all, NPCID.KingSlime, NPC.downedSlimeKing);
                AddVanilla(all, NPCID.EyeofCthulhu, NPC.downedBoss1);
                AddVanilla(all, NPCID.EaterofWorldsHead, NPC.downedBoss2);
                AddVanilla(all, NPCID.SkeletronHead, NPC.downedBoss3);
                AddVanilla(all, NPCID.QueenBee, NPC.downedQueenBee);
                AddVanilla(all, NPCID.WallofFlesh, Main.hardMode);
                AddVanilla(all, NPCID.TheDestroyer, NPC.downedMechBoss1);
                AddVanilla(all, NPCID.Retinazer, NPC.downedMechBoss2);
                AddVanilla(all, NPCID.SkeletronPrime, NPC.downedMechBoss3);
                AddVanilla(all, NPCID.Plantera, NPC.downedPlantBoss);
                AddVanilla(all, NPCID.Golem, NPC.downedGolemBoss);
                AddVanilla(all, NPCID.DukeFishron, NPC.downedFishron);
                AddVanilla(all, NPCID.CultistBoss, NPC.downedAncientCultist);
                AddVanilla(all, NPCID.MoonLordHead, NPC.downedMoonlord);
            }

            dto.defeated = all.Where(x => x.isDowned).ToList();
            dto.undefeated = all.Where(x => !x.isDowned).ToList();

            return dto;
        }

        private static void AddVanilla(List<BossEntryDto> list, int id, bool downed)
        {
            list.Add(new BossEntryDto { name = Lang.GetNPCNameValue(id), isDowned = downed, type = "Vanilla" });
        }

        public static string GetBossProgressText()
        {
            var dto = GetBossProgressDto();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Boss.Header", dto.worldName));
            sb.AppendLine($"{Language.GetTextValue("Mods.TerraNoneBridge.Commands.Boss.Mode")}: {dto.difficulty}");

            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Boss.ListDefeatedBoss"));
            if (dto.defeated.Count == 0) sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Common.None"));
            else foreach (var b in dto.defeated) sb.AppendLine($" [x] {b.name}");

            sb.AppendLine();
            sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Commands.Boss.ListUndefeatedBoss"));
            if (dto.undefeated.Count == 0) sb.AppendLine(Language.GetTextValue("Mods.TerraNoneBridge.Common.None"));
            else foreach (var b in dto.undefeated) sb.AppendLine($" [ ] {b.name}");

            return sb.ToString();
        }
    }
}