using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Newtonsoft.Json;

namespace TerraNoneBridge.Core.Commands
{
    public class InGameCaller : ICmdCaller
    {
        private readonly CommandCaller _nativeCaller;

        public InGameCaller(CommandCaller caller)
        {
            _nativeCaller = caller;
        }

        public Player Player => _nativeCaller.Player;

        public CommandType CommandType => _nativeCaller.CommandType;

        public void Reply(string text, Color color = default)
        {
            if (color == default) color = Color.White;
            _nativeCaller.Reply(text, color);
        }

        public void Reply(object data, string message = null, bool success = true)
        {
            Color color = success ? Color.Green : Color.Red;

            // Prioritize the human-readable message if available
            if (!string.IsNullOrEmpty(message))
            {
                _nativeCaller.Reply(message, color);
            }
            else if (data != null)
            {
                // Fallback: If no message but data exists, print JSON (rare case in game)
                _nativeCaller.Reply(JsonConvert.SerializeObject(data), color);
            }
        }
    }
}