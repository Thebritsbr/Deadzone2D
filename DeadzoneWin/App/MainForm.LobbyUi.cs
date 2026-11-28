// MainForm.LobbyUi.cs — Lobby reorganizado (usa DrawLobbyV2 no lugar do DrawLobby antigo).
//   • botões alinhados e centralizados com o START (antes "Create Group" + "FIND MATCH" ficavam deslocados para a direita)
//   • chip de Dead Coins alinhado verticalmente com o seletor de mapa
//   • aviso de atualização no rodapé (antes ele ficava em cima dos botões 1V1/2V2)
//   • cartão do jogador + estatísticas preenchem o espaço vazio quando não há grupo
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Deadzone2D
{
    public partial class MainForm
    {
        private void DrawLobbyV2(Graphics g, int w, int h)
        {
            bool isRoyale = _map.Mode == "DeadRoyale";
            bool isDeal = _map.Mode == "DealDuo";
            int cx = w / 2;
            int y0 = BarH + 22;

            // saldo de Dead Coins (alinhado ao centro vertical do seletor)
            DrawCoinsChip(g, new Rectangle(w - 48 - 238, y0 + 9, 238, 46), "nav:DeadRoyal");

            // ---------- seletor de mapa/modo ----------
            var mode = new Rectangle(cx - 230, y0, 460, 64);
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
            Spaced(g, $"MAP {_mapIndex + 1}/{_maps.Count}", FTiny, Red, mode.X + 108, mode.Y + 15, 3f);
            Txt(g, _map.Name.ToUpperInvariant(), VName, Ink, new RectangleF(mode.X + 108, mode.Y + 20, 290, 28));
            Txt(g, _map.Type, FSmall, Dim, new RectangleF(mode.X + 108, mode.Y + 45, 290, 16));
            for (int i = 0; i < _maps.Count; i++)
                g.FillEllipse(Br(i == _mapIndex ? Red : A(Line, 220)), mode.Right - 56 - i * 12, mode.Y + 8, 6, 6);

            // ---------- tamanho de equipe ----------
            int under;
            if (isRoyale || isDeal)
            {
                string[] labels = isRoyale ? new[] { "SOLO", "DUO", "TRIO", "SQUAD" } : new[] { "1V1", "2V2" };
                int bw = 96, gap2 = 8;
                int total = labels.Length * bw + (labels.Length - 1) * gap2;
                int sx = cx - total / 2, sy = mode.Bottom + 12;
                for (int i = 0; i < labels.Length; i++)
                {
                    bool active = isRoyale ? _royaleSquad == i + 1 : _teamSize == i + 1;
                    SizeBtn(g, new Rectangle(sx + i * (bw + gap2), sy, bw, 34), "size" + (i + 1), labels[i], active);
                }
                under = sy + 34;
            }
            else
            {
                Spaced(g, "SQUAD 4V4", FLabel, Dim, cx, mode.Bottom + 26, 3f, StringAlignment.Center);
                under = mode.Bottom + 40;
            }

            // ---------- START ----------
            var btn = new Rectangle(cx - 180, under + 20, 360, 72);
            Button(g, btn, "start", "START", true, VStart, 8f);

            // ---------- criar grupo / achar partida (par centralizado, mesma largura do START) ----------
            int py = btn.Bottom + 12;
            string squadId = _squadOpen ? "squadLeave" : "squadCreate";
            string squadLabel = _squadOpen ? "(-) Leave Group" : "(+) Create Group";
            Button(g, new Rectangle(cx - 180, py, 174, 38), squadId, squadLabel, false, FTiny, 1.8f);
            Button(g, new Rectangle(cx + 6, py, 174, 38), "matchFind",
                _queueing ? "CANCEL SEARCH" : "FIND MATCH", true, FTiny, 1.8f);

            // ---------- grupo ou cartão do jogador ----------
            if (_squadOpen) DrawSquadRow(g, w, py + 38 + 8);
            else LbPlayerCard(g, cx, py + 38 + 30, h);

            // ---------- aviso de atualização (rodapé, não cobre mais os botões) ----------
            if (!string.IsNullOrEmpty(_updateNotice))
            {
                var nr = new Rectangle(cx - 230, h - 96, 460, 44);
                using (var np = Chamfer(nr, 8, true, false, true, false))
                {
                    g.FillPath(Br(A(Red, 35)), np);
                    g.DrawPath(Pn(Red, 1.2f), np);
                }
                Txt(g, _updateNotice, FTiny, Ink, new RectangleF(nr.X + 14, nr.Y, nr.Width - 140, nr.Height));
                Button(g, new Rectangle(nr.Right - 122, nr.Y + 6, 114, 32), "viewUpd", "UPDATE", true, FTiny, 1.5f);
            }
        }

        // cartão do jogador (clique abre o perfil) + 3 estatísticas
        private void LbPlayerCard(Graphics g, int cx, int top, int h)
        {
            var card = new Rectangle(cx - 230, top, 460, 132);
            if (card.Bottom > h - 24) return;                       // janela baixa demais: não desenha
            Reg(card, "accountOpen");
            float hv = Hv("accountOpen");

            using (var p = Chamfer(card, 14, true, false, true, false))
            {
                g.FillPath(Br(Mix(A(Panel, 235), A(PanelHi, 245), hv * .5f)), p);
                g.DrawPath(Pn(Mix(Line, Red, hv * .8f), 1f), p);
            }
            g.FillRectangle(Br(Red), card.X + 14, card.Y, 90, 3);

            string nick = (_account?.Nick ?? "GUEST").ToUpperInvariant();
            string display = _account?.DisplayName ?? "Guest session";
            DrawAccountIcon(g, new RectangleF(card.X + 24, card.Y + 24, 84, 84), AccountStore.Icon(_account?.IconId));

            int tx = card.X + 132;
            Spaced(g, "YOUR PROFILE", FTiny, Red, tx, card.Y + 26, 3f);
            Txt(g, nick, VName, Ink, new RectangleF(tx, card.Y + 38, card.Width - 150, 32));
            Txt(g, display, FSmall, Dim, new RectangleF(tx, card.Y + 70, card.Width - 150, 18));
            g.FillEllipse(Br(_online ? Blue : (_account == null ? OffC : Green)), tx, card.Y + 100, 8, 8);
            Spaced(g, _online ? "ONLINE" : (_account == null ? "OFFLINE" : "LOCAL"), FTiny,
                _online ? Blue : (_account == null ? Dim : Green), tx + 16, card.Y + 104, 2f);

            // estatísticas (3 blocos que somam a largura do cartão)
            int tw = 148, gap = 8, ty = card.Bottom + 12;
            if (ty + 56 > h - 24) return;
            string level = _account == null ? "-" : _account.Level.ToString();
            string region = _account?.Region ?? "-";
            string age = _account == null ? "-" : AccountStore.AgeText(_account.CreatedAt);
            LbStatTile(g, new Rectangle(card.X, ty, tw, 56), "LEVEL", level);
            LbStatTile(g, new Rectangle(card.X + tw + gap, ty, tw, 56), "REGION", region);
            LbStatTile(g, new Rectangle(card.X + 2 * (tw + gap), ty, tw, 56), "ACCOUNT AGE", age);
        }

        private void LbStatTile(Graphics g, Rectangle r, string label, string value)
        {
            using (var p = Chamfer(r, 8, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 225)), p);
                g.DrawPath(Pn(A(Line, 200), 1f), p);
            }
            Spaced(g, label, FTiny, Dim, r.X + 14, r.Y + 16, 2.5f);
            Txt(g, value, FMid, Ink, new RectangleF(r.X + 14, r.Y + 26, r.Width - 24, 24));
        }
    }
}
