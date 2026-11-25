// MainForm.cs â€” Deadzone (WinForms, sem engine) â€” visual estilo Valorant
//   Loading â†’ Login â†’ Lobby â†’ Armas, com lista de amigos no canto inferior direito.
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

namespace CrossfireWin
{
    public partial class MainForm : Form
    {
        private const int BarH = 64;

        // ---------- Paleta (inspirada no Valorant) ----------
        private static readonly Color Bg      = Color.FromArgb(15, 25, 35);
        private static readonly Color BgDeep  = Color.FromArgb(9, 15, 22);
        private static readonly Color Panel   = Color.FromArgb(24, 35, 47);
        private static readonly Color PanelHi = Color.FromArgb(33, 47, 62);
        private static readonly Color Line    = Color.FromArgb(48, 62, 78);
        private static readonly Color Red     = Color.FromArgb(255, 70, 85);
        private static readonly Color RedHi   = Color.FromArgb(255, 105, 118);
        private static readonly Color Ink     = Color.FromArgb(236, 232, 225);
        private static readonly Color Dim     = Color.FromArgb(139, 151, 159);
        private static readonly Color Green   = Color.FromArgb(61, 220, 151);
        private static readonly Color Blue    = Color.FromArgb(76, 201, 240);
        private static readonly Color Amber   = Color.FromArgb(245, 197, 66);
        private static readonly Color OffC    = Color.FromArgb(91, 103, 112);
        private static readonly Color[] Pal =
        {
            Color.FromArgb(255, 70, 85), Color.FromArgb(76, 201, 240), Color.FromArgb(61, 220, 151),
            Color.FromArgb(245, 197, 66), Color.FromArgb(170, 120, 255), Color.FromArgb(255, 140, 66)
        };

        // ---------- Fontes ----------
        private static readonly string Fam = PickFamily("Bahnschrift", "Segoe UI");
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

        private static string PickFamily(params string[] names)
        {
            using var c = new InstalledFontCollection();
            var have = c.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return names.FirstOrDefault(n => have.Contains(n)) ?? "Segoe UI";
        }
        private static Font F(float px, FontStyle s = FontStyle.Bold) => new Font(Fam, px, s, GraphicsUnit.Pixel);

