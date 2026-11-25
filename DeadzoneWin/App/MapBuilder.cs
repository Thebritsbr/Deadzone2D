// MapBuilder.cs — Layout dos 3 mapas iniciais.
using System;
using System.Collections.Generic;
using System.Drawing;

namespace CrossfireWin
{
    public sealed partial class WorldMap
    {
        private void Boundary(float t = 26f)
        {
            AddWall(new RectangleF(0, 0, W, t));
            AddWall(new RectangleF(0, H - t, W, t));
            AddWall(new RectangleF(0, 0, t, H));
            AddWall(new RectangleF(W - t, 0, t, H));
        }

        // =========================================================
        // DEAL DUO — vila abandonada, mapa curto, carros queimados
        // =========================================================
        private void BuildDealDuo(Random rng)
        {
            Ground = Color.FromArgb(36, 47, 39);
            GroundAlt = Color.FromArgb(45, 57, 46);
            RoadColor = Color.FromArgb(62, 58, 50);
            RoadEdge = Color.FromArgb(27, 31, 27);
            WallColor = Color.FromArgb(91, 78, 61);
            WallEdge = Color.FromArgb(25, 22, 18);
            FloorColor = Color.FromArgb(55, 46, 37);
            Water = Color.FromArgb(33, 76, 84);

            Boundary();
            AddRoad(0, 552, W, 176);
            AddRoad(430, 0, 156, H);
            AddRoad(1462, 0, 156, H);
            AddPlaza(842, 470, 364, 340);
            AddDecals(rng, 130);

            // casas da vila
            AddHouse(new RectangleF(168, 148, 282, 202), 0, rng, false, 0);
            AddHouse(new RectangleF(696, 118, 306, 224), 0, rng, false, 0);
            AddHouse(new RectangleF(1236, 138, 286, 212), 0, rng, false, 0);
            AddHouse(new RectangleF(1702, 168, 210, 190), 2, rng, false, 0);
            AddHouse(new RectangleF(238, 852, 264, 222), 1, rng, false, 0);
            AddHouse(new RectangleF(706, 902, 322, 202), 1, rng, false, 0);
            AddHouse(new RectangleF(1210, 882, 282, 202), 1, rng, false, 0);
            AddHouse(new RectangleF(1686, 826, 232, 224), 1, rng, false, 0);

            // celeiro sólido
            AddBuilding(new RectangleF(78, 560, 250, 150));
            AddBuilding(new RectangleF(1748, 560, 220, 160));

            // praça central
            AddFountain(958, 574, rng);
            AddCrate(892, 498, 44, rng);
            AddCrate(1118, 790, 44, rng);
            AddBarrel(860, 758, rng);
            AddBarrel(1160, 500, rng);

            // carros e caminhões queimados (barreiras)
            AddVehicle(584, 470, true, false, rng);
            AddVehicle(1320, 470, true, false, rng);
            AddVehicle(930, 720, false, false, rng);
            AddVehicle(1540, 700, true, false, rng);
            AddVehicle(760, 522, true, true, rng);
            AddVehicle(1180, 650, false, true, rng);

            // caixas e barris
            AddCrate(500, 790, 40, rng);
            AddCrate(542, 790, 40, rng);
            AddCrate(1520, 540, 40, rng);
            AddBarrel(470, 410, rng);
            AddBarrel(1580, 830, rng);
            AddBarrel(1030, 900, rng);

            // árvores nas bordas
            for (int i = 0; i < 22; i++)
            {
                float x = 90 + (float)rng.NextDouble() * (W - 180);
                float y = rng.Next(2) == 0 ? 60 + (float)rng.NextDouble() * 34 : H - 96 + (float)rng.NextDouble() * 30;
                AddTree(x, y, 26 + (float)rng.NextDouble() * 16, rng);
            }
            AddTree(330, 470, 34, rng);
            AddTree(1690, 470, 34, rng);
            AddTree(1060, 180, 36, rng);
            AddTree(1010, 1080, 34, rng);

            Spawns0.Add(new PointF(310, 1130));
            Spawns0.Add(new PointF(410, 1160));
            Spawns1.Add(new PointF(1780, 108));
            Spawns1.Add(new PointF(1680, 88));
        }

