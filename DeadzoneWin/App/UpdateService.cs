// UpdateService.cs — Verificação, download e instalação automática de atualizações.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Deadzone2D
{
    public sealed class UpdateService
    {
        public sealed record UpdateInfo(string Version, string PackageUrl, string? Notes, string[]? Files = null, string? FilesUrl = null);

        public static string LocalVersion = "0.0";
        public static string IgnoreVersion = "";
        public static DateTime IgnoreUntilUtc;
        public static UpdateInfo? Available;
        public static volatile float DownloadProgress;
        public static bool SoftUpdateReady;
        public static bool DownloadStarted;

        private static Task? _download;              // download do instalador completo
        private static Thread? _installThread;       // fluxo "Instalar now" (thread em 1º plano)
        private static Thread? _softThread;          // fluxo "Sem reinstalar" (thread em 1º plano)
        private static string? _installerPath;
        private static string? _lastCheckUrl;        // URL do último manifesto consultado com sucesso
        private static string? _lastError;
        private static bool _installerStarted;
        private static bool _downloadFailed;

        // Aspas geradas em tempo de execução (evita escapes no código).
        private static readonly string Q = new string(Convert.ToChar(34), 1);

        public static bool Enabled => UpdateSettings.Enabled &&
            (!string.IsNullOrEmpty(UpdateSettings.ServerUrl) || ServerList.Any(u => !string.IsNullOrWhiteSpace(u)));

        public static string ServerUrl => UpdateSettings.ServerUrl;

        // Servidores alternativos consultados quando o ServerUrl configurado falha.
        public static readonly string[] ServerList =
        {
            "https://thebritsbr.github.io/Deadzone2D/update.json",
        };

        // ---------- Consulta de versão ----------
        public static async Task<UpdateInfo?> CheckAsync()
        {
            if (!Enabled) return null;

            // Servidor principal primeiro, depois os alternativos (sem repetir URL).
            var urls = new List<string>();
            void Add(string? u)
            {
                u = u?.Trim();
                if (!string.IsNullOrEmpty(u) && !urls.Contains(u)) urls.Add(u);
            }
            Add(UpdateSettings.ServerUrl);
            foreach (var u in ServerList) Add(u);

            string? fail = null;
            foreach (var url in urls)
            {
                try
                {
                    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    string json = (await client.GetStringAsync(url)).TrimStart('\uFEFF');
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    string? v = Get(root, "Version");
                    string? p = Get(root, "PackageUrl") ?? Get(root, "DownloadUrl") ?? Get(root, "Url");
                    string[] files = GetFiles(root);
                    if (string.IsNullOrEmpty(v) || string.IsNullOrEmpty(p))
                    {
                        fail = "Manifest missing version or package at " + url;
                        continue;
                    }
                    if (CompareVersions(v, LocalVersion) <= 0) { _lastError = null; return null; }
                    if (IsIgnored(v)) { _lastError = null; return null; }
                    var info = new UpdateInfo(v, p, Get(root, "Notes"), files, Get(root, "FilesUrl"));
                    _lastCheckUrl = url; // lembra de onde veio o manifesto (base dos arquivos avulsos)
                    Available = info;
                    _lastError = null;
                    return info;
                }
                catch (Exception ex) { fail = url + ": " + ex.Message; }
            }
            _lastError = fail;
            return null;
        }

        // Base (diretório) onde ficam os arquivos avulsos da atualização.
        // Ordem: FilesUrl do manifesto; PackageUrl com prefixo /file/; diretório do manifesto.
        private static string ResolveFilesBase(UpdateInfo info)
        {
            if (!string.IsNullOrWhiteSpace(info.FilesUrl)) return info.FilesUrl.Trim().TrimEnd('/');

            var pkg = info.PackageUrl ?? "";
            int idx = pkg.IndexOf("/file/", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) return pkg.Substring(0, idx + "/file/".Length).TrimEnd('/');

            string baseRef = string.IsNullOrWhiteSpace(_lastCheckUrl) ? pkg : _lastCheckUrl!;
            if (!string.IsNullOrWhiteSpace(baseRef))
            {
                int slash = baseRef.LastIndexOf('/');
                if (slash > 8) return baseRef.Substring(0, slash); // preserva scheme://host
            }
            return "";
        }

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

        // ---------- Download do instalador completo ----------
        public static void StartDownload()
        {
            var info = Available;
            if (info == null) return;
            if (_download != null && !_download.IsCompleted) return; // já em andamento
            DownloadStarted = true;
            _downloadFailed = false;
            _lastError = null;
            DownloadProgress = 0f;
            _download = Task.Run(async () =>
            {
                try
                {
                    var dir = Path.Combine(Path.GetTempPath(), "deadzone-updates");
                    Directory.CreateDirectory(dir);
                    var path = Path.Combine(dir, FileNameFromUrl(info.PackageUrl));
                    _installerPath = path;
                    await DownloadInstallerAsync(info.PackageUrl, path, p => DownloadProgress = p);
                    DownloadProgress = 1f; // só marca "pronto" quando o download completou
                }
                catch (Exception ex)
                {
                    _downloadFailed = true; // falha NÃO é mais mascarada como 100%
                    _lastError = ex.Message;
                }
            });
        }

        public static bool DownloadFinished => DownloadProgress >= 1f && !_downloadFailed;
        public static bool DownloadFailed => _downloadFailed;
        public static string? LastError => _lastError;
        public static bool InstallerStarted => _installerStarted;

        private static string FileNameFromUrl(string url)
        {
            try
            {
                var name = Path.GetFileName(new Uri(url).LocalPath);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            catch { }
            return "DeadzoneSetup.exe";
        }

        // Baixa em streaming para um arquivo .part e só promove ao nome final no sucesso.
        private static async Task DownloadInstallerAsync(string url, string destPath, Action<float> progress)
        {
            string partPath = destPath + ".part";
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                long total = response.Content.Headers.ContentLength ?? -1;
                using var stream = await response.Content.ReadAsStreamAsync();
                using var file = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024);
                var buffer = new byte[64 * 1024];
                long done = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    file.Write(buffer, 0, read);
                    done += read;
                    if (total > 0) progress(Math.Min(1f, (float)done / total));
                }
            }
            File.Move(partPath, destPath, true);
            progress(1f);
        }

        // ---------- Instalação ----------
        // Roda em thread de 1º plano: sobrevive ao fechamento do jogo até o instalador abrir.
        public static Task InstallNow()
        {
            var info = Available;
            if (info == null) return Task.CompletedTask;
            if (_installerStarted) return Task.CompletedTask; // instalador já foi iniciado
            if (_installThread != null && _installThread.IsAlive) return Task.CompletedTask;
            DownloadStarted = true;
            var t = new Thread(() =>
            {
                try { RunInstallerFlow(info).GetAwaiter().GetResult(); }
                catch (Exception ex)
                {
                    _downloadFailed = true;
                    _lastError = ex.Message;
                    _installerStarted = false;
                }
            });
            t.IsBackground = false;
            _installThread = t;
            t.Start();
            return Task.CompletedTask;
        }

        private static async Task RunInstallerFlow(UpdateInfo info)
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "deadzone-updates");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, FileNameFromUrl(info.PackageUrl));
                _installerPath = path;

                // Espera um download já em andamento antes de decidir rebaixar.
                var running = _download;
                if (running != null && !running.IsCompleted)
                {
                    try { running.Wait(TimeSpan.FromMinutes(10)); } catch { }
                }

                bool ready = !_downloadFailed && File.Exists(path) && new FileInfo(path).Length > 0;
                if (!ready)
                {
                    _downloadFailed = false;
                    _lastError = null;
                    DownloadProgress = 0f;
                    await DownloadInstallerAsync(info.PackageUrl, path, p => DownloadProgress = p);
                }
                DownloadProgress = 1f;

                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Arguments = "/VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"
                });
                _installerStarted = true;
                _lastError = null;
            }
            catch (Exception ex)
            {
                _downloadFailed = true;
                _lastError = ex.Message;
                _installerStarted = false;
            }
        }

        // ---------- Atualização sem reinstalar ----------
        // Também em thread de 1º plano: o jogo pode fechar e o download continua.
        public static bool StartSoftUpdate()
        {
            var info = Available;
            if (info == null) return false;
            if (!CanSoftUpdate) { _lastError = "A pasta do jogo exige administrador. Use 'Instalar now'."; return false; }
            var files = info.Files ?? Array.Empty<string>();
            if (files.Length == 0) { _lastError = "Manifest missing file list"; return false; }
            if (_download != null && !_download.IsCompleted) return false; // não atropela o instalador
            if (_softThread != null && _softThread.IsAlive) return false;  // já em andamento
            string baseUrl = ResolveFilesBase(info);
            if (string.IsNullOrEmpty(baseUrl)) { _lastError = "URL base dos arquivos indefinida"; return false; }

            SoftUpdateReady = false;
            DownloadStarted = true;
            _downloadFailed = false;
            _lastError = null;
            DownloadProgress = 0f;

            var t = new Thread(() =>
            {
                try { RunSoftUpdateCore(info, baseUrl).GetAwaiter().GetResult(); }
                catch (Exception ex)
                {
                    _downloadFailed = true;
                    _lastError = ex.Message;
                    SoftUpdateReady = false;
                }
            });
            t.IsBackground = false;
            _softThread = t;
            t.Start();
            return true;
        }

        private static async Task RunSoftUpdateCore(UpdateInfo info, string baseUrl)
        {
            var files = info.Files ?? Array.Empty<string>();
            string tmpDir = Path.Combine(Path.GetTempPath(), "deadzone-update", "files");
            string marker = Path.Combine(Path.GetTempPath(), "deadzone-update", "pending");
            var ok = new List<string>();
            var errors = new List<string>();
            try
            {
                Directory.CreateDirectory(tmpDir);
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                for (int i = 0; i < files.Length; i++)
                {
                    string rel = files[i].Trim().TrimStart('/');
                    if (rel.Length == 0) continue;
                    try
                    {
                        string url = baseUrl + "/" + rel; // URL correta: diretório do manifesto + arquivo
                        string dest = Path.Combine(tmpDir, Path.GetFileName(rel));
                        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();
                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var file = File.Create(dest);
                        await stream.CopyToAsync(file);
                        ok.Add(Path.GetFileName(rel));
                    }
                    catch (Exception ex) { errors.Add(rel + ": " + ex.Message); }
                    DownloadProgress = Math.Min(1f, (float)(i + 1) / files.Length);
                }

                if (ok.Count > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
                    // Marcador em linhas (um arquivo por linha) para o apply.cmd saber reaproveitar.
                    File.WriteAllText(marker, string.Join(Environment.NewLine, ok) + Environment.NewLine);
                    SoftUpdateReady = true;
                    DownloadProgress = 1f;
                    _lastError = errors.Count > 0 ? string.Join(" | ", errors) : null;
                }
                else
                {
                    _downloadFailed = true;
                    _lastError = errors.Count > 0 ? string.Join(" | ", errors) : "Nenhum arquivo baixado";
                }
            }
            catch (Exception ex)
            {
                _downloadFailed = true;
                _lastError = ex.Message;
                SoftUpdateReady = false;
            }
        }

        // ---------- Aplicação no início ----------
        // Aplica os arquivos baixados por uma atualização sem reinstalar.
        public static void ApplyPending()
        {
            string root = Path.Combine(Path.GetTempPath(), "deadzone-update");
            string marker = Path.Combine(root, "pending");
            try
            {
                if (!File.Exists(marker)) return;
                var names = new List<string>();
                foreach (var line in File.ReadAllLines(marker))
                {
                    var n = line.Trim();
                    if (n.Length > 0 && !names.Contains(n)) names.Add(n);
                }
                if (names.Count == 0) { CleanupSoft(root); return; }

                string appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
                string srcDir = Path.Combine(root, "files");
                var viaScript = new List<string>();  // arquivos travados: troca ao sair
                var stillPending = new List<string>(); // falha real: tenta de novo no próximo início

                foreach (var name in names)
                {
                    string dst = Path.Combine(appDir, name);
                    string newFile = dst + ".new";
                    string src = Path.Combine(srcDir, name);
                    if (!File.Exists(src)) src = File.Exists(newFile) ? newFile : "";
                    if (src.Length == 0) continue; // nunca baixado: descarta

                    if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(newFile), StringComparison.OrdinalIgnoreCase))
                    {
                        if (!viaScript.Contains(name)) viaScript.Add(name); // já está em .new; só falta trocar
                        continue;
                    }

                    try
                    {
                        File.Copy(src, dst, true); // substitui direto quando o arquivo está livre
                        try { if (File.Exists(newFile)) File.Delete(newFile); } catch { }
                    }
                    catch
                    {
                        try
                        {
                            File.Copy(src, newFile, true); // em uso: prepara a troca
                            if (!viaScript.Contains(name)) viaScript.Add(name);
                        }
                        catch
                        {
                            if (!stillPending.Contains(name)) stillPending.Add(name);
                        }
                    }
                }

                if (viaScript.Count > 0)
                {
                    bool launched = false;
                    try
                    {
                        string script = WriteSwapScript(appDir, viaScript);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c " + Q + script + Q,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        launched = true;
                    }
                    catch { }
                    if (!launched) stillPending.AddRange(viaScript);
                }

                if (stillPending.Count > 0)
                    File.WriteAllText(marker, string.Join(Environment.NewLine, stillPending) + Environment.NewLine);
                else
                    CleanupSoft(root);
            }
            catch { }
        }

        // Gera um apply.cmd: espera o processo atual sair, troca os arquivos travados
        // (até 7 tentativas) e reabre o jogo já atualizado.
        private static string WriteSwapScript(string appDir, List<string> names)
        {
            string root = Path.Combine(Path.GetTempPath(), "deadzone-update");
            Directory.CreateDirectory(root);
            string script = Path.Combine(root, "apply-" + Process.GetCurrentProcess().Id + ".cmd");
            string marker = Path.Combine(root, "pending");
            string exe = Application.ExecutablePath;
            int pid = Process.GetCurrentProcess().Id;

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("rem Deadzone: troca de arquivos que estavam em uso");
            sb.AppendLine(":waitpid");
            sb.AppendLine("tasklist /FI " + Q + "PID eq " + pid + Q + " 2>nul | find " + Q + pid + Q + " >nul");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("  ping -n 3 127.0.0.1 >nul");
            sb.AppendLine("  goto waitpid");
            sb.AppendLine(")");
            foreach (var name in names)
            {
                string newFile = Path.Combine(appDir, name + ".new");
                string dst = Path.Combine(appDir, name);
                sb.AppendLine("move /y " + Q + newFile + Q + " " + Q + dst + Q + " >nul 2>&1");
                sb.AppendLine("if errorlevel 1 call :swfile " + Q + newFile + Q + " " + Q + dst + Q + " " + Q + name + Q);
            }
            sb.AppendLine("start " + Q + Q + " " + Q + exe + Q);
            sb.AppendLine("del " + Q + "%~f0" + Q);
            sb.AppendLine("exit /b 0");
            sb.AppendLine(":swfile");
            sb.AppendLine("set SWTRIES=0");
            sb.AppendLine(":swtry");
            sb.AppendLine("move /y %1 %2 >nul 2>&1");
            sb.AppendLine("if errorlevel 1 (");
            sb.AppendLine("  set /a SWTRIES=SWTRIES+1");
            sb.AppendLine("  if %SWTRIES% GEQ 7 goto swgive");
            sb.AppendLine("  ping -n 3 127.0.0.1 >nul");
            sb.AppendLine("  goto swtry");
            sb.AppendLine(")");
            sb.AppendLine("goto :eof");
            sb.AppendLine(":swgive");
            sb.AppendLine("echo %~3 >> " + Q + marker + Q);
            sb.AppendLine("goto :eof");

            File.WriteAllText(script, sb.ToString());
            return script;
        }

        private static void CleanupSoft(string root)
        {
            try { File.Delete(Path.Combine(root, "pending")); } catch { }
            try { Directory.Delete(Path.Combine(root, "files"), true); } catch { }
        }

        // ---------- Ignorar versão por 1 hora ----------
        private static string IgnoreFile => Path.Combine(Path.GetTempPath(), "deadzone-update", "ignore.txt");

        public static bool IsIgnored(string version)
        {
            LoadIgnore();
            if (string.IsNullOrEmpty(IgnoreVersion) || version != IgnoreVersion) return false;
            if (DateTime.UtcNow >= IgnoreUntilUtc) { ClearIgnore(); return false; }
            return true;
        }

        public static void IgnoreCurrent()
        {
            IgnoreVersion = Available?.Version ?? "";
            IgnoreUntilUtc = DateTime.UtcNow.AddHours(1);
            try
            {
                var dir = Path.GetDirectoryName(IgnoreFile)!;
                Directory.CreateDirectory(dir);
                File.WriteAllText(IgnoreFile, IgnoreVersion + "|" + IgnoreUntilUtc.Ticks);
            }
            catch { }
        }

        private static void LoadIgnore()
        {
            if (IgnoreUntilUtc != default) return;
            try
            {
                if (!File.Exists(IgnoreFile)) return;
                var parts = File.ReadAllText(IgnoreFile).Split('|');
                if (parts.Length == 2 && long.TryParse(parts[1], out long ticks))
                {
                    IgnoreVersion = parts[0];
                    IgnoreUntilUtc = new DateTime(ticks, DateTimeKind.Utc);
                }
            }
            catch { }
        }

        private static void ClearIgnore()
        {
            IgnoreVersion = "";
            IgnoreUntilUtc = default;
            try { File.Delete(IgnoreFile); } catch { }
        }

        // A atualização "sem reinstalar" grava na pasta do jogo; em Program Files
        // isso exige administrador, então o instalador é o caminho recomendado.
        public static bool CanSoftUpdate
        {
            get
            {
                try
                {
                    string dir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                    if (string.IsNullOrEmpty(dir)) return false;
                    string probe = Path.Combine(dir, ".dz-write-" + Guid.NewGuid().ToString("N") + ".tmp");
                    File.WriteAllText(probe, "x");
                    File.Delete(probe);
                    return true;
                }
                catch { return false; }
            }
        }

        // ---------- Comparação de versões ----------
        public static int CompareVersions(string a, string b)
        {
            var pa = Parts(a);
            var pb = Parts(b);
            int n = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < n; i++)
            {
                int da = i < pa.Length ? pa[i] : 0;
                int db = i < pb.Length ? pb[i] : 0;
                if (da != db) return da.CompareTo(db);
            }
            return 0;
        }

        // "v2.4", "2.4-beta", "2.10" viram números de forma estável.
        private static int[] Parts(string s)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(s)) { result.Add(0); return result.ToArray(); }
            foreach (var raw in s.Trim().Split('.'))
            {
                string p = raw.Trim();
                if (p.Length > 0 && (p[0] == 'v' || p[0] == 'V')) p = p.Substring(1);
                int digits = 0;
                while (digits < p.Length && char.IsDigit(p[digits])) digits++;
                if (digits > 9) digits = 9;
                result.Add(digits > 0 && int.TryParse(p.Substring(0, digits), out var val) ? val : 0);
            }
            return result.ToArray();
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
