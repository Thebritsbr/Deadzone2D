// MainForm.cs — Deadzone (WinForms, sem engine) — visual DeadTheme
//   Loading → Login → Lobby → Armas → Partida, com lista de amigos no canto inferior direito.
//   Salve todos os .cs como UTF-8 (com BOM) para os acentos aparecerem certos.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Deadzone2D
{
    public partial class MainForm : Form
    {
        private const int BarH = 64;

        // ---------- Paleta (cores da identidade DeadTheme) ----------
        private static readonly Color Bg      = Color.FromArgb(15, 25, 35);    // #0F1923
        private static readonly Color BgDeep  = Color.FromArgb(9, 15, 22);
        private static readonly Color Panel   = Color.FromArgb(24, 35, 47);
        private static readonly Color PanelHi = Color.FromArgb(33, 47, 62);
        private static readonly Color Line    = Color.FromArgb(48, 62, 78);
        private static readonly Color Red     = Color.FromArgb(255, 70, 85);   // #FF4655
        private static readonly Color RedHi   = Color.FromArgb(255, 105, 118);
        private static readonly Color Ink     = Color.FromArgb(236, 232, 225); // #ECE8E1
        private static readonly Color Dim     = Color.FromArgb(139, 151, 159);
        private static readonly Color Green   = Color.FromArgb(61, 220, 151);
        private static readonly Color Blue    = Color.FromArgb(76, 201, 240);
        private static readonly Color Amber   = Color.FromArgb(245, 197, 66);
        private static readonly Color OffC    = Color.FromArgb(91, 103, 112);
        private static readonly Color FieldBg = Color.FromArgb(28, 40, 53);
        private static readonly Color[] Pal =
        {
            Color.FromArgb(255, 70, 85), Color.FromArgb(76, 201, 240), Color.FromArgb(61, 220, 151),
            Color.FromArgb(245, 197, 66), Color.FromArgb(170, 120, 255), Color.FromArgb(255, 140, 66)
        };

        // ---------- Fontes ----------
        private static readonly string Fam = PickFamily("Bahnschrift", "Segoe UI");
        // Títulos em fonte condensada; texto corrido na Bahnschrift (parecida com a DIN).
        // ATENÇÃO: todas as fontes "V*" ficam AQUI, depois de UiFamHead, para a ordem de inicialização estática ficar garantida.
        private static readonly string UiFamHead = PickFamily("Bahnschrift SemiBold Condensed", "Bahnschrift Condensed", "Bahnschrift", "Segoe UI");
        private static readonly FontStyle UiHeadStyle = UiFamHead.Contains("SemiBold") ? FontStyle.Regular : FontStyle.Bold;

        private static readonly Font FHuge   = F(84);
        private static readonly Font FWater  = F(120);
        private static readonly Font FTitle  = F(46);
        private static readonly Font FBtn    = F(19);
        private static readonly Font FNav    = F(15);
        private static readonly Font FName   = F(20);
        private static readonly Font FBody   = F(17, FontStyle.Regular);
        private static readonly Font FMid    = F(16);
        private static readonly Font FSmall  = F(13, FontStyle.Regular);
        private static readonly Font FSmallB = F(13);
        private static readonly Font FLabel  = F(12);
        private static readonly Font FTiny   = F(11);
        private static readonly Font FAvatar = F(18);
        private static readonly Font FLogo   = F(22);
        private static readonly Font FStart  = F(26);

        private static readonly Font VHuge  = FH(104);
        private static readonly Font VWater = FH(170);
        private static readonly Font VTitle = FH(60);
        private static readonly Font VSub   = FH(40);
        private static readonly Font VStart = FH(34);
        private static readonly Font VBtn   = FH(24);
        private static readonly Font VNav   = FH(20);
        private static readonly Font VName  = FH(26);
        private static readonly Font VLogo  = FH(26);
        private static readonly Font VNum   = FH(56);   // números grandes do HUD (vida, munição)
        private static readonly Font VNumS  = FH(34);   // números médios do HUD (placar, colete)

        private static string PickFamily(params string[] names)
        {
            using var c = new InstalledFontCollection();
            var have = c.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return names.FirstOrDefault(n => have.Contains(n)) ?? "Segoe UI";
        }
        private static Font F(float px, FontStyle s = FontStyle.Bold) => new Font(Fam, px, s, GraphicsUnit.Pixel);
        private static Font FH(float px) => new Font(UiFamHead, px, UiHeadStyle, GraphicsUnit.Pixel);

        private static StringFormat MakeSf(StringAlignment a) => new StringFormat(StringFormatFlags.NoWrap)
        { Alignment = a, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat SfL = MakeSf(StringAlignment.Near);
        private static readonly StringFormat SfC = MakeSf(StringAlignment.Center);
        private static readonly StringFormat SfR = MakeSf(StringAlignment.Far);
        private static readonly StringFormat SfWrap = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisWord };

        // ---------- Dados ----------
        private enum FStatus { InGame, Online, Away, Offline }
        private sealed record Friend(string Name, string Sub, FStatus St, string UserId = "");
        private sealed class Hit { public Rectangle R; public string Id = ""; }

        private readonly List<Friend> _friendList = new();
        private readonly List<string> _friendRequests = new();
        private bool _requestsOpen;
        private bool _cfgOpen;
        private int _cfgVol = 60;
        private bool _cfgLangEn;
        private bool _cfgOpt = true;
        private Rectangle _uiVolBar;
        private bool _updInstallRequested;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        private readonly Customize _custom;
        private readonly string _appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
        private readonly GunData[] _guns;
        private string[] _players = { "PLAYER 1", "PLAYER 2", "PLAYER 3", "PLAYER 4" };

        // ---------- Login / conta ----------
        private TextBox _tbUser = new();
        private TextBox _tbPass = new();
        private TextBox _tbLoginCode = new();

        private Account? _account;
        private bool _accountInputsReady;
        private bool _profileOpen, _notifOpen, _adminOpen, _adminAuthed, _addFriendOpen, _squadOpen;
        private bool _notifyUiOpen, _announceUiOpen, _giveOpen, _adminGmailOpen, _rankInfoOpen;
        private int _langSelected;
        private bool _langConfirmOpen;
        private float _langConfirmLeft;
        private int _giveTab;
        private string _giveItemSel = "";
        private readonly List<string> _tagsToGive = new();
        private string _notifyTarget = "all";
        private readonly List<string> _groupMembers = new();

        // tela de carregamento ao entrar/sair de partida e ao entrar no jogo
        private bool _loading;
        private string _loadingTitle = "";
        private float _loadingLeft, _loadingTotal;
        private Action? _loadingDone;
        private bool _allowGuest;
        private string _cofreTab = "Profile";
        private string _annCategory = "Updates";
        private float _annScroll, _shopScroll;
        private string _regIcon = "icon_01";
        private string _regRegion = "Brazil";
        private string _profileError = "", _adminError = "", _regError = "", _regCodeHint = "", _loginCodeHint = "";
        private readonly List<Account> _friendResults = new();
        private readonly List<string> _adminLog = new();

        private TextBox _tbRegGmail = new();
        private TextBox _tbRegNick = new();
        private TextBox _tbRegDisplay = new();
        private TextBox _tbRegPass = new();
        private TextBox _tbRegPass2 = new();
        private TextBox _tbRegCode = new();
        private TextBox _tbRegDesc = new();
        private TextBox _tbDisplay = new();
        private TextBox _tbDesc = new();
        private TextBox _tbFriendSearch = new();
        private TextBox _tbAdminUser = new();
        private TextBox _tbAdminPass = new();
        private TextBox _tbAdminCmd = new();
        private TextBox _tbNotifyMsg = new();
        private TextBox _tbAnnTitle = new();
        private TextBox _tbAnnBody = new();
        private TextBox _tbAnnFile = new();
        private TextBox _tbAnnAuthor = new();
        private TextBox _tbGiveTarget = new();
        private TextBox _tbGiveQuery = new();
        private TextBox _tbGiveCoins = new();
        private TextBox _tbGiveLevel = new();
        private TextBox _tbGiveWins = new();
        private TextBox _tbGiveKills = new();
        private TextBox _tbAdminGmail = new();
        private TextBox _tbChat = new();
        private TextBox _tbServerUrl = new();
        private TextBox _tbNewPass = new();
        private TextBox _tbNewPass2 = new();
        private TextBox _tbPwdCode = new();
        private int _cfgSection;
        private NotifyIcon? _tray;
        private string _updateNotice = "";
        private bool _fpsVisible;
        private int _fpsValue, _fpsFrames;
        private float _fpsTimer;
        private bool _pwdChangeOpen;

        private string _pwdCodeHint = "";
        private bool _online;
        private string _onlineNick = "", _onlineId = "";
        private readonly List<(string Id, string Nick)> _onlineFriends = new();
        private readonly List<(string Id, string Nick)> _onlineRequests = new();
        private readonly List<string> _matchPlayers = new();
        private readonly List<string> _matchNetIds = new();
        private readonly List<int> _matchTeams = new();
        private float _netSendTimer;
        private bool _queueing;
        private bool _playWithMobile;

        private bool _chatOpen;
        private int _chatScope; // 0 = team, 1 = all


        // ---------- Estado ----------
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private double _last;
        private float _clock;
        private int _page;                 // 0=Loading 1=Login 2=Lobby 3=Armas 4=Partida 5=Update 6=Registro 7=Anúncios 8=Cofre 9=Dead Royal
        private float _progress;
        private float _fade = 1f;
        private bool _queue;
        private readonly Stopwatch _queueSw = new();
        private bool _friendsOpen;
        private float _fo;                 // animação do painel de amigos (0..1)
        private float _fscroll;
        private int _gun;
        private readonly Dictionary<string, int> _skinSel = new();
        private readonly Dictionary<string, string> _equipped = new();
        private sealed class ToastItem { public string Text = ""; public float Left; }
        private readonly List<ToastItem> _toasts = new();

        // ---------- Entrada / hits ----------
        private readonly List<Hit> _hits = new();
        private readonly Dictionary<string, float> _anim = new();
        private string? _hover;
        private string? _down;
        private Point _off = Point.Empty;

        // ---------- Gameplay: Deal Duo, Squad Battle, Dead Royale ----------
        private readonly List<GameMap> _maps = new();
        private GameplayConfig _rules = new();
        private int _mapIndex;
        private int _teamSize = 2;        // Deal Duo: 1v1 ou 2v2
        private int _royaleSquad = 1;     // Dead Royale: solo/dupla/trio/squad
        private GameSession? _g;
        private GameMap _map = new();
        private bool _inGame;
        private bool _inventoryOpen;
        private float _invAnim;
        private bool _paused;
        private float _pauseAnim;
        private bool _firing;
        private readonly HashSet<Keys> _keys = new();
        private Point _mouse;
        private float _camX, _camY, _zoom = 1f;
        private int _spectate = -1;
        private bool _storeOpen;
        private float _storeTimer;
        private float _deathFade;
        private float _scorePop;
        private float _bannerAnim;
        private float _hpAnim = 100f;
        private float _armorAnim;
        private bool _wasPlayerAlive = true;
        private int _lastScore0, _lastScore1;
        private float _hitFlash;

        private static readonly Color HpGreen = Color.FromArgb(61, 220, 151);
        private static readonly Color HpRed = Color.FromArgb(255, 70, 85);
        private static readonly Color HpGray = Color.FromArgb(70, 72, 84);
        private static readonly Color IconBlue = Color.FromArgb(76, 201, 240);
        private static readonly Color IconPurple = Color.FromArgb(170, 120, 255);
        private static readonly Color IconRed = Color.FromArgb(255, 88, 96);
        private static readonly Color IconYellow = Color.FromArgb(245, 197, 66);

        // ---------- Caches ----------
        private readonly Dictionary<int, SolidBrush> _br = new();
        private readonly Dictionary<(int, float), Pen> _pn = new();
        private readonly Dictionary<(Font, char), float> _cw = new();
        private readonly Dictionary<(string, Font, float), float[]> _spacedCache = new();
        private readonly Dictionary<string, Image?> _imgs = new();
        private Bitmap? _bgCache;

        public MainForm()
        {
            try { Icon = new Icon(Path.Combine(_appDir, "Assets", "DeadzoneIcon.ico")); } catch { }
            try
            {
                _tray = new NotifyIcon { Icon = Icon, Text = "Deadzone2D", Visible = true };
                _tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
            }
            catch { }
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1440, 900);
            Size = new Size(Math.Min(1440, wa.Width), Math.Min(900, wa.Height));
            MinimumSize = new Size(Math.Min(1100, wa.Width), Math.Min(680, wa.Height));
            BackColor = Bg;

            _custom = LoadCustomize();
            Text = _custom.WindowTitle;
            ChatStore.Load();
            OnlineClient.Load(_appDir);
            OnlineClient.OnMessage = (type, data) =>
            {
                try { BeginInvoke((Action)(() => HandleOnlineMessage(type, data))); } catch { }
            };
            // Reconecta automaticamente ao servidor público quando já há sessão salva.
            if (!string.IsNullOrWhiteSpace(OnlineClient.Token))
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    bool ok = await OnlineClient.Connect();
                    if (ok) try { BeginInvoke((Action)(() => ShowToast("Reconnected to online server."))); } catch { }
                });
            Loc.Load();
            _langSelected = Array.IndexOf(Loc.Languages, Loc.Language);
            if (_langSelected < 0) _langSelected = 0;
            AccountStore.Load();
            MailService.Load(_appDir);
            GoogleAuth.Load(_appDir);
            RankStore.Load(_appDir);
            UpdateSettings.Load(_appDir);
            AppInfo.Load(_appDir);
            UpdateService.LocalVersion = AppInfo.Versao;
            _guns = LoadGuns();
            InjectShopSkins();
            var data = GameData.Load(_appDir);
            _maps.AddRange(data.Maps);
            _rules = data.Rules;
            if (_maps.Count == 0)
                _maps.Add(new GameMap { Id = "deal_duo", Name = "Deal Duo", Mode = "DealDuo", MaxWins = 3 });
            _map = _maps[0];
            KeyPreview = true;

            // Pré-preenchimento do login com o último usuário
            string lastUser = LoadLastUser();
            if (!string.IsNullOrEmpty(lastUser)) { _players[0] = lastUser; _tbUser.Text = lastUser; }

            // Checagem de atualizações
            Task.Run(async () =>
            {
                try
                {
                    var info = await UpdateService.CheckAsync();
                    if (info != null) Invoke((Action)(() =>
                    {
                        if (IsDisposed) return;
                        _updateNotice = "UPDATE AVAILABLE: v" + info.Version + " - press CHECK UPDATE in Settings.";
                        ShowToast(_updateNotice);
                        SystemNotify("Deadzone2D update", "Version " + info.Version + " is available.");
                    }));
                }
                catch { }
            });

            _timer = new System.Windows.Forms.Timer { Interval = 50 };
            _timer.Tick += (s, e) =>
            {
                double now = _sw.Elapsed.TotalSeconds;
                float dt = (float)Math.Min(0.1, now - _last);
                _last = now;
                Step(dt);
                if (WindowState != FormWindowState.Minimized) Invalidate();
            };
            _timer.Start();
            FormClosed += (s, e) => { _timer.Stop(); _tray?.Dispose(); };
            Shown += (s, e) => { if (WindowState == FormWindowState.Normal) ToggleMax(); };   // abre em tela cheia
        }

        // ---------- Dados externos ----------
        public void QuickPlay(string[] args)
        {
            _allowGuest = true;
            string mapId = "";
            string page = "Partida";
            int squad = 0;
            foreach (var a in args)
            {
                if (a.StartsWith("--map=", StringComparison.OrdinalIgnoreCase)) mapId = a[6..];
                else if (a.StartsWith("--page=", StringComparison.OrdinalIgnoreCase)) page = a[7..];
                else if (a.StartsWith("--squad=", StringComparison.OrdinalIgnoreCase) && int.TryParse(a[8..], out var n)) squad = n;
            }
            if (args.Contains("--update"))
            {
                UpdateService.Available = new UpdateService.UpdateInfo("1.2.0", "https://example.com/DeadzoneSetup_v1.2.0.exe", "Test notes: renderer fix.");
                if (page == "Partida") page = "Update";
            }
            if (!string.IsNullOrEmpty(mapId))
            {
                int idx = _maps.FindIndex(m => string.Equals(m.Id, mapId, StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(m.Mode, mapId, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _mapIndex = idx;
            }
            if (_maps.Count > 0) _map = _maps[Math.Clamp(_mapIndex, 0, _maps.Count - 1)];
            if (squad > 0)
            {
                if (_map.Mode == "DeadRoyale") _royaleSquad = Math.Clamp(squad, 1, 4);
                else if (_map.Mode == "DealDuo") _teamSize = squad <= 1 ? 1 : 2;
            }
            Shown += (s, e) => ShowPage(page);
        }

        private Customize LoadCustomize()
        {
            try
            {
                var json = File.ReadAllText(Path.Combine(_appDir, "Config", "Customize.json"));
                using var doc = JsonDocument.Parse(json);
                string title = doc.RootElement.GetProperty("Window").GetProperty("Title").GetString() ?? "Deadzone";
                // instalações antigas traziam "Deadzone2D2D"; normaliza para o nome correto
                if (title.EndsWith("2D2D", StringComparison.OrdinalIgnoreCase)) title = title[..^2];
                return new Customize { WindowTitle = title };
            }
            catch { return new Customize(); }
        }

        private GunData[] LoadGuns()
        {
            try
            {
                var dir = Path.Combine(_appDir, "Assets", "Guns");
                var guns = new List<GunData>();
                if (Directory.Exists(dir))
                    foreach (var f in Directory.GetFiles(dir, "*.json").OrderBy(x => x))
                    {
                        var data = JsonSerializer.Deserialize<GunData>(File.ReadAllText(f), JsonOpts);
                        if (data != null && data.Skins.Count > 0) guns.Add(data);
                    }
                if (guns.Count > 0) return guns.ToArray();
            }
            catch { }
            return DefaultGuns();
        }

        private static GunData[] DefaultGuns()
        {
            GunData G(string id, string name) => new GunData
            {
                GunId = id, Name = name,
                Skins = new List<Skin>
                {
                    new Skin { SkinId = "default", Name = "Default" },
                    new Skin { SkinId = "amber_royal", Name = "Amber Royal" },
                    new Skin { SkinId = "neon_fury", Name = "Neon Fury" },
                    new Skin { SkinId = "rusty_tatical", Name = "Rusty Tatical" },
                }
            };
            return new[] { G("glock", "Glock"), G("ak47", "AK-47"), G("mp40", "MP40"), G("miniuzi", "Mini-Uzi"), G("awm", "Sniper AWM") };
        }

        private Image? Preview(GunData g, Skin s)
        {
            string[] names = { $"{g.GunId}_{s.SkinId}.png", $"{g.GunId}.png", s.SkinId + ".png" };
            string[] dirs = { Path.Combine(_appDir, "Assets", "Previews"), Path.Combine(_appDir, "Assets", "Skins") };
            foreach (var dir in dirs)
            foreach (var n in names)
            {
                var path = Path.Combine(dir, n);
                if (_imgs.TryGetValue(path, out var cached)) { if (cached != null) return cached; continue; }
                Image? img = null;
                try
                {
                    if (File.Exists(path))
                    {
                        using var fs = File.OpenRead(path);
                        using var tmp = Image.FromStream(fs);
                        img = new Bitmap(tmp);
                    }
                }
                catch { img = null; }
                _imgs[path] = img;
                if (img != null) return img;
            }
            return null;
        }

        // ---------- Lógica ----------
        private void Step(float dt)
        {
            _clock += dt;
            if (_page == 4)
            {
                _fpsFrames++;
                _fpsTimer += dt;
                if (_fpsTimer >= 0.5f)
                {
                    _fpsValue = (int)(_fpsFrames / _fpsTimer);
                    _fpsFrames = 0;
                    _fpsTimer = 0f;
                }
            }
            if (_langConfirmOpen && _langConfirmLeft > 0f) _langConfirmLeft = Math.Max(0f, _langConfirmLeft - dt);
            if (_loading)
            {
                _loadingLeft -= dt;
                if (_loadingLeft <= 0f)
                {
                    _loading = false;
                    var done = _loadingDone;
                    _loadingDone = null;
                    done?.Invoke();
                }
                return;
            }
            SyncAccountInputs();
            if (_page == 0)
            {
                _progress = Math.Min(100f, _progress + dt * 38f);
                if (_progress >= 100f) ShowPage("Login");
            }
            if (_page == 4 && _inGame && _g != null) UpdateGame(dt);
            if (_page == 5 && _updInstallRequested)
            {
                if (UpdateService.InstallerStarted) { _updInstallRequested = false; Close(); }   // instalador abriu: fecha o jogo para ele atualizar
                else if (UpdateService.DownloadFailed)
                {
                    _updInstallRequested = false;
                    ShowToast("Update failed: " + (UpdateService.LastError ?? "try again"));
                }
            }
            _fade = Math.Max(0f, _fade - dt * 3.2f);
            _fo = Math.Clamp(_fo + (_friendsOpen ? 1f : -1f) * dt * 3.6f, 0f, 1f);
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                _toasts[i].Left -= dt;
                if (_toasts[i].Left <= 0f) _toasts.RemoveAt(i);
            }

            _invAnim = Math.Clamp(_invAnim + ((_inventoryOpen ? 1f : 0f) - _invAnim) * Math.Min(1f, dt * 11f), 0f, 1f);
            _pauseAnim = Math.Clamp(_pauseAnim + ((_paused ? 1f : 0f) - _pauseAnim) * Math.Min(1f, dt * 12f), 0f, 1f);
            _deathFade = Math.Clamp(_deathFade + ((_g != null && !_g.Player.Alive ? 1f : 0f) - _deathFade) * Math.Min(1f, dt * 7f), 0f, 1f);
            _bannerAnim = Math.Clamp(_bannerAnim + ((_g != null && _g.BannerLife > 0f ? 1f : 0f) - _bannerAnim) * Math.Min(1f, dt * 8f), 0f, 1f);
            _scorePop = Math.Max(0f, _scorePop - dt * 2.2f);
            _hitFlash = Math.Max(0f, _hitFlash - dt * 5f);
            if (_g != null)
            {
                _hpAnim += (_g.Player.Health - _hpAnim) * Math.Min(1f, dt * 9f);
                _armorAnim += (_g.Player.Armor - _armorAnim) * Math.Min(1f, dt * 9f);
            }
            foreach (var k in _anim.Keys.ToList())
            {
                float target = k == _hover ? 1f : 0f;
                float v = _anim[k];
                _anim[k] = v + (target - v) * Math.Min(1f, dt * 14f);
            }
        }

        private void UpdateGame(float dt)
        {
            if (_g == null) return;
            if (_storeOpen && _storeTimer > 0f)
            {
                _storeTimer -= dt;
                if (_storeTimer <= 0f) _storeOpen = false;
            }
            if (_g != null && !_storeOpen)
            {
                var input = new GameInput
                {
                    Up = _keys.Contains(Keys.W) || _keys.Contains(Keys.Up),
                    Down = _keys.Contains(Keys.S) || _keys.Contains(Keys.Down),
                    Left = _keys.Contains(Keys.A) || _keys.Contains(Keys.Left),
                    Right = _keys.Contains(Keys.D) || _keys.Contains(Keys.Right),
                    Sprint = _keys.Contains(Keys.ShiftKey),
                    Fire = _firing && !_inventoryOpen,
                    AimX = ScreenToWorldX(_mouse.X),
                    AimY = ScreenToWorldY(_mouse.Y),
                };
                _g.Update(dt, input);
                if (_online && OnlineClient.Connected)
                {
                    _netSendTimer -= dt;
                    if (_netSendTimer <= 0f)
                    {
                        _netSendTimer = 0.05f;
                        var p = _g.Player;
                        OnlineClient.Send("match:state", new
                        {
                            x = p.X, y = p.Y, angle = p.Angle, weapon = p.WeaponId,
                            firing = _firing && !_inventoryOpen && !_paused && p.Alive
                        });
                    }
                }
            }

            if (_g.Score0 != _lastScore0 || _g.Score1 != _lastScore1)
            {
                _lastScore0 = _g.Score0;
                _lastScore1 = _g.Score1;
                _scorePop = 1f;
            }

            bool alive = _g.Player.Alive;
            if (_wasPlayerAlive && !alive)
            {
                _spectate = 0;
                _deathFade = 0f;
            }
            if (alive) _spectate = -1;
            _wasPlayerAlive = alive;

            var list = _g.SpectateList();
            if (_spectate >= list.Count) _spectate = list.Count > 0 ? 0 : -1;

            var focus = _g.Player;
            if (!focus.Alive && _spectate >= 0 && _spectate < list.Count) focus = list[_spectate];

            int cw = Math.Max(1, ClientSize.Width), ch = Math.Max(1, ClientSize.Height);
            _zoom = Math.Clamp(cw / 1280f, .9f, 1.45f);
            float halfW = cw / (2f * _zoom), halfH = ch / (2f * _zoom);
            float tx = _g.Map.W <= halfW * 2f ? _g.Map.W / 2f : Math.Clamp(focus.X, halfW, _g.Map.W - halfW);
            float ty = _g.Map.H <= halfH * 2f ? _g.Map.H / 2f : Math.Clamp(focus.Y, halfH, _g.Map.H - halfH);
            _camX += (tx - _camX) * Math.Min(1f, dt * 7f);
            _camY += (ty - _camY) * Math.Min(1f, dt * 7f);
        }

        private Fighter? SpectatedFighter()
        {
            if (_g == null) return null;
            var list = _g.SpectateList();
            if (list.Count == 0) return null;
            if (_spectate < 0 || _spectate >= list.Count) _spectate = 0;
            return list[_spectate];
        }

        private void CycleSpectate(int dir)
        {
            if (_g == null) return;
            var list = _g.SpectateList();
            if (list.Count == 0) return;
            _spectate = ((_spectate < 0 ? 0 : _spectate) + dir + list.Count) % list.Count;
        }

        private float ScreenToWorldX(float sx) => _camX + (sx - ClientSize.Width / 2f) / _zoom;
        private float ScreenToWorldY(float sy) => _camY + (sy - ClientSize.Height / 2f) / _zoom;

        private void ShowPage(string name)
        {
            _page = name switch
            {
                "Login" => 1,
                "Lobby" => 2,
                "Weapons" => 3,
                "Partida" => 4,
                "Update" => 5,
                "Register" => 6,
                "Announcements" => 7,
                "Cofre" => 8,
                "DeadRoyal" => 9,
                _ => 0
            };
            _fade = 1f;
            _progress = 0f;
            if (_timer != null) _timer.Interval = _page == 4 ? 33 : 50;
            _profileOpen = _notifOpen = _adminOpen = _addFriendOpen = _cfgOpen = _requestsOpen = false;
            _notifyUiOpen = _announceUiOpen = _giveOpen = _adminGmailOpen = _rankInfoOpen = false;
            _langConfirmOpen = false;
            _tagsToGive.Clear();
            _adminAuthed = false;
            _profileError = _adminError = _regError = _loginCodeHint = "";
            if (_page < 2) _friendsOpen = false;
            if (_page != 4)
            {
                _inGame = false;
                _firing = false;
                _keys.Clear();
                _paused = false;
            }
            if (_page >= 2 && _page != 4 && _page != 5 && _page != 6 && _account == null && !_allowGuest)
            {
                ShowPage("Login");
                return;
            }
            _tbUser.Visible = _page == 1;
            _tbPass.Visible = _page == 1;
            SyncAccountInputs();
            if (_page == 1) BeginInvoke((Action)(() => { if (_page == 1) _tbUser.Focus(); }));
            if (_page == 6) BeginInvoke((Action)(() => { if (_page == 6) _tbRegGmail.Focus(); }));
            if (_page == 4) StartGame();
        }

        private void EnsureGuestAccount()
        {
            if (_account != null) return;
            var acc = new Account
            {
                UserId = "DZ-GUEST",
                Gmail = "convidado@local",
                Nick = "CONVIDADO",
                DisplayName = "Convidado",
                IconId = "icon_01",
                BannerId = "banner_01",
                Description = "Guest session (test mode).",
                Region = "Brazil",
                CreatedAt = DateTime.UtcNow,
                DeadCoins = 0,
                OwnershipV2 = true
            };
            acc.OwnedIcons.Add(acc.IconId);
            acc.OwnedBanners.Add(acc.BannerId);
            _account = acc;
            _players[0] = acc.Nick;
            LoadFriendsFromAccount();
            LoadEquippedFromAccount();
            ResetGroup();
        }

        private void StartGame()
        {
            if (_maps.Count == 0) return;
            _map = _maps[Math.Clamp(_mapIndex, 0, _maps.Count - 1)];
            int teamSize = _map.Mode == "SquadBattle" ? 4 : _map.Mode == "DeadRoyale" ? _royaleSquad : _teamSize;
            _g = new GameSession(_map, _rules, teamSize, _players);
            if (_online && _matchNetIds.Count > 0)
                _g.ConfigureOnline(_matchNetIds.ToArray(), _matchTeams.ToArray());
            if (_map.Mode == "DealDuo") { _storeOpen = true; _storeTimer = 60f; }
            _inGame = true;
            _inventoryOpen = false;
            _invAnim = 0f;
            _paused = false;
            _pauseAnim = 0f;
            _firing = false;
            _keys.Clear();
            _spectate = -1;
            _deathFade = 0f;
            _scorePop = 0f;
            _bannerAnim = 0f;
            _hpAnim = 100f;
            _armorAnim = 0f;
            _wasPlayerAlive = true;
            _lastScore0 = _g.Score0;
            _lastScore1 = _g.Score1;
            _camX = _g.Player.X;
            _camY = _g.Player.Y;
            _zoom = Math.Clamp(ClientSize.Width / 1280f, .9f, 1.45f);
            _page = 4;
            _fade = 1f;
            _progress = 0f;
        }

        private void BackToLobby()
        {
            if (_loading) return;
            StartLoading("RETURNING TO LOBBY", 1.6f, () =>
            {
                _inGame = false;
                _g = null;
                _paused = false;
                _inventoryOpen = false;
                _storeOpen = false;
                _firing = false;
                _keys.Clear();
                _spectate = -1;
                ShowPage("Lobby");
            });
        }

        private void SystemNotify(string title, string text)
        {
            try
            {
                if (_tray == null) return;
                if (WindowState == FormWindowState.Minimized || !Focused)
                    _tray.ShowBalloonTip(6000, title, text, ToolTipIcon.Info);
            }
            catch { }
        }

        private void ShowToast(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            _toasts.Insert(0, new ToastItem { Text = msg, Left = 2.6f });
            while (_toasts.Count > 5) _toasts.RemoveAt(_toasts.Count - 1);
        }

        private void SendChat()
        {
            if (!_chatOpen || _account == null) return;
            string text = (_tbChat.Text ?? "").Trim();
            if (text.Length == 0) return;
            string scope = _chatScope == 1 ? "all" : "team";
            if (_online && OnlineClient.Connected)
            {
                OnlineClient.Send("chat", new { scope, text });
                _tbChat.Text = "";
                ShowToast("Message sent.");
                return;
            }
            ChatStore.Add(new ChatMessage
            {
                UserId = _account.UserId,
                Author = _account.Nick,
                Text = text,
                Scope = _chatScope == 1 ? "all" : "team",
                At = DateTime.UtcNow
            });
            _tbChat.Text = "";
            ShowToast("Message sent" + (ChatStore.Messages.Last().Text != text ? " (filtered)" : "") + ".");
        }

        private void StartLoading(string title, float seconds, Action done)
        {
            _loading = true;
            _loadingTitle = title;
            _loadingLeft = seconds;
            _loadingTotal = seconds;
            _loadingDone = done;
            _firing = false;
            _keys.Clear();
            _paused = false;
        }

        private static string JsonString(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";

        private void AutoOnline(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return;
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                var result = await OnlineClient.Login(email, password);
                if (!result.Ok) result = await OnlineClient.Register(email, password, email.Split('@')[0]);
                if (!result.Ok) return;
                bool ok = await OnlineClient.Connect();
                if (ok) try { BeginInvoke((Action)(() => ShowToast("Online account ready."))); } catch { }
            });
        }

        private void TryOnlineLogin()
        {
            string email = _tbUser.Text.Trim();
            string password = _tbPass.Text;
            if (email.Length == 0 || password.Length == 0) { ShowToast("Enter your online Gmail and password."); return; }
            ShowToast("Connecting to server...");
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                var result = await OnlineClient.Login(email, password);
                if (!result.Ok)
                {
                    string nick = email.Split('@')[0];
                    if (nick.Length < 3) nick = "Player";
                    result = await OnlineClient.Register(email, password, nick);
                    if (!result.Ok)
                    {
                        try { BeginInvoke((Action)(() => ShowToast(result.Error))); } catch { }
                        return;
                    }
                }
                bool connected = await OnlineClient.Connect();
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        if (!connected) { ShowToast(OnlineClient.LastError ?? "Connection failed."); return; }
                        _online = true;
                        _onlineNick = JsonString(result.Profile, "nick");
                        _onlineId = JsonString(result.Profile, "id");
                        _players[0] = _onlineNick;
                        ShowToast("Online: " + _onlineNick);
                        StartLoading("ENTERING ONLINE LOBBY", 1.5f, () => ShowPage("Lobby"));
                    }));
                }
                catch { }
            });
        }

        private void HandleOnlineMessage(string type, JsonElement data)
        {
            switch (type)
            {
                case "welcome":
                {
                    _online = true;
                    if (data.TryGetProperty("profile", out var p))
                    {
                        _onlineNick = JsonString(p, "nick");
                        _onlineId = JsonString(p, "id");
                        _players[0] = _onlineNick;
                    }
                    _onlineFriends.Clear();
                    if (data.TryGetProperty("friends", out var fs) && fs.ValueKind == JsonValueKind.Array)
                        foreach (var f in fs.EnumerateArray())
                            _onlineFriends.Add((JsonString(f, "id"), JsonString(f, "nick")));
                    _onlineRequests.Clear();
                    if (data.TryGetProperty("requests", out var rs) && rs.ValueKind == JsonValueKind.Array)
                        foreach (var q in rs.EnumerateArray())
                            _onlineRequests.Add((JsonString(q, "from"), JsonString(q, "nick")));
                    RebuildOnlineFriends();
                    if (data.TryGetProperty("chat", out var cs) && cs.ValueKind == JsonValueKind.Array)
                        foreach (var m in cs.EnumerateArray())
                        {
                            string text = JsonString(m, "text");
                            if (!ChatStore.Messages.Any(x => x.Text == text && x.Author == JsonString(m, "author")))
                                ChatStore.Messages.Add(new ChatMessage
                                {
                                    Author = JsonString(m, "author"), Text = text,
                                    Scope = JsonString(m, "scope"), UserId = JsonString(m, "from"),
                                    At = DateTimeOffset.FromUnixTimeMilliseconds(m.TryGetProperty("at", out var at) ? at.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime
                                });
                        }
                    break;
                }
                case "chat":
                {
                    if (!data.TryGetProperty("message", out var m)) break;
                    ChatStore.Messages.Add(new ChatMessage
                    {
                        Author = JsonString(m, "author"), Text = JsonString(m, "text"),
                        Scope = JsonString(m, "scope"), UserId = JsonString(m, "from"),
                        At = DateTime.UtcNow
                    });
                    break;
                }
                case "info": ShowToast(JsonString(data, "message")); break;
                case "error": ShowToast(JsonString(data, "message")); break;
                case "friend:request":
                    _onlineRequests.Add((JsonString(data, "from"), JsonString(data, "nick")));
                    ShowToast("Friend request from " + JsonString(data, "nick"));
                    SystemNotify("Deadzone2D friend request", JsonString(data, "nick") + " wants to be your friend.");
                    break;
                case "friend:list":
                {
                    _onlineFriends.Clear();
                    if (data.TryGetProperty("friends", out var fs) && fs.ValueKind == JsonValueKind.Array)
                        foreach (var f in fs.EnumerateArray())
                            _onlineFriends.Add((JsonString(f, "id"), JsonString(f, "nick")));
                    RebuildOnlineFriends();
                    break;
                }
                case "queue": ShowToast("Looking for players... " + (data.TryGetProperty("count", out var c) ? c.GetInt32().ToString() : "")); break;
                case "match:state":
                {
                    if (_g == null) break;
                    _g.ApplyRemoteState(
                        JsonString(data, "id"),
                        data.TryGetProperty("x", out var sx) ? (float)sx.GetDouble() : 0f,
                        data.TryGetProperty("y", out var sy) ? (float)sy.GetDouble() : 0f,
                        data.TryGetProperty("angle", out var sa) ? (float)sa.GetDouble() : 0f,
                        JsonString(data, "weapon"),
                        data.TryGetProperty("firing", out var sf) && sf.GetBoolean());
                    break;
                }
                case "match:start":
                {
                    if (!data.TryGetProperty("match", out var m)) break;
                    _matchPlayers.Clear();
                    _matchNetIds.Clear();
                    _matchTeams.Clear();
                    if (m.TryGetProperty("players", out var ps) && ps.ValueKind == JsonValueKind.Array)
                        foreach (var pl in ps.EnumerateArray())
                        {
                            string nick = JsonString(pl, "nick");
                            string pid = JsonString(pl, "id");
                            if (pid == _onlineId) continue; // o jogador local já é o índice 0
                            _matchPlayers.Add(nick);
                            _matchNetIds.Add(pid);
                            _matchTeams.Add(pl.TryGetProperty("team", out var tm) ? tm.GetInt32() : 1);
                        }
                    for (int i = 0; i < Math.Min(4, _matchPlayers.Count); i++) _players[i] = _matchPlayers[i];
                    _queueing = false;
                    ShowToast("Match found!");
                    StartLoading("LOADING ONLINE MATCH", 2f, () => ShowPage("Partida"));
                    break;
                }
            }
        }

        private void RebuildOnlineFriends()
        {
            _friendList.Clear();
            foreach (var f in _onlineFriends)
                _friendList.Add(new Friend(f.Nick, "Online • " + f.Id, FStatus.Online, f.Id));
        }

        private void TryGoogleSignIn()
        {
            if (!GoogleAuth.IsConfigured)
            {
                ShowToast("Configure Config/Google.json (Client ID and Client Secret) to sign in with Google.");
                return;
            }
            ShowToast("Abrindo o Google...");
            System.Threading.Tasks.Task.Run(() =>
            {
                bool ok = GoogleAuth.SignIn(out var profile, out var error);
                try
                {
                    BeginInvoke((Action)(() =>
                    {
                        if (!ok || profile == null) { ShowToast(error); return; }
                        var acc = AccountStore.LoginOrCreateWithGoogle(profile, out var accErr);
                        if (acc == null) { ShowToast(accErr); return; }
                        _account = acc;
                        _players[0] = acc.Nick;
                        SaveLastUser(acc.Gmail);
                        _royaleSquad = 1;
                        _teamSize = 1;
                        LoadFriendsFromAccount();
                        LoadEquippedFromAccount();
                        ResetGroup();
                        AutoOnline(profile.Email, "G-" + profile.Id);
                        ShowToast("Bem-vindo, " + acc.DisplayName + ".");
                        StartLoading("SIGNING IN WITH GOOGLE", 1.8f, () => ShowPage("Lobby"));
                    }));
                }
                catch { }
            });
        }

        private void ResetGroup()
        {
            _groupMembers.Clear();
            _groupMembers.Add(_account?.Nick ?? "PLAYER 1");
        }

        // tela cheia de verdade (cobre a barra de tarefas); F11 alterna
        private void ToggleMax()
        {
            if (WindowState == FormWindowState.Maximized) WindowState = FormWindowState.Normal;
            else { MaximizedBounds = Screen.FromControl(this).Bounds; WindowState = FormWindowState.Maximized; }
        }

        private void Activate(string id)
        {
            if (id.StartsWith("frdAccept:", StringComparison.OrdinalIgnoreCase) && _online &&
                int.TryParse(id.Substring(10), out var oa) && oa >= 0 && oa < _onlineRequests.Count)
            {
                string oid = _onlineRequests[oa].Id;
                OnlineClient.Send("friend:accept", new { id = oid });
                _onlineRequests.RemoveAt(oa);
                ShowToast("Friend request accepted.");
                return;
            }
            if (id.StartsWith("frdAccept:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(10), out var fa) && fa >= 0 && fa < _friendRequests.Count)
            {
                string raw = _friendRequests[fa];
                if (raw.StartsWith("G:", StringComparison.OrdinalIgnoreCase))
                {
                    string inviterId = raw.Substring(2);
                    var inviter = AccountStore.FindByUserId(inviterId);
                    if (_account != null) AccountStore.AcceptGroupInvite(_account, inviterId);
                    if (_squadOpen && _groupMembers.Count < 4 && inviter != null)
                        _groupMembers.Add(inviter.Nick);
                    LoadFriendsFromAccount();
                    ShowToast("Joined " + (inviter?.Nick ?? inviterId) + "'s group.");
                    return;
                }
                string uid = raw;
                var who = AccountStore.FindByUserId(uid);
                if (_account != null) AccountStore.AcceptFriendRequest(_account, uid);
                LoadFriendsFromAccount();
                ShowToast("Request accepted: " + (who?.Nick ?? uid));
                return;
            }
            if (id.StartsWith("frdReject:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(10), out var frq) && frq >= 0 && frq < _friendRequests.Count)
            {
                string raw = _friendRequests[frq];
                string uid = raw.StartsWith("G:", StringComparison.OrdinalIgnoreCase) ? raw.Substring(2) : raw;
                var who = AccountStore.FindByUserId(uid);
                if (_account != null) AccountStore.DeclineFriendRequest(_account, uid);
                LoadFriendsFromAccount();
                ShowToast("Request declined: " + (who?.Nick ?? uid));
                return;
            }
            if (id.StartsWith("frdRemove:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(10), out var ri))
            {
                if (ri >= 0 && ri < _friendList.Count)
                {
                    var rf = _friendList[ri];
                    if (_account != null && !string.IsNullOrEmpty(rf.UserId))
                        AccountStore.RemoveFriend(_account, rf.UserId);
                    _friendList.RemoveAt(ri);
                    ShowToast("Removido: " + rf.Name);
                }
                return;
            }
            if (id.StartsWith("annCat:", StringComparison.OrdinalIgnoreCase))
            {
                _annCategory = AccountStore.NormalizeCategory(id.Substring(7));
                _annScroll = 0f;
                return;
            }
            if (id.StartsWith("annFile:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(8), out var afi))
            {
                OpenAnnouncementFile(afi);
                return;
            }
            if (id.StartsWith("cofreTab:", StringComparison.OrdinalIgnoreCase))
            {
                _cofreTab = id.Substring(9);
                return;
            }
            if (id.StartsWith("equipIcon:", StringComparison.OrdinalIgnoreCase) && _account != null)
            {
                string iid = id.Substring(10);
                if (_account.OwnedIcons.Contains(iid, StringComparer.OrdinalIgnoreCase))
                {
                    _account.IconId = AccountStore.Icon(iid).Id;
                    AccountStore.SaveAccounts();
                    ShowToast("Icon equipped.");
                }
                else ShowToast("You do not own this icon yet.");
                return;
            }
            if (id.StartsWith("equipBanner:", StringComparison.OrdinalIgnoreCase) && _account != null)
            {
                string bid = id.Substring(12);
                if (_account.OwnedBanners.Contains(bid, StringComparer.OrdinalIgnoreCase))
                {
                    _account.BannerId = AccountStore.Banner(bid).Id;
                    AccountStore.SaveAccounts();
                    ShowToast("Banner equipado.");
                }
                else ShowToast("You do not own this banner yet.");
                return;
            }
            if (id.StartsWith("addRemote:", StringComparison.OrdinalIgnoreCase))
            {
                string value = id.Substring(10);
                if (_account != null)
                {
                    if (!AccountStore.AddRemoteContact(_account, value, out var rerr)) ShowToast(rerr);
                    else { LoadFriendsFromAccount(); ShowToast("Contact added: " + value); }
                }
                return;
            }
            if (id.StartsWith("chatScope:", StringComparison.OrdinalIgnoreCase))
            {
                _chatScope = id.EndsWith("1") ? 1 : 0;
                return;
            }
            if (id.StartsWith("cfgSection:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(id.Substring(11), out var sec) && sec >= 0 && sec <= 2) _cfgSection = sec;
                return;
            }
            if (id.StartsWith("lang:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(id.Substring(5), out var li) && li >= 0 && li < Loc.Languages.Length) _langSelected = li;
                return;
            }
            if (id.StartsWith("giveTab:", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(id.Substring(8), out var giveIdx) && giveIdx >= 0 && giveIdx <= 2) _giveTab = giveIdx;
                return;
            }
            if (id.StartsWith("giveItem:", StringComparison.OrdinalIgnoreCase))
            {
                _giveItemSel = id.Substring(9);
                return;
            }
            if (id.StartsWith("giveTag:", StringComparison.OrdinalIgnoreCase))
            {
                string tag = id.Substring(8);
                if (_tagsToGive.Contains(tag, StringComparer.OrdinalIgnoreCase)) _tagsToGive.RemoveAll(x => x == tag);
                else _tagsToGive.Add(tag);
                return;
            }
            if (id.StartsWith("notifyRegion:", StringComparison.OrdinalIgnoreCase))
            {
                _notifyTarget = AccountStore.NormalizeRegion(id.Substring(13)) ?? "all";
                return;
            }
            if (id.StartsWith("announceCat:", StringComparison.OrdinalIgnoreCase))
            {
                _annCategory = AccountStore.NormalizeCategory(id.Substring(12));
                return;
            }
            if (id.StartsWith("shopHint:", StringComparison.OrdinalIgnoreCase))
            {
                string kind = id.Substring(9);
                ShowToast("Compre este " + (kind.StartsWith("Banner", StringComparison.OrdinalIgnoreCase) ? "banner" : "icon") + " na Dead Royal.");
                return;
            }
            if (id.StartsWith("shopBuy:", StringComparison.OrdinalIgnoreCase)) { BuyShopItem(id.Substring(8)); return; }
            if (id.StartsWith("shopEquip:", StringComparison.OrdinalIgnoreCase)) { EquipShopItem(id.Substring(10)); return; }
            if (id.StartsWith("regIcon:", StringComparison.OrdinalIgnoreCase)) { _regIcon = id.Substring(8); return; }
            if (id.StartsWith("regRegion:", StringComparison.OrdinalIgnoreCase)) { _regRegion = id.Substring(10); return; }
            if (id.StartsWith("addFriendResult:", StringComparison.OrdinalIgnoreCase)) { AddFriendByUserId(id.Substring(16)); return; }
            if (id == "shopClose" || id == "cofreClose" || id == "annClose")
            {
                _shopScroll = 0f;
                ShowPage("Lobby");
                return;
            }
            if (id.StartsWith("nav:")) { ShowPage(id.Substring(4)); return; }
            if (id.StartsWith("friend:"))
            {
                var f = _friendList[int.Parse(id.Substring(7))];
                ShowToast(f.St == FStatus.Offline ? $"{f.Name} is offline" : $"Invite sent to {f.Name}");
                return;
            }
            if (id.StartsWith("gun:")) { _gun = int.Parse(id.Substring(4)); return; }
            if (id.StartsWith("skin:")) { _skinSel[_guns[_gun].GunId] = int.Parse(id.Substring(5)); return; }
            if (id.StartsWith("buy:") && _storeOpen && _g != null)
            {
                string buyId = id.Substring(4);
                var gun = Arsenal.Defs[buyId];
                int price = ShopPrices.Get(buyId);
                var p = _g.Player;
                if (p.Coins >= price)
                {
                    p.WeaponId = buyId;
                    p.Mag = p.Weapon.MagSize;
                    p.Reserve = p.Mag * 2;
                    p.Coins -= price;
                    ShowToast("Comprado: " + gun.Name);
                }
                else ShowToast("Not enough coins");
                return;
            }

            switch (id)
            {
                case "min": WindowState = FormWindowState.Minimized; break;
                case "max": ToggleMax(); break;
                case "close": Close(); break;
                case "notif":
                    _notifOpen = !_notifOpen;
                    if (_notifOpen)
                    {
                        _profileOpen = false;
                        _addFriendOpen = false;
                        _adminOpen = false;
                        if (_account != null) AccountStore.MarkNotificationsRead(_account);
                    }
                    break;
                case "notifClose":
                case "notifBackdrop": _notifOpen = false; break;
                case "login":
                {
                    string user = _tbUser.Text.Trim();
                    if (string.IsNullOrEmpty(user)) { ShowToast("Digite seu Gmail."); return; }
                    if (_tbPass.Text.Length == 0) { ShowToast("Enter your password."); return; }
                    var acc = AccountStore.Login(user, _tbPass.Text);
                    if (acc == null) { ShowToast("Invalid Gmail/nick or password."); return; }
                    if (!AccountStore.VerifyCode(acc.Gmail, _tbLoginCode.Text, out var loginCodeErr))
                    { ShowToast(loginCodeErr); return; }
                    _account = acc;
                    _players[0] = acc.Nick;
                    SaveLastUser(acc.Gmail);
                    _royaleSquad = 1;
                    _teamSize = 1;
                    LoadFriendsFromAccount();
                    LoadEquippedFromAccount();
                    AutoOnline(acc.Gmail, _tbPass.Text);
                    _tbPass.Text = "";
                    _tbLoginCode.Text = "";
                    _loginCodeHint = "";
                    ResetGroup();
                    ShowToast("Bem-vindo, " + acc.DisplayName + ".");
                    StartLoading("ENTERING GAME", 1.8f, () => ShowPage("Lobby"));
                    break;
                }
                case "loginSendCode":
                {
                    string user = _tbUser.Text.Trim();
                    var acc = AccountStore.FindByGmail(user) ?? AccountStore.FindByNick(user);
                    if (acc == null) { ShowToast("Account not found."); return; }
                    if (!AccountStore.SendLoginCode(acc.Gmail, out var code, out var sendErr))
                    { ShowToast(sendErr); return; }
                    _loginCodeHint = MailService.IsConfigured
                        ? "Code sent to " + acc.Gmail + "."
                        : "LOCAL MODE - test code: " + code;
                    ShowToast(MailService.IsConfigured ? "Code sent to your Gmail." : "Local code: " + code);
                    BeginInvoke((Action)(() => { if (_page == 1) _tbLoginCode.Focus(); }));
                    break;
                }
                case "register":
                    _regError = "";
                    _regCodeHint = "";
                    _regIcon = "icon_01";
                    _regRegion = "Brazil";
                    _tbRegGmail.Text = _tbUser.Text.Trim();
                    _tbRegNick.Text = "";
                    _tbRegDisplay.Text = "";
                    _tbRegPass.Text = "";
                    _tbRegPass2.Text = "";
                    _tbRegCode.Text = "";
                    _tbRegDesc.Text = "";
                    ShowPage("Register");
                    break;
                case "registerBack":
                    ShowPage("Login");
                    break;
                case "googleSignIn": TryGoogleSignIn(); break;
                case "onlineLogin": TryOnlineLogin(); break;
                case "matchFind":
                    if (!_online) { ShowToast("Sign in online first."); break; }
                    _queueing = !_queueing;
                    if (_queueing) OnlineClient.Send("queue:join", new { mode = _playWithMobile ? "mobile" : "computer", teamSize = 1 });
                    else OnlineClient.Send("queue:leave");
                    ShowToast(_queueing ? "Searching for a match..." : "Search cancelled.");
                    break;
                case "registerSendCode":
                {
                    _regError = "";
                    if (!AccountStore.SendCode(_tbRegGmail.Text, out var code, out var sendErr))
                    {
                        _regError = sendErr;
                        return;
                    }
                    _regCodeHint = MailService.IsConfigured
                        ? "Code sent to " + _tbRegGmail.Text.Trim() + "."
                        : "LOCAL MODE - test code: " + code + " (configure Config/Mail.json para envio real)";
                    ShowToast(MailService.IsConfigured ? "Code sent to your Gmail." : "Locally generated code: " + code);
                    BeginInvoke((Action)(() => { if (_page == 6) _tbRegCode.Focus(); }));
                    break;
                }
                case "registerCreate":
                {
                    if (_tbRegPass.Text != _tbRegPass2.Text) { _regError = "Passwords do not match."; return; }
                    if (!AccountStore.VerifyCode(_tbRegGmail.Text, _tbRegCode.Text, out var codeErr))
                    {
                        _regError = codeErr;
                        return;
                    }
                    var acc = AccountStore.CreateAccount(_tbRegGmail.Text, _tbRegNick.Text, _tbRegDisplay.Text,
                        _tbRegPass.Text, _regIcon, _tbRegDesc.Text, _regRegion, out var err);
                    if (acc == null) { _regError = err; return; }
                    _account = acc;
                    _players[0] = acc.Nick;
                    SaveLastUser(acc.Gmail);
                    LoadFriendsFromAccount();
                    LoadEquippedFromAccount();
                    ResetGroup();
                    ShowToast("Account created. Welcome, " + acc.DisplayName + ".");
                    StartLoading("ENTERING GAME", 1.8f, () => ShowPage("Lobby"));
                    break;
                }
                case "chatOpen":
                    _chatOpen = true;
                    _chatScope = 0;
                    _tbChat.Text = "";
                    BeginInvoke((Action)(() => { if (_chatOpen) _tbChat.Focus(); }));
                    break;
                case "chatClose": _chatOpen = false; break;
                case "chatSend": SendChat(); break;
                case "security2fa":
                    if (_account == null) { ShowToast("Sign in first."); break; }
                    _account.TwoFactor = !_account.TwoFactor;
                    AccountStore.SaveAccounts();
                    ShowToast(_account.TwoFactor ? "Two-factor authentication enabled." : "Two-factor authentication disabled.");
                    break;
                case "securityPassword":
                    if (_account == null) { ShowToast("Sign in first."); break; }
                    _pwdChangeOpen = true;
                    _pwdCodeHint = "";
                    _tbNewPass.Text = "";
                    _tbNewPass2.Text = "";
                    _tbPwdCode.Text = "";
                    break;
                case "securityPwdSend":
                {
                    if (_account == null) break;
                    if (!AccountStore.SendCode(_account.Gmail, out var code, out var err)) { ShowToast(err); break; }
                    _pwdCodeHint = MailService.IsConfigured
                        ? "Code sent to " + _account.Gmail + "."
                        : "LOCAL MODE - test code: " + code;
                    ShowToast(MailService.IsConfigured ? "Code sent to your Gmail." : "Local code: " + code);
                    break;
                }
                case "securityPwdSave":
                {
                    if (_account == null) break;
                    if (_tbNewPass.Text != _tbNewPass2.Text) { ShowToast("Passwords do not match."); break; }
                    if (!AccountStore.ChangePasswordWithCode(_account, _account.Gmail, _tbPwdCode.Text, _tbNewPass.Text, out var perr))
                    { ShowToast(perr); break; }
                    _pwdChangeOpen = false;
                    ShowToast("Password changed.");
                    break;
                }
                case "securityPwdClose": _pwdChangeOpen = false; break;
                case "serverSave":
                    OnlineClient.SaveUrl(_tbServerUrl.Text);
                    ShowToast("Server URL saved: " + OnlineClient.Config.ServerUrl);
                    break;
                case "langSave":
                    _langConfirmOpen = true;
                    _langConfirmLeft = 3f;
                    break;
                case "langCancel":
                    _langConfirmOpen = false;
                    break;
                case "langRevert":
                    _langSelected = Array.IndexOf(Loc.Languages, Loc.Language);
                    if (_langSelected < 0) _langSelected = 0;
                    _langConfirmOpen = false;
                    break;
                case "langConfirm":
                    if (_langConfirmLeft > 0f) break;
                    Loc.Save(Loc.Languages[_langSelected]);
                    _langConfirmOpen = false;
                    ShowToast("Language saved: " + Loc.Language);
                    break;
                case "rankInfo":
                    _rankInfoOpen = true;
                    break;
                case "rankInfoClose":
                case "rankInfoBackdrop": _rankInfoOpen = false; break;
                case "accountOpen":
                    _notifOpen = false;
                    _addFriendOpen = false;
                    OpenProfile();
                    break;
                case "profileClose":
                case "profileBackdrop": CloseProfile(); break;
                case "profileSave": SaveProfile(); break;
                case "profileLogout":
                    CloseProfile();
                    _account = null;
                    _groupMembers.Clear();
                    _loading = false;
                    _loadingDone = null;
                    _friendList.Clear();
                    _equipped.Clear();
                    _players[0] = "PLAYER 1";
                    ShowPage("Login");
                    ShowToast("You have been logged out.");
                    break;
                case "adminOpen":
                    _profileOpen = false;
                    _notifOpen = false;
                    _addFriendOpen = false;
                    ToggleAdmin();
                    break;
                case "adminAuth": AdminLogin(); break;
                case "adminClose":
                case "adminBackdrop": CloseAdmin(); break;
                case "adminNotifyUi":
                    if (!_adminAuthed) break;
                    _announceUiOpen = false;
                    _notifyUiOpen = true;
                    _notifyTarget = "all";
                    _tbNotifyMsg.Text = "";
                    BeginInvoke((Action)(() => { if (_notifyUiOpen) _tbNotifyMsg.Focus(); }));
                    break;
                case "adminAnnounceUi":
                    if (!_adminAuthed) break;
                    _notifyUiOpen = false;
                    _announceUiOpen = true;
                    _annCategory = "Updates";
                    _tbAnnTitle.Text = "";
                    _tbAnnBody.Text = "";
                    _tbAnnFile.Text = "";
                    _tbAnnAuthor.Text = _account?.Nick ?? "sr.idev";
                    BeginInvoke((Action)(() => { if (_announceUiOpen) _tbAnnTitle.Focus(); }));
                    break;
                case "notifyUiClose":
                case "notifyUiBackdrop": _notifyUiOpen = false; break;
                case "announceUiClose":
                case "announceUiBackdrop": _announceUiOpen = false; break;
                case "notifySend":
                {
                    string msg = _tbNotifyMsg.Text.Trim();
                    if (msg.Length == 0) { ShowToast("Enter a message."); break; }
                    var n = AccountStore.AddNotification(_notifyTarget, msg, _account?.Nick ?? "sr.idev");
                    _adminLog.Add("Notification sent to " + (_notifyTarget == "all" ? "ALL" : _notifyTarget) + ": " + n.Message);
                    _notifyUiOpen = false;
                    ShowToast("Notification sent.");
                    break;
                }
                case "announcePublish":
                {
                    string title = _tbAnnTitle.Text.Trim();
                    if (title.Length == 0) { ShowToast("Enter a title."); break; }
                    var ann = AccountStore.AddAnnouncement(_annCategory, title, _tbAnnBody.Text,
                        string.IsNullOrWhiteSpace(_tbAnnAuthor.Text) ? (_account?.Nick ?? "sr.idev") : _tbAnnAuthor.Text.Trim(),
                        string.IsNullOrWhiteSpace(_tbAnnFile.Text) ? null : _tbAnnFile.Text.Trim().Trim('"'));
                    _adminLog.Add("Announcement published in " + ann.Category + ": " + ann.Title);
                    if (!string.IsNullOrEmpty(ann.AttachmentName)) _adminLog.Add("Anexo: " + ann.AttachmentName);
                    else if (!string.IsNullOrWhiteSpace(_tbAnnFile.Text)) _adminLog.Add("[error] attachment file not found.");
                    _announceUiOpen = false;
                    ShowToast("Announcement published.");
                    break;
                }
                case "adminGiveUi":
                    if (!_adminAuthed) break;
                    _notifyUiOpen = _announceUiOpen = _adminGmailOpen = false;
                    _giveOpen = true;
                    _giveTab = 0;
                    _giveItemSel = "";
                    _tbGiveTarget.Text = "";
                    _tbGiveQuery.Text = "";
                    BeginInvoke((Action)(() => { if (_giveOpen) _tbGiveTarget.Focus(); }));
                    break;
                case "giveClose":
                case "giveBackdrop": _giveOpen = false; break;
                case "adminGmailUi":
                    if (!_adminAuthed) break;
                    _notifyUiOpen = _announceUiOpen = _giveOpen = false;
                    _adminGmailOpen = true;
                    _tbAdminGmail.Text = "";
                    BeginInvoke((Action)(() => { if (_adminGmailOpen) _tbAdminGmail.Focus(); }));
                    break;
                case "adminGmailClose":
                case "adminGmailBackdrop": _adminGmailOpen = false; break;
                case "adminGmailAdd":
                {
                    if (!AccountStore.AddAdminGmail(_tbAdminGmail.Text, out var aerr)) { ShowToast(aerr); break; }
                    _adminLog.Add("Administrador adicionado: " + _tbAdminGmail.Text.Trim());
                    _adminGmailOpen = false;
                    ShowToast("Gmail added to the admin list.");
                    break;
                }
                case "giveTab":
                    if (int.TryParse(id.Substring(8), out var gt) && gt >= 0 && gt <= 2) _giveTab = gt;
                    break;
                case "giveItemGo":
                {
                    var target = AccountStore.FindByAny(_tbGiveTarget.Text);
                    if (target == null) { ShowToast("Player not found."); break; }
                    if (_giveItemSel.Length == 0) { ShowToast("Selecione um item."); break; }
                    if (!AccountStore.GiveItem(target, _giveItemSel, out var gerr)) { ShowToast(gerr); break; }
                    _adminLog.Add("Item dado a " + target.Nick + ": " + _giveItemSel);
                    ShowToast("Item entregue a " + target.Nick + ".");
                    break;
                }
                case "giveStatsGo":
                {
                    var target = AccountStore.FindByAny(_tbGiveTarget.Text);
                    if (target == null) { ShowToast("Player not found."); break; }
                    int.TryParse(_tbGiveCoins.Text, out var coins);
                    int.TryParse(_tbGiveLevel.Text, out var level);
                    int.TryParse(_tbGiveWins.Text, out var wins);
                    int.TryParse(_tbGiveKills.Text, out var kills);
                    AccountStore.GiveStats(target, coins, level, wins, kills);
                    _adminLog.Add($"Status dado a {target.Nick}: {coins} DC, level {(level > 0 ? level : target.Level)}, {wins} wins, {kills} kills.");
                    ShowToast("Status aplicado a " + target.Nick + ".");
                    break;
                }
                case "giveTagGo":
                {
                    var target = AccountStore.FindByAny(_tbGiveTarget.Text);
                    if (target == null) { ShowToast("Player not found."); break; }
                    if (_tagsToGive.Count == 0) { ShowToast("Selecione uma tag."); break; }
                    foreach (var t in _tagsToGive) AccountStore.GiveTag(target, t, out _);
                    _adminLog.Add("Tags dadas a " + target.Nick + ": " + string.Join(", ", _tagsToGive));
                    ShowToast("Tags aplicadas.");
                    _tagsToGive.Clear();
                    break;
                }
                case "addFriend":
                    _notifOpen = false;
                    _addFriendOpen = true;
                    _tbFriendSearch.Text = "";
                    _friendResults.Clear();
                    BeginInvoke((Action)(() => { if (_addFriendOpen) _tbFriendSearch.Focus(); }));
                    break;
                case "addFriendClose":
                case "addFriendBackdrop": _addFriendOpen = false; break;
                case "squadCreate":
                    _squadOpen = true;
                    ResetGroup();
                    ShowToast("Group created. Use INVITE to add friends.");
                    break;
                case "squadLeave":
                    _squadOpen = false;
                    _groupMembers.Clear();
                    ShowToast("You left the group.");
                    break;
                case "shopReload":
                    AccountStore.ReloadShop();
                    InjectShopSkins();
                    ShowToast("Store reloaded from JSON.");
                    break;
                case "updateInstall":
                    if (!_updInstallRequested)
                    {
                        _updInstallRequested = true;
                        _ = UpdateService.InstallNow();     // baixa (se preciso) e abre o instalador; o Step() fecha o jogo quando ele abrir
                        ShowToast("Downloading update...");
                    }
                    break;
                case "updateLater":
                    ShowPage("Login");
                    break;
                case "updateIgnore":
                    UpdateService.IgnoreCurrent();          // ignora esta versão por 1 hora
                    ShowPage("Login");
                    break;
                case "updateSoft":
                    if (UpdateService.StartSoftUpdate())
                    {
                        ShowToast("Update downloaded; it will be applied on the next Deadzone start.");
                        Close();
                    }
                    else ShowToast(UpdateService.LastError ?? "This update requires the installer");
                    break;
                case "start":
                    _queue = false;
                    if (_page == 2) StartLoading("LOADING MATCH", 2.2f, () => ShowPage("Partida"));
                    else { _queue = !_queue; if (_queue) _queueSw.Restart(); else _queueSw.Reset(); }
                    break;
                case "mapNext":
                    if (_maps.Count > 0) { _mapIndex = (_mapIndex + 1) % _maps.Count; _map = _maps[_mapIndex]; ShowToast(_map.Name + " • " + _map.Type); }
                    break;
                case "mapPrev":
                    if (_maps.Count > 0) { _mapIndex = (_mapIndex - 1 + _maps.Count) % _maps.Count; _map = _maps[_mapIndex]; ShowToast(_map.Name + " • " + _map.Type); }
                    break;
                case "size1": SetSquadSize(1); break;
                case "size2": SetSquadSize(2); break;
                case "size3": SetSquadSize(3); break;
                case "size4": SetSquadSize(4); break;
                case "inv": _inventoryOpen = !_inventoryOpen; break;
                case "invClose": _inventoryOpen = false; break;
                case "invMed":
                    if (_g != null && _g.UseMedkit(_g.Player)) ShowToast("Vida restaurada");
                    else ShowToast("No medkit or health already full");
                    break;
                case "storeClose": _storeOpen = false; break;
                case "pause": if (_inGame) _paused = true; break;
                case "resume": _paused = false; break;
                case "lobbyNow":
                case "specLobby":
                case "lobbyAfterWin": BackToLobby(); break;
                case "specPrev": CycleSpectate(-1); break;
                case "specNext": CycleSpectate(1); break;
                case "skipCountdown": _g?.SkipCountdown(); break;
                case "reload": if (_g != null) _g.TryReload(_g.Player); break;
                case "mode": ShowToast("Use as setas para trocar de mapa"); break;
                case "invite":
                {
                    if (!_squadOpen) { ShowToast("Crie um grupo primeiro."); break; }
                    if (_groupMembers.Count >= 4) { ShowToast("Grupo completo (4 jogadores)."); break; }
                    var cand = _friendList.FirstOrDefault(f => !_groupMembers.Contains(f.Name));
                    if (cand == null) { ShowToast("Add friends to invite."); _friendsOpen = true; break; }
                    var target = AccountStore.FindByUserId(cand.UserId);
                    if (_account == null) break;
                    if (target == null)
                    {
                        AccountStore.QueueRemoteGroupInvite(_account, cand.UserId);
                        ShowToast("Group invite queued for " + cand.Name + ".");
                        break;
                    }
                    if (!AccountStore.SendGroupInvite(_account, target, out var gerr))
                    { ShowToast(gerr); break; }
                    LoadFriendsFromAccount();
                    ShowToast("Group invite sent to " + cand.Name + ".");
                    break;
                }
                case "friendsOpen": _friendsOpen = true; break;
                case "friendsClose": _friendsOpen = false; break;
                case "frdMenu":
                    LoadFriendsFromAccount();
                    _requestsOpen = !_requestsOpen;
                    break;
                case "cfg": _cfgOpen = !_cfgOpen; break;
                case "cfgBackdrop": _cfgOpen = false; break;
                case "cfgVolUp": _cfgVol = Math.Min(100, _cfgVol + 5); break;
                case "cfgVolDn": _cfgVol = Math.Max(0, _cfgVol - 5); break;
                case "cfgVolBar":
                    if (_uiVolBar.Width > 0)
                        _cfgVol = Math.Clamp((int)Math.Round((_mouse.X - _uiVolBar.X) * 100f / _uiVolBar.Width), 0, 100);
                    break;
                case "cfgLang": _cfgLangEn = !_cfgLangEn; break;
                case "cfgOpt": _cfgOpt = !_cfgOpt; break;
                case "playWithToggle":
                    _playWithMobile = !_playWithMobile;
                    ShowToast(_playWithMobile ? "Playing with mobile players." : "Playing with computer players.");
                    break;
                case "fpsToggle":
                    _fpsVisible = !_fpsVisible;
                    ShowToast(_fpsVisible ? "FPS counter on." : "FPS counter off.");
                    break;
                case "viewUpd":
                    if (UpdateService.Available != null) { _cfgOpen = false; ShowPage("Update"); }
                    else ShowToast("No updates available");
                    break;
                case "equip":
                    {
                        var gun = _guns[_gun];
                        var sk = gun.Skins[SkinIdx(gun)];
                        var shopItem = AccountStore.ShopItemForSkin(gun.GunId, sk.SkinId);
                        if (shopItem != null && (_account == null || !AccountStore.Owns(_account, shopItem)))
                        {
                            ShowToast("Compre esta skin na Dead Royal.");
                            break;
                        }
                        _equipped[gun.GunId] = sk.SkinId;
                        SaveEquippedToAccount();
                        ShowToast($"{sk.Name} equipada na {gun.Name}");
                        break;
                    }
            }
        }

        private void SetSquadSize(int n)
        {
            if (_map.Mode == "DeadRoyale")
            {
                _royaleSquad = Math.Clamp(n, 1, 4);
                ShowToast("Dead Royale: " + (_royaleSquad == 1 ? "SOLO" : _royaleSquad == 2 ? "DUPLA" : _royaleSquad == 3 ? "TRIO" : "SQUAD"));
            }
            else if (_map.Mode == "DealDuo")
            {
                _teamSize = n <= 1 ? 1 : 2;
                ShowToast("Deal Duo: " + (_teamSize == 1 ? "1v1" : "2v2"));
            }
        }

        private int SkinIdx(GunData g) =>
            _skinSel.TryGetValue(g.GunId, out var i) && i >= 0 && i < g.Skins.Count ? i : 0;

        // ---------- Entrada ----------
        private string? HitAt(Point p)
        {
            for (int i = _hits.Count - 1; i >= 0; i--)
                if (_hits[i].R.Contains(p)) return _hits[i].Id;
            return null;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Tab || keyData == Keys.Escape ||
                keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_loading)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (_chatOpen && e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SendChat();
                return;
            }
            if (_chatOpen && e.KeyCode == Keys.Escape)
            {
                _chatOpen = false;
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.T && !_chatOpen && (_page == 2 || _page == 3 || _page == 4 || _page == 7 || _page == 8 || _page == 9))
            {
                Activate("chatOpen");
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.P)
            {
                if (_page >= 1 && _page != 4) ToggleAdmin();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (e.KeyCode == Keys.F11) { ToggleMax(); e.Handled = true; return; }

            if (e.KeyCode == Keys.Escape)
            {
                if (_notifyUiOpen) { _notifyUiOpen = false; e.Handled = true; return; }
                if (_announceUiOpen) { _announceUiOpen = false; e.Handled = true; return; }
                if (_langConfirmOpen) { _langConfirmOpen = false; e.Handled = true; return; }
                if (_rankInfoOpen) { _rankInfoOpen = false; e.Handled = true; return; }
                if (_giveOpen) { _giveOpen = false; e.Handled = true; return; }
                if (_adminGmailOpen) { _adminGmailOpen = false; e.Handled = true; return; }
                if (_adminOpen) { CloseAdmin(); e.Handled = true; return; }
                if (_profileOpen) { CloseProfile(); e.Handled = true; return; }
                if (_addFriendOpen) { _addFriendOpen = false; e.Handled = true; return; }
                if (_notifOpen) { _notifOpen = false; e.Handled = true; return; }
                if (_cfgOpen) { _cfgOpen = false; e.Handled = true; return; }
            }

            if (_page == 1)
            {
                if (e.KeyCode == Keys.Enter) { Activate("login"); e.Handled = true; e.SuppressKeyPress = true; return; }
                if (e.KeyCode == Keys.Tab)
                {
                    if (_tbUser.Focused) _tbPass.Focus(); else _tbUser.Focus();
                    e.Handled = true; e.SuppressKeyPress = true;
                    return;
                }
            }
            if (_page == 6 && e.KeyCode == Keys.Enter)
            {
                Activate("registerCreate");
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            if (_page != 4 || _g == null) return;

            _keys.Add(e.KeyCode);
            if (e.KeyCode == Keys.Escape)
            {
                if (_storeOpen) _storeOpen = false;
                else if (_inventoryOpen) _inventoryOpen = false;
                else if (_g.Phase == SessionPhase.MatchOver) BackToLobby();
                else { _paused = !_paused; _firing = false; }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.OemQuotes || e.KeyCode == Keys.Oem7)
            {
                _inventoryOpen = !_inventoryOpen;
                _firing = false;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.R) { _g.TryReload(_g.Player); e.Handled = true; }
            else if (e.KeyCode == Keys.E)
            {
                var item = _g.TryPickup(_g.Player);
                if (item != null) ShowToast("Pegou: " + item.Label);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.H)
            {
                if (_g.UseMedkit(_g.Player)) ShowToast("Vida restaurada");
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.W || e.KeyCode == Keys.A || e.KeyCode == Keys.S || e.KeyCode == Keys.D ||
                     e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                e.Handled = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _keys.Remove(e.KeyCode);
            base.OnKeyUp(e);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            _firing = false;
            _keys.Clear();
            base.OnDeactivate(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _mouse = e.Location;
            _hover = HitAt(e.Location);
            Cursor = _page == 4 ? (_hover != null ? Cursors.Hand : Cursors.Cross)
                                : (_hover != null && !_hover.StartsWith("slot:")) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            MaximizedBounds = Screen.FromControl(this).Bounds;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = null;
            _firing = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (_loading) return;
            _mouse = e.Location;
            _down = e.Button == MouseButtons.Left ? HitAt(e.Location) : null;
            if (_page == 4 && _g != null && e.Button == MouseButtons.Left && _down == null &&
                !_paused && !_inventoryOpen && !_storeOpen && _g.Player.Alive)
                _firing = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_loading) return;
            _mouse = e.Location;
            if (e.Button == MouseButtons.Left) _firing = false;
            var up = e.Button == MouseButtons.Left ? HitAt(e.Location) : null;
            if (up != null && up == _down) Activate(up);
            _down = null;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_page == 7) _annScroll = Math.Max(0f, _annScroll - e.Delta / 120f * 64f);
            if (_page == 9) _shopScroll = Math.Max(0f, _shopScroll - e.Delta / 120f * 64f);
            if (_page >= 2 && _friendsOpen && FriendsRect(ClientSize.Width, ClientSize.Height).Contains(e.Location))
                _fscroll -= e.Delta / 120f * 60f;
        }

        // Arrastar/redimensionar a janela sem borda (WM_NCHITTEST)
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084)
            {
                long lp = m.LParam.ToInt64();
                var pt = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                bool onHit = _hits.Any(h => h.R.Contains(pt));
                if (!onHit && WindowState == FormWindowState.Normal)
                {
                    const int e = 8;
                    bool l = pt.X < e, r = pt.X >= ClientSize.Width - e, t = pt.Y < e, b = pt.Y >= ClientSize.Height - e;
                    int code = 0;
                    if (t && l) code = 13; else if (t && r) code = 14; else if (b && l) code = 16; else if (b && r) code = 17;
                    else if (l) code = 10; else if (r) code = 11; else if (t) code = 12; else if (b) code = 15;
                    if (code != 0) { m.Result = (IntPtr)code; return; }
                    if (_page != 4 && pt.Y >= 0 && pt.Y < BarH) { m.Result = (IntPtr)2; return; }   // HTCAPTION (só em janela)
                }
                m.Result = (IntPtr)1; return;                                                          // HTCLIENT
            }
            base.WndProc(ref m);
        }

        // ---------- Helpers de desenho ----------
        private static void FillRoundedRect(Graphics g, Rectangle r, Color bg, int rad = 10)
        {
            using var p = RoundRect(r, rad);
            using var b = new SolidBrush(bg);
            g.FillPath(b, p);
        }

        private static void DrawBtn(Graphics g, Rectangle r, Color bg, Color border, Color txt, Font f, string label, int rad = 12)
        {
            FillRoundedRect(g, r, bg, rad);
            using var p = RoundRect(r, rad);
            using var bp = new Pen(border, 2f);
            g.DrawPath(bp, p);
            var s = g.MeasureString(label, f);
            using var sb = new SolidBrush(txt);
            g.DrawString(label, f, sb, r.X + (r.Width - s.Width) / 2f, r.Y + (r.Height - s.Height) / 2f);
        }

        private Brush Br(Color c)
        {
            int k = c.ToArgb();
            if (!_br.TryGetValue(k, out var b)) _br[k] = b = new SolidBrush(c);
            return b;
        }

        private Pen Pn(Color c, float w)
        {
            var k = (c.ToArgb(), w);
            if (!_pn.TryGetValue(k, out var p)) _pn[k] = p = new Pen(c, w);
            return p;
        }

        private static Color A(Color c, int a) => Color.FromArgb(Math.Clamp(a, 0, 255), c);

        private static Color Mix(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
                                  (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        private static float Ease(float t) => 1f - (float)Math.Pow(1f - t, 3);
        private static int Stable(string s) { int h = 7; foreach (var ch in s) h = unchecked(h * 31 + ch); return h == int.MinValue ? 0 : Math.Abs(h); }

        private void Reg(Rectangle r, string id)
        {
            r.Offset(_off);
            _hits.Add(new Hit { R = r, Id = id });
        }

        private float Hv(string id)
        {
            if (!_anim.TryGetValue(id, out var v)) { _anim[id] = 0f; return 0f; }
            return v;
        }

        private static GraphicsPath Chamfer(RectangleF r, float c, bool tl = false, bool tr = true, bool br = false, bool bl = true)
        {
            var pts = new List<PointF>();
            pts.Add(tl ? new PointF(r.Left + c, r.Top) : new PointF(r.Left, r.Top));
            if (tr) { pts.Add(new PointF(r.Right - c, r.Top)); pts.Add(new PointF(r.Right, r.Top + c)); }
            else pts.Add(new PointF(r.Right, r.Top));
            if (br) { pts.Add(new PointF(r.Right, r.Bottom - c)); pts.Add(new PointF(r.Right - c, r.Bottom)); }
            else pts.Add(new PointF(r.Right, r.Bottom));
            if (bl) { pts.Add(new PointF(r.Left + c, r.Bottom)); pts.Add(new PointF(r.Left, r.Bottom - c)); }
            else pts.Add(new PointF(r.Left, r.Bottom));
            if (tl) pts.Add(new PointF(r.Left, r.Top + c));
            var p = new GraphicsPath();
            p.AddPolygon(pts.ToArray());
            return p;
        }

        private static GraphicsPath RoundRect(RectangleF r, float rad)
        {
            float d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void Txt(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment a = StringAlignment.Near)
            => g.DrawString(Loc.T(s), f, Br(c), r, a == StringAlignment.Near ? SfL : a == StringAlignment.Center ? SfC : SfR);

        private float CharW(Graphics g, char ch, Font f)
        {
            if (ch == ' ') return f.Size * 0.32f;
            if (!_cw.TryGetValue((f, ch), out var w))
                _cw[(f, ch)] = w = g.MeasureString(ch.ToString(), f, PointF.Empty, StringFormat.GenericTypographic).Width;
            return w;
        }

        // Texto com espaçamento entre letras (cy = centro vertical)
        private float Spaced(Graphics g, string s, Font f, Color c, float x, float cy, float sp, StringAlignment al = StringAlignment.Near)
        {
            s = Loc.T(s);
            var key = (s, f, sp);
            if (!_spacedCache.TryGetValue(key, out var ws))
            {
                ws = new float[s.Length];
                for (int i = 0; i < s.Length; i++) ws[i] = CharW(g, s[i], f);
                _spacedCache[key] = ws;
                if (_spacedCache.Count > 2000) _spacedCache.Clear();
            }
            float total = 0;
            for (int i = 0; i < ws.Length; i++) total += ws[i] + sp;
            total = Math.Max(0, total - sp);
            float px = al == StringAlignment.Center ? x - total / 2f : al == StringAlignment.Far ? x - total : x;
            float py = cy - f.GetHeight(g) / 2f;
            var br = Br(c);
            if (sp <= 2f)
            {
                g.DrawString(s, f, br, px, py, SfL);
                return total;
            }
            for (int i = 0; i < s.Length; i++)
            {
                g.DrawString(s[i].ToString(), f, br, px, py, StringFormat.GenericTypographic);
                px += ws[i] + sp;
            }
            return total;
        }

        // Botão no estilo Riot: cantos cortados na diagonal, moldura interna fina e brilho que atravessa ao passar o mouse
        private void Button(Graphics g, Rectangle r, string id, string label, bool primary, Font? f = null, float sp = 3f)
        {
            f ??= FBtn;
            Reg(r, id);
            float hv = Hv(id);
            using var path = Chamfer(r, 12, true, false, true, false);
            if (primary)
            {
                if (hv > 0.01f)
                    for (int i = 3; i >= 1; i--)
                    {
                        using var gp = Chamfer(Rectangle.Inflate(r, i * 4, i * 3), 12 + i * 2, true, false, true, false);
                        g.FillPath(Br(A(Red, (int)(24 * hv))), gp);
                    }
                g.FillPath(Br(Mix(Red, RedHi, hv)), path);
                using (var inner = Chamfer(Rectangle.Inflate(r, -5, -5), 8, true, false, true, false))
                    g.DrawPath(Pn(A(Color.White, 70), 1f), inner);
                if (hv > 0.01f)
                {
                    g.SetClip(path);
                    float sx = r.X - 40 + (r.Width + 80) * hv;
                    int al = (int)(70 * Math.Sin(Math.PI * hv));
                    g.FillPolygon(Br(A(Color.White, al)), new[]
                    {
                        new PointF(sx, r.Y), new PointF(sx + 34, r.Y), new PointF(sx + 4, r.Bottom), new PointF(sx - 30, r.Bottom)
                    });
                    g.ResetClip();
                }
                Spaced(g, label, f, Color.White, r.X + r.Width / 2f, r.Y + r.Height / 2f, sp, StringAlignment.Center);
            }
            else
            {
                g.FillPath(Br(A(Ink, (int)(hv * 22))), path);
                g.DrawPath(Pn(Mix(A(Ink, 110), Ink, hv), 2f), path);
                Spaced(g, label, f, Ink, r.X + r.Width / 2f, r.Y + r.Height / 2f, sp, StringAlignment.Center);
            }
        }

        private void Arrow(Graphics g, Rectangle r, int dir, float hv)
        {
            var pen = Pn(Mix(Dim, Ink, hv), 2f);
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            if (dir < 0)
                g.DrawLines(pen, new[] { new Point(cx + 4, cy - 7), new Point(cx - 4, cy), new Point(cx + 4, cy + 7) });
            else
                g.DrawLines(pen, new[] { new Point(cx - 4, cy - 7), new Point(cx + 4, cy), new Point(cx - 4, cy + 7) });
        }

        private void SizeBtn(Graphics g, Rectangle r, string id, string label, bool active)
        {
            Reg(r, id);
            float hv = Hv(id);
            using var p = Chamfer(r, 7, true, false, true, false);
            if (active) g.FillPath(Br(Mix(Red, RedHi, hv * .35f)), p);
            else
            {
                g.FillPath(Br(Mix(A(Panel, 235), A(PanelHi, 245), hv)), p);
                g.DrawPath(Pn(A(Line, 190), 1f), p);
            }
            Spaced(g, label, FLabel, active ? Color.White : Mix(Dim, Ink, hv), r.X + r.Width / 2f, r.Y + r.Height / 2f, 2f, StringAlignment.Center);
        }

        private void Avatar(Graphics g, float cx, float cy, float d, string name, bool dim)
        {
            var c = Pal[Stable(name) % Pal.Length];
            if (dim) c = Mix(c, Panel, 0.55f);
            g.FillEllipse(Br(c), cx - d / 2, cy - d / 2, d, d);
            Txt(g, name.Substring(0, 1).ToUpperInvariant(), FAvatar, dim ? A(Color.White, 150) : Color.White,
                new RectangleF(cx - d / 2, cy - d / 2, d, d), StringAlignment.Center);
        }

        private static Color StatusColor(FStatus s) => s switch
        {
            FStatus.InGame => Blue, FStatus.Online => Green, FStatus.Away => Amber, _ => OffC
        };

        private static Color SkinColor(Skin s)
        {
            var n = (s.Name + " " + s.SkinId).ToLowerInvariant();
            if (n.Contains("amber")) return Color.FromArgb(245, 170, 40);
            if (n.Contains("neon")) return Color.FromArgb(255, 45, 190);
            if (n.Contains("rust")) return Color.FromArgb(200, 95, 40);
            if (n.Contains("default")) return Color.FromArgb(150, 160, 172);
            return Pal[Stable(n) % Pal.Length];
        }

        // colchetes nos cantos (detalhe típico da UI)
        private void UiBrackets(Graphics g, Rectangle r, Color c, int len = 12, int gap = 6)
        {
            var pen = Pn(c, 1.5f);
            int l = r.Left - gap, t = r.Top - gap, rr = r.Right + gap, b = r.Bottom + gap;
            g.DrawLines(pen, new[] { new Point(l, t + len), new Point(l, t), new Point(l + len, t) });
            g.DrawLines(pen, new[] { new Point(rr - len, t), new Point(rr, t), new Point(rr, t + len) });
            g.DrawLines(pen, new[] { new Point(rr, b - len), new Point(rr, b), new Point(rr - len, b) });
            g.DrawLines(pen, new[] { new Point(l + len, b), new Point(l, b), new Point(l, b - len) });
        }

        private void UiLogoMark(Graphics g, float cx, float y, float size)
        {
            using var lp = Chamfer(new RectangleF(cx - size / 2, y, size, size), size * .25f, true, false, true, false);
            g.FillPath(Br(Red), lp);
            using var ip = Chamfer(new RectangleF(cx - size / 2 + 6, y + 6, size - 12, size - 12), size * .18f, true, false, true, false);
            g.DrawPath(Pn(A(Color.White, 90), 1.2f), ip);
            Txt(g, _custom.WindowTitle.Substring(0, 1).ToUpperInvariant(), VStart, Color.White,
                new RectangleF(cx - size / 2, y, size, size), StringAlignment.Center);
        }

        private void UiHex(Graphics g, float cx, float cy, float r, Color c)
        {
            var pts = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                double a = Math.PI / 3 * i + Math.PI / 6;
                pts[i] = new PointF(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r);
            }
            g.FillPolygon(Br(A(c, 40)), pts);
            g.DrawPolygon(Pn(c, 2f), pts);
        }

        private void EnsureBg(int w, int h)
        {
            if (_bgCache != null && _bgCache.Width == w && _bgCache.Height == h) return;
            _bgCache?.Dispose();
            _bgCache = new Bitmap(w, h);
            using var g = Graphics.FromImage(_bgCache);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var lg = new LinearGradientBrush(new Rectangle(0, 0, w, h), Bg, BgDeep, 90f))
                g.FillRectangle(lg, 0, 0, w, h);
            using var b1 = new SolidBrush(A(Red, 16));
            using var b2 = new SolidBrush(A(Red, 9));
            g.FillPolygon(b1, new[] { new PointF(w * .58f, 0), new PointF(w * .82f, 0), new PointF(w * .50f, h), new PointF(w * .26f, h) });
            g.FillPolygon(b2, new[] { new PointF(w * .80f, 0), new PointF(w * .88f, 0), new PointF(w * .58f, h), new PointF(w * .50f, h) });
            using var pen = new Pen(A(Ink, 6), 1);
            for (int x = -h; x < w; x += 46) g.DrawLine(pen, x, h, x + h, 0);
            // vinheta
            int vw = Math.Max(8, w / 4);
            using (var lv = new LinearGradientBrush(new Rectangle(0, 0, vw, h), A(Color.Black, 140), A(Color.Black, 0), 0f))
                g.FillRectangle(lv, 0, 0, vw, h);
            using (var rv = new LinearGradientBrush(new Rectangle(w - vw, 0, vw, h), A(Color.Black, 0), A(Color.Black, 140), 0f))
                g.FillRectangle(rv, w - vw, 0, vw, h);
            int vh = Math.Max(8, h / 5);
            using (var bv = new LinearGradientBrush(new Rectangle(0, h - vh, w, vh), A(Color.Black, 0), A(Color.Black, 120), 90f))
                g.FillRectangle(bv, 0, h - vh, w, vh);
        }

        // ---------- Pintura ----------
        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.InterpolationMode = InterpolationMode.Low;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            _hits.Clear();
            _off = Point.Empty;

            if (_page != 4)
            {
                EnsureBg(w, h);
                g.DrawImageUnscaled(_bgCache!, 0, 0);
                DrawTopBar(g, w);
                int sh = (int)(20 * _fade * _fade);     // o conteúdo sobe suavemente ao trocar de tela
                g.TranslateTransform(0, sh);
                _off = new Point(0, sh);
            }

            switch (_page)
            {
                case 0: DrawLoading(g, w, h); break;
                case 1: DrawLogin(g, w, h); break;
                case 2: DrawLobbyV2(g, w, h); break;
                case 3: DrawWeapons(g, w, h); break;
                case 4: DrawGame(g, w, h); break;
                case 5: DrawUpdatePage(g, w, h); break;
                case 6: DrawRegister(g, w, h); break;
                case 7: DrawAnnouncements(g, w, h); break;
                case 8: DrawCofre(g, w, h); break;
                case 9: DrawDeadRoyal(g, w, h); break;
            }

            g.ResetTransform();
            _off = Point.Empty;
            if (_fade > 0.003f) g.FillRectangle(Br(A(BgDeep, (int)(255 * _fade))), 0, 0, w, h);

            bool hub = _page == 2 || _page == 3 || _page == 7 || _page == 8 || _page == 9;
            if (hub) DrawSideNav(g, w, h);
            if (hub) DrawFriends(g, w, h);
            if (hub) DrawConfigPanel(g, w, h);
            if (_chatOpen && (_page == 2 || _page == 3 || _page == 4 || _page == 7 || _page == 8 || _page == 9)) DrawChat(g, w, h);
            if (_notifOpen) DrawNotifications(g, w, h);
            if (_profileOpen) DrawProfilePanel(g, w, h);
            if (_rankInfoOpen) DrawRankInfo(g, w, h);
            if (_langConfirmOpen) DrawLangConfirm(g, w, h);
            if (_pwdChangeOpen) DrawSecurityPassword(g, w, h);
            if (_addFriendOpen) DrawAddFriend(g, w, h);
            if (_adminOpen) DrawAdminPanel(g, w, h);
            if (_notifyUiOpen) DrawNotifyUi(g, w, h);
            if (_announceUiOpen) DrawAnnounceUi(g, w, h);
            if (_giveOpen) DrawGiveUi(g, w, h);
            if (_adminGmailOpen) DrawAdminGmailUi(g, w, h);
            DrawToast(g, w);
            if (_loading) DrawLoadingOverlay(g, w, h);
        }

        private void DrawTopBar(Graphics g, int w)
        {
            var bar = new Rectangle(0, 0, w, BarH);
            using (var lb = new LinearGradientBrush(bar, A(Color.FromArgb(8, 13, 20), 245), A(Color.FromArgb(8, 13, 20), 150), 90f))
                g.FillRectangle(lb, bar);
            g.DrawLine(Pn(A(Ink, 18), 1f), 0, BarH - 1, w, BarH - 1);

            // logo
            using (var lp = Chamfer(new RectangleF(32, 16, 32, 32), 8, true, false, true, false))
                g.FillPath(Br(Red), lp);
            var title = _custom.WindowTitle.ToUpperInvariant();
            Txt(g, title.Substring(0, 1), VNav, Color.White, new RectangleF(32, 16, 32, 32), StringAlignment.Center);
            Spaced(g, title, VLogo, Ink, 78, BarH / 2f, 5f);

            if (_page >= 2 && _page != 5 && _page != 6)
            {
                // perfil + notificações + configurações (antes dos botões da janela)
                int chipX = w - 440;
                var chip = new Rectangle(chipX, 8, 190, 48);
                Reg(chip, "accountOpen");
                float chv = Hv("accountOpen");
                if (chv > 0.01f)
                {
                    using var cp = RoundRect(chip, 10);
                    g.FillPath(Br(A(Color.White, (int)(18 * chv))), cp);
                }
                DrawAccountIcon(g, new RectangleF(chipX + 8, 15, 34, 34), AccountStore.Icon(_account?.IconId));
                Txt(g, (_account?.Nick ?? "CONVIDADO").ToUpperInvariant(), FMid, Ink,
                    new RectangleF(chipX + 50, 11, 132, 20));
                g.FillEllipse(Br(_account == null ? OffC : Green), chipX + 51, 35, 7, 7);
                Txt(g, _online ? "ONLINE • " + (_onlineNick.Length > 0 ? _onlineNick : _account?.Nick ?? "")
                        : (_account == null ? "No account" : "Local • " + _account.Region), FSmall,
                    _account == null ? Dim : (_online ? Blue : Green), new RectangleF(chipX + 62, 29, 120, 18));

                UiIconBtn(g, new Rectangle(w - 236, 12, 40, 40), "notif", 0);
                if (_account != null)
                {
                    int unread = AccountStore.UnreadCount(_account);
                    if (unread > 0)
                    {
                        var badge = new Rectangle(w - 236 + 23, 10, 18, 18);
                        g.FillEllipse(Br(Red), badge);
                        Txt(g, unread > 9 ? "9+" : unread.ToString(), FTiny, Color.White, badge, StringAlignment.Center);
                    }
                }
                UiIconBtn(g, new Rectangle(w - 188, 12, 40, 40), "cfg", 1);
                g.DrawLine(Pn(A(Ink, 30), 1f), w - 138, BarH / 2 - 11, w - 138, BarH / 2 + 11);
            }

            // botões da janela: só o icon fino, afastados da borda
            UiIconBtn(g, new Rectangle(w - 120, 12, 40, 40), "min", 2);
            UiIconBtn(g, new Rectangle(w - 72, 12, 40, 40), "close", 3);
        }

        // Navegação principal: fica só na parte de baixo, presente em todas as telas do lobby.
        // Única navegação do lobby: lateral esquerda, vertical.
        private void DrawSideNav(Graphics g, int w, int h)
        {
            string[] labels = { "LOBBY", "WEAPONS", "ANNOUNCEMENTS", "VAULT", "DEAD ROYAL" };
            string[] pages = { "Lobby", "Weapons", "Announcements", "Cofre", "DeadRoyal" };
            int[] indexes = { 2, 3, 7, 8, 9 };
            int bx = 32, by = BarH + 34, bw = 190, bh = 48, gap = 10;
            Spaced(g, "MENU", FTiny, Dim, bx + 4, by - 18, 3f);
            for (int i = 0; i < labels.Length; i++)
            {
                bool active = _page == indexes[i] || (_page == 3 && indexes[i] == 2);
                Button(g, new Rectangle(bx, by + i * (bh + gap), bw, bh),
                    "nav:" + pages[i], labels[i], active, FSmallB, 2.2f);
            }
        }

        // Carregamento exibido ao entrar no jogo, iniciar partida e voltar ao lobby.
        private void DrawLoadingOverlay(Graphics g, int w, int h)
        {
            g.FillRectangle(Br(A(Color.FromArgb(5, 8, 12), 250)), 0, 0, w, h);
            int cx = w / 2;
            UiLogoMark(g, cx, h / 2f - 196, 64);
            Spaced(g, _loadingTitle, VTitle, Ink, cx, h / 2f - 100, 8f, StringAlignment.Center);

            var bar = new Rectangle(cx - 260, h / 2 - 18, 520, 5);
            g.FillRectangle(Br(A(Ink, 28)), bar);
            float p = _loadingTotal > 0 ? 1f - Math.Clamp(_loadingLeft / _loadingTotal, 0f, 1f) : 1f;
            g.FillRectangle(Br(Red), bar.X, bar.Y, (int)(bar.Width * p), bar.Height);

            if (_groupMembers.Count == 0) ResetGroup();
            Spaced(g, "TEAM MEMBERS", FLabel, Red, cx, h / 2f + 36, 4f, StringAlignment.Center);
            int n = Math.Clamp(_groupMembers.Count, 1, 4);
            int cw = 176, gap = 12;
            int total = n * cw + (n - 1) * gap;
            int x = (w - total) / 2;
            for (int i = 0; i < n; i++)
            {
                var r = new Rectangle(x + i * (cw + gap), h / 2 + 62, cw, 78);
                using (var path = Chamfer(r, 10, true, false, true, false))
                {
                    g.FillPath(Br(A(Panel, 240)), path);
                    g.DrawPath(Pn(A(Line, 210), 1f), path);
                }
                DrawAccountIcon(g, new RectangleF(r.X + 12, r.Y + 16, 46, 46),
                    AccountStore.Icon(i == 0 ? _account?.IconId : null));
                Txt(g, _groupMembers[i].ToUpperInvariant(), FMid, Ink,
                    new RectangleF(r.X + 68, r.Y + 18, r.Width - 78, 20));
                Spaced(g, i == 0 ? "LEADER" : "MEMBER", FTiny, i == 0 ? Red : Green, r.X + 68, r.Y + 42, 2f);
                Spaced(g, "READY", FTiny, A(Dim, 190), r.X + 68, r.Y + 58, 2f);
            }
            Txt(g, "Deadzone v" + AppInfo.Versao, FSmall, A(Dim, 170),
                new RectangleF(0, h - 54, w, 20), StringAlignment.Center);
        }

        // Compatibilidade com o GameRenderer.cs antigo (DrawGameWindowButtons chama WinBtn com kind 0=min, 1=max, 2=fechar).
        // Dentro da partida só aparecem minimizar e fechar (F11 alterna a tela cheia), com o mesmo visual do menu.
        private void WinBtn(Graphics g, Rectangle r, string id, int kind)
        {
            if (kind == 1) return;
            UiIconBtn(g, new Rectangle(kind == 0 ? r.X + 18 : r.X - 26, 12, 40, 40), id, kind == 0 ? 2 : 3);
        }

        // kind: 0=sino 1=engrenagem 2=minimizar 3=fechar
        private void UiIconBtn(Graphics g, Rectangle r, string id, int kind)
        {
            Reg(r, id);
            float hv = Hv(id);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            g.FillEllipse(Br(kind == 3 ? A(Red, (int)(38 * hv)) : A(Color.White, (int)(22 * hv))), cx - 18, cy - 18, 36, 36);
            var col = kind == 3 ? Mix(A(Ink, 210), Red, hv) : Mix(A(Ink, 190), Color.White, hv);
            var pen = Pn(col, 1.6f);
            switch (kind)
            {
                case 0:
                    g.DrawArc(pen, cx - 6, cy - 9, 12, 12, 180, 180);
                    g.DrawLine(pen, cx - 6, cy - 3, cx - 8, cy + 4);
                    g.DrawLine(pen, cx + 6, cy - 3, cx + 8, cy + 4);
                    g.DrawLine(pen, cx - 8, cy + 4, cx + 8, cy + 4);
                    g.DrawArc(pen, cx - 2.5f, cy + 5, 5, 4, 0, 180);
                    break;
                case 1:
                    g.DrawEllipse(pen, cx - 6, cy - 6, 12, 12);
                    g.DrawEllipse(pen, cx - 2.5f, cy - 2.5f, 5, 5);
                    for (int k = 0; k < 8; k++)
                    {
                        double a = k * Math.PI / 4;
                        g.DrawLine(pen, cx + (float)Math.Cos(a) * 6, cy + (float)Math.Sin(a) * 6,
                                        cx + (float)Math.Cos(a) * 9, cy + (float)Math.Sin(a) * 9);
                    }
                    break;
                case 2:
                    g.DrawLine(pen, cx - 7, cy, cx + 7, cy);
                    break;
                default:
                    g.DrawLine(pen, cx - 6, cy - 6, cx + 6, cy + 6);
                    g.DrawLine(pen, cx + 6, cy - 6, cx - 6, cy + 6);
                    break;
            }
        }

        // ---------- Loading ----------
        private void DrawLoading(Graphics g, int w, int h)
        {
            float cy = h / 2f;
            UiLogoMark(g, w / 2f, cy - 190, 64);
            Spaced(g, _custom.WindowTitle.ToUpperInvariant(), VHuge, Ink, w / 2f, cy - 70, 16f, StringAlignment.Center);

            var sp = new RectangleF(w / 2f - 14, cy + 8, 28, 28);
            g.DrawEllipse(Pn(A(Ink, 25), 3f), sp);
            using (var pen = new Pen(Red, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(pen, sp, (_clock * 360f) % 360f, 100f);

            int bw = 460, bx = (w - bw) / 2, by = (int)cy + 70;
            g.FillRectangle(Br(A(Ink, 28)), bx, by, bw, 4);
            int fw = (int)(bw * Math.Clamp(_progress, 0f, 100f) / 100f);
            if (fw > 0)
            {
                g.FillRectangle(Br(A(Red, 50)), bx, by - 3, fw, 10);
                g.FillRectangle(Br(Red), bx, by, fw, 4);
            }
            string status = _progress < 30 ? "Starting services" : _progress < 60 ? "Loading weapons"
                          : _progress < 90 ? "Syncing profile" : "Almost there";
            Spaced(g, status.ToUpperInvariant(), FLabel, Dim, bx, by + 24, 2f);
            Txt(g, $"{(int)_progress}%", FSmallB, Ink, new RectangleF(bx, by + 14, bw, 20), StringAlignment.Far);
            Txt(g, "v" + AppInfo.Versao, FSmall, A(Dim, 160), new RectangleF(32, h - 44, 120, 20));
        }

        // ---------- Login (sem caixa, como o Riot Client) ----------
        private void LoadLoginControls()
        {
            var tf = new Font(Fam, 17f, FontStyle.Regular, GraphicsUnit.Pixel);
            foreach (var tb in new[] { _tbUser, _tbPass })
            {
                tb.Parent = this;
                tb.BorderStyle = BorderStyle.None;
                tb.BackColor = FieldBg;
                tb.ForeColor = Ink;
                tb.Font = tf;
                tb.TextAlign = HorizontalAlignment.Left;
                tb.Visible = _page == 1;
            }
            _tbPass.UseSystemPasswordChar = true;
            if (_page == 1) _tbUser.Focus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            InitAccountControls();
            LoadLoginControls();
        }

        private void SaveLastUser(string name)
        {
            try
            {
                var json = JsonSerializer.Serialize(new { User = name }, JsonOpts);
                File.WriteAllText(Path.Combine(_appDir, "Config", "LastUser.json"), json);
            }
            catch { }
        }

        private string LoadLastUser()
        {
            try
            {
                var p = Path.Combine(_appDir, "Config", "LastUser.json");
                if (!File.Exists(p)) return "";
                using var doc = JsonDocument.Parse(File.ReadAllText(p));
                return doc.RootElement.TryGetProperty("User", out var u) ? u.GetString() ?? "" : "";
            }
            catch { return ""; }
        }

        // campo estilo Riot: rótulo pequeno em cima, valor embaixo, linha vermelha quando está com foco
        private void UiField(Graphics g, Rectangle f, string label, TextBox tb)
        {
            g.FillRectangle(Br(FieldBg), f);
            g.DrawRectangle(Pn(A(Line, 200), 1f), f);
            Spaced(g, label, FTiny, Dim, f.X + 18, f.Y + 15, 3f);
            var loc = new Point(f.X + 18, f.Y + 29);
            if (tb.Location != loc) tb.Location = loc;
            if (tb.Width != f.Width - 36) tb.Width = f.Width - 36;
            g.FillRectangle(Br(tb.Focused ? Red : A(Line, 200)), f.X, f.Bottom - 2, f.Width, 2);
        }

        private void DrawLogin(Graphics g, int w, int h)
        {
            int cx = w / 2;
            int top = Math.Max(BarH + 20, (h - 600) / 2);

            UiLogoMark(g, cx, top, 56);
            Spaced(g, "SIGN IN", VTitle, Ink, cx, top + 96, 8f, StringAlignment.Center);
            Txt(g, "Use your Google account or confirm your Gmail with the code sent.", FSmall, Dim,
                new RectangleF(cx - 280, top + 126, 560, 20), StringAlignment.Center);

            var uf = new Rectangle(cx - 190, top + 150, 380, 50);
            var pf = new Rectangle(cx - 190, top + 210, 380, 50);
            UiField(g, uf, "GMAIL (@GMAIL.COM)", _tbUser);
            UiField(g, pf, "PASSWORD", _tbPass);

            UiField(g, new Rectangle(cx - 190, top + 270, 240, 46), "GMAIL CODE", _tbLoginCode);
            Button(g, new Rectangle(cx - 190 + 252, top + 270, 128, 46), "loginSendCode", "SEND CODE", false, FTiny, 1.5f);
            if (!string.IsNullOrEmpty(_loginCodeHint))
                Txt(g, _loginCodeHint, FTiny, MailService.IsConfigured ? Green : Amber,
                    new RectangleF(cx - 190, top + 320, 380, 16));

            // botão redondo com seta
            var ab = new Rectangle(cx - 28, top + 340, 56, 56);
            Reg(ab, "login"); float hv = Hv("login");
            if (hv > 0.01f)
                for (int i = 3; i >= 1; i--)
                    g.FillEllipse(Br(A(Red, (int)(26 * hv))), ab.X - i * 4, ab.Y - i * 4, ab.Width + i * 8, ab.Height + i * 8);
            g.FillEllipse(Br(Mix(Red, RedHi, hv)), ab);
            var ap = Pn(Color.White, 3f);
            float ax = cx - 8 + hv * 3, ay = top + 368;
            g.DrawLine(ap, ax - 8, ay, ax + 12, ay);
            g.DrawLines(ap, new[] { new PointF(ax + 4, ay - 8), new PointF(ax + 12, ay), new PointF(ax + 4, ay + 8) });

            Button(g, new Rectangle(cx - 170, top + 404, 340, 36), "googleSignIn", "SIGN IN WITH GOOGLE", false, FLabel, 2.2f);
            Button(g, new Rectangle(cx - 170, top + 446, 340, 36), "onlineLogin", "SIGN IN ONLINE", true, FLabel, 2.5f);
            Button(g, new Rectangle(cx - 170, top + 488, 340, 36), "register", "CREATE LOCAL ACCOUNT", false, FLabel, 2.2f);
            if (AccountStore.Accounts.Count > 0)
                Txt(g, $"{AccountStore.Accounts.Count} local account(s) on this PC", FTiny, A(Dim, 170),
                    new RectangleF(0, top + 534, w, 18), StringAlignment.Center);

            Txt(g, "Tab alterna os campos • Enter entra", FTiny, A(Dim, 150), new RectangleF(0, h - 64, w, 18), StringAlignment.Center);
            Txt(g, "v" + AppInfo.Versao, FSmall, A(Dim, 160), new RectangleF(32, h - 44, 120, 20));
        }

        // ---------- Atualização ----------
        private void DrawUpdatePage(Graphics g, int w, int h)
        {
            var info = UpdateService.Available;
            string version = info?.Version ?? "";
            var card = new Rectangle((w - 720) / 2, Math.Max(BarH + 30, (h - 360) / 2), 720, 360);
            for (int i = 4; i >= 1; i--) g.FillRectangle(Br(A(Color.Black, 14)), Rectangle.Inflate(card, i * 4, i * 4));
            using (var p = Chamfer(card, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 250)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), card.X + 18, card.Y, card.Width - 18, 3);

            Spaced(g, "UPDATE AVAILABLE", FLabel, Red, card.X + 40, card.Y + 36, 4f);
            Txt(g, "NEW VERSION", VSub, Ink, new RectangleF(card.X + 40, card.Y + 52, card.Width - 80, 52));
            Txt(g, UpdateService.LocalVersion + " > " + version + " (Latest)", FMid, Dim,
                new RectangleF(card.X + 40, card.Y + 106, card.Width - 80, 24));
            if (!string.IsNullOrEmpty(info?.Notes))
                g.DrawString(info!.Notes, FBody, Br(A(Ink, 200)), new RectangleF(card.X + 40, card.Y + 140, card.Width - 80, 56), SfWrap);

            // barra de progresso fina, estilo loader do DeadTheme
            var bar = new Rectangle(card.X + 40, card.Y + 214, card.Width - 80, 4);
            g.FillRectangle(Br(A(Ink, 28)), bar);
            float prog = Math.Clamp(UpdateService.DownloadProgress, 0f, 1f);
            if (prog > 0f)
            {
                g.FillRectangle(Br(A(Red, 50)), bar.X, bar.Y - 3, (int)(bar.Width * prog), 10);
                g.FillRectangle(Br(Red), bar.X, bar.Y, (int)(bar.Width * prog), 4);
            }
            if (UpdateService.DownloadFailed)
                Spaced(g, "DOWNLOAD FAILED - TRY AGAIN", FTiny, HpRed, card.X + 40, card.Y + 236, 3f);
            else if (UpdateService.InstallerStarted)
                Spaced(g, "OPENING INSTALLER...", FTiny, Green, card.X + 40, card.Y + 236, 3f);

            string installLabel = UpdateService.InstallerStarted ? "OPENING INSTALLER"
                : _updInstallRequested ? $"DOWNLOADING {(int)(prog * 100)}%" : "UPDATE NOW";
            int by = card.Bottom - 92;
            Button(g, new Rectangle(card.X + 40, by, 250, 56), "updateInstall", installLabel, true, VBtn, 3f);
            Button(g, new Rectangle(card.X + 306, by, 200, 56), "updateSoft", "NO REINSTALL", false, FLabel, 2.5f);
            Button(g, new Rectangle(card.X + 522, by, 158, 56), "updateLater", "LATER", false, FLabel, 2.5f);

            if (UpdateService.IgnoreVersion == UpdateService.Available?.Version)
                Spaced(g, "THIS VERSION WILL BE IGNORED", FTiny, A(Dim, 200), card.X + 40, card.Bottom - 22, 2f);
            else
            {
                var ig = new Rectangle(card.X + 40, card.Bottom - 34, 140, 22);
                Reg(ig, "updateIgnore"); float ih = Hv("updateIgnore");
                Spaced(g, "IGNORAR 1H", FTiny, Mix(Dim, Ink, ih), card.X + 40, card.Bottom - 22, 3f);
            }
        }

        // ---------- Configurações (janela modal) ----------
        private void DrawConfigPanel(Graphics g, int w, int h)
        {
            if (!_cfgOpen) return;
            g.FillRectangle(Br(A(Color.Black, 185)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "cfgBackdrop");

            int pw = Math.Min(920, w - 60), ph = Math.Min(620, h - 40);
            var r = new Rectangle((w - pw) / 2, (h - ph) / 2, pw, ph); // sempre no centro
            Reg(r, "cfgPanel");
            using (var p = Chamfer(r, 16, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(A(Line, 220), 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 18, r.Y, r.Width - 18, 3);
            Spaced(g, "SETTINGS", FLabel, Red, r.X + 28, r.Y + 24, 4f);
            Txt(g, "CONFIGURATION", VSub, Ink, new RectangleF(r.X + 28, r.Y + 36, pw - 100, 44));
            var close = new Rectangle(r.Right - 48, r.Y + 16, 30, 30);
            Reg(close, "cfg");
            var xp = Pn(Mix(Dim, Ink, Hv("cfg")), 1.8f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            // menu lateral
            int sx = r.X + 24, sw = 210, sy = r.Y + 96;
            string[] sections = { "ACCOUNT", "SECURITY", "OTHERS" };
            for (int i = 0; i < sections.Length; i++)
                Button(g, new Rectangle(sx, sy + i * 56, sw, 46), "cfgSection:" + i,
                    sections[i], _cfgSection == i, FSmallB, 2.5f);

            int cx = r.X + 258, cw = pw - 282, cy = r.Y + 96;
            g.DrawLine(Pn(A(Line, 140), 1f), sx + sw + 12, r.Y + 90, sx + sw + 12, r.Bottom - 24);

            if (_cfgSection == 0)
            {
                Spaced(g, "ACCOUNT INFORMATION", FLabel, Dim, cx, cy, 3f);
                int y = cy + 26;
                InfoLine(g, cx, y, cw, "NICK", _account?.Nick ?? "-"); y += 34;
                InfoLine(g, cx, y, cw, "DISPLAY NAME", _account?.DisplayName ?? "-"); y += 34;
                InfoLine(g, cx, y, cw, "GMAIL", _account?.Gmail ?? "-"); y += 34;
                InfoLine(g, cx, y, cw, "USER ID", _account?.UserId ?? "-"); y += 34;
                InfoLine(g, cx, y, cw, "LOGIN METHOD", _online ? "Google / Online" : "Local account"); y += 34;
                InfoLine(g, cx, y, cw, "CREATED", _account == null ? "-" : AccountStore.CreationText(_account)); y += 34;
                InfoLine(g, cx, y, cw, "ACCOUNT AGE", _account == null ? "-" : AccountStore.AgeText(_account.CreatedAt)); y += 34;
                Button(g, new Rectangle(cx, r.Bottom - 82, 240, 44), "profileLogout", "LOGOUT", true, FLabel, 2.5f);
            }
            else if (_cfgSection == 1)
            {
                Spaced(g, "SECURITY SETTINGS", FLabel, Dim, cx, cy, 3f);
                Button(g, new Rectangle(cx, cy + 24, cw, 52), "securityPassword", "CHANGE PASSWORD", false, FLabel, 2.5f);
                Txt(g, "Requires a verification code sent to your Gmail.", FSmall, Dim,
                    new RectangleF(cx, cy + 80, cw, 20));
                g.DrawLine(Pn(A(Line, 140), 1f), cx, cy + 112, cx + cw, cy + 112);
                Spaced(g, "TWO-FACTOR AUTHENTICATION", FLabel, Dim, cx, cy + 132, 3f);
                Button(g, new Rectangle(cx, cy + 158, 240, 46), "security2fa",
                    _account != null && _account.TwoFactor ? "ENABLED - TAP TO DISABLE" : "DISABLED - TAP TO ENABLE",
                    _account != null && _account.TwoFactor, FTiny, 1.5f);
                Txt(g, "When enabled, sign-in requires a Gmail code.", FSmall, Dim,
                    new RectangleF(cx, cy + 216, cw, 20));
            }
            else
            {
                Spaced(g, "APPLICATION", FLabel, Dim, cx, cy, 3f);
                int y = cy + 26;
                InfoLine(g, cx, y, cw, "VERSION", "v" + AppInfo.Versao); y += 34;
                InfoLine(g, cx, y, cw, "PUBLISHER", AppInfo.EmpresaFundadora + " INC"); y += 34;
                InfoLine(g, cx, y, cw, "THEME", AppInfo.Tema); y += 34;
                InfoLine(g, cx, y, cw, "PLATFORM", "Windows x64 / .NET 8"); y += 34;
                InfoLine(g, cx, y, cw, "SERVER", OnlineClient.Config.ServerUrl); y += 34;

                Spaced(g, "LANGUAGE", FLabel, Dim, cx, y, 3f);
                string[] langs = { "en-US", "pt-BR", "es-ES" };
                for (int i = 0; i < langs.Length; i++)
                    SizeBtn(g, new Rectangle(cx + i * 106, y + 18, 100, 28), "lang:" + i, langs[i], _langSelected == i);
                Button(g, new Rectangle(cx + 326, y + 18, cw - 326, 28), "langSave", "SAVE LANGUAGE", false, FTiny, 1.2f);
                y += 62;

                Button(g, new Rectangle(cx, y, 240, 42), "viewUpd", "CHECK UPDATE", false, FLabel, 2.5f);
                Button(g, new Rectangle(cx + 252, y, 180, 42), "fpsToggle", _fpsVisible ? "FPS: ON" : "FPS: OFF", false, FLabel, 2.5f);
                y += 52;
                Button(g, new Rectangle(cx, y, cw, 42), "playWithToggle",
                    _playWithMobile ? "PLAY WITH MOBILE" : "PLAY WITH COMPUTER", false, FLabel, 2.5f);
                y += 56;
                UiToggleRow(g, cx, y, cw, "PERFORMANCE MODE", _cfgOpt ? "ON" : "OFF", "cfgOpt", _cfgOpt);
            }
        }

        private void DrawConfigPanelOld(Graphics g, int w, int h)
        {
            if (!_cfgOpen) return;
            g.FillRectangle(Br(A(Color.Black, 150)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "cfgBackdrop");

            int pw = 560, ph = 682;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 20, (h - ph) / 2), pw, ph);
            Reg(r, "cfgPanel");   // clicar dentro do painel não fecha
            using (var p = Chamfer(r, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 18, r.Y, r.Width - 18, 3);
            Spaced(g, "AJUSTES", FLabel, Red, r.X + 40, r.Y + 36, 4f);
            Txt(g, "SETTINGS", VSub, Ink, new RectangleF(r.X + 40, r.Y + 52, r.Width - 80, 52));

            int rowH = 62, y = r.Y + 120, rx = r.X + 40, rw = r.Width - 80;

            // volume
            Spaced(g, "VOLUME", FLabel, Dim, rx, y + 14, 3f);
            Txt(g, _cfgVol + "%", FMid, Ink, new RectangleF(rx + rw - 40, y + 4, 40, 20), StringAlignment.Far);
            _uiVolBar = new Rectangle(rx, y + 34, rw - 100, 6);
            Reg(Rectangle.Inflate(_uiVolBar, 0, 10), "cfgVolBar");
            g.FillRectangle(Br(A(Ink, 30)), _uiVolBar);
            int fill = (int)(_uiVolBar.Width * _cfgVol / 100f);
            g.FillRectangle(Br(Red), _uiVolBar.X, _uiVolBar.Y, fill, _uiVolBar.Height);
            g.FillEllipse(Br(Color.White), _uiVolBar.X + fill - 7, _uiVolBar.Y - 4, 14, 14);
            Button(g, new Rectangle(rx + rw - 84, y + 24, 38, 28), "cfgVolDn", "−", false, FMid, 1f);
            Button(g, new Rectangle(rx + rw - 42, y + 24, 38, 28), "cfgVolUp", "+", false, FMid, 1f);
            y += rowH;

            Spaced(g, "LANGUAGE", FLabel, Dim, rx, y + 4, 3f);
            string[] langLabels = { "en-US", "pt-BR", "es-ES" };
            for (int i = 0; i < langLabels.Length; i++)
                SizeBtn(g, new Rectangle(rx + i * 106, y + 16, 100, 30), "lang:" + i, langLabels[i], _langSelected == i);
            Button(g, new Rectangle(rx + 326, y + 16, rw - 326, 30), "langSave", "SAVE LANGUAGE", false, FTiny, 1.5f);
            y += rowH;

            UiField(g, new Rectangle(rx, y + 12, rw - 180, 44), "SERVER URL", _tbServerUrl);
            Button(g, new Rectangle(rx + rw - 170, y + 14, 170, 36), "serverSave", "SAVE URL", false, FTiny, 1.5f);
            y += rowH;
            UiToggleRow(g, rx, y, rw, "PERFORMANCE MODE", _cfgOpt ? "ON" : "DESON", "cfgOpt", _cfgOpt);
            y += rowH;

            Spaced(g, "VERSION", FLabel, Dim, rx, y + 14, 3f);
            Txt(g, "v" + AppInfo.Versao, FMid, Ink, new RectangleF(rx, y + 26, 200, 24));
            Button(g, new Rectangle(rx + rw - 200, y + 10, 200, 36), "viewUpd", "CHECK UPDATE", false, FLabel, 2.5f);
            y += rowH;

            Spaced(g, "ACCOUNT", FLabel, Dim, rx, y + 14, 3f);
            Txt(g, _account?.Nick ?? "No account", FMid, Ink, new RectangleF(rx, y + 26, 260, 24));
            Button(g, new Rectangle(rx + rw - 200, y + 10, 200, 36), "profileLogout", "LOGOUT", false, FLabel, 2.5f);
            y += rowH;

            Spaced(g, "LOGIN COM GOOGLE", FLabel, Dim, rx, y + 4, 3f);
            Spaced(g, GoogleAuth.IsConfigured ? "CONFIGURADO" : "NAO CONFIGURADO", FTiny,
                GoogleAuth.IsConfigured ? Green : Dim, rx, y + 30, 2f);

            Button(g, new Rectangle(r.X + 40, r.Bottom - 62, 150, 42), "cfg", "CLOSE", true, FLabel, 3f);
        }

        private void UiToggleRow(Graphics g, int x, int y, int w, string label, string value, string id, bool on)
        {
            var row = new Rectangle(x, y, w, 52);
            Reg(row, id);
            float hv = Hv(id);
            g.FillRectangle(Br(A(Color.White, (int)(8 * hv))), row);
            g.DrawLine(Pn(A(Line, 150), 1f), x, y + 52, x + w, y + 52);
            Spaced(g, label, FLabel, Dim, x, y + 14, 3f);
            Txt(g, value, FMid, Ink, new RectangleF(x, y + 24, 260, 24));
            // chave liga/desliga
            var sw = new Rectangle(x + w - 56, y + 14, 48, 24);
            using (var p = RoundRect(sw, 12)) g.FillPath(Br(on ? Red : A(Ink, 40)), p);
            g.FillEllipse(Br(Color.White), sw.X + (on ? 26 : 4), sw.Y + 4, 16, 16);
        }

        // ---------- Lobby ----------
        private void DrawLobby(Graphics g, int w, int h)
        {
            bool isRoyale = _map.Mode == "DeadRoyale";
            bool isDeal = _map.Mode == "DealDuo";
            int cx = w / 2;

            // saldo de Dead Coins no topo do lobby
            DrawCoinsChip(g, new Rectangle(w - 48 - 238, BarH + 22, 238, 46), "nav:DeadRoyal");

            // seletor de mapa/modo (topo central)
            var mode = new Rectangle(cx - 230, BarH + 22, 460, 64);
            Reg(mode, "mode"); float mh = Hv("mode");
            using (var p = Chamfer(mode, 12, true, false, true, false))
            {
                g.FillPath(Br(Mix(A(Panel, 235), A(PanelHi, 245), mh * .6f)), p);
                g.DrawPath(Pn(Mix(Line, Red, mh), 1f), p);
            }
            var prev = new Rectangle(mode.X + 6, mode.Y + 12, 36, 40);
            var next = new Rectangle(mode.Right - 42, mode.Y + 12, 36, 40);
            Reg(prev, "mapPrev"); Reg(next, "mapNext");
            Arrow(g, prev, -1, Hv("mapPrev"));
            Arrow(g, next, 1, Hv("mapNext"));
            UiHex(g, mode.X + 74, mode.Y + 32, 18, Red);
            Spaced(g, $"MAPA {_mapIndex + 1}/{_maps.Count}", FTiny, Red, mode.X + 108, mode.Y + 15, 3f);
            Txt(g, _map.Name.ToUpperInvariant(), VName, Ink, new RectangleF(mode.X + 108, mode.Y + 20, 290, 28));
            Txt(g, _map.Type, FSmall, Dim, new RectangleF(mode.X + 108, mode.Y + 45, 290, 16));
            for (int i = 0; i < _maps.Count; i++)
                g.FillEllipse(Br(i == _mapIndex ? Red : A(Line, 220)), mode.Right - 56 - i * 12, mode.Y + 8, 6, 6);

            // tamanho de equipe (uma única linha)
            int under = mode.Bottom;
            if (isRoyale || isDeal)
            {
                string[] labels = isRoyale ? new[] { "SOLO", "DUPLA", "TRIO", "SQUAD" } : new[] { "1V1", "2V2" };
                int bw = 84, gap2 = 8;
                int total = labels.Length * bw + (labels.Length - 1) * gap2;
                int sx = (w - total) / 2, sy = mode.Bottom + 10;
                for (int i = 0; i < labels.Length; i++)
                {
                    bool active = isRoyale ? _royaleSquad == i + 1 : _teamSize == i + 1;
                    SizeBtn(g, new Rectangle(sx + i * (bw + gap2), sy, bw, 30), "size" + (i + 1), labels[i], active);
                }
                under = sy + 30;
            }
            else
            {
                Spaced(g, "SQUAD 4V4", FLabel, Dim, cx, mode.Bottom + 22, 3f, StringAlignment.Center);
                under = mode.Bottom + 36;
            }

            // INICIAR grande
            var btn = new Rectangle(cx - 170, under + 16, 340, 70);
            Button(g, btn, "start", "START", true, VStart, 8f);

            // squad só existe depois que o jogador cria uma (nada é criado no login)
            Button(g, new Rectangle(cx - 92, btn.Bottom + 8, 184, 34),
                _squadOpen ? "squadLeave" : "squadCreate",
                _squadOpen ? "(-) Leave Group" : "(+) Create Group", false, FTiny, 1.8f);
            if (_online)
                Button(g, new Rectangle(cx + 100, btn.Bottom + 8, 184, 34), "matchFind",
                    _queueing ? "CANCEL SEARCH" : "FIND MATCH", true, FTiny, 1.8f);
            DrawSquadRow(g, w, btn.Bottom + 50);

            if (!string.IsNullOrEmpty(_updateNotice))
            {
                var nr = new Rectangle(cx - 230, BarH + 96, 460, 40);
                using (var np = Chamfer(nr, 8, true, false, true, false))
                {
                    g.FillPath(Br(A(Red, 35)), np);
                    g.DrawPath(Pn(Red, 1.2f), np);
                }
                Txt(g, _updateNotice, FTiny, Ink, new RectangleF(nr.X + 12, nr.Y, nr.Width - 130, 40));
                Button(g, new Rectangle(nr.Right - 122, nr.Y + 4, 114, 32), "viewUpd", "UPDATE", true, FTiny, 1.5f);
            }
        }

        private void DrawPartyCard(Graphics g, Rectangle rect, int pi, bool center)
        {
            string id = "slot:" + pi;
            Reg(rect, id); float hv = Hv(id);
            rect.Y -= (int)(6 * hv);
            bool me = pi == 0;

            using (var path = Chamfer(rect, 16, true, false, true, false))
            {
                using (var lb = new LinearGradientBrush(rect, A(PanelHi, 235), A(Panel, 235), 90f)) g.FillPath(lb, path);
                g.DrawPath(Pn(Mix(Line, Red, hv * .8f), 1f), path);
            }
            g.FillRectangle(Br(me ? Red : Line), rect.X + 16, rect.Y, rect.Width - 16, center ? 4 : 3);
            if (center) UiBrackets(g, rect, A(Ink, 120));

            // silhueta (no lugar da arte do personagem)
            g.SetClip(new Rectangle(rect.X + 1, rect.Y + 4, rect.Width - 2, rect.Height - 8));
            float hd = rect.Width * .30f;
            var sc = me ? Red : Ink;
            g.FillEllipse(Br(A(sc, me ? 90 : 40)), rect.X + (rect.Width - hd) / 2f, rect.Y + rect.Height * .22f, hd, hd);
            float sw = rect.Width * .80f;
            g.FillEllipse(Br(A(sc, me ? 70 : 30)), rect.X + (rect.Width - sw) / 2f, rect.Y + rect.Height * .22f + hd + 10, sw, sw);
            g.ResetClip();

            var plate = new Rectangle(rect.X, rect.Bottom - 72, rect.Width, 72);
            g.FillRectangle(Br(A(Color.Black, 130)), plate);
            Txt(g, _players[pi].ToUpperInvariant(), VName, Ink, new RectangleF(plate.X + 18, plate.Y + 8, plate.Width - 36, 30));
            if (me) Spaced(g, "LEADER", FTiny, Red, plate.X + 18, plate.Y + 52, 3f);
            else
            {
                g.FillEllipse(Br(Green), plate.X + 18, plate.Y + 47, 8, 8);
                Spaced(g, "READY", FTiny, Green, plate.X + 32, plate.Y + 52, 3f);
            }
        }

        private void DrawInviteSlot(Graphics g, Rectangle rect)
        {
            Reg(rect, "invite"); float hv = Hv("invite");
            rect.Y -= (int)(6 * hv);
            using (var path = Chamfer(rect, 16, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, (int)(120 + 60 * hv))), path);
                using var dp = new Pen(Mix(Line, Ink, hv), 1.5f) { DashStyle = DashStyle.Dash };
                g.DrawPath(dp, path);
            }
            float cx = rect.X + rect.Width / 2f, cy = rect.Y + rect.Height / 2f - 10;
            var pen = Pn(Mix(Dim, Ink, hv), 2.5f);
            g.DrawLine(pen, cx - 14, cy, cx + 14, cy);
            g.DrawLine(pen, cx, cy - 14, cx, cy + 14);
            Spaced(g, "INVITE", FTiny, Mix(Dim, Ink, hv), cx, cy + 40, 3f, StringAlignment.Center);
        }

        // ---------- Armas (Coleção) ----------
        private void DrawWeapons(Graphics g, int w, int h)
        {
            int lx = 250, ly = BarH + 48;
            Spaced(g, "ARSENAL", FLabel, Red, lx, ly, 4f);
            for (int i = 0; i < _guns.Length; i++)
            {
                var rr = new Rectangle(lx, ly + 24 + i * 62, 300, 56);
                if (rr.Bottom > h - 24) break;
                string id = "gun:" + i; Reg(rr, id);
                float hv = Hv(id); bool sel = i == _gun;
                g.FillRectangle(Br(sel ? A(PanelHi, 255) : A(Color.White, (int)(10 * hv))), rr);
                if (sel) g.FillRectangle(Br(Red), rr.X, rr.Y, 4, rr.Height);
                Txt(g, _guns[i].Name.ToUpperInvariant(), VName, sel ? Ink : Mix(Dim, Ink, hv), new RectangleF(rr.X + 22, rr.Y + 3, rr.Width - 80, 32));
                Txt(g, $"{_guns[i].Skins.Count} SKINS", FTiny, Dim, new RectangleF(rr.X + 22, rr.Y + 35, rr.Width - 40, 16));
                Spaced(g, (i + 1).ToString("00"), FTiny, A(Dim, sel ? 220 : 120), rr.Right - 16, rr.Y + 28, 2f, StringAlignment.Far);
            }

            var gun = _guns[_gun];
            int si = SkinIdx(gun);
            var skin = gun.Skins[si];
            int rx = 400, right = w - 48;
            if (right - rx < 320) return;

            Txt(g, gun.Name.ToUpperInvariant(), VWater, A(Ink, 9), new RectangleF(rx - 6, BarH + 10, right - rx, 160));
            Txt(g, gun.Name.ToUpperInvariant(), VTitle, Ink, new RectangleF(rx, BarH + 46, right - rx - 230, 80));
            Spaced(g, "SKIN  /  " + skin.Name.ToUpperInvariant(), FLabel, Red, rx, BarH + 138, 3f);

            bool equipped = _equipped.TryGetValue(gun.GunId, out var eq) && eq == skin.SkinId;
            var shopSkin = AccountStore.ShopItemForSkin(gun.GunId, skin.SkinId);
            bool ownsSkin = shopSkin == null || (_account != null && AccountStore.Owns(_account, shopSkin));
            var eb = new Rectangle(right - 210, BarH + 64, 210, 52);
            if (!ownsSkin)
            {
                using var p = Chamfer(eb, 12, true, false, true, false);
                g.FillPath(Br(A(Amber, 24)), p);
                g.DrawPath(Pn(Amber, 1.6f), p);
                Spaced(g, "COMPRE NA DEAD ROYAL", FTiny, Amber, eb.X + eb.Width / 2f, eb.Y + eb.Height / 2f, 1.5f, StringAlignment.Center);
            }
            else if (equipped)
            {
                using var p = Chamfer(eb, 12, true, false, true, false);
                g.FillPath(Br(A(Green, 30)), p);
                g.DrawPath(Pn(Green, 2f), p);
                Spaced(g, "EQUIPADA", VNav, Green, eb.X + eb.Width / 2f, eb.Y + eb.Height / 2f, 3f, StringAlignment.Center);
            }
            else Button(g, eb, "equip", "EQUIP", true, VNav, 3f);

            // prévia
            int py = BarH + 168, skinH = 112, skinY = h - 100 - skinH;
            var prev = new Rectangle(rx, py, right - rx, Math.Max(140, skinY - 28 - py));
            using (var p = Chamfer(prev, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 200)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            UiBrackets(g, prev, A(Ink, 90), 14, 8);
            var sc = SkinColor(skin);
            g.FillEllipse(Br(A(sc, 22)), prev.X + prev.Width * .2f, prev.Y + prev.Height * .15f, prev.Width * .6f, prev.Height * .7f);
            g.FillRectangle(Br(sc), prev.X + 18, prev.Y, prev.Width - 18, 3);
            var img = Preview(gun, skin);
            if (img != null)
            {
                float k = Math.Min((prev.Width - 48f) / img.Width, (prev.Height - 48f) / img.Height);
                float dw = img.Width * k, dh = img.Height * k;
                g.DrawImage(img, prev.X + (prev.Width - dw) / 2f, prev.Y + (prev.Height - dh) / 2f, dw, dh);
            }
            else
            {
                Spaced(g, "WEAPON PREVIEW", VNav, Dim, prev.X + prev.Width / 2f, prev.Y + prev.Height / 2f - 10, 5f, StringAlignment.Center);
                Txt(g, $"Coloque um PNG em Assets/Previews/{gun.GunId}_{skin.SkinId}.png", FSmall, A(Dim, 170),
                    new RectangleF(prev.X, prev.Y + prev.Height / 2f + 6, prev.Width, 22), StringAlignment.Center);
            }

            // skins
            int n = gun.Skins.Count, sgap = 14;
            int scw = Math.Min(176, (right - rx - sgap * (n - 1)) / Math.Max(1, n));
            for (int i = 0; i < n; i++)
            {
                var s = gun.Skins[i];
                var rect = new Rectangle(rx + i * (scw + sgap), skinY, scw, skinH);
                string id = "skin:" + i; Reg(rect, id);
                float hv = Hv(id); bool sel = i == si;
                var c = SkinColor(s);
                using (var p = Chamfer(rect, 12, true, false, true, false))
                {
                    g.FillPath(Br(Mix(Panel, PanelHi, hv)), p);
                    var band = new Rectangle(rect.X, rect.Y, rect.Width, 58);
                    g.SetClip(p);
                    using (var lb = new LinearGradientBrush(band, Mix(c, Color.Black, .6f), c, 0f)) g.FillRectangle(lb, band);
                    g.ResetClip();
                    g.DrawPath(Pn(sel ? Red : Mix(Line, Ink, hv * .5f), sel ? 2f : 1f), p);
                }
                Txt(g, s.Name, FMid, Ink, new RectangleF(rect.X + 14, rect.Y + 66, rect.Width - 28, 22));
                bool isEq = _equipped.TryGetValue(gun.GunId, out var e2) && e2 == s.SkinId;
                var sShop = AccountStore.ShopItemForSkin(gun.GunId, s.SkinId);
                bool sOwned = sShop == null || (_account != null && AccountStore.Owns(_account, sShop));
                if (!sOwned) Spaced(g, "BLOQUEADA", FTiny, Amber, rect.X + 14, rect.Y + 98, 2f);
                else if (isEq) Spaced(g, "EQUIPADA", FTiny, Green, rect.X + 14, rect.Y + 98, 2f);
            }
        }

        // ---------- Amigos (canto inferior direito) ----------
        private static Rectangle FriendsRect(int w, int h)
        {
            int ph = Math.Min(580, h - BarH - 48);
            return new Rectangle(w - 24 - 330, h - 24 - ph, 330, ph);
        }

        private void DrawFriends(Graphics g, int w, int h)
        {
            int online = _friendList.Count(f => f.St != FStatus.Offline);
            int off = _friendList.Count - online;

            if (_fo <= 0.01f)
            {
                var pill = new Rectangle(w - 24 - 200, h - 24 - 48, 200, 48);
                Reg(pill, "friendsOpen"); float hv = Hv("friendsOpen");
                using (var p = Chamfer(pill, 10, true, false, true, false))
                {
                    g.FillPath(Br(Mix(A(Panel, 240), A(PanelHi, 250), hv)), p);
                    g.DrawPath(Pn(Mix(Line, Red, hv), 1f), p);
                }
                g.FillEllipse(Br(Green), pill.X + 18, pill.Y + 19, 10, 10);
                Spaced(g, "FRIENDS", FNav, Ink, pill.X + 40, pill.Y + 24, 3f);
                var badge = new RectangleF(pill.Right - 48, pill.Y + 12, 30, 24);
                using (var bp = RoundRect(badge, 12)) g.FillPath(Br(Red), bp);
                Txt(g, online.ToString(), FSmallB, Color.White, badge, StringAlignment.Center);
                return;
            }

            var pr = FriendsRect(w, h);
            float t = Ease(_fo);
            int dy = (int)((1f - t) * (pr.Height + 40));
            g.TranslateTransform(0, dy);
            _off = new Point(0, dy);

            for (int i = 4; i >= 1; i--) g.FillRectangle(Br(A(Color.Black, 14)), Rectangle.Inflate(pr, i * 3, i * 3));
            using (var p = Chamfer(pr, 16, true, false, false, false))
            {
                g.FillPath(Br(A(Panel, 248)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), pr.X + 16, pr.Y, pr.Width - 16, 3);

            // cabeçalho
            float hw = Spaced(g, "FRIENDS", FNav, Ink, pr.X + 22, pr.Y + 32, 3f);
            Txt(g, $"{online} online", FSmall, Green, new RectangleF(pr.X + 22 + hw + 14, pr.Y + 22, 120, 20));
            var cb = new Rectangle(pr.Right - 48, pr.Y + 16, 32, 32);
            Reg(cb, "friendsClose"); float chv = Hv("friendsClose");
            g.FillEllipse(Br(A(Ink, (int)(chv * 25))), cb);
            g.DrawLine(Pn(Mix(Dim, Ink, chv), 2f), cb.X + 9, cb.Y + 16, cb.X + 23, cb.Y + 16);
            g.DrawLine(Pn(Line, 1f), pr.X, pr.Y + 60, pr.Right, pr.Y + 60);

            // lista
            var view = new Rectangle(pr.X, pr.Y + 61, pr.Width, pr.Height - 61 - 68);
            int total = 16 + (online > 0 ? 30 : 0) + online * 60 + (off > 0 ? 30 : 0) + off * 60;
            float max = Math.Max(0, total - view.Height);
            _fscroll = Math.Clamp(_fscroll, 0f, max);

            if (_friendList.Count == 0)
            {
                Spaced(g, "NO FRIENDS YET", FLabel, Dim, view.X + view.Width / 2f, view.Y + view.Height / 2f - 8, 3f, StringAlignment.Center);
                Txt(g, "Use ADD FRIEND to get started", FSmall, A(Dim, 160),
                    new RectangleF(view.X, view.Y + view.Height / 2f + 6, view.Width, 20), StringAlignment.Center);
            }

            g.SetClip(view);
            int y = view.Y + 8 - (int)_fscroll;
            bool onHdr = false, offHdr = false;
            for (int i = 0; i < _friendList.Count; i++)
            {
                var f = _friendList[i];
                if (f.St != FStatus.Offline && !onHdr) { Spaced(g, $"ONLINE — {online}", FTiny, Dim, pr.X + 22, y + 15, 2f); y += 30; onHdr = true; }
                if (f.St == FStatus.Offline && !offHdr) { Spaced(g, $"OFFLINE — {off}", FTiny, Dim, pr.X + 22, y + 15, 2f); y += 30; offHdr = true; }
                var row = new Rectangle(pr.X, y, pr.Width, 60);
                if (row.Bottom > view.Top && row.Top < view.Bottom) DrawFriendRow(g, row, view, f, i);
                y += 60;
            }
            g.ResetClip();

            if (max > 0)
            {
                float th = Math.Max(30f, view.Height * (view.Height / (float)total));
                float ty = view.Y + (view.Height - th) * (_fscroll / max);
                g.FillRectangle(Br(A(Ink, 40)), pr.Right - 7, ty, 3, th);
            }

            // rodapé
            g.DrawLine(Pn(Line, 1f), pr.X, view.Bottom, pr.Right, view.Bottom);
            Button(g, new Rectangle(pr.X + 20, pr.Bottom - 56, 90, 42), "frdMenu", "REQUESTS", false, FLabel, 2f);
            Button(g, new Rectangle(pr.X + 118, pr.Bottom - 56, pr.Width - 138, 42), "addFriend", "ADD FRIEND", false, FLabel, 2.5f);

            g.ResetTransform();
            _off = Point.Empty;
            if (_requestsOpen) DrawFriendRequests(g, pr);
        }

        // popup de pedidos: aparece acima do painel (antes ficava fora da tela, abaixo dele)
        private void DrawFriendRequests(Graphics g, Rectangle basePr)
        {
            if (_online)
            {
                int onlineRh = _onlineRequests.Count == 0 ? 64 : 44 + _onlineRequests.Count * 34 + 8;
                var onlineRect = new Rectangle(basePr.Right - 360, basePr.Y - onlineRh - 8, 360, onlineRh);
                using (var onlinePanel = Chamfer(onlineRect, 12, true, false, true, false))
                {
                    g.FillPath(Br(A(Panel, 252)), onlinePanel);
                    g.DrawPath(Pn(Line, 1f), onlinePanel);
                }
                g.FillRectangle(Br(Red), onlineRect.X + 12, onlineRect.Y, onlineRect.Width - 12, 3);
                Spaced(g, "ONLINE REQUESTS", FLabel, Ink, onlineRect.X + 18, onlineRect.Y + 22, 3f);
                if (_onlineRequests.Count == 0)
                    Txt(g, "No pending requests", FSmall, Dim, new RectangleF(onlineRect.X + 18, onlineRect.Y + 36, onlineRect.Width - 36, 20));
                for (int i = 0; i < _onlineRequests.Count; i++)
                {
                    var row = new Rectangle(onlineRect.X + 14, onlineRect.Y + 44 + i * 34, onlineRect.Width - 28, 28);
                    Txt(g, _onlineRequests[i].Nick, FMid, Ink, new RectangleF(row.X, row.Y, row.Width - 160, row.Height));
                    var accept = new Rectangle(row.Right - 150, row.Y, 70, 28);
                    Reg(accept, "frdAccept:" + i);
                    using (var ap = Chamfer(accept, 6, true, false, true, false)) g.FillPath(Br(Mix(Red, RedHi, Hv("frdAccept:" + i))), ap);
                    Spaced(g, "ACCEPT", FTiny, Color.White, accept.X + 35, accept.Y + 14, 2f, StringAlignment.Center);
                }
                return;
            }
            int rh = _friendRequests.Count == 0 ? 64 : 44 + _friendRequests.Count * 34 + 8;
            var r = new Rectangle(basePr.Right - 360, basePr.Y - rh - 8, 360, rh);
            using (var p = Chamfer(r, 12, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 12, r.Y, r.Width - 12, 3);
            Spaced(g, "REQUESTS", FLabel, Ink, r.X + 18, r.Y + 22, 3f);
            if (_friendRequests.Count == 0)
            {
                Txt(g, "No pending requests", FSmall, Dim, new RectangleF(r.X + 18, r.Y + 36, r.Width - 36, 20));
                return;
            }
            for (int i = 0; i < _friendRequests.Count; i++)
            {
                var row = new Rectangle(r.X + 14, r.Y + 44 + i * 34, r.Width - 28, 28);
                string rawReq = _friendRequests[i];
                bool groupReq = rawReq.StartsWith("G:", StringComparison.OrdinalIgnoreCase);
                string reqId = groupReq ? rawReq.Substring(2) : rawReq;
                var requester = AccountStore.FindByUserId(reqId);
                string reqText = requester == null ? reqId : requester.Nick + "  •  " + requester.DisplayName;
                if (groupReq) reqText = "GROUP: " + reqText;
                Txt(g, reqText, FMid, Ink, new RectangleF(row.X, row.Y, row.Width - 160, row.Height));
                var accept = new Rectangle(row.Right - 150, row.Y, 70, 28);
                var rec = new Rectangle(row.Right - 74, row.Y, 74, 28);
                Reg(accept, "frdAccept:" + i); Reg(rec, "frdReject:" + i);
                float ah = Hv("frdAccept:" + i), rhv = Hv("frdReject:" + i);
                using (var ap = Chamfer(accept, 6, true, false, true, false)) g.FillPath(Br(Mix(Red, RedHi, ah)), ap);
                Spaced(g, "ACCEPT", FTiny, Color.White, accept.X + accept.Width / 2f, accept.Y + 14, 2f, StringAlignment.Center);
                using (var rp = Chamfer(rec, 6, true, false, true, false))
                {
                    g.FillPath(Br(A(Ink, (int)(rhv * 22))), rp);
                    g.DrawPath(Pn(Mix(A(Ink, 110), Ink, rhv), 1.5f), rp);
                }
                Spaced(g, "DECLINE", FTiny, Ink, rec.X + rec.Width / 2f, rec.Y + 14, 2f, StringAlignment.Center);
            }
        }

        private void DrawFriendRow(Graphics g, Rectangle row, Rectangle view, Friend f, int idx)
        {
            string id = "friend:" + idx;
            Reg(Rectangle.Intersect(row, view), id);
            float hv = Hv(id);
            bool off = f.St == FStatus.Offline;
            g.FillRectangle(Br(A(Color.White, (int)(10 * hv))), row);

            float cx = row.X + 42, cy = row.Y + 30;
            Avatar(g, cx, cy, 40, f.Name, off);
            float sx = cx + 14, sy = cy + 14;
            g.FillEllipse(Br(Panel), sx - 8, sy - 8, 16, 16);
            g.FillEllipse(Br(StatusColor(f.St)), sx - 5, sy - 5, 10, 10);

            Txt(g, f.Name, FMid, off ? Dim : Ink, new RectangleF(row.X + 76, row.Y + 11, row.Width - 170, 22));
            Txt(g, f.Sub, FSmall, f.St == FStatus.InGame ? Blue : Dim, new RectangleF(row.X + 76, row.Y + 33, row.Width - 100, 18));
            if (!off && hv > 0.02f)
                Spaced(g, "INVITE", FTiny, A(Red, (int)(255 * hv)), row.Right - 52, row.Y + 21, 2f, StringAlignment.Far);

            // botão remover (X desenhado, sem depender de glifo da fonte)
            var rm = new Rectangle(row.Right - 34, row.Y + 19, 22, 22);
            Reg(Rectangle.Intersect(rm, view), "frdRemove:" + idx);
            float rv = Hv("frdRemove:" + idx);
            g.FillEllipse(Br(A(Red, (int)(rv * 70))), rm);
            var xp = Pn(Mix(A(Dim, 200), Red, rv), 1.5f);
            g.DrawLine(xp, rm.X + 7, rm.Y + 7, rm.Right - 7, rm.Bottom - 7);
            g.DrawLine(xp, rm.Right - 7, rm.Y + 7, rm.X + 7, rm.Bottom - 7);
        }

        // ---------- Avisos: lista de até 5 no canto superior direito ----------
        private void DrawToast(Graphics g, int w)
        {
            if (_toasts.Count == 0) return;
            float top = _page == 4 ? 70 : BarH + 72;
            int shown = 0;
            foreach (var toast in _toasts)
            {
                if (shown++ >= 5) break;
                float a = Math.Min(1f, Math.Min(toast.Left / 0.4f, (2.6f - toast.Left) / 0.2f));
                float tw = Math.Min(420f, g.MeasureString(toast.Text, FMid).Width + 56);
                var r = new RectangleF(w - tw - 24, top + (shown - 1) * 50, tw, 44);
                using (var p = Chamfer(r, 10, true, false, true, false))
                {
                    g.FillPath(Br(A(Panel, (int)(250 * a))), p);
                    g.DrawPath(Pn(A(Line, (int)(255 * a)), 1f), p);
                }
                g.FillRectangle(Br(A(Red, (int)(255 * a))), r.X + 10, r.Y, 4, r.Height);
                Txt(g, toast.Text, FMid, A(Ink, (int)(255 * a)), new RectangleF(r.X + 28, r.Y, r.Width - 40, r.Height));
            }
        }
    }

    public class Customize { public string WindowTitle { get; set; } = "Deadzone"; }
    public class GunData
    {
        public string GunId { get; set; } = "";
        public string Name { get; set; } = "";
        public List<Skin> Skins { get; set; } = new();
    }
    public class Skin { public string SkinId { get; set; } = ""; public string Name { get; set; } = ""; }
}