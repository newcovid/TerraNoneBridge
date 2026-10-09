using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TerraNoneBridge.Core.Commands
{
    public interface ICmdCaller
    {
        // Old text-based reply (still used for Chat packets or simple feedback)
        void Reply(string text, Color color = default);

        // [New] Object-based reply for JSON communication
        // For In-Game caller: it should print the message or a string representation of data.
        // For Rcon caller: it should serialize data to JSON.
        void Reply(object data, string message = null, bool success = true);

        Player Player { get; }
        CommandType CommandType { get; }
    }
}