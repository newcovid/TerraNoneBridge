using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerraNoneBridge.Core.Net;
using Terraria.Localization;
using Newtonsoft.Json;

namespace TerraNoneBridge.Core.Commands
{
    public class RconCaller : ICmdCaller
    {
        public CommandType CommandType => CommandType.Console;

        public Player Player => null;

        // Store the request ID for this session
        private readonly string _requestId;

        // Updated constructor to accept Request ID
        public RconCaller(string requestId = null)
        {
            _requestId = requestId;
        }

        public void Reply(string text, Color color = default)
        {
            // Legacy reply: wrap as a simple JSON message
            Reply(null, text, true);
        }

        public void Reply(object data, string message = null, bool success = true)
        {
            var socket = ModContent.GetInstance<SocketClient>();
            if (socket != null)
            {
                socket.Send(new CommandResponsePacket
                {
                    Status = success ? "success" : "error",
                    Message = message ?? "",
                    Data = data,
                    Id = _requestId // Echo back the ID here
                });
            }
        }
    }
}