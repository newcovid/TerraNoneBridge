using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TerraNoneBridge.Core.Net
{
    // --- Base Packet ---
    public class BasePacket
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    // --- Auth Packet (TML -> Server) ---
    public class AuthPacket : BasePacket
    {
        public AuthPacket() { Type = "auth"; }
        [JsonProperty("token")] public string Token { get; set; }
    }

    // --- Auth Response Packet (Server -> TML) ---
    public class AuthResponsePacket : BasePacket
    {
        public AuthResponsePacket() { Type = "auth_response"; }
        [JsonProperty("success")] public bool Success { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
    }

    // --- Event Packet ---
    public class EventPacket : BasePacket
    {
        public EventPacket(string eventType) { Type = "event"; EventType = eventType; }
        [JsonProperty("event_type")] public string EventType { get; set; }
        [JsonProperty("world_name")] public string WorldName { get; set; }
        [JsonProperty("motd")] public string Motd { get; set; }
    }

    // --- Chat Packet ---
    public class ChatPacket : BasePacket
    {
        public ChatPacket() { Type = "chat"; }
        [JsonProperty("user_name")] public string UserName { get; set; }
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("color")] public string ColorHex { get; set; }
    }

    // --- Command Packet (Incoming) ---
    public class CommandPacket : BasePacket
    {
        public CommandPacket() { Type = "command"; }
        [JsonProperty("command")] public string Command { get; set; }
        [JsonProperty("args")] public List<string> Args { get; set; }

        // [New] Request ID to support concurrent requests
        [JsonProperty("id")] public string Id { get; set; }
    }

    // --- Command Response Packet (Outgoing) ---
    public class CommandResponsePacket : BasePacket
    {
        public CommandResponsePacket() { Type = "command_response"; }

        [JsonProperty("status")]
        public string Status { get; set; } // "success" or "error"

        [JsonProperty("message")]
        public string Message { get; set; } // Human readable message (optional)

        [JsonProperty("data")]
        public object Data { get; set; } // Structured data payload

        // [New] Echo back the Request ID
        [JsonProperty("id")] public string Id { get; set; }
    }
}