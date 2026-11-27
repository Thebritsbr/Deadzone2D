// RankStore.cs — ranks e progressão de nível carregados de Config/Ranks.json.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Deadzone2D
{
    public sealed class RankDef
    {
        public int Level { get; set; }
        public string Name { get; set; } = "Ferro";
        public int Xp { get; set; }
        public string Color { get; set; } = "#8B979F";
        public string Description { get; set; } = "";
    }

    public static class RankStore
    {
        public static List<RankDef> Ranks { get; private set; } = new()
        {
            new RankDef { Level = 1, Name = "Ferro", Xp = 0, Color = "#8B979F" }
        };

        public static void Load(string dir)
        {
            try
            {
                var p = Path.Combine(dir, "Config", "Ranks.json");
                if (!File.Exists(p)) return;
                var list = JsonSerializer.Deserialize<List<RankDef>>(File.ReadAllText(p),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null && list.Count > 0) Ranks = list.OrderBy(r => r.Xp).ToList();
            }
            catch { }
        }

        public static RankDef ForXp(int xp)
        {
            var r = Ranks.LastOrDefault(x => xp >= x.Xp);
            return r ?? Ranks[0];
        }

        public static RankDef? Next(int xp) => Ranks.FirstOrDefault(x => x.Xp > xp);

        public static float Progress(int xp)
        {
            var cur = ForXp(xp);
            var next = Next(xp);
            if (next == null) return 1f;
            int span = Math.Max(1, next.Xp - cur.Xp);
            return Math.Clamp((xp - cur.Xp) / (float)span, 0f, 1f);
        }

        public static Color Parse(string hex)
        {
            try { return ColorTranslator.FromHtml(string.IsNullOrWhiteSpace(hex) ? "#8B979F" : hex); }
            catch { return Color.FromArgb(139, 151, 159); }
        }
    }
}
