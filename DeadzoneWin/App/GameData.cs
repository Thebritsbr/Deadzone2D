// GameData.cs — Dados de gameplay: Mapas (Maps.json) + regras (Gameplay.json)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CrossfireWin
{
    public sealed class GameMap
    {
        public string Id = "";
        public string Name = "";
        public string Mode = "";
        public string Type = "";
        public float SizeX = 2048f, SizeY = 1280f;
        public int MaxWins = 3, TeamSize = 2, MinPlayers = 29, MaxPlayers = 30;
        public int CountdownSeconds = 180;
        public bool SpawnItems, SpawnKeys;
        public List<GameVehicle> SpawnVehicles = new();
    }

    public sealed class GameVehicle
    {
        public string Type = "car";
        public int Count = 0;
        public bool Usable = false;
    }

    public sealed class ModeRules
    {
        public int MaxWins = 3;
        public string WinText = "You Win!";
        public string LossText = "Defeat";
        public string WinBackground = "#F2C846";
        public string LossBackground = "#555555";
        public string CounterFormat = "{0} X {1}";
    }

    public sealed class GameplayConfig
    {
        public ModeRules DealDuo = new();
        public ModeRules SquadBattle = new();
        public int DeadRoyaleMinPlayers = 29;
        public int DeadRoyaleMaxPlayers = 30;
        public int DeadRoyaleCountdown = 180;

        public ModeRules For(string mode) =>
            mode == "DealDuo" ? DealDuo : mode == "SquadBattle" ? SquadBattle : DealDuo;

        public static GameplayConfig Load(string dir)
        {
            var cfg = new GameplayConfig();
            try
            {
                var p = Path.Combine(dir, "Config", "Gameplay.json");
                if (!File.Exists(p)) return cfg;
                using var doc = JsonDocument.Parse(File.ReadAllText(p));
                var root = doc.RootElement;
                if (root.TryGetProperty("DealDuo", out var dd)) ReadMode(dd, cfg.DealDuo);
                if (root.TryGetProperty("SquadBattle", out var sb)) ReadMode(sb, cfg.SquadBattle);
                if (root.TryGetProperty("DeadRoyale", out var dr))
                {
                    cfg.DeadRoyaleMinPlayers = GetInt(dr, "MinPlayers", cfg.DeadRoyaleMinPlayers);
                    cfg.DeadRoyaleMaxPlayers = GetInt(dr, "MaxPlayers", cfg.DeadRoyaleMaxPlayers);
                    cfg.DeadRoyaleCountdown = GetInt(dr, "CountdownSeconds", cfg.DeadRoyaleCountdown);
                }
            }
            catch { }
            return cfg;
        }

        private static void ReadMode(JsonElement e, ModeRules r)
        {
            r.MaxWins = GetInt(e, "MaxWins", r.MaxWins);
            r.WinText = GetStr(e, "WinText", r.WinText);
            r.LossText = GetStr(e, "LossText", r.LossText);
            r.WinBackground = GetStr(e, "WinBackground", r.WinBackground);
            r.LossBackground = GetStr(e, "LossBackground", r.LossBackground);
            r.CounterFormat = GetStr(e, "CounterFormat", r.CounterFormat);
        }

        private static int GetInt(JsonElement e, string name, int fallback) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : fallback;

        private static string GetStr(JsonElement e, string name, string fallback) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? fallback : fallback;
    }

    public sealed class GameData
    {
        public List<GameMap> Maps = new();
        public GameplayConfig Rules = new();

        public static GameData Load(string dir)
        {
            var g = new GameData();
            try
            {
                var p = Path.Combine(dir, "Config", "Maps.json");
                if (File.Exists(p))
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(p));
                    foreach (var m in doc.RootElement.GetProperty("Maps").EnumerateArray())
                    {
                        var gm = new GameMap
                        {
                            Id = m.TryGetProperty("Id", out var id) ? id.GetString() ?? "" : "",
                            Name = m.TryGetProperty("Name", out var nm) ? nm.GetString() ?? "" : "",
                            Mode = m.TryGetProperty("Mode", out var md) ? md.GetString() ?? "" : "",
                            Type = m.TryGetProperty("Type", out var ty) ? ty.GetString() ?? "" : "",
                            SizeX = m.TryGetProperty("SizeX", out var sx) && sx.ValueKind == JsonValueKind.Number ? sx.GetInt32() : 2048,
                            SizeY = m.TryGetProperty("SizeY", out var sy) && sy.ValueKind == JsonValueKind.Number ? sy.GetInt32() : 1280,
                            MaxWins = m.TryGetProperty("MaxWins", out var mw) && mw.ValueKind == JsonValueKind.Number ? mw.GetInt32() : 3,
                            TeamSize = m.TryGetProperty("TeamSize", out var tm) && tm.ValueKind == JsonValueKind.Number ? tm.GetInt32() : 2,
                            MinPlayers = m.TryGetProperty("MinPlayers", out var mn) && mn.ValueKind == JsonValueKind.Number ? mn.GetInt32() : 29,
                            MaxPlayers = m.TryGetProperty("MaxPlayers", out var mx) && mx.ValueKind == JsonValueKind.Number ? mx.GetInt32() : 30,
                            CountdownSeconds = m.TryGetProperty("CountdownSeconds", out var cs) && cs.ValueKind == JsonValueKind.Number ? cs.GetInt32() : 180,
                            SpawnItems = m.TryGetProperty("SpawnItems", out var si) && si.ValueKind == JsonValueKind.True,
                            SpawnKeys = m.TryGetProperty("SpawnKeys", out var sk) && sk.ValueKind == JsonValueKind.True,
                        };
                        if (m.TryGetProperty("SpawnVehicles", out var sv))
                            foreach (var v in sv.EnumerateArray())
                                gm.SpawnVehicles.Add(new GameVehicle
                                {
                                    Type = v.GetProperty("Type").GetString() ?? "car",
                                    Count = v.GetProperty("Count").GetInt32(),
                                    Usable = v.GetProperty("Usable").GetBoolean(),
                                });
                        g.Maps.Add(gm);
                    }
                }
                g.Rules = GameplayConfig.Load(dir);
            }
            catch { }
            return g;
        }
    }
}