        // =========================================================
        // SQUAD BATTLE — cidade abandonada média, veículos no meio
        // =========================================================
        private void BuildSquadBattle(Random rng)
        {
            Ground = Color.FromArgb(43, 46, 44);
            GroundAlt = Color.FromArgb(50, 53, 49);
            RoadColor = Color.FromArgb(56, 59, 60);
            RoadEdge = Color.FromArgb(28, 31, 32);
            WallColor = Color.FromArgb(78, 80, 78);
            WallEdge = Color.FromArgb(22, 24, 25);
            FloorColor = Color.FromArgb(48, 48, 45);
            Water = Color.FromArgb(30, 72, 80);

            Boundary(30f);
            AddRoad(0, 700, W, 120);
            AddRoad(0, 1400, W, 236);
            AddRoad(0, 2300, W, 140);
            AddRoad(1000, 0, 132, H);
            AddRoad(1900, 0, 236, H);
            AddRoad(3000, 0, 132, H);
            AddPlaza(1450, 1180, 1200, 700);
            AddDecals(rng, 210);

            // quarteirões noroeste
            AddHouse(new RectangleF(300, 240, 360, 260), 0, rng, false, 0);
            AddHouse(new RectangleF(830, 238, 320, 300), 0, rng, false, 0);
            AddHouse(new RectangleF(290, 780, 300, 220), 1, rng, false, 0);
            AddHouse(new RectangleF(830, 800, 340, 240), 1, rng, false, 0);

            // nordeste
            AddHouse(new RectangleF(2900, 244, 360, 260), 0, rng, false, 0);
            AddHouse(new RectangleF(3400, 236, 320, 300), 0, rng, false, 0);
            AddHouse(new RectangleF(3300, 782, 300, 220), 1, rng, false, 0);
            AddHouse(new RectangleF(2680, 760, 320, 240), 1, rng, false, 0);

            // sudoeste
            AddHouse(new RectangleF(320, 2080, 340, 260), 0, rng, false, 0);
            AddHouse(new RectangleF(830, 2120, 300, 260), 0, rng, false, 0);
            AddHouse(new RectangleF(340, 2530, 320, 240), 1, rng, false, 0);
            AddHouse(new RectangleF(870, 2600, 280, 220), 1, rng, false, 0);

            // sudeste
            AddHouse(new RectangleF(3100, 2080, 360, 260), 0, rng, false, 0);
            AddHouse(new RectangleF(2680, 2440, 320, 240), 1, rng, false, 0);
            AddHouse(new RectangleF(3300, 2540, 320, 220), 1, rng, false, 0);

            // centro norte / sul
            AddHouse(new RectangleF(1460, 880, 340, 240), 0, rng, false, 0);
            AddHouse(new RectangleF(2080, 860, 320, 240), 0, rng, false, 0);
            AddHouse(new RectangleF(1360, 2020, 320, 260), 1, rng, false, 0);
            AddHouse(new RectangleF(2280, 2000, 360, 260), 1, rng, false, 0);

            // galpões sólidos
            AddBuilding(new RectangleF(1500, 360, 400, 250));
            AddBuilding(new RectangleF(2140, 2420, 400, 250));
            AddBuilding(new RectangleF(50, 1600, 300, 260));
            AddBuilding(new RectangleF(3740, 1600, 300, 260));

            // veículos abandonados no meio (não utilizáveis)
            AddVehicle(1480, 1220, true, false, rng);
            AddVehicle(1680, 1280, true, false, rng);
            AddVehicle(2180, 1230, true, false, rng);
            AddVehicle(2400, 1320, true, false, rng);
            AddVehicle(1600, 1710, true, false, rng);
            AddVehicle(2000, 1760, true, false, rng);
            AddVehicle(2450, 1690, false, false, rng);
            AddVehicle(1340, 1540, false, false, rng);
            AddVehicle(1810, 1460, true, true, rng);
            AddVehicle(2320, 1500, true, true, rng);
            AddVehicle(1440, 1810, true, true, rng);
            AddVehicle(2600, 1840, false, true, rng);

            // entulho
            for (int i = 0; i < 18; i++)
            {
                float x = 120 + (float)rng.NextDouble() * (W - 240);
                float y = 120 + (float)rng.NextDouble() * (H - 240);
                if (BlockedCircle(x, y, 28)) continue;
                AddCrate(x, y, 36 + (float)rng.NextDouble() * 12, rng);
            }
            for (int i = 0; i < 14; i++)
            {
                float x = 120 + (float)rng.NextDouble() * (W - 240);
                float y = 120 + (float)rng.NextDouble() * (H - 240);
                if (BlockedCircle(x, y, 22)) continue;
                AddBarrel(x, y, rng);
            }

            // árvores nas laterais
            for (int i = 0; i < 40; i++)
            {
                float x = 70 + (float)rng.NextDouble() * 260;
                float y = 70 + (float)rng.NextDouble() * (H - 140);
                if (!BlockedCircle(x, y, 30)) AddTree(x, y, 24 + (float)rng.NextDouble() * 18, rng);
                x = W - 330 + (float)rng.NextDouble() * 260;
                if (!BlockedCircle(x, y, 30)) AddTree(x, y, 24 + (float)rng.NextDouble() * 18, rng);
            }
            for (int i = 0; i < 20; i++)
            {
                float x = 400 + (float)rng.NextDouble() * (W - 800);
                float y = rng.Next(2) == 0 ? 70 + (float)rng.NextDouble() * 90 : H - 150 + (float)rng.NextDouble() * 80;
                if (!BlockedCircle(x, y, 30)) AddTree(x, y, 24 + (float)rng.NextDouble() * 16, rng);
            }

            Spawns0.Add(new PointF(300, 2860));
            Spawns0.Add(new PointF(460, 2810));
            Spawns0.Add(new PointF(300, 2660));
            Spawns0.Add(new PointF(570, 2900));
            Spawns1.Add(new PointF(3800, 220));
            Spawns1.Add(new PointF(3640, 300));
            Spawns1.Add(new PointF(3860, 410));
            Spawns1.Add(new PointF(3540, 200));
        }

