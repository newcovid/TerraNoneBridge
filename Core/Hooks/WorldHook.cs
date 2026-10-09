using Terraria;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Net;
using TerraNoneBridge.Core.Config;
using Terraria.Localization;

namespace TerraNoneBridge.Core.Hooks
{
    /// <summary>
    /// 负责处理世界加载、卸载等全局事件。
    /// </summary>
    public class WorldHook : ModSystem
    {
        public override void OnWorldLoad()
        {
            if (ModContent.GetInstance<ServerConfig>().EnableEventBroadcast)
            {
                var socket = ModContent.GetInstance<SocketClient>();
                socket.Send(new EventPacket("world_load")
                {
                    WorldName = Main.worldName ?? Language.GetTextValue("Mods.TerraNoneBridge.Common.Unknown"),
                    Motd = Language.GetTextValue("Mods.TerraNoneBridge.Hooks.World.Load")
                });
            }
        }

        public override void OnWorldUnload()
        {
            if (ModContent.GetInstance<ServerConfig>().EnableEventBroadcast)
            {
                var socket = ModContent.GetInstance<SocketClient>();
                socket.Send(new EventPacket("world_unload")
                {
                    WorldName = Main.worldName ?? Language.GetTextValue("Mods.TerraNoneBridge.Common.Unknown"),
                    Motd = Language.GetTextValue("Mods.TerraNoneBridge.Hooks.World.Unload")
                });
            }
        }
    }
}