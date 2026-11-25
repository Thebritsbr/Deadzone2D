// GameOverlays.cs — Inventário, pausa, tela de morte/espectador e banners de vitória/derrota.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace CrossfireWin
{
    public partial class MainForm
    {
        private int SpectatorCount()
        {
            if (_g == null) return 1;
            return Math.Max(1, 1 + _g.Fighters.Count(f => f.Team == 0 && f.Alive && !f.IsPlayer));
        }

        // ---------- Inventário ----------
        private void DrawInventoryPanel(Graphics g, int w, int h)
        {
            if (_g == null) return;
            float t = _invAnim;
            int a = (int)(210 * t);
            g.FillRectangle(Br(A(BgDeep, a)), 0, 0, w, h);

            int pw = 760, ph = 486;
            int px = (w - pw) / 2;
            int py = (h - ph) / 2 + (int)((1f - t) * 30f);
            var r = new Rectangle(px, py, pw, ph);
            FillRoundedRect(g, r, A(PanelHi, (int)(250 * t)), 14);
            using (var path = RoundRect(r, 14))
                g.DrawPath(Pn(A(Red, (int)(220 * t)), 1.5f), path);
            g.FillRectangle(Br(A(Red, (int)(255 * t))), r.X + 22, r.Y, r.Width - 44, 3);

            Txt(g, "INVENTÁRIO", F(40), A(Ink, (int)(255 * t)), new RectangleF(r.X + 26, r.Y + 16, 320, 44));
            Spaced(g, "EQUIPAMENTO E CONSUMÍVEIS", FTiny, A(Dim, (int)(230 * t)), r.X + 28, r.Y + 66, 3f);

            var close = new Rectangle(r.Right - 54, r.Y + 18, 34, 34);
            Reg(close, "invClose");
            float chv = Hv("invClose");
            using (var b = new SolidBrush(A(Red, (int)((30 + 90 * chv) * t)))) g.FillEllipse(b, close);
            using (var xp = new Pen(A(Ink, (int)((160 + 95 * chv) * t)), 1.8f))
            {
                g.DrawLine(xp, close.X + 11, close.Y + 11, close.Right - 11, close.Bottom - 11);
                g.DrawLine(xp, close.Right - 11, close.Y + 11, close.X + 11, close.Bottom - 11);
            }

            var p = _g.Player;
            // arma equipada
            var gunCard = new Rectangle(r.X + 26, r.Y + 96, 330, 208);
            FillRoundedRect(g, gunCard, A(BgDeep, (int)(190 * t)), 10);
            using (var path = RoundRect(gunCard, 10)) g.DrawPath(Pn(A(Line, (int)(210 * t)), 1f), path);
            Spaced(g, "ARMA EQUIPADA", FTiny, A(Red, (int)(240 * t)), gunCard.X + 16, gunCard.Y + 18, 3f);
            Txt(g, p.Weapon.Name, FTitle, A(Ink, (int)(255 * t)), new RectangleF(gunCard.X + 16, gunCard.Y + 30, 290, 46));
            Txt(g, "DANO", FTiny, Dim, new RectangleF(gunCard.X + 16, gunCard.Y + 86, 70, 16));
            Txt(g, p.Weapon.Damage.ToString(), FName, A(Ink, (int)(255 * t)), new RectangleF(gunCard.X + 88, gunCard.Y + 82, 60, 24));
            Txt(g, "CADÊNCIA", FTiny, Dim, new RectangleF(gunCard.X + 16, gunCard.Y + 114, 80, 16));
            Txt(g, $"{1f / Math.Max(.01f, p.Weapon.FireInterval):0.0}/s", FSmallB, A(Ink, (int)(255 * t)), new RectangleF(gunCard.X + 96, gunCard.Y + 112, 90, 20));
            Txt(g, "ALCANCE", FTiny, Dim, new RectangleF(gunCard.X + 16, gunCard.Y + 142, 80, 16));
            Txt(g, $"{(int)p.Weapon.Range}m", FSmallB, A(Ink, (int)(255 * t)), new RectangleF(gunCard.X + 96, gunCard.Y + 140, 90, 20));
            Txt(g, p.Mag + " / " + p.Reserve, FTitle, A(Amber, (int)(255 * t)),
                new RectangleF(gunCard.X + 16, gunCard.Y + 160, 290, 40));
            // silhueta da arma
            using (var pen = new Pen(A(Ink, (int)(120 * t)), 5f))
            {
                g.DrawLine(pen, gunCard.Right - 132, gunCard.Bottom - 34, gunCard.Right - 20, gunCard.Bottom - 34);
                g.DrawLine(pen, gunCard.Right - 100, gunCard.Bottom - 34, gunCard.Right - 84, gunCard.Bottom - 54);
                g.DrawLine(pen, gunCard.Right - 52, gunCard.Bottom - 34, gunCard.Right - 62, gunCard.Bottom - 20);
            }

            // consumíveis
            var med = new Rectangle(r.X + 376, r.Y + 96, 172, 100);
            var coin = new Rectangle(r.X + 560, r.Y + 96, 174, 100);
            var vest = new Rectangle(r.X + 376, r.Y + 208, 172, 100);
            var ammo = new Rectangle(r.X + 560, r.Y + 208, 174, 100);
            InvCard(g, med, "KIT MÉDICO", "x" + p.Medkits, p.Medkits > 0 ? HpGreen : Dim, t, "invMed", p.Medkits > 0);
            InvCard(g, coin, "MOEDAS", p.Coins.ToString(), Amber, t, null, false);
            InvCard(g, vest, "COLETE", ((int)p.Armor).ToString(), Blue, t, null, false);
            InvCard(g, ammo, "MUNIÇÃO", p.Reserve.ToString(), Ink, t, null, false);

            // resumo
            var info = new Rectangle(r.X + 26, r.Y + 324, 708, 104);
            FillRoundedRect(g, info, A(BgDeep, (int)(170 * t)), 8);
            Txt(g, "KILLS  " + p.Kills, FSmallB, A(Ink, (int)(230 * t)), new RectangleF(info.X + 18, info.Y + 16, 160, 20));
            Txt(g, "MORTES  " + p.Deaths, FSmallB, A(Dim, (int)(230 * t)), new RectangleF(info.X + 180, info.Y + 16, 160, 20));
            Txt(g, "MODO  " + (_g.Kind == SessionKind.Royale ? "DEAD ROYALE" : _g.Mode == "SquadBattle" ? "SQUAD BATTLE" : "DEAL DUO"),
                FSmallB, A(Dim, (int)(230 * t)), new RectangleF(info.X + 18, info.Y + 48, 420, 20));
            Txt(g, "H  USAR KIT MÉDICO     TAB  FECHAR", FTiny, A(Dim, (int)(190 * t)),
                new RectangleF(info.X + 18, info.Y + 76, 600, 16));
        }

        private void InvCard(Graphics g, Rectangle r, string title, string value, Color accent, float t, string? id, bool clickable)
        {
            if (id != null) Reg(r, id);
            float hv = id != null ? Hv(id) : 0f;
            FillRoundedRect(g, r, A(BgDeep, (int)((180 + 40 * hv) * t)), 9);
            using (var path = RoundRect(r, 9))
                g.DrawPath(Pn(A(clickable ? Mix(accent, Color.White, hv * .4f) : Line, (int)((180 + 60 * hv) * t)), 1.1f), path);
            g.FillRectangle(Br(A(accent, (int)(220 * t))), r.X + 12, r.Y + 12, 3, 18);
            Spaced(g, title, FTiny, A(Dim, (int)(220 * t)), r.X + 22, r.Y + 20, 2f);
            Txt(g, value, FName, A(accent, (int)(255 * t)), new RectangleF(r.X + 14, r.Y + 34, 110, 30));
            if (clickable)
            {
                Txt(g, "USAR", FLabel, A(Color.White, (int)((140 + 100 * hv) * t)),
                    new RectangleF(r.Right - 62, r.Y + 62, 50, 20), StringAlignment.Far);
            }
        }

        // ---------- Pausa ----------
        private void DrawPause(Graphics g, int w, int h)
        {
            float t = _pauseAnim;
            g.FillRectangle(Br(A(BgDeep, (int)(205 * t))), 0, 0, w, h);
            float y = h / 2f - 180;
            Spaced(g, "PAUSADO", FTitle, A(Ink, (int)(255 * t)), w / 2f, y, 8f, StringAlignment.Center);
            Txt(g, "A partida está congelada", FBody, A(Dim, (int)(220 * t)),
                new RectangleF(0, y + 38, w, 24), StringAlignment.Center);
            var b1 = new Rectangle(w / 2 - 190, (int)y + 96, 380, 62);
            var b2 = new Rectangle(w / 2 - 190, (int)y + 174, 380, 54);
            Button(g, b1, "resume", "CONTINUAR", true, FBtn, 4f);
            Button(g, b2, "lobbyNow", "SAIR PARA O LOBBY", false, FMid, 3f);
            Txt(g, "ESC para voltar ao jogo", FTiny, A(Dim, (int)(180 * t)),
                new RectangleF(0, y + 246, w, 18), StringAlignment.Center);
        }

        // ---------- Morte / espectador ----------
        private void DrawDeathScreen(Graphics g, int w, int h)
        {
            if (_g == null) return;
            float t = Math.Max(.001f, _deathFade);
            using (var top = new LinearGradientBrush(new Rectangle(0, 0, w, 190), A(Color.FromArgb(6, 9, 13), (int)(225 * t)), A(Color.FromArgb(6, 9, 13), 0), 90f))
                g.FillRectangle(top, 0, 0, w, 190);
            using (var bot = new LinearGradientBrush(new Rectangle(0, h - 180, w, 180), A(Color.FromArgb(6, 9, 13), 0), A(Color.FromArgb(6, 9, 13), (int)(225 * t)), 90f))
                g.FillRectangle(bot, 0, h - 180, w, 180);

            int pw = 620, ph = 108;
            int px = (w - pw) / 2, py = 102;
            var panel = new Rectangle(px, py, pw, ph);
            FillRoundedRect(g, panel, A(Panel, (int)(225 * t)), 10);
            using (var path = RoundRect(panel, 10)) g.DrawPath(Pn(A(HpRed, (int)(180 * t)), 1.2f), path);
            Spaced(g, "VOCÊ MORREU", FLabel, A(HpRed, (int)(255 * t)), panel.X + 22, panel.Y + 18, 4f);
            Txt(g, "ESPECTANDO", FTiny, A(Dim, (int)(210 * t)), new RectangleF(panel.Right - 120, panel.Y + 10, 100, 16), StringAlignment.Far);

            var list = _g.SpectateList();
            var spec = SpectatedFighter();
            string name = spec != null ? spec.Name : "—";
            var prev = new Rectangle(panel.X + 22, panel.Y + 36, 42, 42);
            var next = new Rectangle(panel.Right - 64, panel.Y + 36, 42, 42);
            Reg(prev, "specPrev"); Reg(next, "specNext");
            float phv = Hv("specPrev"), nhv = Hv("specNext");
            FillRoundedRect(g, prev, A(BgDeep, (int)(180 * t)), 7);
            FillRoundedRect(g, next, A(BgDeep, (int)(180 * t)), 7);
            using (var p = new Pen(A(Mix(Dim, Ink, phv), (int)(240 * t)), 2f))
                g.DrawLines(p, new[] { new Point(prev.X + 25, prev.Y + 14), new Point(prev.X + 16, prev.Y + 21), new Point(prev.X + 25, prev.Y + 28) });
            using (var p = new Pen(A(Mix(Dim, Ink, nhv), (int)(240 * t)), 2f))
                g.DrawLines(p, new[] { new Point(next.X + 17, next.Y + 14), new Point(next.X + 26, next.Y + 21), new Point(next.X + 17, next.Y + 28) });

            Txt(g, name, FName, A(Ink, (int)(255 * t)), new RectangleF(panel.X + 80, panel.Y + 36, panel.Width - 160, 28), StringAlignment.Center);
            Txt(g, list.Count > 0 ? $"{(_spectate < 0 ? 1 : _spectate + 1)} / {list.Count}" : "0 / 0", FTiny, A(Dim, (int)(210 * t)),
                new RectangleF(panel.X + 80, panel.Y + 62, panel.Width - 160, 16), StringAlignment.Center);

            // olhinho de espectadores
            int watch = SpectatorCount();
            EyeIcon(g, panel.Right - 112, panel.Y + 84, 17, A(Ink, (int)(220 * t)));
            Txt(g, watch.ToString(), FSmallB, A(Ink, (int)(230 * t)), new RectangleF(panel.Right - 100, panel.Y + 74, 40, 20));
            Txt(g, "assistindo", FTiny, A(Dim, (int)(190 * t)), new RectangleF(panel.Right - 64, panel.Y + 74, 60, 20));

            // botão branco LOBBY
            var lobby = new Rectangle(w / 2 - 120, h - 118, 240, 56);
            Reg(lobby, "specLobby");
            float lhv = Hv("specLobby");
            FillRoundedRect(g, lobby, Mix(Color.White, Color.FromArgb(225, 230, 235), lhv), 8);
            if (lhv > .01f)
                using (var p = RoundRect(Rectangle.Inflate(lobby, 3, 3), 10))
                    g.DrawPath(Pn(A(Color.White, (int)(120 * lhv)), 2f), p);
            Txt(g, "LOBBY", FName, Color.FromArgb(20, 24, 28), new RectangleF(lobby.X, lobby.Y, lobby.Width, lobby.Height), StringAlignment.Center);
            Txt(g, "Use as setas para trocar de jogador", FTiny, A(Dim, (int)(190 * t)),
                new RectangleF(0, h - 54, w, 16), StringAlignment.Center);
        }

        // ---------- Banner de resultado ----------
        private void DrawBanner(Graphics g, int w, int h)
        {
            if (_g == null || (_g.BannerLife <= 0f && _bannerAnim <= .01f)) return;
            float alpha;
            if (_g.BannerMatch) alpha = 1f;
            else
            {
                float intro = Math.Clamp((_g.BannerMax - _g.BannerLife) / .28f, 0f, 1f);
                float outro = Math.Clamp(_g.BannerLife / .45f, 0f, 1f);
                alpha = Math.Min(intro, outro);
            }
            alpha *= Math.Max(.001f, _bannerAnim);
            if (alpha <= .01f) return;

            bool match = _g.BannerMatch;
            int bw = match ? (int)(w * .78f) : (int)(w * .66f);
            int bh = match ? 158 : 112;
            int bx = (w - bw) / 2;
            int by = (int)(h / 2f - bh / 2f - (match ? 30 : 0) + (1f - _bannerAnim) * 26f);
            var r = new Rectangle(bx, by, bw, bh);
            var bg = _g.BannerBg;
            using (var path = Chamfer(r, 22, true, false, true, false))
            {
                using (var lg = new LinearGradientBrush(r, Mix(bg, Color.White, .10f), Mix(bg, Color.Black, .22f), 90f))
                    g.FillPath(lg, path);
                g.DrawPath(Pn(A(Mix(bg, Color.White, .35f), (int)(230 * alpha)), 1.4f), path);
            }
            // detalhes
            g.FillRectangle(Br(A(match ? Color.White : Color.Black, (int)(60 * alpha))), r.X + 30, r.Y + 8, r.Width - 60, 2);
            g.FillRectangle(Br(A(match ? Color.White : Color.Black, (int)(40 * alpha))), r.X + 30, r.Bottom - 10, r.Width - 60, 2);
            if (match && bg.GetBrightness() > .5f)
            {
                // brilho dourado
                for (int i = 0; i < 14; i++)
                {
                    float sx = r.X + 30 + (i * 97 % (r.Width - 60));
                    float sy = r.Y + 18 + (float)Math.Sin(_clock * 2.2 + i) * 6 + (i % 3) * 8;
                    using var b = new SolidBrush(A(Color.White, (int)(90 * alpha)));
                    g.FillEllipse(b, sx, sy, 3 + i % 3, 3 + i % 3);
                }
            }

            var fg = _g.BannerFg;
            Txt(g, _g.BannerText, match ? FHuge : FTitle, A(fg, (int)(255 * alpha)),
                new RectangleF(r.X + 20, r.Y + (match ? 18 : 12), r.Width - 40, match ? 92 : 56), StringAlignment.Center);
            Txt(g, _g.BannerSub, FName, A(fg, (int)(215 * alpha)),
                new RectangleF(r.X + 20, r.Y + (match ? 108 : 70), r.Width - 40, 30), StringAlignment.Center);

            if (match)
            {
                var lobby = new Rectangle(w / 2 - 120, r.Bottom + 28, 240, 54);
                Reg(lobby, "lobbyAfterWin");
                float hv = Hv("lobbyAfterWin");
                FillRoundedRect(g, lobby, Mix(Color.White, Color.FromArgb(225, 230, 235), hv), 8);
                Txt(g, "LOBBY", FName, Color.FromArgb(20, 24, 28), new RectangleF(lobby.X, lobby.Y, lobby.Width, lobby.Height), StringAlignment.Center);
            }
        }
    }
}