        // =========================================================
        // DEAD ROYALE — mapa grande, vilas, cidade e casas com loot
        // =========================================================
        private void BuildRoyale(Random rng)
        {
            Ground = Color.FromArgb(33, 49, 36);
            GroundAlt = Color.FromArgb(41, 58, 42);
            RoadColor = Color.FromArgb(58, 56, 47);
            RoadEdge = Color.FromArgb(25, 29, 24);
            WallColor = Color.FromArgb(87, 76, 61);
            WallEdge = Color.FromArgb(24, 21, 18);
            FloorColor = Color.FromArgb(54, 46, 37);
            Water = Color.FromArgb(31, 78, 86);

            Boundary(30f);
            // estradas principais
            AddRoad(0, 2140, W, 210);
            AddRoad(2960, 0, 210, H);
            AddRoad(0, 760, W, 140);
            AddRoad(0, 3760, W, 140);
            AddRoad(760, 0, 140, H);
            AddRoad(5240, 0, 140, H);
            AddPlaza(2820, 2040, 520, 400);
            AddDecals(rng, 420);

            // ---- vila noroeste ----
            AddHouse(new RectangleF(760, 700, 270, 200), 0, rng, true, 2);
            AddHouse(new RectangleF(1110, 720, 240, 190), 0, rng, true, 2);
            AddHouse(new RectangleF(750, 1000, 240, 190), 1, rng, true, 2);
            AddHouse(new RectangleF(1080, 1010, 280, 210), 1, rng, true, 3);
            AddHouse(new RectangleF(1370, 900, 220, 180), 2, rng, true, 1);
            AddHouse(new RectangleF(470, 860, 210, 180), 3, rng, true, 2);

            // ---- vila nordeste ----
            AddHouse(new RectangleF(5000, 700, 270, 200), 0, rng, true, 2);
            AddHouse(new RectangleF(4640, 720, 240, 190), 0, rng, true, 2);
            AddHouse(new RectangleF(5010, 1000, 240, 190), 1, rng, true, 2);
            AddHouse(new RectangleF(4660, 1010, 280, 210), 1, rng, true, 3);
            AddHouse(new RectangleF(4340, 900, 220, 180), 2, rng, true, 1);
            AddHouse(new RectangleF(5320, 860, 210, 180), 3, rng, true, 2);

            // ---- cidade central ----
            AddHouse(new RectangleF(2600, 1780, 300, 220), 0, rng, true, 3);
            AddHouse(new RectangleF(3040, 1780, 300, 220), 0, rng, true, 3);
            AddHouse(new RectangleF(3420, 1960, 270, 210), 2, rng, true, 2);
            AddHouse(new RectangleF(2560, 2200, 280, 220), 1, rng, true, 2);
            AddHouse(new RectangleF(3380, 2220, 300, 220), 1, rng, true, 3);
            AddHouse(new RectangleF(2760, 2540, 300, 220), 1, rng, true, 2);
            AddHouse(new RectangleF(3180, 2540, 280, 220), 1, rng, true, 3);
            AddHouse(new RectangleF(2960, 1560, 260, 190), 0, rng, true, 2);

            // ---- vila sudoeste ----
            AddHouse(new RectangleF(760, 3540, 270, 200), 1, rng, true, 2);
            AddHouse(new RectangleF(1110, 3560, 240, 190), 1, rng, true, 2);
            AddHouse(new RectangleF(750, 3900, 240, 190), 0, rng, true, 2);
            AddHouse(new RectangleF(1080, 3910, 280, 210), 0, rng, true, 3);
            AddHouse(new RectangleF(1370, 3760, 220, 180), 2, rng, true, 1);
            AddHouse(new RectangleF(470, 3660, 210, 180), 3, rng, true, 2);

            // ---- vila sudeste ----
            AddHouse(new RectangleF(5000, 3540, 270, 200), 1, rng, true, 2);
            AddHouse(new RectangleF(4640, 3560, 240, 190), 1, rng, true, 2);
            AddHouse(new RectangleF(5010, 3900, 240, 190), 0, rng, true, 2);
            AddHouse(new RectangleF(4660, 3910, 280, 210), 0, rng, true, 3);
            AddHouse(new RectangleF(4340, 3760, 220, 180), 2, rng, true, 1);
            AddHouse(new RectangleF(5320, 3660, 210, 180), 3, rng, true, 2);

            // ---- 10 lojas espalhadas em casas (bem distantes) ----
            var shopSpots = new (float x, float y)[]
            {
                (820,760),(1100,1050),(5100,760),(4780,1050),(2720,1830),
                (3450,2000),(820,3600),(1120,3940),(5060,3600),(4720,3970),
            };
            foreach (var s in shopSpots) Loot.Add(new LootItem { Kind = "shop", Label = "Gun Shop", X = s.x, Y = s.y, Seed = (float)rng.NextDouble() });

            // ---- posto sul ----
            AddHouse(new RectangleF(2860, 4120, 300, 210), 0, rng, true, 2);
            AddHouse(new RectangleF(2500, 4200, 240, 190), 1, rng, true, 2);
            AddHouse(new RectangleF(3300, 4200, 240, 190), 1, rng, true, 2);

            // praça central
            AddFountain(3010, 2140, rng);
            AddCrate(2860, 2070, 44, rng);
            AddCrate(3270, 2380, 44, rng);

            // veículos pelas estradas
            for (int i = 0; i < 20; i++)
            {
                float x = 200 + (float)rng.NextDouble() * (W - 400);
                float y = 200 + (float)rng.NextDouble() * (H - 400);
                if (BlockedCircle(x, y, 80)) continue;
                AddVehicle(x, y, rng.Next(2) == 0, rng.Next(4) == 0, rng);
            }

            // árvores e pedras
            for (int i = 0; i < 150; i++)
            {
                float x = 70 + (float)rng.NextDouble() * (W - 140);
                float y = 70 + (float)rng.NextDouble() * (H - 140);
                if (BlockedCircle(x, y, 42)) continue;
                AddTree(x, y, 22 + (float)rng.NextDouble() * 22, rng);
            }
            for (int i = 0; i < 45; i++)
            {
                float x = 90 + (float)rng.NextDouble() * (W - 180);
                float y = 90 + (float)rng.NextDouble() * (H - 180);
                if (BlockedCircle(x, y, 40)) continue;
                AddRock(x, y, 26 + (float)rng.NextDouble() * 26, 20 + (float)rng.NextDouble() * 22, rng);
            }
            for (int i = 0; i < 20; i++)
            {
                float x = 120 + (float)rng.NextDouble() * (W - 240);
                float y = 120 + (float)rng.NextDouble() * (H - 240);
                if (BlockedCircle(x, y, 28)) continue;
                AddBarrel(x, y, rng);
            }

            // 30 pontos de spawn (jogador primeiro, ao sul)
            RoyaleSpawns.Add(FindFree(3072, 4410));
            int guard = 0;
            while (RoyaleSpawns.Count < 30 && guard++ < 8000)
            {
                float x = 260 + (float)rng.NextDouble() * (W - 520);
                float y = 260 + (float)rng.NextDouble() * (H - 520);
                if (BlockedCircle(x, y, 24)) continue;
                bool ok = true;
                foreach (var p in RoyaleSpawns)
                    if (Dist(p.X, p.Y, x, y) < 240) { ok = false; break; }
                if (ok) RoyaleSpawns.Add(new PointF(x, y));
            }
            while (RoyaleSpawns.Count < 30)
                RoyaleSpawns.Add(FindFree(400 + (float)rng.NextDouble() * (W - 800), 400 + (float)rng.NextDouble() * (H - 800)));
        }

        private static float Dist(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}

