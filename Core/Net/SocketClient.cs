using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using TerraNoneBridge.Core.Config;
using TerraNoneBridge.Core.Hooks;
using TerraNoneBridge.Core.Commands;
using Terraria.Localization;
using System.Collections.Generic;
using System.Linq;

namespace TerraNoneBridge.Core.Net
{
    public class SocketClient : ModSystem
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _lifecycleCts;

        private volatile bool _isAuthenticated = false;

        private ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

        private bool ShouldRun => Main.dedServ;

        private const int UnsafeThresholdSeconds = 3;

        public override void OnModLoad()
        {
            if (!ShouldRun) return;
            StartService();
        }

        public override void OnModUnload()
        {
            StopService();
        }

        public override void PostUpdateEverything()
        {
            TerraNoneBridge.LastGameUpdate = DateTime.Now;

            if (!ShouldRun) return;

            while (_mainThreadQueue.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Mod.Logger.Error(Language.GetTextValue("Mods.TerraNoneBridge.Log.MainThreadPacketError", ex.Message));
                }
            }
        }

        private void StartService()
        {
            if (_lifecycleCts != null) return;
            _lifecycleCts = new CancellationTokenSource();
            Task.Run(() => ConnectionLifecycleLoop(_lifecycleCts.Token));
        }

        private void StopService()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;
            CleanupSocket();
        }

        public void Reconnect()
        {
            if (!ShouldRun) return;
            StopService();
            StartService();
        }

        private void CleanupSocket()
        {
            try { if (_ws != null) { _ws.Dispose(); _ws = null; } } catch { }
            _isAuthenticated = false;
        }

        private async Task ConnectionLifecycleLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RunConnectionSession(token);
                }
                catch (Exception ex)
                {
                    Mod.Logger.Warn(Language.GetTextValue("Mods.TerraNoneBridge.Net.ConnectFail", ex.Message));
                }

                CleanupSocket();
                if (token.IsCancellationRequested) break;

                int interval = ModContent.GetInstance<ServerConfig>().ReconnectInterval;
                if (interval <= 0) { Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.ReconnectDisabled")); break; }

                Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.ReconnectingIn", interval));
                try { await Task.Delay(interval * 1000, token); } catch (OperationCanceledException) { break; }
            }
        }

        private Uri GetServerUri()
        {
            var config = ModContent.GetInstance<ServerConfig>();
            string ip = string.IsNullOrWhiteSpace(config.IpAddress) ? "127.0.0.1" : config.IpAddress;
            return new Uri($"ws://{ip}:{config.Port}");
        }

        private async Task RunConnectionSession(CancellationToken token)
        {
            _ws = new ClientWebSocket();
            Uri uri = GetServerUri();

            Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.Connecting", uri));

            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                connectCts.CancelAfter(TimeSpan.FromSeconds(10));
                await _ws.ConnectAsync(uri, connectCts.Token);
            }

            Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.Connected"));
            SendAuth();

            var receiveTask = ReceiveLoop(token);
            var sendTask = SendLoop(token);
            await Task.WhenAny(receiveTask, sendTask);
        }

        private void SendAuth()
        {
            var config = ModContent.GetInstance<ServerConfig>();
            Send(new AuthPacket { Token = config.AccessToken });
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[1024 * 64];
            try
            {
                while (_ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    DispatchMessage(message);
                }
            }
            catch (Exception) { throw; }
            finally { Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.Disconnected")); }
        }

        private void DispatchMessage(string json)
        {
            try
            {
                var basePacket = JsonConvert.DeserializeObject<BasePacket>(json);
                if (basePacket == null) return;

                if (basePacket.Type == "auth_response")
                {
                    var authPacket = JsonConvert.DeserializeObject<AuthResponsePacket>(json);
                    if (authPacket.Success)
                    {
                        _isAuthenticated = true;
                        Mod.Logger.Info(Language.GetTextValue("Mods.TerraNoneBridge.Net.AuthSuccess"));
                        if (ModContent.GetInstance<ServerConfig>().EnableEventBroadcast)
                        {
                            Send(new EventPacket("server_ready")
                            {
                                WorldName = Main.worldName ?? "Unknown",
                                Motd = Language.GetTextValue("Mods.TerraNoneBridge.Net.MotdEstablished")
                            });
                        }
                    }
                    else
                    {
                        Mod.Logger.Error(Language.GetTextValue("Mods.TerraNoneBridge.Net.AuthFail", authPacket.Message));
                        _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Auth Failed", CancellationToken.None);
                    }
                    return;
                }

                if (!_isAuthenticated) return;

                if (basePacket.Type == "command")
                {
                    var cmdPacket = JsonConvert.DeserializeObject<CommandPacket>(json);

                    // [Updated] Extract Request ID
                    string requestId = cmdPacket.Id;

                    // 预处理变量
                    string cmdName = cmdPacket.Command;
                    List<string> args = cmdPacket.Args ?? new List<string>();

                    // 1. 正常尝试查找指令
                    var cmdInfo = CommandManager.GetCommandInfo(cmdName);

                    // 2. [容错修复] 如果找不到指令，且参数为空，但指令名包含空格
                    if (cmdInfo == null && args.Count == 0 && cmdName.Contains(' '))
                    {
                        var parts = cmdName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (parts.Count > 0)
                        {
                            string potentialCmd = parts[0];
                            if (CommandManager.GetCommandInfo(potentialCmd) != null)
                            {
                                cmdName = potentialCmd;
                                parts.RemoveAt(0);
                                args = parts;
                                cmdInfo = CommandManager.GetCommandInfo(cmdName);
                            }
                        }
                    }

                    if (cmdInfo != null)
                    {
                        if (cmdInfo.IsModify)
                        {
                            bool isEnvironmentSafe = (DateTime.Now - TerraNoneBridge.LastGameUpdate).TotalSeconds < UnsafeThresholdSeconds;

                            if (isEnvironmentSafe)
                            {
                                // [Updated] Pass requestId to RconCaller
                                _mainThreadQueue.Enqueue(() => CommandManager.HandleCommand(cmdName, args, new RconCaller(requestId)));
                            }
                            else
                            {
                                // [Updated] Pass requestId to RconCaller
                                new RconCaller(requestId).Reply(Language.GetTextValue("Mods.TerraNoneBridge.Net.EnvironmentUnsafe"), null, false);
                            }
                        }
                        else
                        {
                            // [Updated] Pass requestId to RconCaller
                            CommandManager.HandleCommand(cmdName, args, new RconCaller(requestId));
                        }
                    }
                    else
                    {
                        // [Updated] Pass requestId to RconCaller
                        CommandManager.HandleCommand(cmdPacket.Command, cmdPacket.Args, new RconCaller(requestId));
                    }
                }
                else if (basePacket.Type == "chat")
                {
                    var chatPacket = JsonConvert.DeserializeObject<ChatPacket>(json);
                    _mainThreadQueue.Enqueue(() =>
                    {
                        if (ModContent.GetInstance<ServerConfig>().EnableChatSync)
                        {
                            ChatHook.OnReceiveRemoteChat(chatPacket);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Error(Language.GetTextValue("Mods.TerraNoneBridge.Log.PacketError", ex.Message));
            }
        }

        private async Task SendLoop(CancellationToken token)
        {
            while (_ws != null && _ws.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                if (_sendQueue.TryDequeue(out string message))
                {
                    try { await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)), WebSocketMessageType.Text, true, token); }
                    catch { }
                }
                else
                {
                    try { await Task.Delay(50, token); } catch { }
                }
            }
        }

        public void Send(BasePacket packet)
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                _sendQueue.Enqueue(JsonConvert.SerializeObject(packet));
            }
        }
    }
}