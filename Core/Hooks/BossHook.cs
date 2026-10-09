using Terraria;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Config;
using Terraria.DataStructures;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Hooks
{
    /// <summary>
    /// 负责监听 Boss 的生成与击杀事件。
    /// </summary>
    public class BossHook : GlobalNPC
    {
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (npc.boss && ModContent.GetInstance<ServerConfig>().EnableEventBroadcast)
            {
                SendEvent("boss_spawn", Language.GetTextValue("Mods.TerraNoneBridge.Hooks.Boss.Spawn", npc.FullName));
            }
        }

        public override void OnKill(NPC npc)
        {
            if (npc.boss && ModContent.GetInstance<ServerConfig>().EnableEventBroadcast)
            {
                SendEvent("boss_kill", Language.GetTextValue("Mods.TerraNoneBridge.Hooks.Boss.Kill", npc.FullName));
            }
        }

        private void SendEvent(string type, string msg)
        {
            var socket = ModContent.GetInstance<SocketClient>();
            if (socket != null)
            {
                socket.Send(new EventPacket(type)
                {
                    WorldName = Main.worldName,
                    Motd = msg
                });
            }
        }
    }
}