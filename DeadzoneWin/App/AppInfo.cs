// AppInfo.cs â€” InformaÃ§Ãµes do aplicativo: nome, empresa fundadora, versÃ£o, tema.
using System;
using System.IO;
using System.Text.Json;

namespace CrossfireWin
{
    public static class AppInfo
    {
        public static string Nome = "Deadzone";
        public static string EmpresaFundadora = "";
        public static string Versao = "1.9";
        public static string Tema = "Valorant";

        public static void Load(string dir)
        {
            try
            {
                var p = Path.Combine(dir, "Config", "AppInfo.json");
                if (!File.Exists(p)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(p));
                var root = doc.RootElement;
                if (root.TryGetProperty("Nome", out var v)) Nome = v.GetString() ?? Nome;
                if (root.TryGetProperty("EmpresaFundadora", out var e)) EmpresaFundadora = e.GetString() ?? EmpresaFundadora;
                if (root.TryGetProperty("Versao", out var vs)) Versao = vs.GetString() ?? Versao;
                if (root.TryGetProperty("Tema", out var t)) Tema = t.GetString() ?? Tema;
            }
            catch { }
        }
    }
}







