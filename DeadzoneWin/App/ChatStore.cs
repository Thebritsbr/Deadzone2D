// ChatStore.cs — chat local do lobby/partida com filtro de linguagem.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Deadzone2D
{
    public sealed class ChatMessage
    {
        public string UserId { get; set; } = "";
        public string Author { get; set; } = "";
        public string Text { get; set; } = "";
        public string Scope { get; set; } = "team"; // team | all
        public DateTime At { get; set; } = DateTime.UtcNow;
    }

    public static class ChatStore
    {
        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deadzone2D", "chat.json");

        public static List<ChatMessage> Messages { get; private set; } = new();

        private static readonly string[] BadWords =
        {
            "fuck", "shit", "bitch", "asshole", "cunt", "nigger", "faggot", "damn",
            "merda", "bosta", "caralho", "foda", "fodido", "viado", "puta", "pilantra",
            "idiota", "estupido", "imbecil", "babaca", "mierda", "basura", "cabron", "pendejo"
        };

        private static readonly Regex Options = new(
            @"\b(" + string.Join("|", BadWords) + @")s?\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var list = JsonSerializer.Deserialize<List<ChatMessage>>(File.ReadAllText(FilePath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (list != null) Messages = list;
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(Messages,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        public static string Censor(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Options.Replace(text, m =>
            {
                string v = m.Value;
                return char.ToUpperInvariant(v[0]) + new string('*', Math.Max(1, v.Length - 1));
            });
        }

        public static void Add(ChatMessage msg)
        {
            msg.Text = Censor(msg.Text);
            Messages.Add(msg);
            while (Messages.Count > 100) Messages.RemoveAt(0);
            Save();
        }

        public static List<ChatMessage> For(string scope, string myUserId, HashSet<string> teamIds)
        {
            var list = new List<ChatMessage>();
            foreach (var m in Messages)
            {
                if (m.Scope == "all" || scope == "all" ||
                    m.UserId == myUserId || teamIds.Contains(m.UserId))
                    list.Add(m);
            }
            return list;
        }
    }
}
