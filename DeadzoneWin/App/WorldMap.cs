// WorldMap.cs — Mapa procedural, colisão e obstáculos (sem engine).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Deadzone2D
{
    public enum PropKind { Car, Truck, Crate, Barrel, Tree, Rock, Fountain }

    public sealed class Wall
    {
        public RectangleF R;
        public bool House;
        public Wall(RectangleF r, bool house = false) { R = r; House = house; }
    }

    public sealed class House
    {
        public RectangleF Floor;
        public int DoorSide; // 0 baixo, 1 cima, 2 esquerda, 3 direita
    }

    public sealed class MapProp
    {
        public PropKind Kind;
        public RectangleF R;
        public float Seed;
        public float VisualRadius;
        public bool Solid;
    }

    public sealed class Decal
    {
        public float X, Y, R;
        public int Kind;
    }

    public sealed partial class WorldMap
    {
        public GameMap Def = new();
        public float W = 2048f, H = 1280f;
        public string Theme = "";
        public Color Ground, GroundAlt, RoadColor, RoadEdge, WallColor, WallEdge, FloorColor, Water;

        public readonly List<Wall> Walls = new();
        public readonly List<RectangleF> Solids = new();
        public readonly List<House> Houses = new();
        public readonly List<RectangleF> Roads = new();
        public readonly List<RectangleF> Plazas = new();
        public readonly List<MapProp> Props = new();
        public readonly List<Decal> Decals = new();
        public readonly List<LootItem> Loot = new();

        public readonly List<PointF> Spawns0 = new();
        public readonly List<PointF> Spawns1 = new();
        public readonly List<PointF> RoyaleSpawns = new();

        private const int CellSize = 192;
        private readonly Dictionary<long, List<RectangleF>> _grid = new();

        public static WorldMap Build(GameMap def, int seed)
        {
            var m = new WorldMap { Def = def, W = def.SizeX, H = def.SizeY, Theme = def.Mode };
            var rng = new Random(seed);
            switch (def.Mode)
            {
                case "SquadBattle": m.BuildSquadBattle(rng); break;
                case "DeadRoyale": m.BuildRoyale(rng); break;
                default: m.BuildDealDuo(rng); break;
            }
            m.BuildGrid();
            return m;
        }

        // ---------- Construção ----------
        public void AddWall(RectangleF r, bool house = false)
        {
            Walls.Add(new Wall(r, house));
            Solids.Add(r);
        }

        public void AddProp(PropKind kind, RectangleF r, bool solid, float visual = 0f, float seed = 0f)
        {
            Props.Add(new MapProp { Kind = kind, R = r, Solid = solid, VisualRadius = visual, Seed = seed });
            if (solid) Solids.Add(r);
        }

        public bool OverlapsSolid(RectangleF r)
        {
            foreach (var s in Solids)
                if (s.IntersectsWith(r)) return true;
            return false;
        }

        public void AddHouse(RectangleF f, int doorSide, Random rng, bool loot, int lootCount)
        {
            const float t = 16f;
            var house = new House { Floor = f, DoorSide = doorSide };
            Houses.Add(house);
            float door = Math.Min(74f, Math.Min(f.Width, f.Height) * 0.34f);

            switch (doorSide)
            {
                case 1: // porta em cima
                    AddWall(new RectangleF(f.X, f.Bottom - t, f.Width, t), true);
                    AddWall(new RectangleF(f.X, f.Y, t, f.Height), true);
                    AddWall(new RectangleF(f.Right - t, f.Y, t, f.Height), true);
                    AddWallWithGap(f.X, f.Y, f.Width, t, true, f.X + f.Width / 2f, door, true);
                    break;
                case 2: // porta à esquerda
                    AddWall(new RectangleF(f.X, f.Y, f.Width, t), true);
                    AddWall(new RectangleF(f.X, f.Bottom - t, f.Width, t), true);
                    AddWall(new RectangleF(f.Right - t, f.Y, t, f.Height), true);
                    AddWallWithGap(f.X, f.Y, t, f.Height, false, f.Y + f.Height / 2f, door, true);
                    break;
                case 3: // porta à direita
                    AddWall(new RectangleF(f.X, f.Y, f.Width, t), true);
                    AddWall(new RectangleF(f.X, f.Bottom - t, f.Width, t), true);
                    AddWall(new RectangleF(f.X, f.Y, t, f.Height), true);
                    AddWallWithGap(f.Right - t, f.Y, t, f.Height, false, f.Y + f.Height / 2f, door, true);
                    break;
                default: // porta embaixo
                    AddWall(new RectangleF(f.X, f.Y, f.Width, t), true);
                    AddWall(new RectangleF(f.X, f.Y, t, f.Height), true);
                    AddWall(new RectangleF(f.Right - t, f.Y, t, f.Height), true);
                    AddWallWithGap(f.X, f.Bottom - t, f.Width, t, true, f.X + f.Width / 2f, door, true);
                    break;
            }

            if (loot)
            {
                for (int i = 0; i < lootCount; i++)
                {
                    float x = f.X + 34 + (float)rng.NextDouble() * Math.Max(1, f.Width - 68);
                    float y = f.Y + 34 + (float)rng.NextDouble() * Math.Max(1, f.Height - 68);
                    Loot.Add(CreateLoot(rng, x, y));
                }
            }
        }

        private void AddWallWithGap(float x, float y, float w, float h, bool horizontal, float center, float gap, bool house)
        {
            float a = center - gap / 2f, b = center + gap / 2f;
            if (horizontal)
            {
                if (a > x) AddWall(new RectangleF(x, y, a - x, h), house);
                if (b < x + w) AddWall(new RectangleF(b, y, x + w - b, h), house);
            }
            else
            {
                if (a > y) AddWall(new RectangleF(x, y, w, a - y), house);
                if (b < y + h) AddWall(new RectangleF(x, b, w, y + h - b), house);
            }
        }

        public void AddBuilding(RectangleF r)
        {
            AddWall(r, false);
        }

        public void AddVehicle(float x, float y, bool horizontal, bool truck, Random rng)
        {
            var r = horizontal
                ? new RectangleF(x, y, truck ? 122 : 88, truck ? 60 : 46)
                : new RectangleF(x, y, truck ? 60 : 46, truck ? 122 : 88);
            AddProp(truck ? PropKind.Truck : PropKind.Car, r, true, 0, (float)rng.NextDouble());
        }

        public void AddCrate(float x, float y, float size, Random rng) =>
            AddProp(PropKind.Crate, new RectangleF(x, y, size, size), true, 0, (float)rng.NextDouble());

        public void AddBarrel(float x, float y, Random rng) =>
            AddProp(PropKind.Barrel, new RectangleF(x, y, 30, 30), true, 15, (float)rng.NextDouble());

        public void AddTree(float x, float y, float radius, Random rng) =>
            AddProp(PropKind.Tree, new RectangleF(x - radius * .35f, y - radius * .35f, radius * .7f, radius * .7f),
                true, radius, (float)rng.NextDouble());

        public void AddRock(float x, float y, float w, float h, Random rng) =>
            AddProp(PropKind.Rock, new RectangleF(x, y, w, h), true, 0, (float)rng.NextDouble());

        public void AddFountain(float x, float y, Random rng) =>
            AddProp(PropKind.Fountain, new RectangleF(x, y, 132, 132), true, 66, (float)rng.NextDouble());

        public void AddRoad(float x, float y, float w, float h) => Roads.Add(new RectangleF(x, y, w, h));

        public void AddPlaza(float x, float y, float w, float h) => Plazas.Add(new RectangleF(x, y, w, h));

        public void AddDecals(Random rng, int count, RectangleF? area = null)
        {
            var a = area ?? new RectangleF(0, 0, W, H);
            for (int i = 0; i < count; i++)
                Decals.Add(new Decal
                {
                    X = a.X + (float)rng.NextDouble() * a.Width,
                    Y = a.Y + (float)rng.NextDouble() * a.Height,
                    R = 4 + (float)rng.NextDouble() * 26,
                    Kind = rng.Next(0, 4),
                });
        }

        public LootItem CreateLoot(Random rng, float x, float y)
        {
            int roll = rng.Next(100);
            var item = new LootItem { X = x, Y = y, Seed = (float)rng.NextDouble() };
            if (roll < 42)
            {
                string[] guns = { "AK47", "MP40", "MiniUZI", "SniperAWM", "Glock" };
                item.Kind = "weapon";
                item.WeaponId = guns[rng.Next(guns.Length)];
                item.Label = Arsenal.Get(item.WeaponId).Name;
            }
            else if (roll < 66)
            {
                item.Kind = "coin";
                item.Amount = 15 + rng.Next(6) * 10;
                item.Label = $"{item.Amount} moedas";
            }
            else if (roll < 86)
            {
                item.Kind = "medkit";
                item.Amount = 1;
                item.Label = "Kit médico";
            }
            else if (roll < 92)
            {
                item.Kind = "capacete";
                item.Label = "Capacete";
            }
            else if (roll < 97)
            {
                item.Kind = "backpack";
                item.Label = "Mochila";
            }
            else
            {
                item.Kind = "vest";
                item.Amount = 50;
                item.Label = "Colete leve";
            }
            return item;
        }

        // ---------- Colisão ----------
        public void BuildGrid()
        {
            _grid.Clear();
            foreach (var s in Solids)
            {
                int x0 = (int)Math.Floor(s.Left / CellSize), x1 = (int)Math.Floor(s.Right / CellSize);
                int y0 = (int)Math.Floor(s.Top / CellSize), y1 = (int)Math.Floor(s.Bottom / CellSize);
                for (int x = x0; x <= x1; x++)
                    for (int y = y0; y <= y1; y++)
                    {
                        long k = Key(x, y);
                        if (!_grid.TryGetValue(k, out var list)) _grid[k] = list = new List<RectangleF>();
                        list.Add(s);
                    }
            }
        }

        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;

        private IEnumerable<RectangleF> Near(float x, float y, float r)
        {
            int x0 = (int)Math.Floor((x - r) / CellSize), x1 = (int)Math.Floor((x + r) / CellSize);
            int y0 = (int)Math.Floor((y - r) / CellSize), y1 = (int)Math.Floor((y + r) / CellSize);
            for (int cx = x0; cx <= x1; cx++)
                for (int cy = y0; cy <= y1; cy++)
                    if (_grid.TryGetValue(Key(cx, cy), out var list))
                        foreach (var rect in list) yield return rect;
        }

        public bool BlockedCircle(float x, float y, float r)
        {
            if (x < r || y < r || x > W - r || y > H - r) return true;
            foreach (var s in Near(x, y, r))
            {
                float nx = Math.Clamp(x, s.Left, s.Right);
                float ny = Math.Clamp(y, s.Top, s.Bottom);
                float dx = x - nx, dy = y - ny;
                if (dx * dx + dy * dy < r * r) return true;
            }
            return false;
        }

        public bool BlockedPoint(float x, float y) => BlockedCircle(x, y, 2.5f);

        public bool SegmentBlocked(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            int steps = Math.Max(1, (int)(len / 14f));
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                if (BlockedPoint(x1 + dx * t, y1 + dy * t)) return true;
            }
            return false;
        }

        public bool Move(Fighter f, float dx, float dy)
        {
            const float r = 13f;
            bool moved = false;
            if (Math.Abs(dx) > 0.01f && !BlockedCircle(f.X + dx, f.Y, r)) { f.X += dx; moved = true; }
            if (Math.Abs(dy) > 0.01f && !BlockedCircle(f.X, f.Y + dy, r)) { f.Y += dy; moved = true; }
            f.X = Math.Clamp(f.X, 28f, W - 28f);
            f.Y = Math.Clamp(f.Y, 28f, H - 28f);
            return moved;
        }

        public PointF FindFree(float x, float y)
        {
            if (!BlockedCircle(x, y, 16)) return new PointF(x, y);
            for (int ring = 1; ring <= 24; ring++)
            {
                for (int i = 0; i < 16; i++)
                {
                    double a = i / 16.0 * Math.PI * 2;
                    float nx = x + (float)Math.Cos(a) * ring * 22f;
                    float ny = y + (float)Math.Sin(a) * ring * 22f;
                    if (!BlockedCircle(nx, ny, 16)) return new PointF(nx, ny);
                }
            }
            return new PointF(W / 2f, H / 2f);
        }

        public float DistanceToWall(float x, float y)
        {
            float best = 9999f;
            foreach (var s in Near(x, y, 80))
            {
                float nx = Math.Clamp(x, s.Left, s.Right), ny = Math.Clamp(y, s.Top, s.Bottom);
                best = Math.Min(best, (float)Math.Sqrt((x - nx) * (x - nx) + (y - ny) * (y - ny)));
            }
            return best;
        }
    }
}
