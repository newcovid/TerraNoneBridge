using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using TerraNoneBridge.Core.Net;

namespace TerraNoneBridge.Core.Config
{
    public class ServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("ConnectionSettings")]

        [DefaultValue("127.0.0.1")]
        public string IpAddress { get; set; }

        [Range(1, 65535)]
        [DefaultValue(7778)]
        public int Port { get; set; }

        [DefaultValue("")]
        public string AccessToken { get; set; }

        [Range(0, 3600)]
        [DefaultValue(5)]
        public int ReconnectInterval { get; set; }

        [Header("FeatureSwitches")]

        [DefaultValue(true)]
        public bool EnableChatSync { get; set; }

        [DefaultValue(true)]
        public bool EnableEventBroadcast { get; set; }

        [Header("ExportSettings")]

        // [新增] 自定义导出路径
        // 留空则默认使用 "TerraNoneBridge_Exports/yyyyMMdd_HHmmss"
        [DefaultValue("")]
        public string CustomExportPath { get; set; }

        public override void OnChanged()
        {
            base.OnChanged();

            var socket = ModContent.GetInstance<SocketClient>();
            if (socket != null)
            {
                System.Threading.Tasks.Task.Run(() => socket.Reconnect());
            }
        }
    }
}