// AppInfo.cs — Informações do aplicativo: nome, empresa fundadora, versão, tema.
using System;
using System.IO;
using System.Text.Json;

namespace Deadzone2D
{
    public static class AppInfo
    {
        public static string Nome = "Deadzone";
        public static string EmpresaFundadora = "Lume Studio";
        public static string Versao = "3.7";
        public static string Tema = "DeadTheme";

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







