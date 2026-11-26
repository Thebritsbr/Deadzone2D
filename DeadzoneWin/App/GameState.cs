// GameState.cs — Tipos de combate: armas, lutadores, projéteis, loot e efeitos.
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Deadzone2D
{
    public enum SessionPhase { Countdown, Live, RoundOver, MatchOver }
    public enum SessionKind { Team, Royale }

    public sealed class WeaponDef
    {
        public string Id = "Glock";
        public string Name = "GLOCK-18";
        public int Damage = 26;
        public float FireInterval = 0.17f;
        public float Spread = 0.035f;
        public float Range = 760f;
        public float BulletSpeed = 1500f;
        public float ReloadTime = 1.05f;
        public int MagSize = 12;
        public Color Tracer = Color.FromArgb(255, 226, 150);
    }

    public static class Arsenal
    {
        public static readonly Dictionary<string, WeaponDef> Defs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Glock"] = new WeaponDef { Id = "Glock", Name = "GLOCK-18", Damage = 26, FireInterval = .17f, Spread = .035f, Range = 760, BulletSpeed = 1500, ReloadTime = 1.05f, MagSize = 12, Tracer = Color.FromArgb(255, 226, 150) },
            ["MP40"] = new WeaponDef { Id = "MP40", Name = "MP40", Damage = 18, FireInterval = .085f, Spread = .062f, Range = 650, BulletSpeed = 1500, ReloadTime = 1.35f, MagSize = 32, Tracer = Color.FromArgb(255, 205, 120) },
            ["AK47"] = new WeaponDef { Id = "AK47", Name = "AK-47", Damage = 33, FireInterval = .12f, Spread = .042f, Range = 980, BulletSpeed = 1750, ReloadTime = 1.55f, MagSize = 30, Tracer = Color.FromArgb(255, 166, 92) },
            ["SniperAWM"] = new WeaponDef { Id = "SniperAWM", Name = "AWM", Damage = 90, FireInterval = 1.25f, Spread = .008f, Range = 1500, BulletSpeed = 2300, ReloadTime = 2.5f, MagSize = 5, Tracer = Color.FromArgb(150, 235, 255) },
            ["MiniUZI"] = new WeaponDef { Id = "MiniUZI", Name = "MINI UZI", Damage = 15, FireInterval = .06f, Spread = .078f, Range = 560, BulletSpeed = 1450, ReloadTime = 1.2f, MagSize = 32, Tracer = Color.FromArgb(255, 220, 140) },
        };

        public static WeaponDef Get(string id) =>
            Defs.TryGetValue(id ?? "", out var w) ? w : Defs["Glock"];

        public static readonly string[] LootGuns = { "AK47", "MP40", "MiniUZI", "SniperAWM", "Glock" };
    }

    public sealed class Fighter
    {
        public int Id;
        public string Name = "";
        public int Team;
        public bool IsPlayer;
        public float X, Y;
        public float Angle;
        public float Health = 100f, MaxHealth = 100f;
        public float Armor;
        public bool Alive = true;
        public int Kills, Deaths, Coins = 120, Medkits, Vests, Capacete, Backpacks;
        public int DamageDealt;
        public int BackpackBonus => Math.Max(0, Backpacks - 1) * 30;
        public string WeaponId = "Glock";
        public int Mag = 12, Reserve = 48;
        public float FireCd, ReloadLeft, Muzzle, HitFlash, SpawnGuard;
        public float WalkPhase;
        public float CorpseAngle;
        public float AiTimer, AiWanderX, AiWanderY, AiStrafe = 1f, AiSkill = .6f;
        public int AiTarget = -1;
        public float AiMoveX, AiMoveY;
        public bool Sprinting;

        public WeaponDef Weapon => Arsenal.Get(WeaponId);
        public float HealthPct => MaxHealth <= 0 ? 0 : Math.Clamp(Health / MaxHealth, 0f, 1f);
    }

    public sealed class Bullet
    {
        public float X, Y, VX, VY;
        public float TrailX, TrailY;
        public int OwnerId, Team;
        public float Damage;
        public float Life = 2.2f;
        public float Traveled;
        public float MaxRange = 1200f;
        public Color Tracer = Color.Gold;
    }

    public sealed class LootItem
    {
        public string Kind = "coin";
        public string Label = "";
        public string WeaponId = "";
        public int Amount;
        public float X, Y, Seed;
        public bool Taken;
    }

    public sealed class Particle
    {
        public float X, Y, VX, VY, Life, MaxLife, Size;
        public Color Color;
        public int Kind; // 0 poeira, 1 faísca, 2 sangue, 3 fumaça
    }

    public sealed class Ping
    {
        public float X, Y, Life, MaxLife;
        public int Team;
    }

    public sealed class KillEntry
    {
        public string Killer = "";
        public string Victim = "";
        public string Weapon = "";
        public int KillerTeam;
        public float Life = 5f;
    }

    public sealed class DamageNumber
    {
        public float X, Y, Life, MaxLife;
        public int Amount;
        public bool ByPlayer;
        public bool Taken;
    }

    public struct GameInput
    {
        public bool Up, Down, Left, Right, Sprint, Fire;
        public float AimX, AimY;
    }
}
