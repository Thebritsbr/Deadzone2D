// UpdateService.cs â€” VerificaÃ§Ã£o e instalaÃ§Ã£o automÃ¡tica de atualizaÃ§Ãµes (estilo Roblox).
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrossfireWin
{
    public sealed class UpdateService
    {
        public sealed record UpdateInfo(string Version, string PackageUrl, string? Notes, string[] Files = null);

        public static string LocalVersion = "1.9";
        public static string IgnoreVersion = "";
        public static UpdateInfo? Available;
        public static float DownloadProgress;
        public static bool SoftUpdateReady;
        private static Task? _download;
        private static string? _installerPath;
        private static bool _installerStarted;
        private static string? _pendingDir;

        public static bool Enabled => UpdateSettings.Enabled && !string.IsNullOrEmpty(UpdateSettings.ServerUrl);

        public static readonly string[] ServerList =
    {
        "https://example.com/deadzone/update.json",
        "https://example.com/deadzone/update2.json",
    };

    public static string ServerUrl => UpdateSettings.ServerUrl;

            // Registra uma nova versão num servidor HTTP (broadcast para todos os clientes).
        public static async Task<bool> PublishAsync(string serverUrl, string version, string packageUrl, string[] files, string notes = "")
        {
            try
            {
                var json = JsonSerializer.Serialize(new { Version = version, PackageUrl = packageUrl, Files = files, Notes = notes });
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await client.PostAsync(serverUrl, content);
                return true;
            }
            catch { return false; }
        }

public static async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                if (!Enabled) return null;
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                using var doc = JsonDocument.Parse(await client.GetStringAsync(UpdateSettings.ServerUrl));
                string? v = Get(doc.RootElement, "Version");
                string? p = Get(doc.RootElement, "PackageUrl") ?? Get(doc.RootElement, "DownloadUrl") ?? Get(doc.RootElement, "Url");
                string[] files = GetFiles(doc.RootElement);
                if (string.IsNullOrEmpty(v) || string.IsNullOrEmpty(p)) return null;
                if (CompareVersions(v, LocalVersion) <= 0) return null;
                var info = new UpdateInfo(v, p, Get(doc.RootElement, "Notes"), files);
                Available = info;
                return info;
            }
            catch { return null; }
        }

        private static string[] GetFiles(JsonElement root)
        {
            try
            {
                if (!root.TryGetProperty("Files", out var f) || f.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
                var list = new List<string>();
                foreach (var item in f.EnumerateArray())
                {
                    var s = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                    if (!string.IsNullOrEmpty(s)) list.Add(s);
                }
                return list.ToArray();
            }
            catch { return Array.Empty<string>(); }
        }

        private static string? Get(JsonElement root, string name) =>
            root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        public static int CompareVersions(string a, string b)
        {
            var pa = Parts(a);
            var pb = Parts(b);
            for (int i = 0; i < 3; i++)
            {
                int da = i < pa.Length ? pa[i] : 0;
                int db = i < pb.Length ? pb[i] : 0;
                if (da != db) return da.CompareTo(db);
            }
            return 0;
        }

        private static int[] Parts(string s) =>
            s.Split('.').Select(p => p.Trim() == "" ? 0 : (int.TryParse(p.TrimStart('v'), out var v) ? v : 0)).ToArray();

        public static void StartDownload()
        {
            if (Available == null || _download != null || _installerStarted) return;
            _installerStarted = true;
            _download = Task.Run(async () =>
            {
                try
                {
                    var dir = Path.Combine(Path.GetTempPath(), "deadzone-updates");
                    Directory.CreateDirectory(dir);
                    var name = Path.GetFileName(new Uri(Available.PackageUrl).LocalPath);
                    if (string.IsNullOrEmpty(name)) name = "DeadzoneSetup.exe";
                    _installerPath = Path.Combine(dir, name);
                    using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                    using var response = await client.GetAsync(Available.PackageUrl, HttpCompletionOption.ResponseHeadersRead);
                    using var file = File.Create(_installerPath);
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var buffer = new byte[64 * 1024];
                    int read;
                    long total = response.Content.Headers.ContentLength ?? 1;
                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await file.WriteAsync(buffer, 0, read);
                        if (total > 0) DownloadProgress = Math.Min(1f, (float)file.Length / total);
                    }
                    DownloadProgress = 1f;
                }
                catch { DownloadProgress = 1f; }
            });
        }

        public static bool DownloadFinished => DownloadProgress >= 1f;

        // ---------- AtualizaÃ§Ã£o sem reinstalar ----------
        public static bool StartSoftUpdate()
        {
            if (Available == null) return false;
            var files = Available.Files ?? Array.Empty<string>();
            if (files.Length == 0) return false;
            _pendingDir = Path.Combine(Path.GetTempPath(), "deadzone-update", "pending");
            var tmpDir = Path.Combine(Path.GetTempPath(), "deadzone-update", "files");
            Directory.CreateDirectory(tmpDir);
            var list = new List<string>();
            _download = Task.Run(async () =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                foreach (var rel in files)
                {
                    try
                    {
                        var url = new Uri(Available.PackageUrl + (Available.PackageUrl.Contains("/file/") ? "" : "/file/") + rel).ToString();
                        var dest = Path.Combine(tmpDir, Path.GetFileName(rel));
                        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();
                        using var fs = File.Create(dest);
                        using var st = await response.Content.ReadAsStreamAsync();
                        await st.CopyToAsync(fs);
                        list.Add(Path.GetFileName(rel));
                    }
                    catch { }
                }
                Directory.CreateDirectory(Path.GetDirectoryName(_pendingDir!)!);
                File.WriteAllText(_pendingDir, JsonSerializer.Serialize(list));
                SoftUpdateReady = list.Count > 0;
                DownloadProgress = 1f;
            });
            return true;
        }

        public static void InstallNow()
        {
            if (string.IsNullOrEmpty(_installerPath) || !File.Exists(_installerPath)) return;
            StartDownload(); // certo se ainda nÃ£o baixou
            Process.Start(new ProcessStartInfo
            {
                FileName = _installerPath,
                UseShellExecute = true,
                Arguments = "/VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            });
        }

        /// Aplicada no inÃ­cio do programa: copia os arquivos baixados a partir de uma
        /// atualizaÃ§Ã£o sem reinstalar.
        public static void ApplyPending()
        {
            try
            {
                string? marker = Path.Combine(Path.GetTempPath(), "deadzone-update", "pending");
                if (!File.Exists(marker)) return;
                var appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
                string root = Path.Combine(Path.GetTempPath(), "deadzone-update");
                foreach (var name in JsonSerializer.Deserialize<List<string>>(File.ReadAllText(marker)) ?? new List<string>())
                    File.Copy(Path.Combine(root, "files", name), Path.Combine(appDir, name), true);
            }
            catch { }
            finally
            {
                try { File.Delete(Path.Combine(Path.GetTempPath(), "deadzone-update", "pending")); Directory.Delete(Path.Combine(Path.GetTempPath(), "deadzone-update", "files"), true); } catch { }
            }
        }
    }

    public static class UpdateSettings
    {
        public static bool Enabled = true;
        public static string ServerUrl = "";

        public static void Load(string dir)
        {
            try
            {
                var p = System.IO.Path.Combine(dir, "Config", "Update.json");
                if (!File.Exists(p)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(p));
                var root = doc.RootElement;
                if (root.TryGetProperty("Enabled", out var e) && e.ValueKind == JsonValueKind.False) Enabled = false;
                if (root.TryGetProperty("ServerUrl", out var s) && s.ValueKind == JsonValueKind.String)
                    ServerUrl = s.GetString() ?? "";
            }
            catch { }
        }
    }
}