        private static StringFormat MakeSf(StringAlignment a) => new StringFormat(StringFormatFlags.NoWrap)
        { Alignment = a, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        private static readonly StringFormat SfL = MakeSf(StringAlignment.Near);
        private static readonly StringFormat SfC = MakeSf(StringAlignment.Center);
        private static readonly StringFormat SfR = MakeSf(StringAlignment.Far);

        // ---------- Dados ----------
        private enum FStatus { InGame, Online, Away, Offline }
        private sealed record Friend(string Name, string Sub, FStatus St);
        private sealed class Hit { public Rectangle R; public string Id = ""; }

        private readonly List<Friend> _friendList = new();
        private readonly List<string> _friendRequests = new();
        private bool _requestsOpen;
        private bool _cfgOpen;
        private int _cfgVol = 60;
        private bool _cfgLangEn;
        private bool _cfgOpt = true;

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        private readonly Customize _custom;
        private readonly string _appDir = Path.GetDirectoryName(Application.ExecutablePath)!;
        private readonly GunData[] _guns;
        private string[] _players = { "JOGADOR 1", "JOGADOR 2", "JOGADOR 3", "JOGADOR 4" };

        // ---------- Login ----------
        private TextBox _tbUser = new();
        private TextBox _tbPass = new();

        // ---------- Estado ----------
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private double _last;
        private float _clock;
        private int _page;                 // 0=Loading 1=Login 2=Lobby 3=Armas
        private float _progress;
        private float _fade = 1f;
        private bool _queue;
        private readonly Stopwatch _queueSw = new();
        private bool _friendsOpen;
        private float _fo;                 // animaÃ§Ã£o do painel de amigos (0..1)
        private float _fscroll;
        private int _gun;
        private readonly Dictionary<string, int> _skinSel = new();
        private readonly Dictionary<string, string> _equipped = new();
        private string _toast = "";
        private float _toastLeft;

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
        private readonly Dictionary<string, Image?> _imgs = new();
        private Bitmap? _bgCache;

        public MainForm()
        {
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
            UpdateSettings.Load(_appDir);
            AppInfo.Load(_appDir);
            _guns = LoadGuns();
            var data = GameData.Load(_appDir);
            _maps.AddRange(data.Maps);
            _rules = data.Rules;
            if (_maps.Count == 0)
                _maps.Add(new GameMap { Id = "deal_duo", Name = "Deal Duo", Mode = "DealDuo", MaxWins = 3 });
            _map = _maps[0];
            KeyPreview = true;

            // Pre-preenchimento do login com o último usuário
            string lastUser = LoadLastUser();
            if (!string.IsNullOrEmpty(lastUser)) { _players[0] = lastUser; _tbUser.Text = lastUser; }

            // Checagem de atualizações
            Task.Run(async () =>
            {
                try
                {
                    var info = await UpdateService.CheckAsync();
                    if (info != null) Invoke((Action)(() => { if (!IsDisposed) ShowPage("Update"); }));
                }
                catch { }
            });

            _timer = new System.Windows.Forms.Timer { Interval = 15 };
            _timer.Tick += (s, e) =>
            {
                double now = _sw.Elapsed.TotalSeconds;
                float dt = (float)Math.Min(0.1, now - _last);
                _last = now;
                Step(dt);
                if (WindowState != FormWindowState.Minimized) Invalidate();
            };
            _timer.Start();
            FormClosed += (s, e) => _timer.Stop();
            Shown += (s, e) => { if (WindowState == FormWindowState.Normal) ToggleMax(); };
        }

        // ---------- Dados externos ----------
        public void QuickPlay(string[] args)
        {
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
                UpdateService.Available = new UpdateService.UpdateInfo("1.2.0", "https://example.com/DeadzoneSetup_v1.2.0.exe", "Notas de teste: correção de renderer.");
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
                return new Customize { WindowTitle = doc.RootElement.GetProperty("Window").GetProperty("Title").GetString() ?? "Deadzone" };
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

        // ---------- LÃ³gica ----------
        private void Step(float dt)
        {
            _clock += dt;
            if (_page == 0)
            {
                _progress = Math.Min(100f, _progress + dt * 38f);
                if (_progress >= 100f) ShowPage("Login");
            }
            if (_page == 4 && _inGame && _g != null) UpdateGame(dt);
            _fade = Math.Max(0f, _fade - dt * 3.2f);
            _fo = Math.Clamp(_fo + (_friendsOpen ? 1f : -1f) * dt * 3.6f, 0f, 1f);
            if (_toastLeft > 0) _toastLeft -= dt;

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
            _page = name switch { "Login" => 1, "Lobby" => 2, "Weapons" => 3, "Partida" => 4, "Update" => 5, _ => 0 };
            _fade = 1f;
            _progress = 0f;
            if (_page < 2) _friendsOpen = false;
            if (_page != 4)
            {
                _inGame = false;
                _firing = false;
                _keys.Clear();
                _paused = false;
            }
            _tbUser.Visible = _page == 1;
            _tbPass.Visible = _page == 1;
            if (_page == 4) StartGame();
        }

        private void StartGame()
        {
            if (_maps.Count == 0) return;
            _map = _maps[Math.Clamp(_mapIndex, 0, _maps.Count - 1)];
            int teamSize = _map.Mode == "SquadBattle" ? 4 : _map.Mode == "DeadRoyale" ? _royaleSquad : _teamSize;
            _g = new GameSession(_map, _rules, teamSize, _players);
            if (_map.Mode == "DealDuo") { _storeOpen = true; _storeTimer = 180f; }
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
            ShowPage("Loading");
            Task.Run(async () => { await Task.Delay(3500); if (!IsDisposed) Invoke((Action)(() => ShowPage("Lobby"))); });
            _inGame = false;
            _g = null;
            _paused = false;
            _inventoryOpen = false;
            _firing = false;
            _keys.Clear();
            _spectate = -1;
            ShowPage("Lobby");
        }

        private void ShowToast(string msg) { _toast = msg; _toastLeft = 2.6f; }

        private void ToggleMax()
        {
            if (WindowState == FormWindowState.Maximized) WindowState = FormWindowState.Normal;
            else { MaximizedBounds = Screen.FromControl(this).WorkingArea; WindowState = FormWindowState.Maximized; }
        }

        private void Activate(string id)
        {
            if (id.StartsWith("frdAccept:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(10), out var fa) && fa >= 0 && fa < _friendRequests.Count)
            {
                ShowToast("Pedido aceito: " + _friendRequests[fa]);
                _friendRequests.RemoveAt(fa);
                return;
            }
            if (id.StartsWith("frdReject:", StringComparison.OrdinalIgnoreCase) && int.TryParse(id.Substring(10), out var frq) && frq >= 0 && frq < _friendRequests.Count)
            {
                ShowToast("Pedido recusado: " + _friendRequests[frq]);
                _friendRequests.RemoveAt(frq);
                return;
            }
            if (id.StartsWith("nav:")) { ShowPage(id.Substring(4)); return; }
            if (id.StartsWith("friend:"))
            {
                var f = _friendList[int.Parse(id.Substring(7))];
                ShowToast(f.St == FStatus.Offline ? $"{f.Name} estÃ¡ offline" : $"Convite enviado para {f.Name}");
                return;
            }
            if (id.StartsWith("gun:")) { _gun = int.Parse(id.Substring(4)); return; }
            if (id.StartsWith("skin:")) { _skinSel[_guns[_gun].GunId] = int.Parse(id.Substring(5)); return; }

            switch (id)
            {
                case "min": WindowState = FormWindowState.Minimized; break;
                case "max": ToggleMax(); break;
                case "close": Close(); break;
                case "guest":
                case "createAcc":
                case "google":
                    SaveLastUser(_players[0]);
                    ShowPage("Lobby");
                    break;
                case "login":
                {
                    string user = _tbUser.Text.Trim();
                    if (string.IsNullOrEmpty(user)) { ShowToast("Digite seu nome de usuário."); return; }
                    if (_tbPass.Text.Length == 0) { ShowToast("Digite sua senha."); return; }
                    SaveLastUser(user);
                    _players[0] = user;
                    _royaleSquad = 1;
                    _teamSize = 1;
                    ShowPage("Loading");
                    Task.Run(async () => { await Task.Delay(3500); if (!IsDisposed) Invoke((Action)(() => ShowPage("Lobby"))); });
                    break;
                }
                case "updateInstall":
                    UpdateService.InstallNow();
                    Close();
                    break;
                case "updateLater":
                    ShowPage("Login");
                    break;
                case "updateIgnore":
                    UpdateService.IgnoreVersion = UpdateService.Available?.Version ?? "";
                    ShowPage("Login");
                    break;
                case "updateSoft":
                    if (UpdateService.StartSoftUpdate())
                    {
                        ShowToast("Atualização baixada; aplicada no próximo início do Deadzone.");
                        Close();
                    }
                    break;
                case "start":
                    _queue = false;
                    if (_page == 2) ShowPage("Partida");
                    else { _queue = !_queue; if (_queue) _queueSw.Restart(); else _queueSw.Reset(); }
                    break;
                case "mapNext":
                    if (_maps.Count > 0) { _mapIndex = (_mapIndex + 1) % _maps.Count; _map = _maps[_mapIndex]; ShowToast(_map.Name + " â€¢ " + _map.Type); }
                    break;
                case "mapPrev":
                    if (_maps.Count > 0) { _mapIndex = (_mapIndex - 1 + _maps.Count) % _maps.Count; _map = _maps[_mapIndex]; ShowToast(_map.Name + " â€¢ " + _map.Type); }
                    break;
                case "size1": SetSquadSize(1); break;
                case "size2": SetSquadSize(2); break;
                case "size3": SetSquadSize(3); break;
                case "size4": SetSquadSize(4); break;
                case "inv": _inventoryOpen = !_inventoryOpen; break;
                case "invClose": _inventoryOpen = false; break;
                case "invMed":
                    if (_g != null && _g.UseMedkit(_g.Player)) ShowToast("Vida restaurada");
                    else ShowToast("Sem kit mÃ©dico ou vida cheia");
                    break;
                case "pause": if (_inGame) _paused = true; break;
                case "resume": _paused = false; break;
                case "lobbyNow":
                case "specLobby":
                case "lobbyAfterWin": BackToLobby(); break;
                case "specPrev": CycleSpectate(-1); break;
                case "specNext": CycleSpectate(1); break;
                case "skipCountdown": _g?.SkipCountdown(); break;
                case "reload": if (_g != null) _g.TryReload(_g.Player); break;
                default:
                    if (id.StartsWith("buy:") && _storeOpen && _g != null)
                    {
                        string buyId = id.Substring(4);
                        var gun = Arsenal.Defs[buyId];
                        int price = ShopPrices.Get(buyId);
                        var p = _g.Player;
                        if (_g.Player.Coins >= price)
                        {
                            p.WeaponId = buyId;
                            p.Mag = p.Weapon.MagSize;
                            p.Reserve = p.Mag * 2;
                            p.Coins -= price;
                            ShowToast("Comprado: " + gun.Name);
                        }
                        else ShowToast("Sem moedas suficientes");
                    }
                    break;
                case "mode": ShowToast("Use as setas para trocar de mapa"); break;
                case "friendsOpen": _friendsOpen = true; break;
                case "frdMenu": _requestsOpen = !_requestsOpen; break;
                case "cfg": _cfgOpen = !_cfgOpen; break;
                case "cfgVolUp": _cfgVol = Math.Min(100, _cfgVol + 5); break;
                case "cfgVolDn": _cfgVol = Math.Max(0, _cfgVol - 5); break;
                case "cfgLang": _cfgLangEn = !_cfgLangEn; break;
                case "cfgOpt": _cfgOpt = !_cfgOpt; break;
                case "viewUpd":
                    if (UpdateService.Available != null) ShowPage("Update");
                    else ShowToast("Sem atualizações disponíveis");
                    break;
                case "friendsClose": _friendsOpen = false; break;
                case "frdRemove":
                {
                    int ri = int.Parse(id.Substring(8));
                    if (ri >= 0 && ri < _friendList.Count)
                    {
                        var f = _friendList[ri];
                        _friendList.RemoveAt(ri);
                        ShowToast("Removido: " + f.Name);
                    }
                    break;
                }
                case "addFriend": ShowToast("Adicionar amigo em breve"); break;
                case "equip":
                    {
                        var gun = _guns[_gun];
                        var sk = gun.Skins[SkinIdx(gun)];
                        _equipped[gun.GunId] = sk.SkinId;
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
            if (e.KeyCode == Keys.F11) { ToggleMax(); e.Handled = true; return; }
            if (_page == 1)
            {
                if (e.KeyCode == Keys.Enter) { Activate("login"); return; }
                if (e.KeyCode == Keys.Tab)
                {
                    _tbPass.Focus();
                    return;
                }
            }
            if (_page != 4 || _g == null) return;

            _keys.Add(e.KeyCode);
            if (e.KeyCode == Keys.Escape)
            {
                if (_inventoryOpen) _inventoryOpen = false;
                else if (_g.Phase == SessionPhase.MatchOver) BackToLobby();
                else { _paused = !_paused; _firing = false; }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Tab)
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
                if (item != null) { if (item.Kind == "shop") { _storeOpen = true; _storeTimer = 180f; } else ShowToast("Pegou: " + item.Label); }
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
            MaximizedBounds = Screen.FromControl(this).WorkingArea;
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
            _mouse = e.Location;
            _down = e.Button == MouseButtons.Left ? HitAt(e.Location) : null;
            if (_page == 4 && _g != null && e.Button == MouseButtons.Left && _down == null &&
                !_paused && !_inventoryOpen && _g.Player.Alive)
                _firing = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) _firing = false;
            var up = e.Button == MouseButtons.Left ? HitAt(e.Location) : null;
            if (up != null && up == _down) Activate(up);
            _down = null;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
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
                }
                if (!onHit && pt.Y >= 0 && pt.Y < BarH) { m.Result = (IntPtr)2; return; }   // HTCAPTION
                m.Result = (IntPtr)1; return;                                                 // HTCLIENT
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
            => g.DrawString(s, f, Br(c), r, a == StringAlignment.Near ? SfL : a == StringAlignment.Center ? SfC : SfR);

        private float CharW(Graphics g, char ch, Font f)
        {
            if (ch == ' ') return f.Size * 0.32f;
            if (!_cw.TryGetValue((f, ch), out var w))
                _cw[(f, ch)] = w = g.MeasureString(ch.ToString(), f, PointF.Empty, StringFormat.GenericTypographic).Width;
            return w;
        }

        // Texto em caixa alta com espaÃ§amento entre letras (cy = centro vertical)
        private float Spaced(Graphics g, string s, Font f, Color c, float x, float cy, float sp, StringAlignment al = StringAlignment.Near)
        {
            float total = 0;
            var ws = new float[s.Length];
            for (int i = 0; i < s.Length; i++) { ws[i] = CharW(g, s[i], f); total += ws[i] + sp; }
            total = Math.Max(0, total - sp);
            float px = al == StringAlignment.Center ? x - total / 2f : al == StringAlignment.Far ? x - total : x;
            float py = cy - f.GetHeight(g) / 2f;
            var br = Br(c);
            for (int i = 0; i < s.Length; i++)
            {
                g.DrawString(s[i].ToString(), f, br, px, py, StringFormat.GenericTypographic);
                px += ws[i] + sp;
            }
            return total;
        }

        private void Button(Graphics g, Rectangle r, string id, string label, bool primary, Font? f = null, float sp = 3f)
        {
            f ??= FBtn;
            Reg(r, id);
            float hv = Hv(id);
            if (primary && hv > 0.01f)
                for (int i = 3; i >= 1; i--)
                {
                    using var gp = Chamfer(Rectangle.Inflate(r, i * 4, i * 3), 12 + i * 2, true, false, true, false);
                    g.FillPath(Br(A(Red, (int)(24 * hv))), gp);
                }
            using var path = Chamfer(r, 12, true, false, true, false);
            if (primary)
            {
                g.FillPath(Br(Mix(Red, RedHi, hv)), path);
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
            FillRoundedRect(g, r, active ? Mix(Red, RedHi, hv * .35f) : Mix(Panel, PanelHi, hv), 6);
            if (!active)
                using (var p = RoundRect(r, 6)) g.DrawPath(Pn(A(Line, 190), 1f), p);
            Txt(g, label, FLabel, active ? Color.White : Mix(Dim, Ink, hv),
                new RectangleF(r.X, r.Y, r.Width, r.Height), StringAlignment.Center);
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
        }

        // ---------- Pintura ----------
        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            _hits.Clear();
            _off = Point.Empty;

            EnsureBg(w, h);
            g.DrawImageUnscaled(_bgCache!, 0, 0);

            DrawTopBarIfNeeded(g, w);
            switch (_page)
            {
                case 0: DrawLoading(g, w, h); break;
                case 1: DrawLogin(g, w, h); break;
                case 2: DrawLobby(g, w, h); DrawConfigPanel(g, w, h); break;                case 3: DrawWeapons(g, w, h); break;
                case 4: DrawGame(g, w, h); break;
                case 5: DrawUpdatePage(g, w, h); break;
            }
            if (_page >= 2 && _page != 4) DrawFriends(g, w, h);
            DrawToast(g, w);

            if (_fade > 0.003f) g.FillRectangle(Br(A(BgDeep, (int)(255 * _fade))), 0, 0, w, h);
        }

        private void DrawTopBarIfNeeded(Graphics g, int w)
        {
            if (_page != 4) DrawTopBar(g, w);
        }

        private void DrawTopBar(Graphics g, int w)
        {
            g.FillRectangle(Br(A(Color.FromArgb(10, 17, 25), 235)), 0, 0, w, BarH);
            g.DrawLine(Pn(Line, 1), 0, BarH - 1, w, BarH - 1);

            // logo
            using (var lp = Chamfer(new RectangleF(28, 16, 32, 32), 8, true, false, true, false))
                g.FillPath(Br(Red), lp);
            var title = _custom.WindowTitle.ToUpperInvariant();
            Txt(g, title.Substring(0, 1), FName, Color.White, new RectangleF(28, 16, 32, 32), StringAlignment.Center);
            Spaced(g, title, FLogo, Ink, 74, BarH / 2f, 5f);

            if (_page >= 2)
            {
                // navegaÃ§Ã£o
                var tabs = new[] { ("JOGAR", "Partida", 2), ("ARMAS", "Weapons", 3) };
                float tw = 0; var widths = new float[tabs.Length];
                for (int i = 0; i < tabs.Length; i++)
                {
                    float sum = 0; foreach (var ch in tabs[i].Item1) sum += CharW(g, ch, FNav) + 3f;
                    widths[i] = sum + 56; tw += widths[i];
                }
                float x = (w - tw) / 2f;
                for (int i = 0; i < tabs.Length; i++)
                {
                    var (label, page, idx) = tabs[i];
                    var r = new Rectangle((int)x, 0, (int)widths[i], BarH - 1);
                    string id = "nav:" + page; Reg(r, id);
                    float hv = Hv(id); bool active = _page == idx;
                    Spaced(g, label, FNav, active ? Ink : Mix(Dim, Ink, hv), r.X + r.Width / 2f, BarH / 2f, 3f, StringAlignment.Center);
                    if (active) g.FillRectangle(Br(Red), r.X + 28, BarH - 4, r.Width - 56, 3);
                    else if (hv > 0.02f) g.FillRectangle(Br(A(Red, (int)(150 * hv))), r.X + 28, BarH - 4, r.Width - 56, 3);
                    x += widths[i];
                }

                // perfil
                int chipX = w - 138 - 24 - 190;
                Avatar(g, chipX + 18, BarH / 2f, 34, _players[0], false);
                Txt(g, _players[0], FMid, Ink, new RectangleF(chipX + 44, 14, 146, 20));
                Txt(g, "Convidado", FSmall, Dim, new RectangleF(chipX + 44, 33, 146, 18));
            }

            WinBtn(g, new Rectangle(w - 138, 0, 46, 36), "min", 0);
            WinBtn(g, new Rectangle(w - 92, 0, 46, 36), "max", 1);
            WinBtn(g, new Rectangle(w - 46, 0, 46, 36), "close", 2);
        }

        private void WinBtn(Graphics g, Rectangle r, string id, int kind)
        {
            Reg(r, id);
            float hv = Hv(id);
            g.FillRectangle(Br(kind == 2 ? A(Red, (int)(255 * hv)) : A(Color.White, (int)(28 * hv))), r);
            var pen = Pn(kind == 2 && hv > .5f ? Color.White : Mix(Dim, Ink, hv), 1.5f);
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            switch (kind)
            {
                case 0: g.DrawLine(pen, cx - 5, cy, cx + 5, cy); break;
                case 1:
                    if (WindowState == FormWindowState.Maximized)
                    {
                        g.DrawRectangle(pen, cx - 5, cy - 2, 7, 7);
                        g.DrawLine(pen, cx - 2, cy - 5, cx + 5, cy - 5);
                        g.DrawLine(pen, cx + 5, cy - 5, cx + 5, cy + 2);
                    }
                    else g.DrawRectangle(pen, cx - 5, cy - 5, 10, 10);
                    break;
                default:
                    g.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
                    g.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
                    break;
            }
        }

        // ---------- Loading ----------
        private void DrawLoading(Graphics g, int w, int h)
        {
            float cy = h / 2f;
            Spaced(g, _custom.WindowTitle.ToUpperInvariant(), FHuge, Ink, w / 2f, cy - 50, 14f, StringAlignment.Center);

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
            string status = _progress < 30 ? "Iniciando serviÃ§os" : _progress < 60 ? "Carregando armas"
                          : _progress < 90 ? "Sincronizando perfil" : "Quase lÃ¡";
            Spaced(g, status.ToUpperInvariant(), FLabel, Dim, bx, by + 24, 2f);
            Txt(g, $"{(int)_progress}%", FSmallB, Ink, new RectangleF(bx, by + 14, bw, 20), StringAlignment.Far);
            Txt(g, "v" + AppInfo.Versao, FSmall, A(Dim, 160), new RectangleF(28, h - 40, 120, 20));
        }

        // ---------- Login ----------
        private void LoadLoginControls()
        {
            _tbUser.Parent = this;
            _tbPass.Parent = this;
            int w = Math.Max(600, ClientSize.Width);
            int cx = (w - 540) / 2;
            _tbUser.Size = new Size(520, 44);
            _tbUser.Location = new Point(cx + 40, BarH + 126);
            _tbUser.BackColor = PanelHi;
            _tbUser.ForeColor = Ink;
            _tbUser.TextAlign = HorizontalAlignment.Center;
            _tbPass.Size = new Size(520, 44);
            _tbPass.Location = new Point(cx + 40, BarH + 182);
            _tbPass.BackColor = PanelHi;
            _tbPass.ForeColor = Ink;
            _tbPass.PasswordChar = '*';
            _tbUser.Focus();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
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

        private void DrawLogin(Graphics g, int w, int h)
        {
            var card = new Rectangle((w - 540) / 2, BarH + 40, 540, 420);
            for (int i = 4; i >= 1; i--)
                g.FillRectangle(Br(A(Color.Black, 14)), Rectangle.Inflate(card, i * 4, i * 4));
            using (var p = Chamfer(card, 18, true, false, true, false))
            {
                g.FillPath(Br(Panel), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), card.X + 18, card.Y, card.Width - 18, 3);

            Spaced(g, "FAZER LOGIN", FTitle, Ink, card.X + 30, card.Y + 26, 4f);
            Txt(g, "Digite seu nome de usuário e senha", FBody, Dim,
                new RectangleF(card.X + 30, card.Y + 58, card.Width - 60, 18), StringAlignment.Center);

            int fy = BarH + 126;
            FillRoundedRect(g, new Rectangle(card.X + 40, fy, card.Width - 80, 46), PanelHi, 8);
            using (var p = RoundRect(new Rectangle(card.X + 40, fy, card.Width - 80, 46), 8))
                g.DrawPath(Pn(A(Line, 190), 1f), p);
            using (var pen = new Pen(A(Red, 160), 1f))
                g.DrawLine(pen, card.X + 60, fy + 45, card.X + card.Width - 60, fy + 45);
            using (var sb = new SolidBrush(Dim))
                g.DrawString("username", FSmallB, sb, card.X + 54, fy + 12);

            int py = BarH + 182;
            FillRoundedRect(g, new Rectangle(card.X + 40, py, card.Width - 80, 46), PanelHi, 8);
            using (var p = RoundRect(new Rectangle(card.X + 40, py, card.Width - 80, 46), 8))
                g.DrawPath(Pn(A(Line, 190), 1f), p);
            using (var sb = new SolidBrush(Dim))
                g.DrawString("****************", FSmallB, sb, card.X + 54, py + 12);

            var login = new Rectangle(card.X + 40, BarH + 240, card.Width - 80, 52);
            Reg(login, "login"); float lhv = Hv("login");
            Button(g, login, "login", "FAZER LOGIN", true, FBtn, 4f);

            var guest = new Rectangle(card.X + 40, login.Bottom + 12, (card.Width - 92) / 2, 40);
            Button(g, guest, "createAcc", "Create Account", false, FMid, 2.5f);
            Button(g, new Rectangle(card.X + 52 + (card.Width - 92) / 2, login.Bottom + 12, (card.Width - 92) / 2, 40), "google", "Google (Recommended)", false, FMid, 2.5f);
            Txt(g, "Local account saves data on this PC.", FTiny, A(Dim, 170), new RectangleF(0, BarH + 366, w, 14), StringAlignment.Center);
            Txt(g, "Tab para avançar, Enter para fazer login", FTiny, A(Dim, 160),
                new RectangleF(0, BarH + 344, w, 14), StringAlignment.Center);
        }

        // ---------- Atualização ----------
        private void DrawUpdatePage(Graphics g, int w, int h)
        {
            var info = UpdateService.Available;
            string version = info?.Version ?? "";
            var card = new Rectangle((w - 640) / 2, BarH + 60, 640, 240);
            FillRoundedRect(g, card, Panel, 12);
            using (var p = RoundRect(card, 12)) g.DrawPath(Pn(Line, 1f), p);
            g.FillRectangle(Br(Red), card.X + 22, card.Y, card.Width - 44, 3);

            Spaced(g, "ATUALIZAÇÃO DISPONÍVEL", FTitle, Red, card.X + 30, card.Y + 22, 5f);
            Txt(g, "Deadzone " + UpdateService.LocalVersion + " → " + version, FMid, Ink,
                new RectangleF(card.X + 30, card.Y + 62, card.Width - 60, 24));
            if (!string.IsNullOrEmpty(info?.Notes))
                Txt(g, info.Notes, FBody, A(Ink, 200), new RectangleF(card.X + 30, card.Y + 96, card.Width - 60, 32), StringAlignment.Center);

            var br = new Rectangle(card.X + 30, card.Y + 130, card.Width - 60, 10);
            FillRoundedRect(g, br, A(HpGray, 180), 4);
            float prog = Math.Clamp(UpdateService.DownloadProgress, 0f, 1f);
            if (prog > 0f) FillRoundedRect(g, new Rectangle(br.X, br.Y, (int)(br.Width * prog), br.Height), Red, 4);

            if (UpdateService.DownloadFinished)
                Spaced(g, "Instalação pronta", FLabel, A(Dim, 200), card.X + 30, card.Y + 150, 2f);

            Button(g, new Rectangle(card.X + 30, card.Bottom - 78, 215, 56), "updateInstall", "Instalar agora", true);
            Button(g, new Rectangle(card.X + 270, card.Bottom - 78, 145, 56), "updateSoft", "Sem reinstalar", false, FMid, 2.5f);
            Button(g, new Rectangle(card.X + 425, card.Bottom - 78, 215, 56), "updateLater", "Compatível depois", false, FMid, 2.5f);
            if (UpdateService.IgnoreVersion == UpdateService.Available?.Version)
                Spaced(g, "Esta versão será ignorada", FTiny, A(Dim, 200), card.X + 30, card.Bottom - 132, 2f);
            else
                Button(g, new Rectangle(card.X + 425, card.Bottom - 78, 215, 56), "updateIgnore", "Ignorar 1h", false, FMid, 2.5f);
            if (UpdateService.IgnoreVersion == UpdateService.Available?.Version) { }
        }

        // ---------- Lobby ----------
        private void DrawConfigPanel(Graphics g, int w, int h)
        {
            if (!_cfgOpen) return;
            var r = new Rectangle((w - 520) / 2, BarH + 42, 520, 216);
            FillRoundedRect(g, r, Panel, 12);
            using (var p = RoundRect(r, 12)) g.DrawPath(Pn(Line, 1f), p);
            g.FillRectangle(Br(Red), r.X + 22, r.Y, r.Width - 44, 3);
            Spaced(g, "CONFIGURAÇÕES", FTitle, Ink, r.X + 30, r.Y + 18, 4f);
            Txt(g, "Ajustes do jogo", FBody, Dim, new RectangleF(r.X + 30, r.Y + 62, r.Width - 60, 18), StringAlignment.Center);

            int y = r.Y + 100;
            int rowH = 44;
            // volume com slider centralizado
            var volRow = new Rectangle(r.X + 30, y, r.Width - 60, rowH);
            using (var b = new SolidBrush(A(Dim, 200))) g.FillRectangle(b, volRow.X + 40, volRow.Y + 20, volRow.Width - 80, 8);
            int barW = volRow.Width - 80;
            using (var b = new SolidBrush(Red)) g.FillRectangle(b, volRow.X + 40, volRow.Y + 20, barW * _cfgVol / 100f, 8);
            using (var b = new SolidBrush(A(Red, 80))) g.FillEllipse(b, volRow.X + 40 + barW * _cfgVol / 100f - 10, volRow.Y + 16, 20, 16);
            Txt(g, $"VOLUME: {_cfgVol}%", FMid, Ink, new RectangleF(volRow.X + 40 + barW + 16, volRow.Y, 90, rowH), StringAlignment.Center);
            Button(g, new Rectangle(volRow.Right - 120, volRow.Y + 7, 38, 30), "cfgVolDn", "−", false, FMid, 1f);
            Button(g, new Rectangle(volRow.Right - 60, volRow.Y + 7, 38, 30), "cfgVolUp", "+", false, FMid, 1f);

            var lb = new Rectangle(r.X + 30, y + rowH + 6, r.Width - 60, rowH);
            var langBtn = new Rectangle(lb.X + (lb.Width - 340) / 2, lb.Y, 340, 34);
            Button(g, langBtn, "cfgLang", _cfgLangEn ? "IDIOMA: EN" : "IDIOMA: PT", false, FMid, 2.5f);

            var ob = new Rectangle(r.X + 30, y + 2 * rowH + 12, r.Width - 60, rowH);
            var optBtn = new Rectangle(ob.X + (ob.Width - 340) / 2, ob.Y, 340, 34);
            Button(g, optBtn, "cfgOpt", _cfgOpt ? "MODO OTIMIZADO: ON" : "MODO OTIMIZADO: OFF", false, FMid, 2.5f);

            Button(g, new Rectangle((r.Right - 180) / 2, r.Bottom - 40, 180, 32), "cfg", "Fechar", false);
            Button(g, new Rectangle(r.X + 8, r.Y + 4, 60, 24), "viewUpd", "View Update", false);
        }

        private void DrawLobby(Graphics g, int w, int h)
        {
            // seletor de mapa
            bool isRoyale = _map.Mode == "DeadRoyale";
            bool isDeal = _map.Mode == "DealDuo";
            var mode = new Rectangle(w / 2 - 210, BarH + 24, 420, 68);
            Reg(mode, "mode"); float mh = Hv("mode");
            using (var p = Chamfer(mode, 10, true, false, true, false))
            {
                g.FillPath(Br(Mix(Panel, PanelHi, mh * .6f)), p);
                g.DrawPath(Pn(Mix(Line, Red, mh), 1f), p);
            }
            // setas
            var prev = new Rectangle(mode.X + 8, mode.Y + 14, 38, 40);
            var next = new Rectangle(mode.Right - 46, mode.Y + 14, 38, 40);
            Reg(prev, "mapPrev"); Reg(next, "mapNext");
            Arrow(g, prev, -1, Hv("mapPrev"));
            Arrow(g, next, 1, Hv("mapNext"));
            Spaced(g, $"MAPA {_mapIndex + 1}/{_maps.Count}", FTiny, Red, mode.X + 56, mode.Y + 13, 3f);
            Txt(g, _map.Name.ToUpperInvariant(), FName, Ink, new RectangleF(mode.X + 56, mode.Y + 22, 270, 26));
            Txt(g, _map.Type, FSmall, Dim, new RectangleF(mode.X + 56, mode.Y + 46, 280, 18));
            // pips
            for (int i = 0; i < _maps.Count; i++)
            {
                var pip = new Rectangle(mode.Right - 24 - i * 12, mode.Y + 10, 6, 6);
                using var pb = new SolidBrush(i == _mapIndex ? Red : A(Line, 220));
                g.FillEllipse(pb, pip);
            }

            // tamanho de equipe
            bool showSizes = isRoyale || isDeal;
            if (showSizes)
            {
                string[] labels = isRoyale ? new[] { "SOLO", "DUPLA", "TRIO", "SQUAD" } : new[] { "1V1", "2V2" };
                int bw = 78, gap2 = 8;
                int total = labels.Length * bw + (labels.Length - 1) * gap2;
                int sx = (w - total) / 2;
                int sy = mode.Bottom + 10;
                for (int i = 0; i < labels.Length; i++)
                {
                    bool active = isRoyale ? _royaleSquad == i + 1 : _teamSize == i + 1;
                    SizeBtn(g, new Rectangle(sx + i * (bw + gap2), sy, bw, 30), "size" + (i + 1), labels[i], active);
                }
            }
            else
            {
                Spaced(g, "SQUAD 4V4", FLabel, Dim, w / 2f, mode.Bottom + 25, 3f, StringAlignment.Center);
            }

            // botÃ£o iniciar
            var btn = new Rectangle(w / 2 - 190, h - 72 - 64, 380, 72);

            // party
            int cw = 200, gap = 20, n = _players.Length;
            int x0 = (w - (n * cw + (n - 1) * gap)) / 2;
            int top = mode.Bottom + (showSizes ? 58 : 26);
            int ch = Math.Clamp(btn.Y - 56 - top, 220, 460);
            for (int i = 0; i < n; i++)
            {
                string id = "slot:" + i;
                var rect = new Rectangle(x0 + i * (cw + gap), top, cw, ch);
                Reg(rect, id); float hv = Hv(id);
                rect.Y -= (int)(6 * hv);

                using (var path = Chamfer(rect, 16, true, false, true, false))
                {
                    using (var lb = new LinearGradientBrush(rect, PanelHi, Panel, 90f)) g.FillPath(lb, path);
                    g.DrawPath(Pn(Mix(Line, Red, hv * .8f), 1f), path);
                }
                g.FillRectangle(Br(i == 0 ? Red : Line), rect.X + 16, rect.Y, rect.Width - 16, 3);

                // silhueta
                g.SetClip(new Rectangle(rect.X + 1, rect.Y + 4, rect.Width - 2, rect.Height - 8));
                float hd = cw * .30f;
                g.FillEllipse(Br(A(i == 0 ? Red : Ink, i == 0 ? 90 : 40)), rect.X + (cw - hd) / 2f, rect.Y + ch * .22f, hd, hd);
                float sw = cw * .80f;
                g.FillEllipse(Br(A(i == 0 ? Red : Ink, i == 0 ? 70 : 30)), rect.X + (cw - sw) / 2f, rect.Y + ch * .22f + hd + 10, sw, sw);
                g.ResetClip();

                // plaquinha de nome
                var plate = new Rectangle(rect.X, rect.Bottom - 70, rect.Width, 70);
                g.FillRectangle(Br(A(Color.Black, 120)), plate);
                Txt(g, _players[i], FMid, Ink, new RectangleF(plate.X + 18, plate.Y + 12, plate.Width - 36, 24));
                if (i == 0) Spaced(g, "LÃDER", FTiny, Red, plate.X + 18, plate.Y + 50, 3f);
                else { g.FillEllipse(Br(Green), plate.X + 18, plate.Y + 45, 8, 8); Spaced(g, "PRONTO", FTiny, Green, plate.X + 32, plate.Y + 50, 3f); }
            }

            // fila
            if (_queue)
            {
                var el = _queueSw.Elapsed;
                float tw = Spaced(g, $"BUSCANDO PARTIDA   {(int)el.TotalMinutes}:{el.Seconds:00}", FSmallB, Ink, w / 2f + 12, btn.Y - 28, 3f, StringAlignment.Center);
                var arc = new RectangleF(w / 2f - tw / 2f - 18, btn.Y - 38, 20, 20);
                using var ap = new Pen(Red, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(ap, arc, (_clock * 360f) % 360f, 100f);
            }
            Button(g, btn, "start", _queue ? "CANCELAR" : "INICIAR", !_queue, FStart, 6f);
            Button(g, new Rectangle((w - 200) / 2, btn.Bottom + 12, 200, 38), "cfg", "CONFIG", false);
            string[] squadLbl = { "SOLO", "DUPLA", "TRIO", "SQUAD" };
            int sqBase = isRoyale ? _royaleSquad - 1 : _teamSize - 1;
            for (int i = 0; i < 4; i++)
                Button(g, new Rectangle((w - 400) / 2 + i * 104, btn.Bottom + 48, 100, 34), "size" + (i + 1), squadLbl[i], i == sqBase, FMid, 2f);
            Txt(g, "Solo = sem aliados", FTiny, Dim, new RectangleF(0, BarH + 300, w, 12), StringAlignment.Center);
        }

        // ---------- Armas ----------
        private void DrawWeapons(Graphics g, int w, int h)
        {
            int lx = 48, ly = BarH + 48;
            Spaced(g, "ARSENAL", FLabel, Red, lx, ly, 4f);
            for (int i = 0; i < _guns.Length; i++)
            {
                var rr = new Rectangle(lx, ly + 24 + i * 62, 300, 56);
                if (rr.Bottom > h - 24) break;
                string id = "gun:" + i; Reg(rr, id);
                float hv = Hv(id); bool sel = i == _gun;
                g.FillRectangle(Br(sel ? A(PanelHi, 255) : A(Color.White, (int)(10 * hv))), rr);
                if (sel) g.FillRectangle(Br(Red), rr.X, rr.Y, 4, rr.Height);
                Txt(g, _guns[i].Name.ToUpperInvariant(), FName, sel ? Ink : Mix(Dim, Ink, hv), new RectangleF(rr.X + 22, rr.Y + 6, rr.Width - 40, 28));
                Txt(g, $"{_guns[i].Skins.Count} SKINS", FTiny, Dim, new RectangleF(rr.X + 22, rr.Y + 33, rr.Width - 40, 16));
            }

            var gun = _guns[_gun];
            int si = SkinIdx(gun);
            var skin = gun.Skins[si];
            int rx = 400, right = w - 48;
            if (right - rx < 320) return;

            Txt(g, gun.Name.ToUpperInvariant(), FWater, A(Ink, 10), new RectangleF(rx - 6, BarH + 20, right - rx, 130));
            Txt(g, gun.Name.ToUpperInvariant(), FTitle, Ink, new RectangleF(rx, BarH + 58, right - rx - 230, 56));
            Spaced(g, "SKIN  /  " + skin.Name.ToUpperInvariant(), FLabel, Red, rx, BarH + 126, 3f);

            bool equipped = _equipped.TryGetValue(gun.GunId, out var eq) && eq == skin.SkinId;
            var eb = new Rectangle(right - 210, BarH + 60, 210, 52);
            if (equipped)
            {
                using var p = Chamfer(eb, 12, true, false, true, false);
                g.FillPath(Br(A(Green, 30)), p);
                g.DrawPath(Pn(Green, 2f), p);
                Spaced(g, "EQUIPADA", FMid, Green, eb.X + eb.Width / 2f, eb.Y + eb.Height / 2f, 3f, StringAlignment.Center);
            }
            else Button(g, eb, "equip", "EQUIPAR", true, FMid, 3f);

            // prÃ©via
            int py = BarH + 156, skinH = 112, skinY = h - 100 - skinH;
            var prev = new Rectangle(rx, py, right - rx, Math.Max(140, skinY - 24 - py));
            using (var p = Chamfer(prev, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 210)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
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
                Spaced(g, "PRÃ‰VIA DA ARMA", FMid, Dim, prev.X + prev.Width / 2f, prev.Y + prev.Height / 2f - 10, 4f, StringAlignment.Center);
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
                if (isEq) Spaced(g, "EQUIPADA", FTiny, Green, rect.X + 14, rect.Y + 98, 2f);
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
                    g.FillPath(Br(Mix(Panel, PanelHi, hv)), p);
                    g.DrawPath(Pn(Mix(Line, Red, hv), 1f), p);
                }
                g.FillEllipse(Br(Green), pill.X + 18, pill.Y + 19, 10, 10);
                Spaced(g, "AMIGOS", FNav, Ink, pill.X + 40, pill.Y + 24, 3f);
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

            // cabeÃ§alho
            float hw = Spaced(g, "AMIGOS", FNav, Ink, pr.X + 22, pr.Y + 32, 3f);
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

            g.SetClip(view);
            int y = view.Y + 8 - (int)_fscroll;
            bool onHdr = false, offHdr = false;
            for (int i = 0; i < _friendList.Count; i++)
            {
                var f = _friendList[i];
                if (f.St != FStatus.Offline && !onHdr) { Spaced(g, $"ONLINE â€” {online}", FTiny, Dim, pr.X + 22, y + 15, 2f); y += 30; onHdr = true; }
                if (f.St == FStatus.Offline && !offHdr) { Spaced(g, $"OFFLINE â€” {off}", FTiny, Dim, pr.X + 22, y + 15, 2f); y += 30; offHdr = true; }
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

            // rodapÃ©
            g.DrawLine(Pn(Line, 1f), pr.X, view.Bottom, pr.Right, view.Bottom);
            Button(g, new Rectangle(pr.X + 20, pr.Bottom - 56, 70, 42), "frdMenu", "PEDIDOS", false, FLabel, 2.5f);
            Button(g, new Rectangle(pr.X + 95, pr.Bottom - 56, pr.Width - 115, 42), "addFriend", "ADICIONAR AMIGO", false, FLabel, 2.5f);

            g.ResetTransform();
            _off = Point.Empty;
            if (_requestsOpen) DrawFriendRequests(g, pr);
        }

        private void DrawFriendRequests(Graphics g, Rectangle basePr)
        {
            var r = new Rectangle(basePr.X, basePr.Bottom + 6, 360, _friendRequests.Count == 0 ? 56 : 26 + _friendRequests.Count * 26 + 8);
            FillRoundedRect(g, r, Panel, 8);
            using (var p = RoundRect(r, 8)) g.DrawPath(Pn(Line, 1f), p);
            Spaced(g, "PEDIDOS", FLabel, Ink, r.X + 16, r.Y + 12, 3f);
            if (_friendRequests.Count == 0)
            {
                Txt(g, "Nenhum pedido disponível", FBody, Dim, new RectangleF(r.X + 18, r.Y + 32, r.Width - 36, 22));
                return;
            }
            for (int i = 0; i < _friendRequests.Count; i++)
            {
                var row = new Rectangle(r.X + 12, r.Y + 32 + i * 26, r.Width - 24, 20);
                Txt(g, _friendRequests[i], FBody, Ink, row);
                var accept = new Rectangle(row.Right - 84, row.Y - 4, 40, 28);
                Reg(accept, "frdAccept:" + i);
                float ah = Hv("frdAccept:" + i);
                FillRoundedRect(g, accept, Mix(Red, RedHi, ah), 5);
                Txt(g, "Aceitar", FTiny, Color.White, accept, StringAlignment.Center);
                var rec = new Rectangle(accept.Right + 8, row.Y - 4, 40, 28);
                Reg(rec, "frdReject:" + i);
                float rh = Hv("frdReject:" + i);
                FillRoundedRect(g, rec, Mix(A(Ink, 120), A(Ink, 60), rh), 5);
                Txt(g, "Recusar", FTiny, Ink, rec, StringAlignment.Center);
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
                Spaced(g, "CONVIDAR", FTiny, A(Red, (int)(255 * hv)), row.Right - 22, row.Y + 21, 2f, StringAlignment.Far);
            var rm = new Rectangle(row.Right - 30, row.Y + 24, 22, 22);
            Reg(rm, "frdRemove:" + idx);
            float rv = Hv("frdRemove:" + idx);
            using (var rb = new SolidBrush(A(Red, off ? 60 : 90 + (int)(50 * rv)))) g.FillEllipse(rb, rm);
            Txt(g, "✕", FSmallB, off ? Dim : Color.White, rm, StringAlignment.Center);
        }

        private void DrawToast(Graphics g, int w)
        {
            if (_toastLeft <= 0f) return;
            float a = Math.Min(1f, Math.Min(_toastLeft / 0.4f, (2.6f - _toastLeft) / 0.2f));
            float tw = g.MeasureString(_toast, FMid).Width + 56;
            var r = new RectangleF((w - tw) / 2f, BarH + 18, tw, 44);
            using (var p = Chamfer(r, 10, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, (int)(250 * a))), p);
                g.DrawPath(Pn(A(Line, (int)(255 * a)), 1f), p);
            }
            g.FillRectangle(Br(A(Red, (int)(255 * a))), r.X + 10, r.Y, 4, r.Height);
            Txt(g, _toast, FMid, A(Ink, (int)(255 * a)), new RectangleF(r.X + 28, r.Y, r.Width - 40, r.Height));
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







