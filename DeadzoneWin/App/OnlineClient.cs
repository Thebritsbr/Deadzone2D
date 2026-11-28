// OnlineClient.cs — cliente online do Deadzone2D (HTTP + WebSocket).
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Deadzone2D
{
    public sealed class OnlineConfig
    {
        public bool Enabled { get; set; } = true;
        public string ServerUrl { get; set; } = "http://localhost:3000";
    }

    public static class OnlineClient
    {
        public static OnlineConfig Config { get; private set; } = new();
        public static bool Connected => _ws?.State == WebSocketState.Open;
        public static string Token { get; private set; } = "";
        public static string? LastError { get; private set; }
        public static Action<string, JsonElement>? OnMessage;

        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static ClientWebSocket? _ws;
        private static CancellationTokenSource? _cts;

        public static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deadzone2D", "Online.json");

        public static void Load(string appDir)
        {
            try
            {
                var app = Path.Combine(appDir, "Config", "Online.json");
                if (File.Exists(app))
                    Config = JsonSerializer.Deserialize<OnlineConfig>(File.ReadAllText(app),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? Config;
            }
            catch { }
            // A URL do servidor público vem do Config/Online.json do próprio jogo.
            // Só usa uma configuração local se o arquivo do app não trouxer uma URL.
            try
            {
                if (string.IsNullOrWhiteSpace(Config.ServerUrl) && File.Exists(ConfigPath))
                {
                    var user = JsonSerializer.Deserialize<OnlineConfig>(File.ReadAllText(ConfigPath),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (user != null && !string.IsNullOrWhiteSpace(user.ServerUrl)) Config = user;
                }
            }
            catch { }
            try
            {
                var saved = Path.Combine(Path.GetDirectoryName(ConfigPath)!, "online-token.txt");
                if (File.Exists(saved)) Token = File.ReadAllText(saved).Trim();
            }
            catch { }
        }

        public static void SaveUrl(string url)
        {
            Config.ServerUrl = url.Trim().TrimEnd('/');
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Config,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static void SaveToken(string token)
        {
            Token = token;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(ConfigPath)!, "online-token.txt"), token);
            }
            catch { }
        }

        public static void ClearToken()
        {
            Token = "";
            try { File.Delete(Path.Combine(Path.GetDirectoryName(ConfigPath)!, "online-token.txt")); } catch { }
        }

        private static string Base => Config.ServerUrl.TrimEnd('/');

        public static async Task<(bool Ok, string Error, JsonElement Profile)> Register(string email, string password, string nick)
        {
            return await Auth("/api/register", new { email, password, nick, displayName = nick });
        }

        public static async Task<(bool Ok, string Error, JsonElement Profile)> Login(string email, string password)
        {
            return await Auth("/api/login", new { email, password });
        }

        private static async Task<(bool, string, JsonElement)> Auth(string path, object body)
        {
            LastError = "";
            try
            {
                var json = JsonSerializer.Serialize(body);
                var res = await Http.PostAsync(Base + path, new StringContent(json, Encoding.UTF8, "application/json"));
                var text = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(text);
                if (!res.IsSuccessStatusCode)
                {
                    LastError = doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() ?? "Server error." : "Server error.";
                    return (false, LastError, default);
                }
                if (!doc.RootElement.TryGetProperty("token", out var t)) return (false, "No token returned.", default);
                SaveToken(t.GetString() ?? "");
                var profile = doc.RootElement.TryGetProperty("profile", out var p) ? p.Clone() : default;
                return (true, "", profile);
            }
            catch (Exception ex)
            {
                LastError = "Connection failed: " + ex.Message;
                return (false, LastError, default);
            }
        }

        public static async Task<bool> Connect()
        {
            if (string.IsNullOrWhiteSpace(Token)) { LastError = "No online session."; return false; }
            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                _ws = new ClientWebSocket();
                var uri = new Uri(Base.Replace("http://", "ws://").Replace("https://", "wss://") + "/ws?token=" + Uri.EscapeDataString(Token));
                await _ws.ConnectAsync(uri, _cts.Token);
                _ = Task.Run(() => ReceiveLoop(_cts.Token));
                return true;
            }
            catch (Exception ex)
            {
                LastError = "WebSocket failed: " + ex.Message;
                return false;
            }
        }

        public static void Disconnect()
        {
            try { _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            _ws?.Dispose();
            _ws = null;
            _cts?.Cancel();
        }

        public static void Send(string type, object? data = null)
        {
            if (!Connected || _ws == null) return;
            var dict = new Dictionary<string, object?> { ["type"] = type };
            if (data != null)
                foreach (var prop in data.GetType().GetProperties())
                    dict[prop.Name] = prop.GetValue(data);
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dict));
            _ = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static async Task ReceiveLoop(CancellationToken token)
        {
            var buf = new byte[65536];
            try
            {
                while (_ws != null && _ws.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), token);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    string text = Encoding.UTF8.GetString(buf, 0, result.Count);
                    using var doc = JsonDocument.Parse(text);
                    string type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
                    OnMessage?.Invoke(type, doc.RootElement.Clone());
                }
            }
            catch { }
        }
    }
}
