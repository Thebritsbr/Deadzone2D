// GameRenderer.cs — Renderização do mundo, personagens, veículos e efeitos.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Deadzone2D
{
    public partial class MainForm
    {
        private Color SquadColor(int index) => index switch
        {
            0 => IconBlue,
            1 => IconPurple,
            2 => IconRed,
            3 => IconYellow,
            _ => Ink,
        };

        // Brushes/pens descartáveis para o mundo (evita poluir o cache da UI).
        private static SolidBrush NB(Color c) => new SolidBrush(c);
        private static Pen NP(Color c, float w) => new Pen(c, w);

        private Bitmap? _groundTex;
        private string _groundTexKey = "";

        private void EnsureGroundTexture(WorldMap map)
        {
            string key = map.Def.Id + "|" + map.Ground.ToArgb();
            if (_groundTex != null && _groundTexKey == key) return;
            _groundTex?.Dispose();
            var bmp = new Bitmap(256, 256);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(map.Ground);
                var rng = new Random(7717);
                for (int i = 0; i < 1400; i++)
                {
                    int x = rng.Next(256), y = rng.Next(256);
                    int a = 10 + rng.Next(26);
                    var c = rng.Next(2) == 0 ? map.GroundAlt : Mix(map.Ground, Color.Black, .28f);
                    using var b = new SolidBrush(A(c, a));
                    g.FillEllipse(b, x, y, 2 + rng.Next(4), 1 + rng.Next(3));
                }
                for (int i = 0; i < 120; i++)
                {
                    int x = rng.Next(256), y = rng.Next(256);
                    var c = A(Mix(map.GroundAlt, Color.White, .12f), 40 + rng.Next(50));
                    using var p = new Pen(c, 1f);
                    for (int k = 0; k < 3; k++)
                    {
                        float dx = (float)(rng.NextDouble() * 6 - 3);
                        g.DrawLine(p, x + dx, y, x + dx - 2 + rng.Next(5), y - 3 - rng.Next(4));
                    }
                }
                for (int i = 0; i < 26; i++)
                {
                    int x = rng.Next(256), y = rng.Next(256);
                    int w = 18 + rng.Next(40), h = 12 + rng.Next(28);
                    using var b = new SolidBrush(A(map.GroundAlt, 26 + rng.Next(20)));
                    g.FillEllipse(b, x, y, w, h);
                }
            }
            _groundTex = bmp;
            _groundTexKey = key;
        }

        private void DrawGame(Graphics g, int w, int h)
        {
            if (_g == null) return;
            g.Clear(BgDeep);
            DrawWorld(g, w, h);
            DrawGameVignette(g, w, h);
            DrawHud(g, w, h);
            if (_keys.Contains(Keys.Tab)) DrawLeaderboard(g, w, h);
            if (_invAnim > .01f) DrawInventoryPanel(g, w, h);
            if (_storeOpen) DrawStore(g, w, h);
            if (_pauseAnim > .01f) DrawPause(g, w, h);
            if (!_g.Player.Alive && _g.Phase != SessionPhase.MatchOver) DrawDeathScreen(g, w, h);
            DrawBanner(g, w, h);
            DrawGameWindowButtons(g, w);
            DrawCrosshair(g, w, h);
        }

        // ---------- Mundo ----------
        private void DrawWorld(Graphics g, int w, int h)
        {
            if (_g == null) return;
            var map = _g.Map;
            float zoom = _zoom;
            float shake = _g.CameraShake;
            float shx = (float)(Math.Sin(_clock * 73.1) * shake);
            float shy = (float)(Math.Cos(_clock * 91.7) * shake * .7f);
            float camX = _camX + shx / zoom;
            float camY = _camY + shy / zoom;
            var view = new RectangleF(camX - w / (2f * zoom) - 90, camY - h / (2f * zoom) - 90,
                                      w / zoom + 180, h / zoom + 180);

            var state = g.Save();
            g.TranslateTransform(w / 2f, h / 2f);
            g.ScaleTransform(zoom, zoom);
            g.TranslateTransform(-camX, -camY);

            EnsureGroundTexture(map);
            using (var b = NB(Color.FromArgb(11, 14, 16))) g.FillRectangle(b, view);
            using (var tb = new TextureBrush(_groundTex!)) g.FillRectangle(tb, new RectangleF(0, 0, map.W, map.H));
            // sombra de borda do mapa
            using (var b = NB(A(Color.Black, 90)))
            {
                g.FillRectangle(b, -14, -14, map.W + 28, 14);
                g.FillRectangle(b, -14, map.H, map.W + 28, 14);
                g.FillRectangle(b, -14, 0, 14, map.H);
                g.FillRectangle(b, map.W, 0, 14, map.H);
            }
            DrawGroundDecals(g, map, view);
            DrawRoads(g, map, view);
            DrawHouseFloors(g, map, view);
            DrawWalls(g, map, view);
            DrawLoot(g, map, view);
            DrawProps(g, map, view);
            DrawCorpses(g, map, view);
            DrawFighters(g, map, view);
            DrawBullets(g, view);
            DrawParticles(g, view);
            DrawDamageNumbers(g, view);

            g.Restore(state);
        }

        private void DrawGroundDecals(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var d in map.Decals)
            {
                if (d.X < view.Left || d.X > view.Right || d.Y < view.Top || d.Y > view.Bottom) continue;
                switch (d.Kind)
                {
                    case 0:
                        using (var p = NP(A(map.GroundAlt, 140), 1.2f))
                        {
                            g.DrawLine(p, d.X, d.Y, d.X - d.R * .25f, d.Y - d.R);
                            g.DrawLine(p, d.X, d.Y, d.X + d.R * .2f, d.Y - d.R * .8f);
                        }
                        break;
                    case 1:
                        using (var b = NB(A(map.GroundAlt, 70))) g.FillEllipse(b, d.X, d.Y, d.R * 2, d.R * .8f);
                        break;
                    case 2:
                        using (var p = NP(A(Color.Black, 80), 1f))
                            g.DrawLine(p, d.X, d.Y, d.X + d.R, d.Y + d.R * .4f);
                        break;
                    default:
                        using (var b = NB(A(map.GroundAlt, 120))) g.FillEllipse(b, d.X, d.Y, d.R * .5f, d.R * .35f);
                        break;
                }
            }
        }

        private void DrawRoads(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var r in map.Plazas)
            {
                if (!view.IntersectsWith(r)) continue;
                using (var p = RoundRect(r, 10)) g.FillPath(NB(map.RoadColor), p);
                using (var p = RoundRect(r, 10)) g.DrawPath(NP(A(map.RoadEdge, 200), 3f), p);
            }
            foreach (var r in map.Roads)
            {
                if (!view.IntersectsWith(r)) continue;
                g.FillRectangle(NB(map.RoadColor), r);
                using (var p = NP(A(map.RoadEdge, 230), 3f))
                {
                    if (r.Width >= r.Height)
                    {
                        g.DrawLine(p, r.Left, r.Top, r.Right, r.Top);
                        g.DrawLine(p, r.Left, r.Bottom, r.Right, r.Bottom);
                        using var dash = new Pen(A(map.RoadEdge, 90), 2f) { DashStyle = DashStyle.Dash };
                        g.DrawLine(dash, r.Left, r.Y + r.Height / 2f, r.Right, r.Y + r.Height / 2f);
                    }
                    else
                    {
                        g.DrawLine(p, r.Left, r.Top, r.Left, r.Bottom);
                        g.DrawLine(p, r.Right, r.Top, r.Right, r.Bottom);
                        using var dash = new Pen(A(map.RoadEdge, 90), 2f) { DashStyle = DashStyle.Dash };
                        g.DrawLine(dash, r.X + r.Width / 2f, r.Top, r.X + r.Width / 2f, r.Bottom);
                    }
                }
            }
        }

        private void DrawHouseFloors(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var h in map.Houses)
            {
                var f = h.Floor;
                if (!view.IntersectsWith(f)) continue;
                g.FillRectangle(NB(Mix(map.FloorColor, Color.White, .04f)), f);
                using (var b = NB(A(Color.FromArgb(255, 208, 150), 14)))
                    g.FillEllipse(b, f.X + f.Width * .16f, f.Y + f.Height * .16f, f.Width * .68f, f.Height * .68f);
                using (var p = NP(A(map.WallEdge, 120), 1f))
                {
                    for (float y = f.Top + 34; y < f.Bottom - 20; y += 34)
                        g.DrawLine(p, f.Left + 18, y, f.Right - 18, y);
                }
                using (var p = NP(A(map.WallEdge, 210), 2f))
                    g.DrawRectangle(p, f.X + 2, f.Y + 2, f.Width - 4, f.Height - 4);
                // tapete
                using (var b = NB(A(map.WallColor, 70)))
                    g.FillRectangle(b, f.X + f.Width * .30f, f.Y + f.Height * .30f, f.Width * .4f, f.Height * .4f);
            }
        }

        private void DrawWalls(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var wall in map.Walls)
            {
                var r = wall.R;
                if (!view.IntersectsWith(r)) continue;
                bool big = r.Width * r.Height > 26000f && !wall.House;
                using (var sh = NB(A(Color.Black, 90))) g.FillRectangle(sh, r.X + 5, r.Y + 6, r.Width, r.Height);
                g.FillRectangle(NB(wall.House ? map.WallColor : Mix(map.WallColor, map.WallEdge, .25f)), r);
                using (var p = NP(map.WallEdge, 2f)) g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
                using (var p = NP(A(Color.White, 32), 1.5f)) g.DrawLine(p, r.Left + 2, r.Top + 2, r.Right - 2, r.Top + 2);
                if (big)
                {
                    using (var p = NP(A(map.WallEdge, 110), 1f))
                    {
                        for (float x = r.Left + 24; x < r.Right - 12; x += 34)
                            g.DrawLine(p, x, r.Top + 8, x, r.Bottom - 8);
                    }
                    using (var b = NB(A(Color.Black, 60)))
                        g.FillRectangle(b, r.X + 10, r.Y + 10, r.Width - 20, r.Height - 20);
                }
            }
        }

        private void DrawProps(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var p in map.Props)
            {
                var r = p.R;
                if (!view.IntersectsWith(r)) continue;
                switch (p.Kind)
                {
                    case PropKind.Car: DrawVehicle(g, r, false, p.Seed); break;
                    case PropKind.Truck: DrawVehicle(g, r, true, p.Seed); break;
                    case PropKind.Crate: DrawCrate(g, r); break;
                    case PropKind.Barrel: DrawBarrel(g, r, p.Seed); break;
                    case PropKind.Tree: DrawTree(g, r, p.VisualRadius, p.Seed); break;
                    case PropKind.Rock: DrawRock(g, r, p.Seed); break;
                    case PropKind.Fountain: DrawFountain(g, r); break;
                }
            }
        }

        private void DrawVehicle(Graphics g, RectangleF r, bool truck, float seed)
        {
            bool horiz = r.Width >= r.Height;
            using (var sh = NB(A(Color.Black, 110)))
            {
                if (horiz) g.FillEllipse(sh, r.X - 4, r.Y + 5, r.Width + 8, r.Height + 2);
                else g.FillEllipse(sh, r.X + 3, r.Y - 4, r.Width + 2, r.Height + 8);
            }
            var body = Color.FromArgb(74, 61, 52);
            var cabin = Color.FromArgb(42, 40, 40);
            using (var p = RoundRect(r, 9)) g.FillPath(NB(body), p);
            using (var p = RoundRect(r, 9)) g.DrawPath(NP(Color.FromArgb(25, 22, 20), 2f), p);

            RectangleF cab, cargo;
            if (horiz)
            {
                cab = new RectangleF(r.X + r.Width * .55f, r.Y + 4, r.Width * .32f, r.Height - 8);
                cargo = new RectangleF(r.X + 6, r.Y + 6, r.Width * .48f, r.Height - 12);
            }
            else
            {
                cab = new RectangleF(r.X + 4, r.Y + r.Height * .55f, r.Width - 8, r.Height * .32f);
                cargo = new RectangleF(r.X + 6, r.Y + 6, r.Width - 12, r.Height * .48f);
            }
            using (var p = RoundRect(cab, 5)) g.FillPath(NB(cabin), p);
            using (var p = RoundRect(cargo, 4)) g.FillPath(NB(Color.FromArgb(55, 48, 42)), p);
            // janelas
            using (var b = NB(Color.FromArgb(22, 33, 38)))
            {
                if (horiz) g.FillRectangle(b, cab.X + 4, cab.Y + 4, cab.Width - 8, cab.Height - 8);
                else g.FillRectangle(b, cab.X + 4, cab.Y + 4, cab.Width - 8, cab.Height - 8);
            }
            // rodas
            using (var b = NB(Color.FromArgb(18, 18, 18)))
            {
                if (horiz)
                {
                    g.FillRectangle(b, r.X + 10, r.Y - 4, 16, 7); g.FillRectangle(b, r.Right - 28, r.Y - 4, 16, 7);
                    g.FillRectangle(b, r.X + 10, r.Bottom - 3, 16, 7); g.FillRectangle(b, r.Right - 28, r.Bottom - 3, 16, 7);
                }
                else
                {
                    g.FillRectangle(b, r.X - 4, r.Y + 10, 7, 16); g.FillRectangle(b, r.X - 4, r.Bottom - 28, 7, 16);
                    g.FillRectangle(b, r.Right - 3, r.Y + 10, 7, 16); g.FillRectangle(b, r.Right - 3, r.Bottom - 28, 7, 16);
                }
            }
            // ferrugem / queimado
            using (var b = NB(A(Color.FromArgb(130, 64, 38), 160)))
            {
                g.FillEllipse(b, r.X + r.Width * .18f, r.Y + r.Height * .25f, 14, 10);
                g.FillEllipse(b, r.X + r.Width * .42f, r.Y + r.Height * .55f, 11, 8);
            }
            if (seed > .72f)
            {
                float fx = r.X + r.Width * .35f, fy = r.Y + r.Height * .45f;
                using (var b = NB(A(Color.FromArgb(255, 130, 40), 130))) g.FillEllipse(b, fx - 12, fy - 12, 24, 24);
                using (var p = NP(Color.FromArgb(255, 170, 60), 2f))
                    g.DrawLine(p, fx, fy + 8, fx + (float)Math.Sin(_clock * 9 + seed * 9) * 5, fy - 12);
            }
        }

        private void DrawCrate(Graphics g, RectangleF r)
        {
            using (var sh = NB(A(Color.Black, 90))) g.FillRectangle(sh, r.X + 3, r.Y + 4, r.Width, r.Height);
            g.FillRectangle(NB(Color.FromArgb(86, 67, 45)), r);
            using (var p = NP(Color.FromArgb(40, 30, 20), 2f))
            {
                g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height);
                g.DrawLine(p, r.Left + 3, r.Top + 3, r.Right - 3, r.Bottom - 3);
                g.DrawLine(p, r.Right - 3, r.Top + 3, r.Left + 3, r.Bottom - 3);
            }
        }

        private void DrawBarrel(Graphics g, RectangleF r, float seed)
        {
            using (var sh = NB(A(Color.Black, 100))) g.FillEllipse(sh, r.X - 1, r.Y + 3, r.Width + 2, r.Height);
            g.FillEllipse(NB(Color.FromArgb(78, 63, 47)), r);
            using (var p = NP(Color.FromArgb(35, 30, 24), 2f)) g.DrawEllipse(p, r);
            using (var p = NP(A(Color.FromArgb(140, 76, 40), 180), 2f))
                g.DrawArc(p, r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6, 10 + seed * 120, 150);
        }

        private void DrawTree(Graphics g, RectangleF r, float radius, float seed)
        {
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            float rad = radius > 0 ? radius : 26f;
            using (var sh = NB(A(Color.Black, 90))) g.FillEllipse(sh, cx - rad, cy + rad * .55f, rad * 2, rad * .8f);
            using (var b = NB(Color.FromArgb(55, 42, 30))) g.FillRectangle(b, cx - 4, cy, 8, rad * .8f);
            using (var b = NB(Color.FromArgb(28, 60, 36))) g.FillEllipse(b, cx - rad, cy - rad, rad * 2, rad * 1.85f);
            using (var b = NB(Color.FromArgb(37, 78, 45))) g.FillEllipse(b, cx - rad * .72f, cy - rad * .95f, rad * 1.35f, rad * 1.3f);
            using (var b = NB(Color.FromArgb(48, 96, 55))) g.FillEllipse(b, cx - rad * .35f, cy - rad * .7f, rad * .9f, rad * .8f);
        }

        private void DrawRock(Graphics g, RectangleF r, float seed)
        {
            var pts = new[]
            {
                new PointF(r.X + r.Width * .1f, r.Bottom),
                new PointF(r.X, r.Y + r.Height * .45f),
                new PointF(r.X + r.Width * .3f, r.Y),
                new PointF(r.X + r.Width * .78f, r.Y + r.Height * .08f),
                new PointF(r.Right, r.Y + r.Height * .55f),
                new PointF(r.X + r.Width * .7f, r.Bottom),
            };
            using (var sh = NB(A(Color.Black, 90)))
            {
                var shp = pts.Select(p => new PointF(p.X + 4, p.Y + 5)).ToArray();
                g.FillPolygon(sh, shp);
            }
            g.FillPolygon(NB(Color.FromArgb(75, 76, 70)), pts);
            using (var p = NP(Color.FromArgb(38, 40, 38), 2f)) g.DrawPolygon(p, pts);
            using (var b = NB(A(Color.White, 18))) g.FillEllipse(b, r.X + r.Width * .25f, r.Y + r.Height * .2f, r.Width * .3f, r.Height * .2f);
        }

        private void DrawFountain(Graphics g, RectangleF r)
        {
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using (var sh = NB(A(Color.Black, 110))) g.FillEllipse(sh, r.X + 3, r.Y + 6, r.Width, r.Height);
            using (var b = NB(Color.FromArgb(78, 76, 68))) g.FillEllipse(b, r);
            using (var p = NP(Color.FromArgb(35, 34, 30), 3f)) g.DrawEllipse(p, r);
            using (var b = NB(Color.FromArgb(28, 70, 80))) g.FillEllipse(b, r.X + 14, r.Y + 14, r.Width - 28, r.Height - 28);
            using (var b = NB(A(Color.FromArgb(120, 220, 235), 70))) g.FillEllipse(b, r.X + 26, r.Y + 26, r.Width - 52, r.Height - 52);
            using (var b = NB(Color.FromArgb(95, 92, 82))) g.FillEllipse(b, cx - 12, cy - 12, 24, 24);
        }

        private void DrawLoot(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var item in _g!.Loot)
            {
                if (item.Taken || item.X < view.Left || item.X > view.Right || item.Y < view.Top || item.Y > view.Bottom) continue;
                float bob = (float)Math.Sin(_clock * 3.2 + item.Seed * 8) * 3f;
                float y = item.Y + bob;
                var glow = item.Kind == "coin" ? Color.FromArgb(255, 205, 80)
                         : item.Kind == "medkit" ? Color.FromArgb(90, 240, 170)
                         : item.Kind == "vest" ? Color.FromArgb(120, 180, 255)
                         : Color.FromArgb(255, 160, 90);
                if (item.Kind == "shop") using (var b = NB(A(Color.FromArgb(255, 180, 60), 60))) g.FillEllipse(b, item.X - 26, y - 26, 52, 52);
                using (var b = NB(A(glow, 30))) g.FillEllipse(b, item.X - 26, y - 26, 52, 52);
                using (var b = NB(A(glow, 70))) g.FillEllipse(b, item.X - 12, y - 12, 24, 24);
                switch (item.Kind)
                {
                    case "coin":
                        using (var b = NB(Color.FromArgb(250, 200, 70))) g.FillEllipse(b, item.X - 8, y - 8, 16, 16);
                        using (var p = NP(Color.FromArgb(120, 88, 20), 1.5f)) g.DrawEllipse(p, item.X - 8, y - 8, 16, 16);
                        break;
                    case "medkit":
                        using (var b = NB(Color.FromArgb(235, 240, 235))) g.FillRectangle(b, item.X - 9, y - 8, 18, 16);
                        using (var b = NB(Color.FromArgb(220, 60, 70)))
                        {
                            g.FillRectangle(b, item.X - 2, y - 6, 4, 12);
                            g.FillRectangle(b, item.X - 6, y - 2, 12, 4);
                        }
                        break;
                    case "vest":
                        using (var b = NB(Color.FromArgb(90, 150, 220))) g.FillRectangle(b, item.X - 7, y - 8, 14, 16);
                        using (var p = NP(Color.White, 1.5f)) g.DrawLines(p, new[] { new PointF(item.X, y - 7), new PointF(item.X + 5, y - 2), new PointF(item.X, y + 7), new PointF(item.X - 5, y - 2), new PointF(item.X, y - 7) });
                        break;
                    default:
                        using (var p = NP(Color.FromArgb(240, 225, 200), 3f))
                            g.DrawLine(p, item.X - 10, y + 4, item.X + 11, y - 3);
                        using (var p = NP(Color.FromArgb(80, 70, 60), 4f))
                            g.DrawLine(p, item.X - 2, y + 1, item.X + 4, y + 7);
                        break;
                }
                bool near = Dist(item.X, item.Y, _g.Player.X, _g.Player.Y) < 88f;
                if (near)
                {
                    string label = "[E] " + item.Label;
                    var sz = g.MeasureString(label, FTiny);
                    using (var b = NB(A(BgDeep, 210)))
                        g.FillRectangle(b, item.X - sz.Width / 2f - 5, y - 34, sz.Width + 10, sz.Height + 4);
                    Txt(g, label, FTiny, Ink, new RectangleF(item.X - sz.Width / 2f, y - 33, sz.Width, sz.Height), StringAlignment.Center);
                }
            }
        }

        private static float Dist(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1, dy = y2 - y1;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void DrawCorpses(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var f in _g!.Fighters)
            {
                if (f.Alive || f.X < view.Left || f.X > view.Right || f.Y < view.Top || f.Y > view.Bottom) continue;
                using (var b = NB(A(Color.FromArgb(90, 20, 26), 110)))
                    g.FillEllipse(b, f.X - 22, f.Y - 12, 44, 26);
                using (var b = NB(Color.FromArgb(55, 50, 48)))
                {
                    var st = g.Save();
                    g.TranslateTransform(f.X, f.Y);
                    g.RotateTransform(f.CorpseAngle * 57.3f + 90);
                    g.FillEllipse(b, -10, -6, 20, 12);
                    g.FillEllipse(b, -17, -4, 9, 8);
                    g.Restore(st);
                }
            }
        }

        private void DrawFighters(Graphics g, WorldMap map, RectangleF view)
        {
            foreach (var f in _g!.Fighters)
            {
                if (!f.Alive || f.X < view.Left || f.X > view.Right || f.Y < view.Top || f.Y > view.Bottom) continue;
                var teamColor = f.IsPlayer ? Ink : f.Team == 0 ? SquadColor(TeamIndex(f)) : Color.FromArgb(210, 80, 86);
                var ring = f.IsPlayer ? Blue : f.Team == 0 ? SquadColor(TeamIndex(f)) : HpRed;

                // laser de mira do jogador
                if (f.IsPlayer && _g.Phase != SessionPhase.MatchOver)
                {
                    using (var p = NP(A(Blue, 45), 1f))
                        g.DrawLine(p, f.X, f.Y, f.X + (float)Math.Cos(f.Angle) * 150, f.Y + (float)Math.Sin(f.Angle) * 150);
                }

                using (var sh = NB(A(Color.Black, 120))) g.FillEllipse(sh, f.X - 15, f.Y + 6, 30, 15);
                using (var ringP = NP(ring, f.IsPlayer ? 2.5f : 1.6f))
                    g.DrawEllipse(ringP, f.X - 18, f.Y - 18, 36, 36);

                var st = g.Save();
                g.TranslateTransform(f.X, f.Y);
                g.RotateTransform(f.Angle * 57.2958f);
                // pernas
                float leg = (float)Math.Sin(f.WalkPhase) * 4f;
                using (var b = NB(Mix(teamColor, Color.Black, .45f)))
                {
                    g.FillRectangle(b, -5, 2 + leg, 5, 12);
                    g.FillRectangle(b, 1, 2 - leg, 5, 12);
                }
                // corpo
                using (var b = NB(teamColor)) g.FillEllipse(b, -10, -10, 20, 20);
                using (var b = NB(A(Color.White, 35))) g.FillEllipse(b, -6, -8, 10, 8);
                // cabeça
                using (var b = NB(Mix(teamColor, Color.White, .35f))) g.FillEllipse(b, -6, -14, 12, 12);
                // arma
                using (var p = NP(Color.FromArgb(28, 28, 30), 3.5f)) g.DrawLine(p, 6, 0, 22, 0);
                using (var p = NP(A(ring, 140), 1f)) g.DrawLine(p, 6, 1, 22, 1);
                if (f.Muzzle > 0f)
                {
                    using (var b = NB(A(Color.FromArgb(255, 220, 120), 230)))
                        g.FillPolygon(b, new[] { new PointF(24, -4), new PointF(34, 0), new PointF(24, 4) });
                    using (var b = NB(A(Color.FromArgb(255, 245, 190), 100)))
                        g.FillEllipse(b, 20, -8, 16, 16);
                }
                g.Restore(st);

                if (f.HitFlash > 0f)
                {
                    using (var b = NB(A(Color.White, (int)(180 * f.HitFlash / .14f))))
                        g.FillEllipse(b, f.X - 12, f.Y - 12, 24, 24);
                }

                // barra de vida e nome
                float bw = 48f;
                using (var b = NB(A(BgDeep, 210))) g.FillRectangle(b, f.X - bw / 2f - 1, f.Y - 38, bw + 2, 6);
                using (var b = NB(f.Health > 50 ? HpGreen : f.Health > 25 ? Amber : HpRed))
                    g.FillRectangle(b, f.X - bw / 2f, f.Y - 37, bw * f.HealthPct, 4);
                if (f.Armor > 0f)
                    using (var b = NB(A(Blue, 200)))
                        g.FillRectangle(b, f.X - bw / 2f, f.Y - 33, bw * Math.Clamp(f.Armor / 100f, 0f, 1f), 2);
                Txt(g, f.Name, FTiny, f.IsPlayer ? Ink : A(Ink, 210),
                    new RectangleF(f.X - 60, f.Y - 55, 120, 16), StringAlignment.Center);
            }
        }

        private int TeamIndex(Fighter f)
        {
            if (_g == null) return 0;
            int idx = 0;
            foreach (var o in _g.Fighters)
            {
                if (o.Team != 0) continue;
                if (o.Id == f.Id) return idx;
                idx++;
            }
            return 0;
        }

        private void DrawBullets(Graphics g, RectangleF view)
        {
            foreach (var b in _g!.Bullets)
            {
                if (b.X < view.Left || b.X > view.Right || b.Y < view.Top || b.Y > view.Bottom) continue;
                using (var p = NP(A(b.Tracer, 90), 4f))
                    g.DrawLine(p, b.TrailX, b.TrailY, b.X, b.Y);
                using (var p = NP(b.Tracer, 1.6f))
                    g.DrawLine(p, b.TrailX, b.TrailY, b.X, b.Y);
                using (var b2 = NB(b.Tracer)) g.FillEllipse(b2, b.X - 2.5f, b.Y - 2.5f, 5, 5);
            }
        }

        private void DrawParticles(Graphics g, RectangleF view)
        {
            foreach (var p in _g!.Particles)
            {
                if (p.X < view.Left || p.X > view.Right || p.Y < view.Top || p.Y > view.Bottom) continue;
                float a = Math.Clamp(p.Life / Math.Max(.01f, p.MaxLife), 0f, 1f);
                using (var b = NB(A(p.Color, (int)(200 * a))))
                {
                    if (p.Kind == 1) g.FillRectangle(b, p.X, p.Y, p.Size * .6f, p.Size * .6f);
                    else g.FillEllipse(b, p.X, p.Y, p.Size, p.Size);
                }
            }
        }

        private void DrawDamageNumbers(Graphics g, RectangleF view)
        {
            foreach (var d in _g!.DamageNumbers)
            {
                if (d.X < view.Left || d.X > view.Right || d.Y < view.Top || d.Y > view.Bottom) continue;
                float a = Math.Clamp(d.Life / d.MaxLife, 0f, 1f);
                var c = d.ByPlayer ? Color.FromArgb(255, 220, 120) : Color.FromArgb(255, 100, 110);
                Txt(g, "-" + d.Amount, FSmallB, A(c, (int)(240 * a)),
                    new RectangleF(d.X - 30, d.Y, 60, 18), StringAlignment.Center);
            }
        }

        // ---------- Overlays de tela ----------
        // ---------- Gun Shop (Deal Duo) ----------
        private void DrawStore(Graphics g, int w, int h)
        {
            if (_g == null) return;
            float t = _storeTimer <= 0f ? 0f : 1f - _storeTimer / 180f;
            g.FillRectangle(NB(A(BgDeep, (int)(190 * (1f - t)))), 0, 0, w, h);
            int rw = 560;
            var r = new Rectangle((w - rw) / 2, (h - 320) / 2, rw, 320);
            FillRoundedRect(g, r, A(PanelHi, 250), 12);
            using (var p = RoundRect(r, 12)) g.DrawPath(NP(A(Red, 240), 3f), p);
            Spaced(g, "GUN SHOP", FTitle, Red, r.X + 30, r.Y + 30, 4f);
            Spaced(g, $"Abre por {(int)Math.Max(0f, _storeTimer)}s", FMid, Ink, r.X + 30, r.Y + 72, 3f);
            int y = r.Y + 120;
            foreach (var gun in Arsenal.Defs.Values.Take(5))
            {
                var row = new Rectangle(r.X + 30, y, r.Width - 60, 50);
                using var b = new SolidBrush(A(Color.Black, 60));
                g.FillRectangle(b, row);
                FillRoundedRect(g, row, A(Ink, 30), 4);
                Txt(g, gun.Name, FName, Ink, new RectangleF(row.X + 20, row.Y, row.Width - 120, 50), StringAlignment.Center);
                Txt(g, $"{gun.Damage} dmg", FTiny, Ink, new RectangleF(row.X + 30, row.Y, 200, 20), StringAlignment.Near);
                var btn = new Rectangle(row.Right - 110, row.Y + 8, 80, 34);
                Reg(btn, "buy:" + gun.Id);
                float bhv = Hv("buy:" + gun.Id);
                FillRoundedRect(g, btn, Mix(A(Panel, 240), A(Red, 120), bhv), 6);
                Txt(g, "Comprar", FTiny, A(Color.White, 200 + (int)(55 * bhv)), btn, StringAlignment.Center);
                y += 60;
                if (y > r.Bottom - 90) break;
            }
                Spaced(g, "MOEDAS DA FURJA — colete as moedas no chão", FBody, Dim, w / 2f, h - 64, 3f, StringAlignment.Center);
        }

                private void DrawGameVignette(Graphics g, int w, int h)
        {
            if (_g == null) return;
            using (var top = new LinearGradientBrush(new Rectangle(0, 0, w, 130), Color.FromArgb(190, 5, 8, 12), Color.FromArgb(0, 5, 8, 12), 90f))
                g.FillRectangle(top, 0, 0, w, 130);
            using (var bot = new LinearGradientBrush(new Rectangle(0, h - 160, w, 160), Color.FromArgb(0, 5, 8, 12), Color.FromArgb(220, 5, 8, 12), 90f))
                g.FillRectangle(bot, 0, h - 160, w, 160);

            if (_g.PlayerDamageFlash > .01f)
                g.FillRectangle(NB(A(Color.FromArgb(255, 30, 40), (int)(70 * _g.PlayerDamageFlash))), 0, 0, w, h);

            if (_g.Player.Alive && _g.Player.Health < 40f)
            {
                float pulse = .35f + .25f * (float)Math.Sin(_clock * 5.2);
                int a = (int)(60 * pulse * (1f - _g.Player.Health / 40f) + 25);
                using (var b = NB(A(Color.FromArgb(255, 20, 35), a)))
                {
                    g.FillRectangle(b, 0, 0, w, 8);
                    g.FillRectangle(b, 0, h - 8, w, 8);
                    g.FillRectangle(b, 0, 0, 8, h);
                    g.FillRectangle(b, w - 8, 0, 8, h);
                }
            }
        }

        private void DrawGameWindowButtons(Graphics g, int w)
        {
            WinBtn(g, new Rectangle(w - 138, 0, 46, 36), "min", 0);
            WinBtn(g, new Rectangle(w - 92, 0, 46, 36), "max", 1);
            WinBtn(g, new Rectangle(w - 46, 0, 46, 36), "close", 2);
        }

        private void DrawCrosshair(Graphics g, int w, int h)
        {
            if (_g == null || !_g.Player.Alive || _paused || _inventoryOpen) return;
            int x = _mouse.X, y = _mouse.Y;
            if (x < 0 || y < 0 || x > w || y > h) return;
            float gap = 6f + _g.PlayerRecoil * 9f;
            float len = 7f;
            var c = Color.FromArgb(235, 245, 240);
            using (var p = NP(Color.FromArgb(180, 0, 0, 0), 3f))
            {
                g.DrawLine(p, x - gap - len, y, x - gap, y);
                g.DrawLine(p, x + gap, y, x + gap + len, y);
                g.DrawLine(p, x, y - gap - len, x, y - gap);
                g.DrawLine(p, x, y + gap, x, y + gap + len);
            }
            using (var p = NP(c, 1.5f))
            {
                g.DrawLine(p, x - gap - len, y, x - gap, y);
                g.DrawLine(p, x + gap, y, x + gap + len, y);
                g.DrawLine(p, x, y - gap - len, x, y - gap);
                g.DrawLine(p, x, y + gap, x, y + gap + len);
            }
            g.FillEllipse(NB(Red), x - 1.5f, y - 1.5f, 3, 3);

            if (_g.HitMarker > 0f)
            {
                float a = Math.Clamp(_g.HitMarker / .3f, 0f, 1f);
                using (var p = NP(A(Color.White, (int)(255 * a)), 2f))
                {
                    g.DrawLine(p, x - 13, y - 13, x - 6, y - 6);
                    g.DrawLine(p, x + 6, y - 6, x + 13, y - 13);
                    g.DrawLine(p, x - 13, y + 13, x - 6, y + 6);
                    g.DrawLine(p, x + 6, y + 6, x + 13, y + 13);
                }
            }
        }
    }
}



