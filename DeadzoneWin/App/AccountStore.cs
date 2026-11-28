// AccountStore.cs — contas locais, perfis, loja Dead Royal, announcements, notificações e admin.
// Tudo é salvo em %LOCALAPPDATA%\Deadzone2D para sobreviver at atualizações do jogo.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Deadzone2D
{
    // ---------- Modelos ----------
    public sealed class Account
    {
        public string UserId { get; set; } = "";
        public string Gmail { get; set; } = "";
        public string Nick { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string PasswordSalt { get; set; } = "";
        public string IconId { get; set; } = "icon_01";
        public string BannerId { get; set; } = "banner_01";
        public string Description { get; set; } = "";
        public string Region { get; set; } = "Brazil";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int DeadCoins { get; set; } = 1500;
        public int DisplayNameChanges { get; set; }
        public List<string> OwnedIcons { get; set; } = new();
        public List<string> OwnedBanners { get; set; } = new();
        public List<string> OwnedShopItems { get; set; } = new();
        public List<string> Friends { get; set; } = new();
        public List<string> FriendRequests { get; set; } = new();
        public List<string> SentFriendRequests { get; set; } = new();
        public List<string> GroupInvites { get; set; } = new();
        public List<string> RemoteContacts { get; set; } = new();
        public List<string> ReadNotifications { get; set; } = new();
        public Dictionary<string, string> EquippedSkins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        // migração única: contas antigas liberavam todos os icons/banners para teste
        public bool OwnershipV2 { get; set; }
        public bool EmailVerified { get; set; }
        public string GoogleId { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public int Level { get; set; } = 1;
        public int Wins { get; set; }
        public int Kills { get; set; }
        public int Xp { get; set; }
        public bool TwoFactor { get; set; }
    }

    public sealed class Announcement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Category { get; set; } = "Updates";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string Author { get; set; } = "Lume Studio INC";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Attachment { get; set; }
        public string? AttachmentName { get; set; }
    }

    public sealed class DzNotification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Target { get; set; } = "all";
        public string Message { get; set; } = "";
        public string Author { get; set; } = "sr.idev";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public sealed class ShopItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "WeaponSkin"; // WeaponSkin | Icon | Banner
        public string WeaponId { get; set; } = "";
        public string SkinId { get; set; } = "";
        public string IconId { get; set; } = "";
        public string BannerId { get; set; } = "";
        public int Price { get; set; }
        public string Rarity { get; set; } = "Rare";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    public sealed class AdminConfig
    {
        public string User { get; set; } = "sr.idev";
        public string Password { get; set; } = "Sdev@1234";
        public List<string> Allowed { get; set; } = new() { "sr.idev", "sr.idev@gmail.com" };
    }

    public sealed record ProfileIcon(string Id, string Name, Color Color, string Glyph);
    public sealed record ProfileBanner(string Id, string Name, Color A, Color B, string Style);

    // ---------- Persistência ----------
    public static class AccountStore
    {
        private static readonly object Gate = new();
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string DataDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deadzone2D");
        public static string MediaDir => Path.Combine(DataDir, "media");
        private static string AccountsFile => Path.Combine(DataDir, "accounts.json");
        private static string AnnouncementsFile => Path.Combine(DataDir, "announcements.json");
        private static string NotificationsFile => Path.Combine(DataDir, "notifications.json");
        private static string AdminFile => Path.Combine(DataDir, "admin.json");
        private static string ShopFile => Path.Combine(DataDir, "DeadRoyalShop.json");

        public static List<Account> Accounts { get; private set; } = new();
        public static List<Announcement> Announcements { get; private set; } = new();
        public static List<DzNotification> Notifications { get; private set; } = new();
        public static List<ShopItem> Shop { get; private set; } = new();
        public static AdminConfig Admin { get; private set; } = new();
        public static bool Loaded { get; private set; }

        public static readonly string[] Regions = { "Brazil", "USA", "Spain", "Portugal", "Mexico", "Argentina" };

        public static readonly ProfileIcon[] Icons =
        {
            new("icon_01", "Sentinela", Color.FromArgb(255, 70, 85), "S"),
            new("icon_02", "Phoenix", Color.FromArgb(255, 140, 66), "F"),
            new("icon_03", "Espectro", Color.FromArgb(170, 120, 255), "E"),
            new("icon_04", "Calavera", Color.FromArgb(76, 201, 240), "C"),
            new("icon_05", "Lobo", Color.FromArgb(61, 220, 151), "L"),
            new("icon_06", "Corvo", Color.FromArgb(245, 197, 66), "V"),
        };

        public static readonly ProfileBanner[] Banners =
        {
            new("banner_01", "Deadzone", Color.FromArgb(255, 70, 85), Color.FromArgb(15, 25, 35), "diag"),
            new("banner_02", "Lume Studio", Color.FromArgb(245, 197, 66), Color.FromArgb(24, 35, 47), "grid"),
            new("banner_03", "Dead Royal", Color.FromArgb(170, 120, 255), Color.FromArgb(9, 15, 22), "crown"),
            new("banner_04", "Neon Fury", Color.FromArgb(76, 201, 240), Color.FromArgb(255, 45, 190), "diag"),
            new("banner_05", "Tactical Gray", Color.FromArgb(139, 151, 159), Color.FromArgb(24, 35, 47), "grid"),
            new("banner_06", "Carmesim", Color.FromArgb(255, 88, 96), Color.FromArgb(30, 8, 14), "crown"),
        };

        public static ProfileIcon Icon(string? id) =>
            Icons.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Icons[0];

        public static ProfileBanner Banner(string? id) =>
            Banners.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Banners[0];

        // ---------- Carregamento ----------
        public static void Load()
        {
            lock (Gate)
            {
                if (Loaded) return;
                Directory.CreateDirectory(DataDir);
                Directory.CreateDirectory(MediaDir);

                Accounts = ReadJson<List<Account>>(AccountsFile) ?? new List<Account>();
                Announcements = ReadJson<List<Announcement>>(AnnouncementsFile) ?? new List<Announcement>();
                Notifications = ReadJson<List<DzNotification>>(NotificationsFile) ?? new List<DzNotification>();
                Admin = ReadJson<AdminConfig>(AdminFile) ?? new AdminConfig();
                Shop = LoadShopInternal();
                SaveJson(ShopFile, Shop); // grava itens novos vindos do Config da versão

                bool dirty = false;
                foreach (var a in Accounts)
                    if (Normalize(a)) dirty = true;
                Admin.Allowed ??= new List<string>();
                if (Admin.Allowed.Count == 0)
                {
                    Admin.Allowed.Add("sr.idev");
                    Admin.Allowed.Add("sr.idev@gmail.com");
                }

                if (Accounts.Count == 0)
                {
                    // Conta de desenvolvedor para o painel admin (pode ser usada para testar tudo).
                    var dev = CreateAccountInternal("sr.idev@gmail.com", "sr.idev", "sr.idev", "Sdev@1234",
                        "icon_06", "Development account for Lume Studio INC.", "Brazil", out _);
                    if (dev != null)
                    {
                        dev.DeadCoins = 5000;
                        dev.DisplayNameChanges = 0;
                        Accounts.Add(dev);
                        dirty = true;
                    }
                }
                if (Announcements.Count == 0)
                {
                    Announcements.Add(new Announcement
                    {
                        Category = "Updates",
                        Title = "Welcome to Deadzone 4.6.4",
                        Body = "New lobby, real Google sign-in, player profile, Cofre, announcements and the Dead Royal store. " +
                               "Use CTRL + SHIFT + P on an admin account to open the admin panel.",
                        Author = "Lume Studio INC",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                    Announcements.Add(new Announcement
                    {
                        Category = "News",
                        Title = "Dead Royal store is open",
                        Body = "Starter items: Crimson Fang (Glock), Neon Strike (MP40), Golden Reaper (AWM), " +
                               "the Calavera icon and the Dead Royal banner. Earn and spend Dead Coins in the lobby.",
                        Author = "Lume Studio INC",
                        CreatedAt = DateTime.UtcNow.AddHours(-6)
                    });
                    dirty = true;
                }
                if (!Announcements.Any(a => a.Title == "Deadzone2D 4.6.4 Release"))
                {
                    Announcements.Add(new Announcement
                    {
                        Category = "Updates",
                        Title = "Deadzone2D 4.6.4 Release",
                        Body = "Friend requests are now sent between accounts and must be accepted. " +
                               "Group invites also send a request. A rank/XP system and a new requirements page are available.",
                        Author = "Lume Studio INC",
                        CreatedAt = DateTime.UtcNow
                    });
                    dirty = true;
                }
                if (Notifications.Count == 0)
                {
                    Notifications.Add(new DzNotification
                    {
                        Target = "all",
                        Message = "Welcome to Deadzone! Check the announcements and the Dead Royal store.",
                        Author = "Lume Studio INC",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    });
                    dirty = true;
                }
                if (Shop.Count == 0)
                {
                    Shop = DefaultShop();
                    dirty = true;
                }

                if (dirty) SaveAll();
                Loaded = true;
            }
        }

        public static void SaveAll()
        {
            lock (Gate)
            {
                SaveJson(AccountsFile, Accounts);
                SaveJson(AnnouncementsFile, Announcements);
                SaveJson(NotificationsFile, Notifications);
                SaveJson(AdminFile, Admin);
                SaveJson(ShopFile, Shop);
            }
        }

        public static void SaveAccounts()
        {
            lock (Gate) SaveJson(AccountsFile, Accounts);
        }

        public static void SaveAnnouncements()
        {
            lock (Gate) SaveJson(AnnouncementsFile, Announcements);
        }

        public static void SaveNotifications()
        {
            lock (Gate) SaveJson(NotificationsFile, Notifications);
        }

        private static T? ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOpts);
            }
            catch
            {
                try
                {
                    if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                }
                catch { }
                return null;
            }
        }

        private static void SaveJson<T>(string path, T value)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(value, JsonOpts), new UTF8Encoding(false));
                File.Move(tmp, path, true);
            }
            catch { }
        }

        private static bool Normalize(Account a)
        {
            bool changed = false;
            a.OwnedIcons ??= new List<string>();
            a.OwnedBanners ??= new List<string>();
            a.OwnedShopItems ??= new List<string>();
            a.Friends ??= new List<string>();
            a.FriendRequests ??= new List<string>();
            a.SentFriendRequests ??= new List<string>();
            a.GroupInvites ??= new List<string>();
            a.RemoteContacts ??= new List<string>();
            a.ReadNotifications ??= new List<string>();
            a.Tags ??= new List<string>();
            if (a.EquippedSkins == null || a.EquippedSkins.Comparer != StringComparer.OrdinalIgnoreCase)
                a.EquippedSkins = new Dictionary<string, string>(a.EquippedSkins ?? new(), StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(a.IconId)) { a.IconId = "icon_01"; changed = true; }
            if (string.IsNullOrWhiteSpace(a.BannerId)) { a.BannerId = "banner_01"; changed = true; }
            if (string.IsNullOrWhiteSpace(a.Region) || !Regions.Contains(a.Region)) { a.Region = "Brazil"; changed = true; }
            if (a.CreatedAt.Kind == DateTimeKind.Unspecified) a.CreatedAt = DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc);
            if (a.CreatedAt > DateTime.UtcNow) a.CreatedAt = DateTime.UtcNow;

            // Migração: versões de teste liberavam todos os icons/banners. Agora a conta
            // só guarda o que escolheu/comprou; itens já comprados na loja são preservados.
            if (!a.OwnershipV2)
            {
                var icons = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { a.IconId };
                var banners = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { a.BannerId };
                foreach (var item in Shop)
                {
                    if (!a.OwnedShopItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase)) continue;
                    if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.IconId))
                        icons.Add(Icon(item.IconId).Id);
                    if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.BannerId))
                        banners.Add(Banner(item.BannerId).Id);
                }
                a.OwnedIcons = icons.ToList();
                a.OwnedBanners = banners.ToList();
                a.OwnershipV2 = true;
                changed = true;
            }
            if (a.OwnedIcons.Count == 0) { a.OwnedIcons.Add(a.IconId); changed = true; }
            if (a.OwnedBanners.Count == 0) { a.OwnedBanners.Add(a.BannerId); changed = true; }
            return changed;
        }

        // ---------- Contas ----------
        public static bool IsGmail(string email) =>
            !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email.Trim(), @"^[A-Za-z0-9._%+\-]+@gmail\.com$", RegexOptions.IgnoreCase);

        public static bool IsNickValid(string nick) =>
            !string.IsNullOrWhiteSpace(nick) && Regex.IsMatch(nick.Trim(), @"^[A-Za-z0-9_.\-]{3,24}$");

        public static Account? FindByGmail(string gmail) =>
            Accounts.FirstOrDefault(a => string.Equals(a.Gmail, gmail.Trim(), StringComparison.OrdinalIgnoreCase));

        public static Account? FindByNick(string nick) =>
            Accounts.FirstOrDefault(a => string.Equals(a.Nick, nick.Trim(), StringComparison.OrdinalIgnoreCase));

        public static Account? FindByUserId(string id) =>
            Accounts.FirstOrDefault(a => string.Equals(a.UserId, id.Trim(), StringComparison.OrdinalIgnoreCase));

        public static Account? Login(string gmail, string password)
        {
            var acc = FindByGmail(gmail) ?? FindByNick(gmail);
            if (acc == null) return null;
            return VerifyPassword(password, acc.PasswordHash, acc.PasswordSalt) ? acc : null;
        }

        // Login/cadastro com a conta Google real do usuário.
        public static Account? LoginOrCreateWithGoogle(GoogleProfile p, out string error)
        {
            error = "";
            string gmail = (p.Email ?? "").Trim();
            if (gmail.Length == 0) { error = "Invalid Google email."; return null; }

            lock (Gate)
            {
                var existing = FindByGmail(gmail);
                if (existing != null)
                {
                    if (string.IsNullOrWhiteSpace(existing.GoogleId)) existing.GoogleId = p.Id;
                    existing.EmailVerified = true;
                    SaveAccounts();
                    return existing;
                }

                string nick = Sanitize(gmail.Split('@')[0], 24, 3);
                if (nick.Length < 3) nick = "USER";
                string baseNick = nick;
                int suffix = 1;
                while (FindByNick(nick) != null) nick = baseNick + (suffix++);
                var acc = CreateAccountInternal(gmail, nick, Sanitize(p.Name, 20, 3), "GoogleAuth!",
                    "icon_01", "Account connected with Google.", "Brazil", out error);
                if (acc == null) return null;
                acc.GoogleId = p.Id;
                acc.EmailVerified = true;
                Accounts.Add(acc);
                SaveAccounts();
                return acc;
            }
        }

        private static string Sanitize(string value, int max, int min)
        {
            var sb = new StringBuilder();
            foreach (char c in (value ?? ""))
                if (char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-') sb.Append(c);
            var s = sb.ToString().Trim('.', '-', '_');
            if (s.Length > max) s = s[..max];
            if (s.Length < min) s = s.PadRight(min, 'X');
            return s;
        }

        // ---------- Verificação por código no Gmail ----------
        private sealed class PendingCode
        {
            public string Code = "";
            public DateTime Expires;
            public int Attempts;
        }

        private static readonly Dictionary<string, PendingCode> Pending = new(StringComparer.OrdinalIgnoreCase);

        // Retorna true e, em modo local (sem SMTP), devolve o código para exibição na tela.
        public static bool SendCode(string gmail, out string code, out string error)
        {
            code = "";
            error = "";
            gmail = (gmail ?? "").Trim();
            if (!IsGmail(gmail)) { error = "Enter a valid @gmail.com address."; return false; }
            if (FindByGmail(gmail) != null) { error = "This Gmail is already registered."; return false; }

            string generated = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            lock (Pending)
                Pending[gmail] = new PendingCode
                {
                    Code = generated,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Attempts = 0
                };

            if (MailService.IsConfigured)
            {
                if (!MailService.SendVerificationCode(gmail, generated, out error))
                {
                    lock (Pending) Pending.Remove(gmail);
                    return false;
                }
                return true; // código real foi enviado ao Gmail do usuário
            }
            code = generated; // modo de teste: mostra o código na tela
            return true;
        }

        // Código enviado no login (a conta já existe, por isso não usa SendCode).
        public static bool SendLoginCode(string gmail, out string code, out string error)
        {
            code = "";
            error = "";
            gmail = (gmail ?? "").Trim();
            if (FindByGmail(gmail) == null && FindByNick(gmail) == null)
            { error = "Account not found."; return false; }
            string generated = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            lock (Pending)
                Pending[gmail] = new PendingCode
                {
                    Code = generated,
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Attempts = 0
                };
            if (MailService.IsConfigured)
            {
                if (!MailService.SendVerificationCode(gmail, generated, out error))
                { lock (Pending) Pending.Remove(gmail); return false; }
                return true;
            }
            code = generated;
            return true;
        }

        public static bool VerifyCode(string gmail, string input, out string error)
        {
            error = "";
            gmail = (gmail ?? "").Trim();
            PendingCode? pending;
            lock (Pending) Pending.TryGetValue(gmail, out pending);
            if (pending == null) { error = "Clique em SEND CODE primeiro."; return false; }
            if (DateTime.UtcNow > pending.Expires)
            {
                lock (Pending) Pending.Remove(gmail);
                error = "Code expired. Send another.";
                return false;
            }
            if (pending.Attempts >= 5) { error = "Too many attempts. Send another code."; return false; }
            if ((input ?? "").Trim() != pending.Code)
            {
                pending.Attempts++;
                error = "Incorrect code.";
                return false;
            }
            lock (Pending) Pending.Remove(gmail);
            return true;
        }

        public static Account? CreateAccount(string gmail, string nick, string displayName, string password,
            string iconId, string description, string region, out string error)
        {
            lock (Gate)
            {
                var acc = CreateAccountInternal(gmail, nick, displayName, password, iconId, description, region, out error);
                if (acc == null) return null;
                Accounts.Add(acc);
                SaveAccounts();
                return acc;
            }
        }

        private static Account? CreateAccountInternal(string gmail, string nick, string displayName, string password,
            string iconId, string description, string region, out string error)
        {
            error = "";
            gmail = (gmail ?? "").Trim();
            nick = (nick ?? "").Trim();
            displayName = (displayName ?? "").Trim();
            description = (description ?? "").Trim();
            region = string.IsNullOrWhiteSpace(region) ? "Brazil" : region.Trim();

            if (!IsGmail(gmail)) { error = "Enter a valid @gmail.com address."; return null; }
            if (!IsNickValid(nick)) { error = "Nick must be 3-24 letters, numbers, _, . or -."; return null; }
            if (displayName.Length < 3 || displayName.Length > 20) { error = "O DisplayName deve ter 3–20 caracteres."; return null; }
            if (password == null || password.Length < 6) { error = "Password must be at least 6 characters."; return null; }
            if (FindByGmail(gmail) != null) { error = "This Gmail is already registered."; return null; }
            if (FindByNick(nick) != null) { error = "This Nick is already in use."; return null; }
            if (displayName.Contains('|')) { error = "DisplayName cannot contain |."; return null; }

            var (hash, salt) = HashPassword(password);
            string id;
            do { id = "DZ-" + RandomId(6); } while (FindByUserId(id) != null);

            var acc = new Account
            {
                UserId = id,
                Gmail = gmail,
                Nick = nick,
                DisplayName = displayName,
                PasswordHash = hash,
                PasswordSalt = salt,
                IconId = Icon(iconId).Id,
                BannerId = Banners[0].Id,
                Description = description.Length > 140 ? description[..140] : description,
                Region = Regions.Contains(region) ? region : "Brazil",
                CreatedAt = DateTime.UtcNow,
                DeadCoins = 1500,
                DisplayNameChanges = 0,
                OwnershipV2 = true,
                EmailVerified = true
            };
            // começa apenas com o icon escolhido no cadastro e o banner padrão;
            // os demais são desbloqueados ao comprar na Dead Royal.
            acc.OwnedIcons.Add(acc.IconId);
            acc.OwnedBanners.Add(acc.BannerId);
            return acc;
        }

        public static bool ChangeDisplayName(Account acc, string newName, out string error)
        {
            error = "";
            newName = (newName ?? "").Trim();
            if (newName.Length < 3 || newName.Length > 20) { error = "O DisplayName deve ter 3–20 caracteres."; return false; }
            if (newName.Contains('|')) { error = "DisplayName cannot contain |."; return false; }
            if (string.Equals(newName, acc.DisplayName, StringComparison.Ordinal)) { error = "DisplayName is the same."; return false; }
            if (acc.DisplayNameChanges > 0)
            {
                const int price = 500;
                if (acc.DeadCoins < price) { error = $"A troca custa {price} Dead Coins."; return false; }
                acc.DeadCoins -= price;
            }
            acc.DisplayName = newName;
            acc.DisplayNameChanges++;
            SaveAccounts();
            return true;
        }

        public static bool ChangePasswordWithCode(Account acc, string gmail, string code, string newPassword, out string error)
        {
            error = "";
            if (!VerifyCode(gmail, code, out error)) return false;
            if (newPassword == null || newPassword.Length < 6)
            { error = "Password must be at least 6 characters."; return false; }
            var (hash, salt) = HashPassword(newPassword);
            acc.PasswordHash = hash;
            acc.PasswordSalt = salt;
            SaveAccounts();
            return true;
        }

        public static void SetDescription(Account acc, string text)
        {
            text = (text ?? "").Trim();
            acc.Description = text.Length > 140 ? text[..140] : text;
            SaveAccounts();
        }

        public static string AgeText(DateTime createdUtc)
        {
            var span = DateTime.UtcNow - createdUtc;
            if (span.TotalMinutes < 1) return "now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours} h";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} days";
            if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)} months";
            return $"{(int)(span.TotalDays / 365)} years";
        }

        public static string CreationText(Account acc) =>
            acc.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy 'at' HH:mm", CultureInfo.InvariantCulture);

        // ---------- Amigos ----------
        public static void AddFriend(Account a, Account b)
        {
            if (a.UserId == b.UserId) return;
            if (!a.Friends.Contains(b.UserId, StringComparer.OrdinalIgnoreCase)) a.Friends.Add(b.UserId);
            if (!b.Friends.Contains(a.UserId, StringComparer.OrdinalIgnoreCase)) b.Friends.Add(a.UserId);
            SaveAccounts();
        }

        public static void RemoveFriend(Account a, string userId)
        {
            a.Friends.RemoveAll(x => string.Equals(x, userId, StringComparison.OrdinalIgnoreCase));
            var b = FindByUserId(userId);
            b?.Friends.RemoveAll(x => string.Equals(x, a.UserId, StringComparison.OrdinalIgnoreCase));
            SaveAccounts();
        }

        // Pedidos de amizade: o destinatário precisa aceitar para virar amigo.
        public static bool SendFriendRequest(Account from, Account to, out string error)
        {
            error = "";
            if (from.UserId == to.UserId) { error = "You cannot add yourself."; return false; }
            if (from.Friends.Contains(to.UserId, StringComparer.OrdinalIgnoreCase))
            { error = "You are already friends."; return false; }
            if (to.FriendRequests.Contains(from.UserId, StringComparer.OrdinalIgnoreCase) ||
                from.SentFriendRequests.Contains(to.UserId, StringComparer.OrdinalIgnoreCase))
            { error = "Request already sent."; return false; }
            to.FriendRequests.Add(from.UserId);
            from.SentFriendRequests.Add(to.UserId);
            SaveAccounts();
            return true;
        }

        public static void AcceptFriendRequest(Account acc, string userId)
        {
            acc.FriendRequests.RemoveAll(x => string.Equals(x, userId, StringComparison.OrdinalIgnoreCase));
            var other = FindByUserId(userId);
            other?.SentFriendRequests.RemoveAll(x => string.Equals(x, acc.UserId, StringComparison.OrdinalIgnoreCase));
            if (other != null && !acc.Friends.Contains(other.UserId, StringComparer.OrdinalIgnoreCase))
            {
                acc.Friends.Add(other.UserId);
                if (!other.Friends.Contains(acc.UserId, StringComparer.OrdinalIgnoreCase))
                    other.Friends.Add(acc.UserId);
            }
            SaveAccounts();
        }

        // Contato informado pelo Gmail/UserID mesmo sem a conta existir neste PC.
        public static bool AddRemoteContact(Account acc, string value, out string error)
        {
            error = "";
            value = (value ?? "").Trim();
            if (value.Length < 3) { error = "Informe um UserID ou Gmail válido."; return false; }
            if (acc.Friends.Contains(value, StringComparer.OrdinalIgnoreCase) ||
                acc.RemoteContacts.Contains(value, StringComparer.OrdinalIgnoreCase))
            { error = "Este contato já está na sua lista."; return false; }
            acc.RemoteContacts.Add(value);
            if (!acc.SentFriendRequests.Contains(value, StringComparer.OrdinalIgnoreCase))
                acc.SentFriendRequests.Add(value);
            SaveAccounts();
            return true;
        }

        // Group invites are stored in FriendRequests with a "G:" prefix.
        public static bool SendGroupInvite(Account from, Account to, out string error)
        {
            error = "";
            if (from.UserId == to.UserId) { error = "You cannot invite yourself."; return false; }
            string key = "G:" + from.UserId;
            if (to.FriendRequests.Contains(key, StringComparer.OrdinalIgnoreCase))
            { error = "Group invite already sent."; return false; }
            to.FriendRequests.Add(key);
            if (!to.GroupInvites.Contains(from.UserId, StringComparer.OrdinalIgnoreCase))
                to.GroupInvites.Add(from.UserId);
            SaveAccounts();
            return true;
        }

        public static void QueueRemoteGroupInvite(Account acc, string remoteId)
        {
            string key = "G:" + remoteId;
            if (!acc.SentFriendRequests.Contains(key, StringComparer.OrdinalIgnoreCase))
                acc.SentFriendRequests.Add(key);
            SaveAccounts();
        }

        public static void AcceptGroupInvite(Account acc, string fromUserId)
        {
            acc.FriendRequests.RemoveAll(x => x.Equals("G:" + fromUserId, StringComparison.OrdinalIgnoreCase));
            acc.GroupInvites.RemoveAll(x => x.Equals(fromUserId, StringComparison.OrdinalIgnoreCase));
            var from = FindByUserId(fromUserId);
            from?.SentFriendRequests.RemoveAll(x => x.Equals(acc.UserId, StringComparison.OrdinalIgnoreCase));
            SaveAccounts();
        }

        public static void DeclineFriendRequest(Account acc, string userId)
        {
            acc.FriendRequests.RemoveAll(x => string.Equals(x, userId, StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(x, "G:" + userId, StringComparison.OrdinalIgnoreCase));
            var other = FindByUserId(userId);
            other?.SentFriendRequests.RemoveAll(x => string.Equals(x, acc.UserId, StringComparison.OrdinalIgnoreCase));
            SaveAccounts();
        }

        public static List<Account> SearchAccounts(string query, string excludeUserId)
        {
            query = (query ?? "").Trim();
            if (query.Length < 2) return new List<Account>();
            return Accounts
                .Where(a => !string.Equals(a.UserId, excludeUserId, StringComparison.OrdinalIgnoreCase))
                .Where(a => a.UserId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            a.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            a.Nick.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            a.Gmail.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Nick)
                .Take(20)
                .ToList();
        }

        // ---------- Notificações ----------
        public static List<DzNotification> NotificationsFor(Account acc) =>
            Notifications
                .Where(n => n.Target.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                            n.Target.Equals(acc.Region, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

        public static int UnreadCount(Account acc) =>
            NotificationsFor(acc).Count(n => !acc.ReadNotifications.Contains(n.Id));

        public static void MarkNotificationsRead(Account acc)
        {
            foreach (var n in NotificationsFor(acc))
                if (!acc.ReadNotifications.Contains(n.Id)) acc.ReadNotifications.Add(n.Id);
            SaveAccounts();
        }

        public static DzNotification AddNotification(string target, string message, string author)
        {
            var n = new DzNotification
            {
                Target = NormalizeRegion(target) ?? "all",
                Message = (message ?? "").Trim(),
                Author = string.IsNullOrWhiteSpace(author) ? "sr.idev" : author.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            if (n.Message.Length == 0) n.Message = "(empty notification)";
            Notifications.Add(n);
            SaveNotifications();
            return n;
        }

        public static string? NormalizeRegion(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var v = value.Trim().ToLowerInvariant().Replace(" ", "");
            return v switch
            {
                "all" or "global" or "todos" or "todas" => "all",
                "brazil" or "brasil" or "br" => "Brazil",
                "usa" or "eua" or "unitedstates" or "us" => "USA",
                "spain" or "espanha" or "es" => "Spain",
                "portugal" or "pt" => "Portugal",
                "mexico" or "méxico" or "mx" => "Mexico",
                "argentina" or "ar" => "Argentina",
                _ => Regions.FirstOrDefault(r => r.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase))
            };
        }

        // ---------- Anúncios ----------
        public static string NormalizeCategory(string? value)
        {
            var v = (value ?? "").Trim().ToLowerInvariant();
            return v switch
            {
                "update" or "updates" or "atualizacao" or "atualização" or "atualizacoes" => "Updates",
                "news" or "noticia" or "notícia" or "noticias" => "News",
                "event" or "events" or "evento" or "eventos" => "Events",
                "patch" or "patchnotes" or "patch notes" or "nota" or "notas" => "Patch Notes",
                _ => "Updates"
            };
        }

        public static Announcement AddAnnouncement(string category, string title, string body,
            string author, string? attachmentPath)
        {
            string? saved = null;
            string? original = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(attachmentPath) && File.Exists(attachmentPath))
                {
                    Directory.CreateDirectory(MediaDir);
                    original = Path.GetFileName(attachmentPath);
                    var ext = Path.GetExtension(attachmentPath);
                    saved = Path.Combine(MediaDir, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + ext);
                    File.Copy(attachmentPath, saved, true);
                }
            }
            catch { saved = null; original = null; }

            var ann = new Announcement
            {
                Category = NormalizeCategory(category),
                Title = string.IsNullOrWhiteSpace(title) ? "No title" : title.Trim(),
                Body = (body ?? "").Trim(),
                Author = string.IsNullOrWhiteSpace(author) ? "sr.idev" : author.Trim(),
                CreatedAt = DateTime.UtcNow,
                Attachment = saved,
                AttachmentName = original
            };
            Announcements.Add(ann);
            SaveAnnouncements();
            return ann;
        }

        // ---------- Admin ----------
        public static bool IsAdminAllowed(Account? acc)
        {
            if (acc == null) return false;
            return Admin.Allowed.Any(v =>
                string.Equals(v, acc.Nick, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, acc.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, acc.Gmail, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v, acc.UserId, StringComparison.OrdinalIgnoreCase));
        }

        public static readonly string[] AdminTags = { "Dev", "Owner", "Tester", "Youtuber" };

        public static Account? FindByAny(string value)
        {
            var v = (value ?? "").Trim();
            if (v.Length == 0) return null;
            return FindByGmail(v) ?? FindByUserId(v) ?? FindByNick(v) ??
                   Accounts.FirstOrDefault(a => a.DisplayName.Equals(v, StringComparison.OrdinalIgnoreCase));
        }

        public static bool AddAdminGmail(string gmail, out string error)
        {
            error = "";
            gmail = (gmail ?? "").Trim();
            if (!IsGmail(gmail)) { error = "Enter a valid Gmail."; return false; }
            if (!Admin.Allowed.Any(v => v.Equals(gmail, StringComparison.OrdinalIgnoreCase)))
                Admin.Allowed.Add(gmail);
            SaveJson(AdminFile, Admin);
            return true;
        }

        public static bool GiveItem(Account target, string itemId, out string error)
        {
            error = "";
            var item = Shop.FirstOrDefault(i => i.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase) ||
                                                i.Name.Equals(itemId, StringComparison.OrdinalIgnoreCase));
            if (item == null) { error = "Item not found in the store."; return false; }
            if (!target.OwnedShopItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
                target.OwnedShopItems.Add(item.Id);
            if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.IconId) &&
                !target.OwnedIcons.Contains(item.IconId, StringComparer.OrdinalIgnoreCase))
                target.OwnedIcons.Add(Icon(item.IconId).Id);
            if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.BannerId) &&
                !target.OwnedBanners.Contains(item.BannerId, StringComparer.OrdinalIgnoreCase))
                target.OwnedBanners.Add(Banner(item.BannerId).Id);
            SaveAccounts();
            return true;
        }

        public static void GiveStats(Account target, int coins, int level, int wins, int kills)
        {
            target.DeadCoins = Math.Max(0, target.DeadCoins + coins);
            if (level > 0) target.Level = Math.Max(1, level);
            target.Wins = Math.Max(0, target.Wins + wins);
            target.Kills = Math.Max(0, target.Kills + kills);
            if (wins != 0 || kills != 0) target.Xp = Math.Max(0, target.Xp + wins * 100 + kills * 10);
            SaveAccounts();
        }

        public static bool GiveTag(Account target, string tag, out string error)
        {
            error = "";
            var t = AdminTags.FirstOrDefault(x => x.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (t == null) { error = "Invalid tag. Use Dev, Owner, Tester or Youtuber."; return false; }
            if (!target.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)) target.Tags.Add(t);
            SaveAccounts();
            return true;
        }

        public static bool VerifyAdmin(string user, string password) =>
            string.Equals(Admin.User, (user ?? "").Trim(), StringComparison.OrdinalIgnoreCase) &&
            Admin.Password == (password ?? "");

        // ---------- Loja Dead Royal ----------
        private static List<ShopItem> LoadShopInternal()
        {
            var list = ReadJson<List<ShopItem>>(ShopFile) ?? new List<ShopItem>();
            // itens novos do JSON da versão são mesclados na loja salva no PC
            var cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "DeadRoyalShop.json");
            try
            {
                if (File.Exists(cfg))
                {
                    var seed = JsonSerializer.Deserialize<List<ShopItem>>(File.ReadAllText(cfg), JsonOpts);
                    if (seed != null)
                        foreach (var item in seed)
                            if (!list.Any(i => string.Equals(i.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
                                list.Add(item);
                }
            }
            catch { }
            if (list.Count == 0) list = DefaultShop();
            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.Id)) item.Id = Guid.NewGuid().ToString("N")[..8];
                if (string.IsNullOrWhiteSpace(item.Type)) item.Type = "WeaponSkin";
                if (item.Price < 0) item.Price = 0;
            }
            return list;
        }

        public static List<ShopItem> DefaultShop()
        {
            try
            {
                var cfg = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "DeadRoyalShop.json");
                if (File.Exists(cfg))
                {
                    var list = JsonSerializer.Deserialize<List<ShopItem>>(File.ReadAllText(cfg), JsonOpts);
                    if (list != null && list.Count > 0) return list;
                }
            }
            catch { }
            return new List<ShopItem>
            {
            new ShopItem
            {
                Id = "glock_crimson", Name = "Crimson Fang", Type = "WeaponSkin", WeaponId = "glock",
                SkinId = "crimson_fang", Price = 900, Rarity = "Legendary", Icon = "◆",
                Description = "Legendary skin for the Glock with crimson details and energy lines."
            },
            new ShopItem
            {
                Id = "mp40_neon", Name = "Neon Strike", Type = "WeaponSkin", WeaponId = "mp40",
                SkinId = "neon_strike", Price = 750, Rarity = "Epic", Icon = "▲",
                Description = "Epic skin for the MP40 with neon pulses and a matte finish."
            },
            new ShopItem
            {
                Id = "awm_gold", Name = "Golden Reaper", Type = "WeaponSkin", WeaponId = "sniper_awm",
                SkinId = "golden_reaper", Price = 1400, Rarity = "Legendary", Icon = "✦",
                Description = "Legendary skin for the AWM, forged in gold and shadows."
            },
            new ShopItem
            {
                Id = "icon_calavera", Name = "Calavera", Type = "Icon", IconId = "icon_04",
                Price = 400, Rarity = "Rare", Icon = "☠",
                Description = "Calavera profile icon for the Vault."
            },
            new ShopItem
            {
                Id = "banner_royal", Name = "Dead Royal", Type = "Banner", BannerId = "banner_03",
                Price = 600, Rarity = "Epic", Icon = "♛",
                Description = "Dead Royal banner to highlight your profile in the lobby."
            },
            };
        }

        public static bool Owns(Account acc, ShopItem item) =>
            acc.OwnedShopItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase);

        public static bool Buy(Account acc, ShopItem item, out string error)
        {
            error = "";
            if (Owns(acc, item)) { error = "You already own this item."; return false; }
            if (acc.DeadCoins < item.Price) { error = "Dead Coins insuficientes."; return false; }
            acc.DeadCoins -= item.Price;
            acc.OwnedShopItems.Add(item.Id);
            if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.IconId))
                if (!acc.OwnedIcons.Contains(item.IconId, StringComparer.OrdinalIgnoreCase)) acc.OwnedIcons.Add(item.IconId);
            if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(item.BannerId))
                if (!acc.OwnedBanners.Contains(item.BannerId, StringComparer.OrdinalIgnoreCase)) acc.OwnedBanners.Add(item.BannerId);
            SaveAccounts();
            return true;
        }

        // Alguns itens da loja usam IDs longos (sniper_awm) enquanto o arsenal usa "awm".
        public static string GunId(string? id)
        {
            var v = (id ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            return v switch
            {
                "sniperawm" or "awm" => "awm",
                "glock" or "glock18" => "glock",
                "mp40" => "mp40",
                "ak47" => "ak47",
                "miniuzi" or "uzi" => "miniuzi",
                _ => v
            };
        }

        public static ShopItem? ShopItemForSkin(string gunId, string skinId) =>
            Shop.FirstOrDefault(i => i.Type.Equals("WeaponSkin", StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(GunId(i.WeaponId), GunId(gunId), StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(i.SkinId, skinId, StringComparison.OrdinalIgnoreCase));

        public static void ReloadShop()
        {
            lock (Gate)
            {
                Shop = LoadShopInternal();
                SaveJson(ShopFile, Shop);
            }
        }

        // ---------- Segurança ----------
        private static (string hash, string salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        private static bool VerifyPassword(string password, string hash, string salt)
        {
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                var expected = Convert.FromBase64String(hash);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password ?? "", saltBytes, 120_000, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            catch { return false; }
        }

        private static string RandomId(int len)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var bytes = RandomNumberGenerator.GetBytes(len);
            var sb = new StringBuilder(len);
            foreach (var b in bytes) sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }
    }
}
