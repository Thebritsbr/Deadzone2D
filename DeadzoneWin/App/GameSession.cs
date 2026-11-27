// GameSession.cs — Lógica da partida: Deal Duo, Squad Battle e Dead Royale.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Deadzone2D
{
    public sealed class GameSession
    {
        public readonly GameMap MapDef;
        public readonly GameplayConfig Rules;
        public readonly WorldMap Map;
        public readonly SessionKind Kind;
        public readonly string Mode;

        public readonly List<Fighter> Fighters = new();
        public readonly List<Bullet> Bullets = new();
        public readonly List<LootItem> Loot = new();
        public readonly List<Particle> Particles = new();
        public readonly List<Ping> Pings = new();
        public readonly List<KillEntry> Feed = new();
        public readonly List<DamageNumber> DamageNumbers = new();

        public SessionPhase Phase = SessionPhase.Live;
        public int Score0, Score1, MaxWins = 3, Round;
        public int WinnerTeam = -1;
        public int PlayerPlacement = 0;

        public string BannerText = "", BannerSub = "";
        public Color BannerBg = Color.FromArgb(85, 85, 85), BannerFg = Color.White;
        public float BannerLife, BannerMax;
        public bool BannerMatch;

        public float Countdown;
        public bool Prep;
        public float PhaseTimer;
        public float IntroTimer;
        public string IntroText = "";
        public float Time, MatchTime;
        public float PlayerDamageFlash, DamageAngle;
        public float HitMarker, PlayerRecoil, CameraShake;

        private readonly Random _rng = new Random(90210);
        private readonly string[] _names;
        private bool _royaleBotsAdded;

        public Fighter Player => Fighters[0];
        public int AliveCount => Fighters.Count(f => f.Alive);
        public int PlayerTeamAlive => Fighters.Count(f => f.Team == 0 && f.Alive);
        public bool PlayerEliminated => !Player.Alive;

        public GameSession(GameMap def, GameplayConfig rules, int teamSize, string[] names)
        {
            MapDef = def;
            Rules = rules;
            Mode = def.Mode;
            Kind = Mode == "DeadRoyale" ? SessionKind.Royale : SessionKind.Team;
            Map = WorldMap.Build(def, 20261124);
            Loot.AddRange(Map.Loot);
            _names = names;
            var r = rules.For(Mode);
            MaxWins = def.MaxWins > 0 ? def.MaxWins : r.MaxWins;
            if (Kind == SessionKind.Royale) SetupRoyale(Math.Clamp(teamSize, 1, 4));
            else SetupTeam(Mode == "SquadBattle" ? 4 : Math.Clamp(teamSize, 1, 2));
        }

        // ---------- Montagem ----------
        private void SetupTeam(int teamSize)
        {
            for (int i = 0; i < teamSize; i++)
                AddFighter(i == 0 ? _names[0] : NameOf(i), 0, Map.Spawns0, i, i == 0);
            for (int i = 0; i < teamSize; i++)
                AddFighter(BotName(i), 1, Map.Spawns1, i, false);
            Score0 = Score1 = 0;
            Round = 0;
            StartRound();
        }

        private void SetupRoyale(int squadSize)
        {
            int total = Math.Clamp(Rules.DeadRoyaleMaxPlayers > 0 ? Rules.DeadRoyaleMaxPlayers : 30, 2, 30);
            var rsp = Map.RoyaleSpawns;
            var first = rsp.Count > 0 ? rsp[0] : new PointF(Map.W / 2f, Map.H / 2f);
            // Dead Royale começa só com jogadores reais (o grupo do jogador); sem bots.
            for (int i = 0; i < squadSize && Fighters.Count < total; i++)
                AddFighter(i == 0 ? _names[0] : NameOf(i), 0, rsp, 0, i == 0, SquadOffset(first, i));

            Score0 = Score1 = 0;
            Round = 1;
            WinnerTeam = -1;
            Phase = SessionPhase.Countdown;
            Prep = true;
            Countdown = Rules.DeadRoyaleCountdown > 0 ? Rules.DeadRoyaleCountdown : MapDef.CountdownSeconds;
            if (Countdown <= 0) Countdown = 180;
            // Com a sala cheia (30 players) a espera cai para 10 segundos.
            if (Fighters.Count >= 30) Countdown = 10f;
            _royaleBotsAdded = false;
            IntroText = "";
            MatchTime = 0;
        }

        // Reforço automático: com 1 minuto restante e 10 players ou menos, entram 20 bots.
        private void AddRoyaleBots(int count)
        {
            int total = Math.Clamp(Rules.DeadRoyaleMaxPlayers > 0 ? Rules.DeadRoyaleMaxPlayers : 30, 2, 30);
            var rsp = Map.RoyaleSpawns;
            int spawnIdx = 1 + Fighters.Count % Math.Max(1, rsp.Count);
            int team = 100 + Fighters.Count;
            for (int i = 0; i < count && Fighters.Count < total; i++)
            {
                var bsp = rsp.Count > 0 ? rsp[spawnIdx++ % rsp.Count] : new PointF(Map.W / 2f, Map.H / 2f);
                var f = AddFighter(BotName(i), team + i, rsp, 0, false, SquadOffset(bsp, i % 4));
                EquipRoyaleBot(f);
            }
        }

        private static PointF SquadOffset(PointF p, int i)
        {
            if (i == 0) return p;
            double a = (i - 1) * 2.399963 + 0.7;
            float r = 30 + (i / 4) * 26;
            return new PointF(p.X + (float)Math.Cos(a) * r, p.Y + (float)Math.Sin(a) * r);
        }

        private string NameOf(int i) => i < _names.Length ? _names[i] : "ALIADO " + i;

        private static readonly string[] BotPool =
        {
            "VYPER", "SABLE", "KAIRO", "NOVA", "RAZE", "ONYX", "ZERO", "HAVOC",
            "DRIFT", "ECHO", "FROST", "GHOST", "IRIS", "JINX", "KILO", "LUNA",
            "MIRAGE", "NEXUS", "ORION", "PULSE", "QUAKE", "RIFT", "STORM", "TALON",
            "UMBER", "VENOM", "WRAITH", "XENO", "YARA", "ZULU", "BLAZE", "CIPHER",
            "DUSK", "EMBER", "FLARE", "GRAVE", "HELIX", "INFERNO", "JOLT", "KARMA",
        };

        private string BotName(int index) => BotPool[index % BotPool.Length] + (index >= BotPool.Length ? "-" + (index / BotPool.Length + 1) : "");

        private Fighter AddFighter(string name, int team, List<PointF> spawns, int index, bool player, PointF? at = null)
        {
            var sp = at ?? (spawns.Count > 0 ? spawns[index % spawns.Count] : new PointF(Map.W / 2f, Map.H / 2f));
            var free = Map.FindFree(sp.X, sp.Y);
            var f = new Fighter
            {
                Id = Fighters.Count,
                Name = name,
                Team = team,
                IsPlayer = player,
                X = free.X,
                Y = free.Y,
                AiSkill = .4f + (float)_rng.NextDouble() * .45f,
                AiWanderX = free.X,
                AiWanderY = free.Y,
                Medkits = player ? 1 : 0,
            };
            f.Mag = f.Weapon.MagSize;
            f.Reserve = f.Mag * 4;
            Fighters.Add(f);
            return f;
        }

        private void EquipRoyaleBot(Fighter f)
        {
            f.WeaponId = Arsenal.LootGuns[_rng.Next(Arsenal.LootGuns.Length)];
            f.Mag = f.Weapon.MagSize;
            f.Reserve = f.Mag * 3;
            f.AiSkill = .35f + (float)_rng.NextDouble() * .5f;
            f.Medkits = 0;
            f.Armor = 25f;
        }

        private void StartRound()
        {
            Round++;
            int i0 = 0, i1 = 0;
            foreach (var f in Fighters)
            {
                f.Alive = true;
                f.Health = 100f;
                f.Armor = 0f;
                f.WeaponId = "Glock";
                f.Mag = f.Weapon.MagSize;
                f.Reserve = f.Mag * 3;
                f.FireCd = 0f;
                f.ReloadLeft = 0f;
                f.HitFlash = 0f;
                f.Muzzle = 0f;
                f.AiTarget = -1;
                f.Medkits = f.IsPlayer ? 1 : 0;
                f.Coins = 0;
                f.Vests = 0;
                var list = f.Team == 0 ? Map.Spawns0 : Map.Spawns1;
                int idx = f.Team == 0 ? i0++ : i1++;
                var sp = list.Count > 0 ? list[idx % list.Count] : new PointF(Map.W / 2f, Map.H / 2f);
                var free = Map.FindFree(sp.X + (idx % 2) * 24, sp.Y + (idx % 3) * 18);
                f.X = free.X;
                f.Y = free.Y;
                f.Angle = f.Team == 0 ? -0.6f : 2.4f;
            }
            Bullets.Clear();
            Particles.Clear();
            DamageNumbers.Clear();
            Pings.Clear();
            BannerLife = 0;
            PlayerDamageFlash = 0;
            HitMarker = 0;
            PlayerRecoil = 0;
            Phase = SessionPhase.Live;
            IntroTimer = 2.2f;
            IntroText = "ROUND " + Round;
        }

        // ---------- Update ----------
        public void Update(float dt, GameInput input)
        {
            dt = Math.Min(dt, .05f);
            TimeDelta = dt;
            Time += dt;
            UpdateEffects(dt);

            if (Phase == SessionPhase.Countdown)
            {
                Countdown = Math.Max(0f, Countdown - dt);
                if (Kind == SessionKind.Royale)
                {
                    // 30 players na sala: contagem cai automaticamente para 00:10.
                    if (Fighters.Count >= 30 && Countdown > 10f) Countdown = 10f;
                    // Último minuto com 10 players ou menos: entram 20 bots de reforço.
                    if (!_royaleBotsAdded && Countdown <= 60f && AliveCount <= 10)
                    {
                        _royaleBotsAdded = true;
                        AddRoyaleBots(20);
                        IntroText = "REINFORCEMENTS ARRIVED";
                        IntroTimer = 2f;
                    }
                }
                UpdatePlayerMovement(input, false);
                UpdateBots(dt, false);
                if (Countdown <= 0f) StartCombat();
            }
            else if (Phase == SessionPhase.Live)
            {
                MatchTime += dt;
                UpdatePlayer(input);
                UpdateBots(dt, true);
                UpdateBullets(dt);
                if (Phase == SessionPhase.Live) CheckEnd();
            }
            else if (Phase == SessionPhase.RoundOver)
            {
                PhaseTimer -= dt;
                if (PhaseTimer <= 0f)
                {
                    if (WinnerTeam >= 0) Phase = SessionPhase.MatchOver;
                    else StartRound();
                }
            }
        }

        private void StartCombat()
        {
            Prep = false;
            Countdown = 0f;
            Phase = SessionPhase.Live;
            IntroTimer = 2.5f;
            IntroText = "SOBREVIVA";
            BannerLife = 0f;
            foreach (var f in Fighters) f.AiTarget = -1;
        }

        public void SkipCountdown()
        {
            if (Kind == SessionKind.Royale && Phase == SessionPhase.Countdown) StartCombat();
        }

        private void UpdatePlayer(GameInput input)
        {
            var p = Player;
            if (!p.Alive) return;
            UpdatePlayerMovement(input, true);
            if (p.ReloadLeft > 0f)
            {
                p.ReloadLeft -= TimeDelta;
                if (p.ReloadLeft <= 0f)
                {
                    p.ReloadLeft = 0f;
                    var w = p.Weapon;
                    int take = Math.Min(w.MagSize - p.Mag, p.Reserve);
                    p.Mag += take;
                    p.Reserve -= take;
                }
            }
            if (input.Fire && p.ReloadLeft <= 0f)
            {
                if (p.Mag <= 0) TryReload(p);
                else Shoot(p, p.Angle);
            }
        }

        private float TimeDelta = .016f;

        private void UpdatePlayerMovement(GameInput input, bool combat)
        {
            var p = Player;
            if (!p.Alive) return;
            p.Angle = (float)Math.Atan2(input.AimY - p.Y, input.AimX - p.X);
            p.Sprinting = input.Sprint;
            float dx = (input.Right ? 1f : 0f) - (input.Left ? 1f : 0f);
            float dy = (input.Down ? 1f : 0f) - (input.Up ? 1f : 0f);
            if (dx != 0f || dy != 0f)
            {
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                float speed = p.Sprinting ? 315f : 238f;
                Map.Move(p, dx / len * speed * TimeDelta, dy / len * speed * TimeDelta);
                p.WalkPhase += TimeDelta * 11f;
                if (_rng.NextDouble() < TimeDelta * 9)
                    Burst(p.X, p.Y + 10, 0, 1, Color.FromArgb(120, 110, 90), 18f);
            }
            else p.WalkPhase += TimeDelta * 2f;
        }

        // ---------- Tiro ----------
        public bool Shoot(Fighter f, float angle)
        {
            if (f == null || !f.Alive || f.FireCd > 0f || f.ReloadLeft > 0f || Phase != SessionPhase.Live) return false;
            var w = f.Weapon;
            if (f.IsPlayer)
            {
                if (f.Mag <= 0) { TryReload(f); return false; }
                f.Mag--;
            }
            f.FireCd = w.FireInterval * (f.IsPlayer ? 1f : Kind == SessionKind.Royale ? 2.55f : 2.35f);
            f.Muzzle = .07f;
            f.Angle = angle;

            float spread = w.Spread * (f.IsPlayer ? (f.Sprinting ? 1.7f : 1f) : Kind == SessionKind.Royale ? 2.35f : 2.1f);
            float a = angle + (float)((_rng.NextDouble() * 2 - 1) * spread);
            float sx = f.X + (float)Math.Cos(a) * 22f;
            float sy = f.Y + (float)Math.Sin(a) * 22f;
            float speed = w.BulletSpeed;
            Bullets.Add(new Bullet
            {
                X = sx, Y = sy, TrailX = sx, TrailY = sy,
                VX = (float)Math.Cos(a) * speed, VY = (float)Math.Sin(a) * speed,
                OwnerId = f.Id, Team = f.Team,
                Damage = w.Damage * (f.IsPlayer ? 1f : Kind == SessionKind.Royale ? .45f : .62f),
                MaxRange = w.Range,
                Tracer = w.Tracer,
            });
            AddPing(f.X, f.Y, f.Team);
            for (int i = 0; i < 4; i++)
                Burst(sx, sy, 1, 1, w.Tracer, 120f);

            if (f.IsPlayer)
            {
                PlayerRecoil = Math.Min(1f, PlayerRecoil + .28f);
                CameraShake = Math.Min(8f, CameraShake + (w.Id == "SniperAWM" ? 4.5f : 1.6f));
            }
            return true;
        }

        public bool TryReload(Fighter f)
        {
            if (f == null || !f.Alive || f.ReloadLeft > 0f) return false;
            var w = f.Weapon;
            if (f.Mag >= w.MagSize || f.Reserve <= 0) return false;
            f.ReloadLeft = w.ReloadTime;
            return true;
        }

        private void UpdateBullets(float dt)
        {
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var b = Bullets[i];
                b.Life -= dt;
                if (b.Life <= 0f) { Bullets.RemoveAt(i); continue; }
                float move = (float)Math.Sqrt(b.VX * b.VX + b.VY * b.VY) * dt;
                int steps = Math.Max(1, (int)Math.Ceiling(move / 16f));
                bool hit = false;
                for (int s = 0; s < steps && !hit; s++)
                {
                    b.TrailX = b.X;
                    b.TrailY = b.Y;
                    b.X += b.VX * dt / steps;
                    b.Y += b.VY * dt / steps;
                    b.Traveled += move / steps;
                    if (b.Traveled > b.MaxRange) { hit = true; break; }
                    if (Map.BlockedPoint(b.X, b.Y))
                    {
                        Burst(b.X, b.Y, 1, 4, Color.FromArgb(220, 190, 130), 90f);
                        hit = true;
                        break;
                    }
                    foreach (var f in Fighters)
                    {
                        if (!f.Alive || f.Team == b.Team || f.Id == b.OwnerId) continue;
                        float dx = f.X - b.X, dy = f.Y - b.Y;
                        if (dx * dx + dy * dy < 19f * 19f)
                        {
                            float dmg = b.Damage * Math.Max(.55f, 1f - b.Traveled / b.MaxRange * .4f);
                            DamageFighter(f, b.OwnerId, dmg, (float)Math.Atan2(b.VY, b.VX));
                            hit = true;
                            break;
                        }
                    }
                }
                if (hit) Bullets.RemoveAt(i);
            }
        }

        // ---------- Dano / morte ----------
        private void DamageFighter(Fighter victim, int attackerId, float amount, float angle)
        {
            if (!victim.Alive) return;
            float dmg = amount;
            if (victim.Armor > 0f)
            {
                float absorb = Math.Min(victim.Armor, dmg * .5f);
                victim.Armor -= absorb;
                dmg -= absorb;
            }
            victim.Health -= dmg;
            victim.HitFlash = .14f;
            var attacker = attackerId >= 0 && attackerId < Fighters.Count ? Fighters[attackerId] : null;
            if (attacker != null && attacker.IsPlayer) HitMarker = .2f;
            if (victim.IsPlayer)
            {
                PlayerDamageFlash = Math.Min(1f, PlayerDamageFlash + .7f);
                DamageAngle = angle;
                CameraShake = Math.Min(10f, CameraShake + 3.5f);
            }
            attacker.DamageDealt += Math.Max(1, (int)Math.Round(dmg));
            DamageNumbers.Add(new DamageNumber
            {
                X = victim.X + (float)(_rng.NextDouble() * 16 - 8),
                Y = victim.Y - 22,
                Life = .8f, MaxLife = .8f,
                Amount = Math.Max(1, (int)Math.Round(dmg)),
                ByPlayer = attacker != null && attacker.IsPlayer,
            });
            for (int i = 0; i < 5; i++) Burst(victim.X, victim.Y, 2, 1, Color.FromArgb(160, 35, 45), 110f);
            if (victim.Health <= 0f) Kill(victim, attacker);
        }

        private void Kill(Fighter victim, Fighter? attacker)
        {
            victim.Alive = false;
            victim.Health = 0f;
            victim.Deaths++;
            victim.CorpseAngle = victim.Angle;
            if (victim.IsPlayer)
                PlayerPlacement = Math.Max(1, AliveCount + 1);
            if (attacker != null && attacker != victim)
            {
                attacker.Kills++;
                Feed.Add(new KillEntry
                {
                    Killer = attacker.Name,
                    Victim = victim.Name,
                    Weapon = attacker.Weapon.Name,
                    KillerTeam = attacker.Team,
                    Life = 5f,
                });
                if (attacker.IsPlayer) HitMarker = .3f;
            }
            else
            {
                Feed.Add(new KillEntry { Killer = "", Victim = victim.Name, Weapon = victim.Weapon.Name, KillerTeam = -1, Life = 5f });
            }
            AddPing(victim.X, victim.Y, -1, 2.2f);
            for (int i = 0; i < 12; i++) Burst(victim.X, victim.Y, 2, 1, Color.FromArgb(130, 25, 35), 150f);
            if (Feed.Count > 8) Feed.RemoveAt(0);
        }

        private void CheckEnd()
        {
            if (Kind == SessionKind.Team)
            {
                bool a0 = Fighters.Any(f => f.Team == 0 && f.Alive);
                bool a1 = Fighters.Any(f => f.Team == 1 && f.Alive);
                if (!a0 || !a1) EndRound(a0 ? 0 : 1);
            }
            else
            {
                var teams = Fighters.Where(f => f.Alive).Select(f => f.Team).Distinct().ToList();
                if (teams.Count <= 1) EndRoyale(teams.Count == 1 ? teams[0] : -1);
            }
        }

        private void EndRound(int winner)
        {
            if (Phase != SessionPhase.Live) return;
            if (winner == 0) Score0++; else Score1++;
            bool match = Score0 >= MaxWins || Score1 >= MaxWins;
            WinnerTeam = match ? (Score0 >= MaxWins ? 0 : 1) : -1;
            var rules = Rules.For(Mode);
            bool playerWon = winner == 0;

            if (match)
            {
                bool pw = WinnerTeam == 0;
                BannerText = (pw ? rules.WinText : rules.LossText).ToUpperInvariant();
                BannerSub = $"{Score0} X {Score1}";
                BannerBg = ParseColor(pw ? rules.WinBackground : rules.LossBackground);
                BannerFg = pw ? Color.FromArgb(22, 20, 12) : Color.White;
                BannerMatch = true;
                BannerLife = 99999f;
                BannerMax = 99999f;
                Phase = SessionPhase.MatchOver;
                PhaseTimer = 0f;
            }
            else
            {
                BannerText = playerWon ? "ROUND WON" : rules.LossText.ToUpperInvariant();
                BannerSub = $"{Score0} X {Score1}";
                BannerBg = playerWon ? Color.FromArgb(26, 42, 50) : ParseColor(rules.LossBackground);
                BannerFg = playerWon ? Color.FromArgb(110, 240, 200) : Color.White;
                BannerMatch = false;
                BannerLife = 2.7f;
                BannerMax = 2.7f;
                Phase = SessionPhase.RoundOver;
                PhaseTimer = 3.1f;
            }
        }

        private void EndRoyale(int winnerTeam)
        {
            if (Phase == SessionPhase.MatchOver) return;
            Phase = SessionPhase.MatchOver;
            WinnerTeam = winnerTeam;
            bool pw = winnerTeam == 0;
            BannerText = pw ? "YOU WIN!" : "DEFEAT";
            BannerSub = pw ? "1º LUGAR • " + Player.Kills + " KILLS"
                           : (PlayerPlacement > 0 ? "#" + PlayerPlacement + " • " + Player.Kills + " KILLS" : Player.Kills + " KILLS");
            BannerBg = pw ? Color.FromArgb(242, 200, 70) : Color.FromArgb(85, 85, 85);
            BannerFg = pw ? Color.FromArgb(24, 20, 10) : Color.White;
            BannerMatch = true;
            BannerLife = 99999f;
            BannerMax = 99999f;
        }

        // ---------- IA ----------
        private void UpdateBots(float dt, bool combat)
        {
            foreach (var f in Fighters)
            {
                if (!f.Alive || f.IsPlayer) continue;
                if (combat && f.Health < 48f && f.Medkits > 0 && _rng.NextDouble() < dt * .9)
                    UseMedkit(f);
                f.AiTimer -= dt;
                if (f.AiTimer <= 0f) ThinkBot(f, combat);
                if (!combat)
                {
                    MoveBot(f, f.AiMoveX, f.AiMoveY, dt, 88f);
                    f.WalkPhase += dt * 7f;
                    continue;
                }
                var target = f.AiTarget >= 0 && f.AiTarget < Fighters.Count ? Fighters[f.AiTarget] : null;
                if (target != null && target.Alive)
                {
                    f.Angle = (float)Math.Atan2(target.Y - f.Y, target.X - f.X);
                    float d = Dist(f.X, f.Y, target.X, target.Y);
                    MoveBot(f, f.AiMoveX, f.AiMoveY, dt, d < 160f ? 105f : 148f);
                    if (!Map.SegmentBlocked(f.X, f.Y, target.X, target.Y) && d < f.Weapon.Range * .92f && f.FireCd <= 0f)
                    {
                        float jitter = (1f - f.AiSkill) * .14f;
                        Shoot(f, f.Angle + (float)((_rng.NextDouble() * 2 - 1) * jitter));
                    }
                }
                else MoveBot(f, f.AiMoveX, f.AiMoveY, dt, 120f);
                f.WalkPhase += dt * 9f;
            }
        }

        private void ThinkBot(Fighter f, bool combat)
        {
            f.AiTimer = .16f + (float)_rng.NextDouble() * .18f;
            if (!combat)
            {
                if (Dist(f.X, f.Y, f.AiWanderX, f.AiWanderY) < 54f || _rng.NextDouble() < .04)
                {
                    f.AiWanderX = f.X + (float)(_rng.NextDouble() * 2 - 1) * 240f;
                    f.AiWanderY = f.Y + (float)(_rng.NextDouble() * 2 - 1) * 240f;
                }
                f.AiMoveX = f.AiWanderX - f.X;
                f.AiMoveY = f.AiWanderY - f.Y;
                return;
            }

            Fighter? target = null;
            float best = float.MaxValue;
            foreach (var o in Fighters)
            {
                if (!o.Alive || o.Team == f.Team) continue;
                float d = Dist(f.X, f.Y, o.X, o.Y);
                if (d < best) { best = d; target = o; }
            }
            if (target == null)
            {
                f.AiTarget = -1;
                f.AiMoveX = Map.W / 2f - f.X;
                f.AiMoveY = Map.H / 2f - f.Y;
                return;
            }

            f.AiTarget = target.Id;
            bool los = !Map.SegmentBlocked(f.X, f.Y, target.X, target.Y);
            float tx = target.X - f.X, ty = target.Y - f.Y;
            float len = Math.Max(1f, (float)Math.Sqrt(tx * tx + ty * ty));
            tx /= len; ty /= len;

            if (best > 430f || !los)
            {
                f.AiMoveX = tx; f.AiMoveY = ty;
            }
            else if (best < 165f)
            {
                f.AiMoveX = -tx; f.AiMoveY = -ty;
            }
            else
            {
                if (_rng.NextDouble() < .22) f.AiStrafe = -f.AiStrafe;
                f.AiMoveX = -ty * f.AiStrafe + tx * .22f;
                f.AiMoveY = tx * f.AiStrafe + ty * .22f;
            }
            f.AiMoveX += (float)(_rng.NextDouble() * 2 - 1) * .22f;
            f.AiMoveY += (float)(_rng.NextDouble() * 2 - 1) * .22f;
        }

        private void MoveBot(Fighter f, float mx, float my, float dt, float speed)
        {
            float len = (float)Math.Sqrt(mx * mx + my * my);
            if (len < .01f) return;
            float nx = mx / len, ny = my / len;
            if (Map.Move(f, nx * speed * dt, ny * speed * dt)) return;

            float[] turns = { .65f, -.65f, 1.25f, -1.25f, 1.9f, -1.9f };
            foreach (var t in turns)
            {
                float c = (float)Math.Cos(t), s = (float)Math.Sin(t);
                float rx = nx * c - ny * s, ry = nx * s + ny * c;
                if (Map.Move(f, rx * speed * dt, ry * speed * dt))
                {
                    f.AiMoveX = rx;
                    f.AiMoveY = ry;
                    return;
                }
            }
        }

        // ---------- Loot ----------
        public LootItem? NearestLoot(Fighter f)
        {
            if (f == null || !f.Alive) return null;
            LootItem? best = null;
            float bd = 88f;
            foreach (var item in Loot)
            {
                if (item.Taken) continue;
                float d = Dist(f.X, f.Y, item.X, item.Y);
                if (d < bd) { bd = d; best = item; }
            }
            return best;
        }

        public LootItem? TryPickup(Fighter f)
        {
            var item = NearestLoot(f);
            if (item == null) return null;
            item.Taken = true;
            switch (item.Kind)
            {
                case "weapon":
                    f.WeaponId = item.WeaponId;
                    f.Mag = f.Weapon.MagSize;
                    f.Reserve = f.Mag * 3;
                    f.ReloadLeft = 0f;
                    break;
                case "coin":
                    f.Coins += item.Amount;
                    break;
                case "medkit":
                    f.Medkits = Math.Min(5, f.Medkits + item.Amount);
                    break;
                case "vest":
                    f.Armor = Math.Min(100f, f.Armor + item.Amount);
                    f.Vests++;
                    break;
                case "capacete":
                    if (f.Capacete == 0) f.Health = Math.Min(f.MaxHealth, f.Health + 40f);
                    f.Capacete++;
                    break;
                case "backpack":
                    f.Backpacks = Math.Min(3, f.Backpacks + 1);
                    break;
            }
            Burst(item.X, item.Y, 1, 10, Color.FromArgb(255, 214, 110), 120f);
            return item;
        }

        public bool UseMedkit(Fighter f)
        {
            if (f == null || !f.Alive || f.Medkits <= 0 || f.Health >= f.MaxHealth) return false;
            f.Medkits--;
            f.Health = Math.Min(f.MaxHealth, f.Health + 55f);
            Burst(f.X, f.Y, 0, 12, Color.FromArgb(90, 240, 170), 80f);
            return true;
        }

        public List<Fighter> SpectateList()
        {
            return Fighters.Where(f => f.Alive)
                           .OrderBy(f => f.Team == 0 ? 0 : 1)
                           .ThenBy(f => f.Id)
                           .ToList();
        }

        // ---------- Efeitos ----------
        private void UpdateEffects(float dt)
        {
            foreach (var f in Fighters)
            {
                f.FireCd = Math.Max(0f, f.FireCd - dt);
                f.Muzzle = Math.Max(0f, f.Muzzle - dt);
                f.HitFlash = Math.Max(0f, f.HitFlash - dt);
                f.SpawnGuard = Math.Max(0f, f.SpawnGuard - dt);
            }
            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                var p = Particles[i];
                p.Life -= dt;
                if (p.Life <= 0f) { Particles.RemoveAt(i); continue; }
                p.X += p.VX * dt;
                p.Y += p.VY * dt;
                p.VX *= .94f;
                p.VY *= .94f;
                if (p.Kind == 2) p.VY += 70f * dt;
            }
            for (int i = Pings.Count - 1; i >= 0; i--)
            {
                Pings[i].Life -= dt;
                if (Pings[i].Life <= 0f) Pings.RemoveAt(i);
            }
            for (int i = Feed.Count - 1; i >= 0; i--)
            {
                Feed[i].Life -= dt;
                if (Feed[i].Life <= 0f) Feed.RemoveAt(i);
            }
            for (int i = DamageNumbers.Count - 1; i >= 0; i--)
            {
                var d = DamageNumbers[i];
                d.Life -= dt;
                d.Y -= 32f * dt;
                if (d.Life <= 0f) DamageNumbers.RemoveAt(i);
            }
            PlayerDamageFlash = Math.Max(0f, PlayerDamageFlash - dt * 2.1f);
            HitMarker = Math.Max(0f, HitMarker - dt);
            PlayerRecoil = Math.Max(0f, PlayerRecoil - dt * 3.2f);
            CameraShake = Math.Max(0f, CameraShake - dt * 9f);
        }

        private void Burst(float x, float y, int kind, int count, Color color, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                double a = _rng.NextDouble() * Math.PI * 2;
                float sp = speed * (.25f + (float)_rng.NextDouble() * .9f);
                float life = .3f + (float)_rng.NextDouble() * .5f;
                Particles.Add(new Particle
                {
                    X = x, Y = y,
                    VX = (float)Math.Cos(a) * sp,
                    VY = (float)Math.Sin(a) * sp,
                    Life = life, MaxLife = life,
                    Size = 2f + (float)_rng.NextDouble() * 4f,
                    Color = color,
                    Kind = kind,
                });
            }
            if (Particles.Count > 420) Particles.RemoveRange(0, Particles.Count - 420);
        }

        private void AddPing(float x, float y, int team, float life = 1.25f)
        {
            Pings.Add(new Ping { X = x, Y = y, Team = team, Life = life, MaxLife = life });
            if (Pings.Count > 40) Pings.RemoveAt(0);
        }

        private static float Dist(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static Color ParseColor(string hex)
        {
            try { return ColorTranslator.FromHtml(hex); }
            catch { return Color.FromArgb(85, 85, 85); }
        }
    }
}

