// GoogleAuth.cs — login real com Google (OAuth 2.0 / authorization code + loopback).
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;

namespace Deadzone2D
{
    public sealed class GoogleConfig
    {
        public bool Enabled { get; set; }
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
    }

    public sealed record GoogleProfile(string Email, string Name, string Id, string Picture);

    public static class GoogleAuth
    {
        public const string RedirectUri = "http://127.0.0.1:53682/";
        private const int Port = 53682;
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
        public static GoogleConfig Config { get; private set; } = new();

        public static string UserConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deadzone2D", "Google.json");

        public static void Load(string dir)
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            try
            {
                var app = Path.Combine(dir, "Config", "Google.json");
                if (File.Exists(app))
                {
                    var baseCfg = JsonSerializer.Deserialize<GoogleConfig>(File.ReadAllText(app), opts);
                    if (baseCfg != null) Config = baseCfg;
                }
            }
            catch { }
            // A configuração salva pelo jogador tem prioridade e fica fora de Program Files.
            try
            {
                if (File.Exists(UserConfigPath))
                {
                    var user = JsonSerializer.Deserialize<GoogleConfig>(File.ReadAllText(UserConfigPath), opts);
                    if (user != null) Config = user;
                }
            }
            catch { }
        }

        public static bool SaveUserConfig(string clientId, string secret, bool enabled, out string error)
        {
            error = "";
            clientId = (clientId ?? "").Trim();
            secret = (secret ?? "").Trim();
            if (enabled && (clientId.Length == 0 || secret.Length == 0))
            {
                error = "Informe o Client ID e o Client Secret.";
                return false;
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(UserConfigPath)!);
                var cfg = new GoogleConfig { Enabled = enabled, ClientId = clientId, ClientSecret = secret };
                File.WriteAllText(UserConfigPath, JsonSerializer.Serialize(cfg,
                    new JsonSerializerOptions { WriteIndented = true }));
                Config = cfg;
                return true;
            }
            catch (Exception ex)
            {
                error = "Could not save: " + ex.Message;
                return false;
            }
        }

        public static bool IsConfigured =>
            Config.Enabled &&
            !string.IsNullOrWhiteSpace(Config.ClientId) &&
            !string.IsNullOrWhiteSpace(Config.ClientSecret);

        public static bool SignIn(out GoogleProfile? profile, out string error)
        {
            profile = null;
            error = "";
            if (!IsConfigured)
            {
                error = "Google sign-in is not available on this installation. Use Gmail and password.";
                return false;
            }

            string state = Guid.NewGuid().ToString("N");
            string url = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code" +
                         "&client_id=" + Uri.EscapeDataString(Config.ClientId.Trim()) +
                         "&redirect_uri=" + Uri.EscapeDataString(RedirectUri) +
                         "&scope=" + Uri.EscapeDataString("openid email profile") +
                         "&state=" + state +
                         "&prompt=select_account&access_type=online";

            try
            {
                using var listener = new TcpListener(IPAddress.Loopback, Port);
                listener.Start();
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                string? raw = null;
                var deadline = DateTime.UtcNow.AddMinutes(5);
                while (DateTime.UtcNow < deadline && raw == null)
                {
                    if (listener.Pending())
                    {
                        using var client = listener.AcceptTcpClient();
                        using var stream = client.GetStream();
                        var buf = new byte[8192];
                        int n = stream.Read(buf, 0, buf.Length);
                        raw = Encoding.UTF8.GetString(buf, 0, Math.Max(0, n));
                        string page = "<html><body style='background:#0F1923;color:#ECE8E1;font-family:Arial;text-align:center;padding:60px'>" +
                                      "<h1>Deadzone2D</h1><p>You can close this window and return to the game.</p></body></html>";
                        var reply = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\n" +
                                                           "Content-Length: " + Encoding.UTF8.GetByteCount(page) + "\r\n" +
                                                           "Connection: close\r\n\r\n" + page);
                        stream.Write(reply, 0, reply.Length);
                    }
                    else Thread.Sleep(100);
                }
                if (raw == null) { error = "Tempo esgotado aguardando o login do Google."; return false; }

                var first = raw.Split('\n')[0];
                var parts = first.Split(' ');
                if (parts.Length < 2) { error = "Invalid response from Google."; return false; }
                var query = ParseQuery(parts[1].Contains('?') ? parts[1].Split('?')[1] : "");
                if (!query.TryGetValue("state", out var returnedState) || returnedState != state)
                { error = "Security error: invalid state. Please try again."; return false; }
                if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
                { error = "Google did not return an authorization code."; return false; }

                using var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("code", code),
                    new KeyValuePair<string,string>("client_id", Config.ClientId.Trim()),
                    new KeyValuePair<string,string>("client_secret", Config.ClientSecret.Trim()),
                    new KeyValuePair<string,string>("redirect_uri", RedirectUri),
                    new KeyValuePair<string,string>("grant_type", "authorization_code")
                });
                var tokenRes = Http.PostAsync("https://oauth2.googleapis.com/token", form).GetAwaiter().GetResult();
                var tokenJson = tokenRes.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!tokenRes.IsSuccessStatusCode) { error = "Failed to exchange the code: " + tokenJson; return false; }
                using var tokenDoc = JsonDocument.Parse(tokenJson);
                if (!tokenDoc.RootElement.TryGetProperty("access_token", out var tok))
                { error = "Google token not received."; return false; }
                string access = tok.GetString() ?? "";

                using var req = new HttpRequestMessage(HttpMethod.Get, "https://openidconnect.googleapis.com/v1/userinfo");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", access);
                var infoRes = Http.SendAsync(req).GetAwaiter().GetResult();
                var infoJson = infoRes.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!infoRes.IsSuccessStatusCode) { error = "Falha ao ler o perfil do Google."; return false; }
                using var info = JsonDocument.Parse(infoJson);
                string email = Get(info, "email");
                string name = Get(info, "name");
                string gid = Get(info, "sub");
                string picture = Get(info, "picture");
                if (string.IsNullOrWhiteSpace(email)) { error = "Google did not provide the account email."; return false; }
                profile = new GoogleProfile(email, name, gid, picture);
                return true;
            }
            catch (Exception ex)
            {
                error = "Erro no login com Google: " + ex.Message;
                return false;
            }
        }

        private static string Get(JsonDocument doc, string name) =>
            doc.RootElement.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in query.Split('&'))
            {
                if (pair.Length == 0) continue;
                var kv = pair.Split('=', 2);
                d[Uri.UnescapeDataString(kv[0])] = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
            }
            return d;
        }
    }
}
