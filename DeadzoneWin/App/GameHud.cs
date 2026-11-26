// GameHud.cs — HUD: vida, grupo, status, minimapa, placar, arma e kill feed.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Deadzone2D
{
    public partial class MainForm
    {
        private void DrawHud(Graphics g, int w, int h)
        {
            if (_g == null) return;
            DrawScoreboard(g, w, h);
            DrawHealthPanel(g);
            DrawInventoryButton(g);
            DrawSquadPanel(g);
            DrawStatusBoxes(g, w);
            DrawMinimap(g, w);
            DrawWeaponPanel(g, w, h);
            DrawKillFeed(g, w, h);
            DrawPickupPrompt(g, w, h);
            DrawHints(g, w, h);
            if (_g.Phase == SessionPhase.MatchOver && !_g.BannerMatch)
                DrawHints(g, w, h);
        }

        private void DrawHealthPanel(Graphics g)
        {
            if (_g == null) return;
            var p = _g.Player;
            var r = new Rectangle(22, 30, 330, 100);
            FillRoundedRect(g, r, A(BgDeep, 235), 10);
            using (var path = RoundRect(r, 10)) g.DrawPath(Pn(A(Line, 190), 1f), path);
            g.FillRectangle(Br(Red), r.X + 12, r.Y, 3, 18);

            Txt(g, "VOCÊ", FTiny, Red, new RectangleF(r.X + 22, r.Y + 8, 80, 14));
            Txt(g, ((int)Math.Round(_hpAnim)).ToString("00"), F(32),
                p.Health > 50 ? Ink : p.Health > 25 ? Amber : HpRed,
                new RectangleF(r.X + 20, r.Y + 24, 66, 42));

            int bx = r.X + 96, by = r.Y + 32, bw = 214, bh = 16;
            FillRoundedRect(g, new Rectangle(bx, by, bw, bh), A(HpGray, 220), 6);
            if (_hpAnim > p.Health + .5f)
                FillRoundedRect(g, new Rectangle(bx, by, (int)(bw * Math.Clamp(_hpAnim / 100f, 0f, 1f)), bh), A(Color.White, 80), 6);
            int fw = (int)(bw * Math.Clamp(p.Health / 100f, 0f, 1f));
            if (fw > 0)
                FillRoundedRect(g, new Rectangle(bx, by, fw, bh), p.Health > 50 ? HpGreen : p.Health > 25 ? Amber : HpRed, 6);
            using (var path = RoundRect(new Rectangle(bx, by, bw, bh), 6)) g.DrawPath(Pn(A(Color.Black, 100), 1f), path);

            int ax = bx, ay = by + bh + 6, aw = bw, ah = 6;
            FillRoundedRect(g, new Rectangle(ax, ay, aw, ah), A(HpGray, 170), 3);
            int awf = (int)(aw * Math.Clamp(p.Armor / 100f, 0f, 1f));
            if (awf > 0) FillRoundedRect(g, new Rectangle(ax, ay, awf, ah), A(Blue, 210), 3);
            Txt(g, "ARMADURA", FTiny, Dim, new RectangleF(ax, ay + 8, 90, 12));
            if (p.Capacete > 0)
            {
                int ch = ay + 26;
                Txt(g, "CAPACETE", FTiny, HpGreen, new RectangleF(bx, ch, 90, 14));
            }
            if (p.Backpacks > 0)
            {
                int bhX = bx + 160, bhY = ay + 2;
                Txt(g, $"MOCHILA ×{p.Backpacks}", FTiny, Amber, new RectangleF(bhX, bhY, 130, 12), StringAlignment.Far);
            }
        }

        private void DrawInventoryButton(Graphics g)
        {
            var r = new Rectangle(22, 140, 172, 34);
            Reg(r, "inv");
            float hv = Hv("inv");
            var bg = _inventoryOpen ? Mix(Red, RedHi, hv) : Mix(A(Panel, 235), PanelHi, hv);
            FillRoundedRect(g, r, bg, 8);
            using (var p = RoundRect(r, 8))
                g.DrawPath(Pn(_inventoryOpen ? RedHi : Mix(Line, Ink, hv), 1.2f), p);
            for (int i = 0; i < 4; i++)
            {
                var c = _inventoryOpen ? Color.White : Mix(Dim, Ink, hv);
                using var b = new SolidBrush(c);
                g.FillRectangle(b, r.X + 13 + (i % 2) * 9, r.Y + 10 + (i / 2) * 9, 7, 7);
            }
            Txt(g, "INVENTÁRIO", FLabel, _inventoryOpen ? Color.White : Ink, new RectangleF(r.X + 42, r.Y + 8, 92, 20));
            Txt(g, "[TAB]", FTiny, _inventoryOpen ? A(Color.White, 190) : Dim, new RectangleF(r.X + 120, r.Y + 10, 44, 16));
        }

        private void DrawSquadPanel(Graphics g)
        {
            if (_g == null) return;
            var team = _g.Fighters.Where(f => f.Team == 0).Take(4).ToList();
            int rows = Math.Max(1, team.Count);
            var r = new Rectangle(22, 184, 300, 28 + rows * 36);
            FillRoundedRect(g, r, A(BgDeep, 215), 10);
            using (var p = RoundRect(r, 10)) g.DrawPath(Pn(A(Line, 160), 1f), p);
            Spaced(g, "GRUPO", FLabel, Ink, r.X + 14, r.Y + 15, 3f);
            Txt(g, $"{team.Count(f => f.Alive)}/{team.Count} VIVOS", FTiny, Dim,
                new RectangleF(r.X + 160, r.Y + 6, 126, 16), StringAlignment.Far);

            int y = r.Y + 26;
            for (int i = 0; i < team.Count; i++)
            {
                var f = team[i];
                var col = SquadColor(i);
                var row = new Rectangle(r.X + 8, y, r.Width - 16, 34);
                if (f.IsPlayer) FillRoundedRect(g, row, A(col, 18), 6);
                using (var b = new SolidBrush(f.Alive ? col : A(col, 90)))
                    g.FillEllipse(b, row.X + 4, row.Y + 6, 22, 22);
                Txt(g, (i + 1).ToString(), FSmallB, f.Alive ? BgDeep : A(BgDeep, 160),
                    new RectangleF(row.X + 4, row.Y + 6, 22, 22), StringAlignment.Center);
                Txt(g, f.Name, FSmallB, f.Alive ? Ink : A(Dim, 170),
                    new RectangleF(row.X + 34, row.Y + 2, 122, 17));
                int bw = 110, bx = row.X + 34, by = row.Y + 21;
                FillRoundedRect(g, new Rectangle(bx, by, bw, 5), A(HpGray, 190), 3);
                if (f.Alive)
                {
                    int fw = (int)(bw * f.HealthPct);
                    if (fw > 0) FillRoundedRect(g, new Rectangle(bx, by, fw, 5), f.Health > 50 ? HpGreen : f.Health > 25 ? Amber : HpRed, 3);
                    Txt(g, ((int)Math.Round(f.Health)).ToString(), FTiny, A(Ink, 220),
                        new RectangleF(row.Right - 46, row.Y + 10, 38, 16), StringAlignment.Far);
                }
                else Txt(g, "MORTO", FTiny, A(HpRed, 200), new RectangleF(row.Right - 56, row.Y + 10, 48, 16), StringAlignment.Far);
                y += 36;
            }
        }

        private void DrawStatusBoxes(Graphics g, int w)
        {
            if (_g == null) return;
            int x = w - 212, bw = 190, bh = 38;
            var alive = new Rectangle(x, 46, bw, bh);
            var kills = new Rectangle(x, 90, bw, bh);

            FillRoundedRect(g, alive, HpGreen, 8);
            using (var p = RoundRect(alive, 8)) g.DrawPath(Pn(A(Color.White, 60), 1f), p);
            PersonIcon(g, alive.X + 10, alive.Y + 8, 22, A(Color.White, 235));
            Txt(g, _g.AliveCount.ToString(), FName, Color.White, new RectangleF(alive.X + 40, alive.Y + 4, 60, 28));
            Spaced(g, "VIVOS", FTiny, A(Color.White, 210), alive.Right - 64, alive.Y + 19, 2f);

            FillRoundedRect(g, kills, HpRed, 8);
            using (var p = RoundRect(kills, 8)) g.DrawPath(Pn(A(Color.White, 60), 1f), p);
            SkullIcon(g, kills.X + 10, kills.Y + 8, 22, A(Color.White, 240));
            Txt(g, _g.Player.Kills.ToString(), FName, Color.White, new RectangleF(kills.X + 40, kills.Y + 4, 60, 28));
            Spaced(g, "KILLS", FTiny, A(Color.White, 210), kills.Right - 64, kills.Y + 19, 2f);
        }

        private void DrawMinimap(Graphics g, int w)
        {
            if (_g == null) return;
            var map = _g.Map;
            int size = 190;
            var outer = new Rectangle(w - 212, 136, size, size);
            var inner = new Rectangle(outer.X + 8, outer.Y + 8, size - 16, size - 16);
            FillRoundedRect(g, outer, A(BgDeep, 215), 95);
            using (var p = RoundRect(outer, 95)) g.DrawPath(Pn(A(Line, 230), 1.5f), p);

            var clip = new GraphicsPath();
            clip.AddEllipse(inner);
            var st = g.Save();
            g.SetClip(clip);
            g.FillEllipse(Br(map.Ground), inner);
            float sc = (inner.Width - 10f) / Math.Max(map.W, map.H);
            float ox = inner.X + (inner.Width - map.W * sc) / 2f;
            float oy = inner.Y + (inner.Height - map.H * sc) / 2f;

            using (var b = new SolidBrush(A(map.RoadColor, 220)))
                foreach (var road in map.Roads) g.FillRectangle(b, ox + road.X * sc, oy + road.Y * sc, Math.Max(1, road.Width * sc), Math.Max(1, road.Height * sc));
            using (var b = new SolidBrush(A(map.RoadColor, 200)))
                foreach (var pl in map.Plazas) g.FillRectangle(b, ox + pl.X * sc, oy + pl.Y * sc, Math.Max(1, pl.Width * sc), Math.Max(1, pl.Height * sc));
            using (var b = new SolidBrush(A(map.WallColor, 230)))
                foreach (var wall in map.Walls)
                {
                    if (wall.R.Width < 40 && wall.R.Height < 40) continue;
                    g.FillRectangle(b, ox + wall.R.X * sc, oy + wall.R.Y * sc, Math.Max(1, wall.R.Width * sc), Math.Max(1, wall.R.Height * sc));
                }

            // pings de tiro (vermelhos)
            foreach (var ping in _g.Pings)
            {
                float a = Math.Clamp(ping.Life / ping.MaxLife, 0f, 1f);
                float px = ox + ping.X * sc, py = oy + ping.Y * sc;
                using (var b = new SolidBrush(A(HpRed, (int)(190 * a))))
                    g.FillEllipse(b, px - 5 - (1 - a) * 5, py - 5 - (1 - a) * 5, 10 + (1 - a) * 10, 10 + (1 - a) * 10);
                using (var b = new SolidBrush(A(Color.White, (int)(220 * a))))
                    g.FillEllipse(b, px - 1.8f, py - 1.8f, 3.6f, 3.6f);
            }

            // aliados
            foreach (var f in _g.Fighters)
            {
                if (!f.Alive || f.Team != 0) continue;
                float px = ox + f.X * sc, py = oy + f.Y * sc;
                if (f.IsPlayer)
                {
                    float ang = f.Angle;
                    var pts = new[]
                    {
                        new PointF(px + (float)Math.Cos(ang) * 7, py + (float)Math.Sin(ang) * 7),
                        new PointF(px + (float)Math.Cos(ang + 2.5) * 5, py + (float)Math.Sin(ang + 2.5) * 5),
                        new PointF(px + (float)Math.Cos(ang - 2.5) * 5, py + (float)Math.Sin(ang - 2.5) * 5),
                    };
                    g.FillPolygon(Br(Color.White), pts);
                    var outline = Pn(A(Blue, 200), 1f);
                    g.DrawPolygon(outline, pts);
                }
                else
                {
                    using var b = new SolidBrush(SquadColor(TeamIndex(f)));
                    g.FillEllipse(b, px - 3, py - 3, 6, 6);
                }
            }

            // varredura de radar
            float sweep = (_clock * 55f) % 360f;
            using (var p = new Pen(A(HpRed, 45), 1.2f))
                g.DrawLine(p, inner.X + inner.Width / 2f, inner.Y + inner.Height / 2f,
                    inner.X + inner.Width / 2f + (float)Math.Cos(sweep * Math.PI / 180) * inner.Width * .48f,
                    inner.Y + inner.Height / 2f + (float)Math.Sin(sweep * Math.PI / 180) * inner.Width * .48f);
            g.Restore(st);
            g.ResetClip();

            using (var p = NP(A(Color.White, 60), 1f)) g.DrawEllipse(p, inner);
            Spaced(g, "N", FTiny, A(Ink, 200), outer.X + outer.Width / 2f, outer.Y + 12, 2f, StringAlignment.Center);
        }

        private void DrawScoreboard(Graphics g, int w, int h)
        {
            if (_g == null) return;
            int cw = 340, ch = 82;
            int x = (w - cw) / 2, y = 8;
            var r = new Rectangle(x, y, cw, ch);
            FillRoundedRect(g, r, A(BgDeep, 210), 10);
            using (var p = RoundRect(r, 10)) g.DrawPath(Pn(A(Line, 210), 1f), p);
            if (_scorePop > .01f)
            {
                using var glow = new Pen(A(Amber, (int)(120 * _scorePop)), 2f);
                g.DrawLine(glow, r.Left + 24, r.Bottom, r.Right - 24, r.Bottom);
            }

            if (_g.Kind == SessionKind.Royale)
            {
                Spaced(g, "DEAD ROYALE", FLabel, Red, r.X + 18, r.Y + 16, 3f);
                Txt(g, _g.AliveCount + " VIVOS", FSmallB, Dim, new RectangleF(r.Right - 130, r.Y + 8, 112, 18), StringAlignment.Far);
                if (_g.Phase == SessionPhase.Countdown)
                {
                    var ts = TimeSpan.FromSeconds(Math.Max(0, _g.Countdown));
                    Txt(g, ts.ToString(@"mm\:ss"), FTitle, Ink, new RectangleF(r.X + 18, r.Y + 24, 150, 48));
                    Spaced(g, "PREPARAÇÃO", FLabel, Amber, r.X + 178, r.Y + 40, 3f);
                    var skip = new Rectangle(r.Right - 92, r.Y + 46, 78, 24);
                    Reg(skip, "skipCountdown");
                    float hv = Hv("skipCountdown");
                    FillRoundedRect(g, skip, Mix(A(Panel, 240), Red, hv), 6);
                    Txt(g, "PULAR", FTiny, hv > .3f ? Color.White : A(Ink, 210), new RectangleF(skip.X, skip.Y, skip.Width, skip.Height), StringAlignment.Center);
                }
                else
                {
                    Txt(g, _g.AliveCount.ToString("00"), FTitle, Ink, new RectangleF(r.X + 18, r.Y + 24, 90, 48));
                    Spaced(g, "RESTANTES", FLabel, Amber, r.X + 104, r.Y + 40, 3f);
                    Txt(g, _g.MatchTime.ToString("mm\\:ss"), FSmallB, Dim, new RectangleF(r.Right - 92, r.Y + 30, 76, 22), StringAlignment.Far);
                    Txt(g, _g.Player.Kills + " KILLS", FTiny, HpRed, new RectangleF(r.Right - 108, r.Y + 50, 92, 18), StringAlignment.Far);
                }
                return;
            }

            var col0 = Ink;
            var col1 = HpRed;
            Txt(g, _g.Score0.ToString(), FTitle, col0, new RectangleF(r.X + 18, r.Y + 12, 92, 52), StringAlignment.Center);
            Txt(g, "X", FName, Dim, new RectangleF(r.X + cw / 2f - 20, r.Y + 24, 40, 34), StringAlignment.Center);
            Txt(g, _g.Score1.ToString(), FTitle, col1, new RectangleF(r.Right - 110, r.Y + 12, 92, 52), StringAlignment.Center);
            Spaced(g, "ALIADOS", FTiny, A(col0, 200), r.X + 18, r.Y + 66, 2f);
            Spaced(g, "INIMIGOS", FTiny, A(col1, 200), r.Right - 110, r.Y + 66, 2f);
            // pips
            for (int i = 0; i < _g.MaxWins; i++)
            {
                using var b0 = new SolidBrush(i < _g.Score0 ? col0 : A(Line, 220));
                using var b1 = new SolidBrush(i < _g.Score1 ? col1 : A(Line, 220));
                g.FillRectangle(b0, r.X + 36 + i * 14, r.Y + 58, 9, 4);
                g.FillRectangle(b1, r.Right - 45 - i * 14, r.Y + 58, 9, 4);
            }
        }

        private void DrawWeaponPanel(Graphics g, int w, int h)
        {
            if (_g == null) return;
            var p = _g.Player;
            var wd = p.Weapon;
            var r = new Rectangle(w - 278, h - 110, 256, 86);
            FillRoundedRect(g, r, A(BgDeep, 210), 10);
            using (var path = RoundRect(r, 10)) g.DrawPath(Pn(A(Line, 200), 1f), path);
            g.FillRectangle(Br(Red), r.X + 12, r.Y, 3, 18);
            Spaced(g, "ARMA", FTiny, Red, r.X + 22, r.Y + 12, 3f);
            Txt(g, wd.Name, FName, Ink, new RectangleF(r.X + 20, r.Y + 20, 150, 26));
            Txt(g, p.Mag + " / " + p.Reserve, FName, p.Mag <= wd.MagSize * .25f ? HpRed : Ink,
                new RectangleF(r.Right - 106, r.Y + 18, 90, 26), StringAlignment.Far);
            Txt(g, p.ReloadLeft > 0 ? "RECARREGANDO..." : "R  RECARREGAR", FTiny, Dim,
                new RectangleF(r.X + 20, r.Y + 48, 150, 16));
            DrawCoinIcon(g, r.Right - 72, r.Y + 56, 10);
            Txt(g, p.Coins.ToString(), FTiny, Amber, new RectangleF(r.Right - 60, r.Y + 47, 40, 16));
            DrawMedkitIcon(g, r.Right - 28, r.Y + 56, 12, p.Medkits > 0 ? HpGreen : Dim);
            Txt(g, "x" + p.Medkits, FTiny, Ink, new RectangleF(r.Right - 16, r.Y + 47, 20, 16));
            if (p.ReloadLeft > 0)
            {
                float total = Math.Max(.1f, wd.ReloadTime);
                float t = 1f - p.ReloadLeft / total;
                var br = new Rectangle(r.X + 20, r.Bottom - 12, r.Width - 40, 4);
                FillRoundedRect(g, br, A(HpGray, 200), 2);
                FillRoundedRect(g, new Rectangle(br.X, br.Y, (int)(br.Width * Math.Clamp(t, 0, 1)), br.Height), Red, 2);
            }
        }

        private void DrawKillFeed(Graphics g, int w, int h)
        {
            if (_g == null || _g.Feed.Count == 0) return;
            var feed = _g.Feed.Skip(Math.Max(0, _g.Feed.Count - 4)).ToList();
            int y = h - 116;
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                var e = feed[i];
                float a = Math.Clamp(e.Life / 1.2f, 0f, 1f);
                int rowW = 280;
                var r = new Rectangle(22, y, rowW, 26);
                FillRoundedRect(g, r, A(BgDeep, (int)(185 * a)), 5);
                g.FillRectangle(Br(A(e.KillerTeam == 0 ? Blue : HpRed, (int)(200 * a))), r.X, r.Y, 3, r.Height);
                string k = string.IsNullOrEmpty(e.Killer) ? "—" : e.Killer;
                Txt(g, k, FSmallB, A(e.KillerTeam == 0 ? Ink : HpRed, (int)(240 * a)),
                    new RectangleF(r.X + 12, r.Y + 3, 112, 18));
                Txt(g, e.Weapon, FTiny, A(Dim, (int)(220 * a)),
                    new RectangleF(r.X + 124, r.Y + 4, 64, 16), StringAlignment.Center);
                Txt(g, e.Victim, FSmallB, A(Ink, (int)(220 * a)),
                    new RectangleF(r.X + 190, r.Y + 3, 84, 18), StringAlignment.Far);
                y -= 30;
            }
        }

        private void DrawPickupPrompt(Graphics g, int w, int h)
        {
            if (_g == null || !_g.Player.Alive || _inventoryOpen || _paused) return;
            var item = _g.NearestLoot(_g.Player);
            if (item == null) return;
            float pulse = .75f + .25f * (float)Math.Sin(_clock * 5.5);
            string text = "PEGAR " + item.Label.ToUpperInvariant();
            var sz = g.MeasureString(text, FMid);
            var r = new Rectangle((int)(w / 2f - sz.Width / 2f - 52), h - 92, (int)sz.Width + 104, 38);
            FillRoundedRect(g, r, A(BgDeep, (int)(210 * pulse)), 7);
            using (var p = RoundRect(r, 7)) g.DrawPath(Pn(A(Amber, (int)(180 * pulse)), 1f), p);
            FillRoundedRect(g, new Rectangle(r.X + 10, r.Y + 8, 30, 22), A(Amber, 230), 4);
            Txt(g, "E", FSmallB, BgDeep, new RectangleF(r.X + 10, r.Y + 8, 30, 22), StringAlignment.Center);
            Txt(g, text, FSmallB, Ink, new RectangleF(r.X + 50, r.Y + 9, r.Width - 60, 22));
        }

        private void DrawHints(Graphics g, int w, int h)
        {
            string hint = "WASD MOVER   •   MOUSE MIRAR   •   CLIQUE ATIRAR   •   R RECARREGAR   •   E PEGAR   •   TAB INVENTÁRIO   •   ESC PAUSA";
            Txt(g, hint, FTiny, A(Dim, 160), new RectangleF(0, h - 26, w, 16), StringAlignment.Center);
        }

        // ---------- Ícones ----------
        private static void PersonIcon(Graphics g, float x, float y, float s, Color c)
        {
            using var b = new SolidBrush(c);
            g.FillEllipse(b, x + s * .30f, y + s * .08f, s * .40f, s * .40f);
            g.FillPie(b, x + s * .08f, y + s * .44f, s * .84f, s * .72f, 180, 180);
        }

        private static void SkullIcon(Graphics g, float x, float y, float s, Color c)
        {
            using var b = new SolidBrush(c);
            g.FillEllipse(b, x + s * .08f, y + s * .06f, s * .84f, s * .72f);
            g.FillRectangle(b, x + s * .26f, y + s * .58f, s * .48f, s * .28f);
            using var e = new SolidBrush(Color.FromArgb(35, 38, 42));
            g.FillEllipse(e, x + s * .26f, y + s * .28f, s * .16f, s * .18f);
            g.FillEllipse(e, x + s * .58f, y + s * .28f, s * .16f, s * .18f);
            g.FillPolygon(e, new[] { new PointF(x + s * .5f, y + s * .48f), new PointF(x + s * .43f, y + s * .6f), new PointF(x + s * .57f, y + s * .6f) });
        }

        private static void EyeIcon(Graphics g, float cx, float cy, float s, Color c)
        {
            using var p = new Pen(c, Math.Max(1.2f, s * .10f));
            using var path = new GraphicsPath();
            path.AddBezier(cx - s * .55f, cy, cx - s * .22f, cy - s * .38f, cx + s * .22f, cy - s * .38f, cx + s * .55f, cy);
            path.AddBezier(cx + s * .55f, cy, cx + s * .22f, cy + s * .38f, cx - s * .22f, cy + s * .38f, cx - s * .55f, cy);
            g.DrawPath(p, path);
            using var b = new SolidBrush(c);
            g.FillEllipse(b, cx - s * .12f, cy - s * .12f, s * .24f, s * .24f);
        }

        private static void DrawCoinIcon(Graphics g, float cx, float cy, float r)
        {
            using var b = new SolidBrush(Color.FromArgb(245, 197, 66));
            g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
            using var p = new Pen(Color.FromArgb(120, 88, 20), 1.4f);
            g.DrawEllipse(p, cx - r, cy - r, r * 2, r * 2);
            g.DrawEllipse(p, cx - r * .45f, cy - r * .45f, r * .9f, r * .9f);
        }

        private static void DrawMedkitIcon(Graphics g, float cx, float cy, float r, Color c)
        {
            using var b = new SolidBrush(c);
            g.FillRectangle(b, cx - r, cy - r * .75f, r * 2, r * 1.5f);
            using var w = new SolidBrush(Color.White);
            g.FillRectangle(w, cx - r * .18f, cy - r * .48f, r * .36f, r * .96f);
            g.FillRectangle(w, cx - r * .48f, cy - r * .18f, r * .96f, r * .36f);
        }

        // TAB — detalhes da partida
        private void DrawLeaderboard(Graphics g, int w, int h)
        {
            if (_g == null) return;
            var rank = _g.Fighters.OrderByDescending(f => f.Kills).ThenByDescending(f => f.DamageDealt).ThenBy(f => f.Deaths).ToList();
            int rw = 560, rh = 64 + rank.Count * 28;
            var r = new Rectangle((w - rw) / 2, (h - rh) / 2, rw, rh);
            FillRoundedRect(g, r, A(BgDeep, 240), 10);
            using (var p = RoundRect(r, 10)) g.DrawPath(Pn(Line, 1f), p);
            Spaced(g, "MATCH DETAILS — TAB", FTitle, Red, r.X + 24, r.Y + 22, 4f);
            Spaced(g, "PLAYER", FTiny, Dim, r.X + 24, r.Y + 52, 2f);
            Spaced(g, "KILLS", FTiny, Dim, r.X + 360, r.Y + 52, 2f);
            Spaced(g, "DAMAGE", FTiny, Dim, r.X + 430, r.Y + 52, 2f);
            Spaced(g, "DEATHS", FTiny, Dim, r.X + 510, r.Y + 52, 2f);
            int y = r.Y + 70;
            foreach (var f in rank)
            {
                var col = f.IsPlayer ? Red : f.Team == 0 ? Blue : HpRed;
                Txt(g, f.Name, FSmallB, f.Alive ? col : A(Dim, 160), new RectangleF(r.X + 24, y, 300, 22));
                Txt(g, f.Kills.ToString(), FSmallB, Ink, new RectangleF(r.X + 360, y, 50, 22), StringAlignment.Center);
                Txt(g, f.DamageDealt.ToString(), FSmallB, Amber, new RectangleF(r.X + 430, y, 60, 22), StringAlignment.Center);
                Txt(g, f.Deaths.ToString(), FSmallB, Dim, new RectangleF(r.X + 510, y, 40, 22), StringAlignment.Center);
                y += 28;
            }
        }
    }
}

