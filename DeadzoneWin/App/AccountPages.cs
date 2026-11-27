// AccountPages.cs — telas de conta, perfil, Cofre, announcements, Dead Royal, notificações e painel admin.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deadzone2D
{
    public partial class MainForm
    {
        private static readonly Font FMono = new Font("Consolas", 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font FMonoS = new Font("Consolas", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font FMonoB = new Font("Consolas", 15f, FontStyle.Bold, GraphicsUnit.Pixel);

        // ---------- Entrada de texto das novas telas ----------
        private void InitAccountControls()
        {
            if (_accountInputsReady) return;
            _accountInputsReady = true;

            var tf = new Font(Fam, 16f, FontStyle.Regular, GraphicsUnit.Pixel);
            var all = new[]
            {
                _tbLoginCode,
                _tbRegGmail, _tbRegNick, _tbRegDisplay, _tbRegPass, _tbRegPass2, _tbRegCode, _tbRegDesc,
                _tbDisplay, _tbDesc, _tbFriendSearch, _tbAdminUser, _tbAdminPass, _tbAdminCmd,
                _tbNotifyMsg, _tbAnnTitle, _tbAnnBody, _tbAnnFile, _tbAnnAuthor,
                _tbGiveTarget, _tbGiveQuery, _tbGiveCoins, _tbGiveLevel, _tbGiveWins, _tbGiveKills, _tbAdminGmail,
                _tbChat, _tbServerUrl
            };
            foreach (var tb in all)
            {
                tb.Parent = this;
                tb.BorderStyle = BorderStyle.None;
                tb.BackColor = FieldBg;
                tb.ForeColor = Ink;
                tb.Font = tf;
                tb.TextAlign = HorizontalAlignment.Left;
                tb.Visible = false;
            }

            _tbRegPass.UseSystemPasswordChar = true;
            _tbRegPass2.UseSystemPasswordChar = true;
            _tbAdminPass.UseSystemPasswordChar = true;
            _tbNotifyMsg.Multiline = true;
            _tbAnnBody.Multiline = true;
            _tbRegCode.MaxLength = 6;
            _tbLoginCode.MaxLength = 6;
            _tbRegDesc.MaxLength = 140;
            _tbDesc.MaxLength = 140;
            _tbAdminCmd.MaxLength = 600;
            _tbNotifyMsg.MaxLength = 200;
            _tbAnnTitle.MaxLength = 80;
            _tbAnnBody.MaxLength = 600;
            _tbAnnFile.MaxLength = 260;
            _tbAnnAuthor.MaxLength = 30;
            _tbChat.MaxLength = 160;
            _tbServerUrl.Text = OnlineClient.Config.ServerUrl;
            _tbAnnAuthor.Text = "sr.idev";
            _tbAdminUser.Text = "sr.idev";

            _tbFriendSearch.TextChanged += (s, e) =>
            {
                SetFriendResults(_account == null
                    ? new List<Account>()
                    : AccountStore.SearchAccounts(_tbFriendSearch.Text, _account.UserId));
            };
            _tbAdminCmd.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    string cmd = _tbAdminCmd.Text;
                    _tbAdminCmd.Clear();
                    ExecuteAdminCommand(cmd);
                }
            };
        }

        private void SyncAccountInputs()
        {
            if (!_accountInputsReady) return;

            ShowInput(_tbLoginCode, _page == 1);
            ShowInput(_tbChat, _chatOpen && (_page >= 2) && _page != 5 && _page != 6);
            ShowInput(_tbServerUrl, _cfgOpen && _page != 4);
            bool reg = _page == 6;
            ShowInput(_tbRegGmail, reg);
            ShowInput(_tbRegNick, reg);
            ShowInput(_tbRegDisplay, reg);
            ShowInput(_tbRegPass, reg);
            ShowInput(_tbRegPass2, reg);
            ShowInput(_tbRegCode, reg);
            ShowInput(_tbRegDesc, reg);

            bool profile = _profileOpen && _page >= 2 && _page != 4;
            ShowInput(_tbDisplay, profile);
            ShowInput(_tbDesc, profile);

            bool add = _addFriendOpen && _page >= 2 && _page != 4;
            ShowInput(_tbFriendSearch, add);

            bool adminAuth = _adminOpen && !_adminAuthed && _page != 4;
            ShowInput(_tbAdminUser, adminAuth);
            ShowInput(_tbAdminPass, adminAuth);
            bool adminCmd = _adminOpen && _adminAuthed && _page != 4 && !_notifyUiOpen && !_announceUiOpen && !_giveOpen && !_adminGmailOpen;
            ShowInput(_tbAdminCmd, adminCmd);
            ShowInput(_tbNotifyMsg, _adminOpen && _adminAuthed && _notifyUiOpen && _page != 4);
            ShowInput(_tbAnnTitle, _adminOpen && _adminAuthed && _announceUiOpen && _page != 4);
            ShowInput(_tbAnnBody, _adminOpen && _adminAuthed && _announceUiOpen && _page != 4);
            ShowInput(_tbAnnFile, _adminOpen && _adminAuthed && _announceUiOpen && _page != 4);
            ShowInput(_tbAnnAuthor, _adminOpen && _adminAuthed && _announceUiOpen && _page != 4);
            bool give = _adminOpen && _adminAuthed && _giveOpen && _page != 4;
            ShowInput(_tbGiveTarget, give);
            ShowInput(_tbGiveQuery, give && _giveTab == 0);
            ShowInput(_tbGiveCoins, give && _giveTab == 1);
            ShowInput(_tbGiveLevel, give && _giveTab == 1);
            ShowInput(_tbGiveWins, give && _giveTab == 1);
            ShowInput(_tbGiveKills, give && _giveTab == 1);
            ShowInput(_tbAdminGmail, _adminOpen && _adminAuthed && _adminGmailOpen && _page != 4);
        }

        private static void ShowInput(TextBox tb, bool visible)
        {
            if (tb.Visible != visible) tb.Visible = visible;
        }

        // ---------- Conta / perfil ----------
        private void OpenProfile()
        {
            if (_account == null) return;
            _profileOpen = true;
            _profileError = "";
            _tbDisplay.Text = _account.DisplayName;
            _tbDesc.Text = _account.Description;
        }

        private void CloseProfile()
        {
            _profileOpen = false;
            _profileError = "";
            _tbDisplay.Text = "";
            _tbDesc.Text = "";
        }

        private void SaveProfile()
        {
            if (_account == null) return;
            _profileError = "";
            if (!string.Equals(_tbDisplay.Text.Trim(), _account.DisplayName, StringComparison.Ordinal))
            {
                if (!AccountStore.ChangeDisplayName(_account, _tbDisplay.Text, out var err))
                {
                    _profileError = err;
                    return;
                }
            }
            AccountStore.SetDescription(_account, _tbDesc.Text);
            ShowToast("Profile atualizado.");
            _tbDisplay.Text = _account.DisplayName;
        }

        private void DrawAccountIcon(Graphics g, RectangleF r, ProfileIcon icon, bool locked = false)
        {
            var col = locked ? Mix(icon.Color, Panel, .7f) : icon.Color;
            using (var p = RoundRect(r, r.Width * .5f))
            {
                using (var lb = new LinearGradientBrush(r, Mix(col, Color.Black, .25f), col, 90f))
                    g.FillPath(lb, p);
                g.DrawPath(Pn(A(Color.White, locked ? 35 : 80), 1.2f), p);
            }
            if (locked)
            {
                var c = A(Ink, 140);
                var pen = Pn(c, 1.5f);
                float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
                g.DrawArc(pen, cx - 6, cy - 8, 12, 11, 180, 180);
                g.FillRectangle(Br(c), cx - 8, cy - 2, 16, 12);
            }
            else
            {
                Txt(g, icon.Glyph, FAvatar, Color.White, r, StringAlignment.Center);
            }
        }

        private void DrawBanner(Graphics g, Rectangle r, ProfileBanner b)
        {
            using var path = Chamfer(r, 14, true, false, true, false);
            var previousClip = g.Clip.Clone();
            g.SetClip(path);
            using (var lb = new LinearGradientBrush(r, b.A, b.B, 0f))
                g.FillRectangle(lb, r);
            if (b.Style == "grid")
            {
                var pen = Pn(A(Color.White, 26), 1f);
                for (int x = r.X - r.Height; x < r.Right + r.Height; x += 22)
                    g.DrawLine(pen, x, r.Bottom, x + r.Height, r.Top);
            }
            else if (b.Style == "crown")
            {
                using var wb = new SolidBrush(A(Color.White, 18));
                for (int i = 0; i < 4; i++)
                    g.FillPolygon(wb, new[]
                    {
                        new PointF(r.X + r.Width * .55f + i * 54, r.Bottom),
                        new PointF(r.X + r.Width * .72f + i * 54, r.Top),
                        new PointF(r.X + r.Width * .80f + i * 54, r.Top),
                        new PointF(r.X + r.Width * .63f + i * 54, r.Bottom)
                    });
            }
            else
            {
                using var wb = new SolidBrush(A(Color.White, 14));
                for (int i = -2; i < 8; i++)
                    g.FillPolygon(wb, new[]
                    {
                        new PointF(r.X + i * 80, r.Bottom),
                        new PointF(r.X + i * 80 + 34, r.Bottom),
                        new PointF(r.X + i * 80 + 94, r.Top),
                        new PointF(r.X + i * 80 + 60, r.Top)
                    });
            }
            using (var shade = new LinearGradientBrush(r, A(Color.Black, 0), A(Color.Black, 130), 0f))
                g.FillRectangle(shade, r);
            g.Clip = previousClip;
            g.DrawPath(Pn(A(Color.White, 55), 1f), path);
        }

        private void DrawProfileCard(Graphics g, Rectangle r)
        {
            Reg(r, "accountOpen");
            float hv = Hv("accountOpen");
            using (var p = Chamfer(r, 16, true, false, true, false))
            {
                using (var lb = new LinearGradientBrush(r, A(PanelHi, 245), A(Panel, 245), 90f))
                    g.FillPath(lb, p);
                g.DrawPath(Pn(Mix(Line, Red, hv * .7f), 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 14, r.Y, r.Width - 14, 3);

            if (_account == null) return;
            var icon = AccountStore.Icon(_account.IconId);
            var banner = AccountStore.Banner(_account.BannerId);

            var ban = new Rectangle(r.X + 14, r.Y + 14, r.Width - 28, 72);
            DrawBanner(g, ban, banner);

            var av = new RectangleF(r.X + 30, r.Y + 70, 72, 72);
            g.FillEllipse(Br(Panel), av.X - 4, av.Y - 4, av.Width + 8, av.Height + 8);
            DrawAccountIcon(g, av, icon);

            Txt(g, _account.Nick.ToUpperInvariant(), VName, Ink, new RectangleF(r.X + 116, r.Y + 78, r.Width - 132, 28));
            Txt(g, _account.DisplayName, FMid, A(Ink, 210), new RectangleF(r.X + 116, r.Y + 105, r.Width - 132, 20));
            g.FillEllipse(Br(Green), r.X + 118, r.Y + 131, 8, 8);
            Spaced(g, "ONLINE • " + _account.Region.ToUpperInvariant(), FTiny, Green, r.X + 132, r.Y + 135, 2f);

            int y = r.Y + 168;
            g.DrawLine(Pn(A(Line, 170), 1f), r.X + 18, y, r.Right - 18, y);
            y += 18;
            ProfileLine(g, r.X + 18, y, "USERID", _account.UserId);
            y += 34;
            ProfileLine(g, r.X + 18, y, "ACCOUNT CREATED", AccountStore.CreationText(_account));
            y += 34;
            ProfileLine(g, r.X + 18, y, "IDADE", AccountStore.AgeText(_account.CreatedAt));
            y += 34;
            Txt(g, "DESCRIPTION", FTiny, Dim, new RectangleF(r.X + 18, y, r.Width - 36, 16));
            g.DrawString(string.IsNullOrWhiteSpace(_account.Description) ? "No description." : _account.Description,
                FSmall, Br(A(Ink, 180)), new RectangleF(r.X + 18, y + 16, r.Width - 36, 48), SfWrap);

            int by = r.Bottom - 54;
            Button(g, new Rectangle(r.X + 18, by, r.Width - 36, 40), "accountOpen", "CONFIGURE ACCOUNT", false, FLabel, 2.5f);
        }

        private void ProfileLine(Graphics g, int x, int y, string label, string value)
        {
            Spaced(g, label, FTiny, Dim, x, y + 6, 2f);
            Txt(g, value, FSmallB, Ink, new RectangleF(x + 100, y, 180, 18), StringAlignment.Far);
        }

        private void DrawProfilePanel(Graphics g, int w, int h)
        {
            if (!_profileOpen || _account == null) return;
            g.FillRectangle(Br(A(Color.Black, 165)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "profileBackdrop");

            int pw = Math.Min(920, w - 60), ph = Math.Min(630, h - BarH - 28);
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 12, (h - ph) / 2), pw, ph);
            Reg(r, "profilePanel");
            using (var p = Chamfer(r, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 254)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 18, r.Y, r.Width - 18, 3);
            Spaced(g, "ACCOUNT", FLabel, Red, r.X + 34, r.Y + 30, 4f);
            Txt(g, "PLAYER PROFILE", VSub, Ink, new RectangleF(r.X + 34, r.Y + 44, r.Width - 68, 46));
            var close = new Rectangle(r.Right - 48, r.Y + 18, 30, 30);
            Reg(close, "profileClose");
            var xp = Pn(Mix(Dim, Ink, Hv("profileClose")), 1.8f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            int lx = r.X + 34, lw = (int)(pw * .45f);
            var ban = new Rectangle(lx, r.Y + 104, lw, 86);
            DrawBanner(g, ban, AccountStore.Banner(_account.BannerId));
            var icon = AccountStore.Icon(_account.IconId);
            DrawAccountIcon(g, new RectangleF(lx + 18, r.Y + 164, 80, 80), icon);
            Txt(g, _account.Nick.ToUpperInvariant(), VName, Ink, new RectangleF(lx + 114, r.Y + 170, lw - 120, 30));
            Txt(g, _account.DisplayName, FMid, A(Ink, 210), new RectangleF(lx + 114, r.Y + 200, lw - 120, 20));
            g.FillEllipse(Br(Green), lx + 116, r.Y + 227, 8, 8);
            Spaced(g, "ONLINE", FTiny, Green, lx + 130, r.Y + 231, 2f);

            int iy = r.Y + 272;
            InfoLine(g, lx, iy, lw, "USERID", _account.UserId); iy += 30;
            InfoLine(g, lx, iy, lw, "GMAIL", _account.Gmail); iy += 30;
            InfoLine(g, lx, iy, lw, "REGION", _account.Region); iy += 30;
            InfoLine(g, lx, iy, lw, "ACCOUNT CREATED", AccountStore.CreationText(_account)); iy += 30;
            InfoLine(g, lx, iy, lw, "ACCOUNT AGE", AccountStore.AgeText(_account.CreatedAt)); iy += 30;
            InfoLine(g, lx, iy, lw, "NICK (LOCKED)", _account.Nick); iy += 30;
            InfoLine(g, lx, iy, lw, "DEAD COINS", _account.DeadCoins.ToString("N0")); iy += 30;
            var rank = RankStore.ForXp(_account.Xp);
            var rankColor = RankStore.Parse(rank.Color);
            Spaced(g, "RANK", FTiny, Dim, lx, iy + 6, 2f);
            Txt(g, rank.Name.ToUpperInvariant(), FSmallB, rankColor,
                new RectangleF(lx + 100, iy, lw - 100, 18), StringAlignment.Far);
            var rbar = new Rectangle(lx, iy + 24, lw, 7);
            Reg(Rectangle.Inflate(rbar, 0, 9), "rankInfo");
            g.FillRectangle(Br(A(Ink, 30)), rbar);
            g.FillRectangle(Br(rankColor), rbar.X, rbar.Y, (int)(rbar.Width * RankStore.Progress(_account.Xp)), rbar.Height);
            Txt(g, _account.Xp + " XP  •  " + _account.Wins + " wins", FTiny, A(Dim, 200),
                new RectangleF(lx, iy + 34, lw, 16));
            iy += 56;
            Spaced(g, "TAGS", FTiny, Dim, lx, iy + 6, 2f);
            Txt(g, _account.Tags.Count == 0 ? "Nenhuma" : string.Join("  •  ", _account.Tags), FSmallB, Amber,
                new RectangleF(lx + 100, iy, lw - 100, 18), StringAlignment.Far);
            iy += 34;

            int rx = lx + lw + 28, rw = r.Right - 34 - rx;
            Spaced(g, "EDITAR PROFILE", FLabel, Red, rx, r.Y + 112, 3f);
            var df = new Rectangle(rx, r.Y + 126, rw, 56);
            UiField(g, df, "DISPLAYNAME", _tbDisplay);
            string hint = _account.DisplayNameChanges == 0
                ? "Free DisplayName change available."
                : "Next DisplayName change: 500 Dead Coins.";
            Txt(g, hint, FTiny, _account.DisplayNameChanges == 0 ? Green : Amber,
                new RectangleF(rx, df.Bottom + 6, rw, 16));

            var ef = new Rectangle(rx, df.Bottom + 32, rw, 56);
            UiField(g, ef, "DESCRIPTION", _tbDesc);
            Txt(g, "140 characters maximum.", FTiny, A(Dim, 170),
                new RectangleF(rx, ef.Bottom + 6, rw, 16));

            Button(g, new Rectangle(rx, ef.Bottom + 34, rw, 46), "profileSave", "SAVE CHANGES", true, FLabel, 2.5f);
            if (!string.IsNullOrEmpty(_profileError))
                Txt(g, _profileError, FSmall, HpRed, new RectangleF(rx, ef.Bottom + 88, rw, 20));

            int by = r.Bottom - 58;
            Button(g, new Rectangle(rx, by, 126, 42), "profileLogout", "LOG OUT", false, FTiny, 2f);
            Button(g, new Rectangle(rx + 136, by, 108, 42), "nav:Cofre", "VAULT", false, FTiny, 2f);
            if (AccountStore.IsAdminAllowed(_account))
                Button(g, new Rectangle(rx + 254, by, 92, 42), "adminOpen", "ADMIN", false, FTiny, 2f);
            Button(g, new Rectangle(rx + rw - 94, by, 94, 42), "profileClose", "CLOSE", false, FTiny, 2f);
        }

        private void InfoLine(Graphics g, int x, int y, int w, string label, string value)
        {
            Spaced(g, label, FTiny, Dim, x, y + 7, 2f);
            Txt(g, value, FSmallB, Ink, new RectangleF(x + 150, y, w - 150, 18), StringAlignment.Far);
            g.DrawLine(Pn(A(Line, 120), 1f), x, y + 21, x + w, y + 21);
        }

        private void DrawRankInfo(Graphics g, int w, int h)
        {
            if (!_rankInfoOpen || _account == null) return;
            g.FillRectangle(Br(A(Color.Black, 160)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "rankInfoBackdrop");
            int pw = 520, ph = 380;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 16, (h - ph) / 2), pw, ph);
            Reg(r, "rankInfoPanel");
            using (var p = Chamfer(r, 16, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            var rank = RankStore.ForXp(_account.Xp);
            var col = RankStore.Parse(rank.Color);
            g.FillRectangle(Br(col), r.X + 18, r.Y, r.Width - 18, 3);
            Spaced(g, "RANK", FLabel, col, r.X + 32, r.Y + 28, 4f);
            Txt(g, rank.Name.ToUpperInvariant(), VSub, Ink, new RectangleF(r.X + 32, r.Y + 42, pw - 90, 48));
            var close = new Rectangle(r.Right - 48, r.Y + 18, 30, 30);
            Reg(close, "rankInfoClose");
            var xp = Pn(Mix(Dim, Ink, Hv("rankInfoClose")), 1.8f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            InfoLine(g, r.X + 32, r.Y + 110, pw - 64, "LEVEL", rank.Level.ToString());
            InfoLine(g, r.X + 32, r.Y + 150, pw - 64, "XP ATUAL", _account.Xp.ToString("N0"));
            var next = RankStore.Next(_account.Xp);
            InfoLine(g, r.X + 32, r.Y + 190, pw - 64, "NEXT RANK", next == null ? "MAX" : next.Name + " • " + next.Xp.ToString("N0") + " XP");
            InfoLine(g, r.X + 32, r.Y + 230, pw - 64, "WINS / KILLS", _account.Wins + " / " + _account.Kills);
            var bar = new Rectangle(r.X + 32, r.Y + 282, pw - 64, 10);
            g.FillRectangle(Br(A(Ink, 30)), bar);
            g.FillRectangle(Br(col), bar.X, bar.Y, (int)(bar.Width * RankStore.Progress(_account.Xp)), bar.Height);
            Txt(g, rank.Description, FSmall, A(Ink, 190), new RectangleF(r.X + 32, r.Y + 306, pw - 64, 38));
        }

        // ---------- Registro ----------
        private void DrawRegister(Graphics g, int w, int h)
        {
            int cw = Math.Min(880, w - 80);
            int ch = Math.Min(636, h - BarH - 36);
            var card = new Rectangle((w - cw) / 2, BarH + 18, cw, ch);
            using (var p = Chamfer(card, 18, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 246)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), card.X + 18, card.Y, card.Width - 18, 3);

            int x = card.X + 34, y = card.Y + 24;
            Spaced(g, "CREATE LOCAL ACCOUNT", FLabel, Red, x, y, 4f);
            Txt(g, "NEW PLAYER", VSub, Ink, new RectangleF(x, y + 12, cw - 68, 44));
            Txt(g, "Use your real Google account or create a local account on this PC.",
                FSmall, Dim, new RectangleF(x, y + 54, cw - 260, 20));
            Button(g, new Rectangle(card.Right - 34 - 180, card.Y + 22, 180, 40),
                "googleSignIn", "USAR GOOGLE", false, FLabel, 2.5f);

            int fy = card.Y + 100;
            var gmailF = new Rectangle(card.X + 34, fy, cw - 68, 50);
            UiField(g, gmailF, "GMAIL REAL (@GMAIL.COM)", _tbRegGmail);

            int half = (cw - 68 - 16) / 2;
            var nickF = new Rectangle(card.X + 34, fy + 54, half, 50);
            var dispF = new Rectangle(card.X + 34 + half + 16, fy + 54, half, 50);
            UiField(g, nickF, "NICK (PERMANENTE)", _tbRegNick);
            UiField(g, dispF, "DISPLAYNAME", _tbRegDisplay);

            var passF = new Rectangle(card.X + 34, fy + 108, half, 50);
            var pass2F = new Rectangle(card.X + 34 + half + 16, fy + 108, half, 50);
            UiField(g, passF, "PASSWORD", _tbRegPass);
            UiField(g, pass2F, "CONFIRM PASSWORD", _tbRegPass2);

            var codeF = new Rectangle(card.X + 34, fy + 162, half, 44);
            UiField(g, codeF, "CODE SENT TO GMAIL", _tbRegCode);
            Button(g, new Rectangle(card.X + 34 + half + 16, fy + 162, half, 44),
                "registerSendCode", "SEND CODE", false, FLabel, 2.5f);
            if (!string.IsNullOrEmpty(_regCodeHint))
                Txt(g, _regCodeHint, FTiny, MailService.IsConfigured ? Green : Amber,
                    new RectangleF(card.X + 34, fy + 208, cw - 68, 16));

            var descF = new Rectangle(card.X + 34, fy + 228, cw - 68, 44);
            UiField(g, descF, "DESCRIPTION (OPCIONAL)", _tbRegDesc);

            Spaced(g, "CHOOSE YOUR ICON", FLabel, Dim, x, fy + 280, 3f);
            int iconSize = 48, iconGap = 10;
            int totalIcons = AccountStore.Icons.Length * iconSize + (AccountStore.Icons.Length - 1) * iconGap;
            int ix = (w - totalIcons) / 2;
            for (int i = 0; i < AccountStore.Icons.Length; i++)
            {
                var icon = AccountStore.Icons[i];
                var rr = new Rectangle(ix + i * (iconSize + iconGap), fy + 292, iconSize, iconSize);
                bool sel = string.Equals(icon.Id, _regIcon, StringComparison.OrdinalIgnoreCase);
                Reg(rr, "regIcon:" + icon.Id);
                if (sel)
                {
                    using var ring = RoundRect(Rectangle.Inflate(rr, 4, 4), iconSize);
                    g.DrawPath(Pn(Red, 2f), ring);
                }
                DrawAccountIcon(g, rr, icon);
                Txt(g, icon.Name, FTiny, sel ? Ink : Dim,
                    new RectangleF(rr.X - 10, rr.Bottom + 2, rr.Width + 20, 14), StringAlignment.Center);
            }

            Spaced(g, "REGION", FLabel, Dim, x, fy + 360, 3f);
            string[] regionLabels = { "BRAZIL", "USA", "SPAIN", "PORTUGAL", "MEXICO", "ARGENTINA" };
            int rbw = 96, rgap = 8;
            int totalR = regionLabels.Length * rbw + (regionLabels.Length - 1) * rgap;
            int rx = (w - totalR) / 2;
            for (int i = 0; i < regionLabels.Length; i++)
            {
                string reg = AccountStore.Regions[i];
                SizeBtn(g, new Rectangle(rx + i * (rbw + rgap), fy + 372, rbw, 26), "regRegion:" + reg,
                    regionLabels[i], string.Equals(reg, _regRegion, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(_regError))
                Txt(g, _regError, FSmall, HpRed, new RectangleF(card.X + 34, fy + 406, cw - 68, 20));

            Button(g, new Rectangle(card.X + 34, card.Bottom - 58, 150, 42), "registerBack", "BACK", false, FLabel, 2.5f);
            Button(g, new Rectangle(card.Right - 34 - 210, card.Bottom - 58, 210, 42), "registerCreate", "CREATE ACCOUNT", true, FLabel, 3f);
        }

        // ---------- Anúncios ----------
        private void DrawAnnouncements(Graphics g, int w, int h)
        {
            int listW = Math.Min(980, w - 320);
            int x = 250 + (w - 250 - listW) / 2;
            int y = BarH + 28;

            Spaced(g, "BOARD", FLabel, Red, x, y, 4f);
            Txt(g, "ANNOUNCEMENTS", VSub, Ink, new RectangleF(x, y + 14, listW - 200, 48));
            Txt(g, "Updates, news and events published by the team.", FSmall, Dim,
                new RectangleF(x, y + 56, listW - 200, 20));
            if (_account != null && AccountStore.IsAdminAllowed(_account))
                Button(g, new Rectangle(x + listW - 190 - 48, y + 10, 190, 42), "adminOpen", "PAINEL ADMIN", false, FLabel, 2.5f);
            DrawPageClose(g, new Rectangle(x + listW - 36, y + 14, 36, 36), "annClose");

            string[] cats = { "Updates", "News", "Events", "Patch Notes" };
            string[] labels = { "UPDATES", "NEWS", "EVENTS", "NOTES" };
            int bw = 148, gap = 8;
            int ty = y + 88;
            for (int i = 0; i < cats.Length; i++)
            {
                bool active = string.Equals(_annCategory, cats[i], StringComparison.OrdinalIgnoreCase);
                SizeBtn(g, new Rectangle(x + i * (bw + gap), ty, bw, 30), "annCat:" + cats[i], labels[i], active);
            }

            var items = AccountStore.Announcements
                .Where(a => string.Equals(a.Category, _annCategory, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            var view = new Rectangle(x, ty + 44, listW, h - (ty + 44) - 140);
            int cardH = 152, cardGap = 14;
            int total = Math.Max(0, items.Count * (cardH + cardGap) - cardGap);
            float max = Math.Max(0, total - view.Height);
            _annScroll = Math.Clamp(_annScroll, 0f, max);

            g.SetClip(view);
            if (items.Count == 0)
            {
                Spaced(g, "NO ANNOUNCEMENTS HERE", FLabel, Dim, view.X + view.Width / 2f,
                    view.Y + view.Height / 2f - 8, 3f, StringAlignment.Center);
            }
            int cy = view.Y - (int)_annScroll;
            for (int i = 0; i < items.Count; i++)
            {
                var a = items[i];
                var r = new Rectangle(x, cy, listW, cardH);
                if (r.Bottom > view.Top && r.Top < view.Bottom) DrawAnnouncementCard(g, r, a, i);
                cy += cardH + cardGap;
            }
            g.ResetClip();

            if (max > 0)
            {
                float th = Math.Max(30f, view.Height * (view.Height / (float)total));
                float tyy = view.Y + (view.Height - th) * (_annScroll / max);
                g.FillRectangle(Br(A(Ink, 40)), view.Right - 6, tyy, 3, th);
            }
        }

        private void DrawAnnouncementCard(Graphics g, Rectangle r, Announcement a, int idx)
        {
            using (var p = Chamfer(r, 12, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 235)), p);
                g.DrawPath(Pn(A(Line, 210), 1f), p);
            }
            var col = a.Category switch
            {
                "News" => Blue,
                "Events" => IconPurple,
                "Patch Notes" => Amber,
                _ => Red
            };
            g.FillRectangle(Br(col), r.X + 14, r.Y, 4, r.Height - 14);

            Spaced(g, a.Category.ToUpperInvariant(), FTiny, col, r.X + 30, r.Y + 20, 2.5f);
            Txt(g, a.Title, VName, Ink, new RectangleF(r.X + 30, r.Y + 32, r.Width - 230, 30));
            Spaced(g, $"{a.Author.ToUpperInvariant()}  •  {a.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}",
                FTiny, Dim, r.X + 30, r.Y + 64, 1.5f);

            bool hasImage = !string.IsNullOrEmpty(a.Attachment) && File.Exists(a.Attachment) && IsImage(a.Attachment);
            int bodyW = r.Width - (hasImage ? 250 : 60);
            g.DrawString(string.IsNullOrWhiteSpace(a.Body) ? "(no content)" : a.Body, FSmall, Br(A(Ink, 200)),
                new RectangleF(r.X + 30, r.Y + 82, bodyW, r.Height - 94), SfWrap);

            if (hasImage)
            {
                var ir = new Rectangle(r.Right - 196, r.Y + 20, 168, 112);
                var img = LoadImage(a.Attachment!);
                if (img != null)
                {
                    float k = Math.Min(ir.Width / (float)img.Width, ir.Height / (float)img.Height);
                    float dw = img.Width * k, dh = img.Height * k;
                    var previousClip = g.Clip.Clone();
                    using (var p = RoundRect(ir, 8)) g.SetClip(p);
                    g.DrawImage(img, ir.X + (ir.Width - dw) / 2f, ir.Y + (ir.Height - dh) / 2f, dw, dh);
                    g.Clip = previousClip;
                }
                Reg(ir, "annFile:" + idx);
            }
            else if (!string.IsNullOrEmpty(a.AttachmentName))
            {
                var fr = new Rectangle(r.Right - 220, r.Y + 84, 192, 40);
                Reg(fr, "annFile:" + idx);
                using (var p = Chamfer(fr, 7, true, false, true, false))
                {
                    g.FillPath(Br(A(Ink, 12)), p);
                    g.DrawPath(Pn(A(Line, 200), 1f), p);
                }
                Txt(g, "ANEXO: " + a.AttachmentName, FTiny, Ink,
                    new RectangleF(fr.X + 10, fr.Y, fr.Width - 20, fr.Height));
            }
        }

        private static bool IsImage(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp";
        }

        private List<Announcement> CurrentAnnouncements() =>
            AccountStore.Announcements
                .Where(a => string.Equals(a.Category, _annCategory, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

        private void OpenAnnouncementFile(int idx)
        {
            var list = CurrentAnnouncements();
            if (idx < 0 || idx >= list.Count) return;
            var path = list[idx].Attachment;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                ShowToast("Attachment not found.");
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { ShowToast("Could not open the attachment."); }
        }

        private Image? LoadImage(string path)
        {
            if (_imgs.TryGetValue(path, out var cached)) return cached;
            Image? img = null;
            try
            {
                using var fs = File.OpenRead(path);
                using var tmp = Image.FromStream(fs);
                img = new Bitmap(tmp);
            }
            catch { img = null; }
            _imgs[path] = img;
            return img;
        }

        // ---------- Cofre ----------
        private void DrawCofre(Graphics g, int w, int h)
        {
            int x = 250, y = BarH + 28;
            Spaced(g, "LOBBY INVENTORY", FLabel, Red, x, y, 4f);
            Txt(g, "VAULT", VSub, Ink, new RectangleF(x, y + 14, 300, 48));
            Txt(g, "Banners, icons and profile customization.", FSmall, Dim,
                new RectangleF(x, y + 56, 500, 20));
            DrawPageClose(g, new Rectangle(w - 48 - 36, y + 8, 36, 36), "cofreClose");
            if (_account != null)
                DrawCoinsChip(g, new Rectangle(w - 48 - 36 - 12 - 238, y + 8, 238, 46), "nav:DeadRoyal");

            string[] tabs = { "Profile", "Banners", "Icons" };
            string[] labels = { "PROFILE", "BANNERS", "ICONS" };
            int ty = y + 90;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = string.Equals(_cofreTab, tabs[i], StringComparison.OrdinalIgnoreCase);
                SizeBtn(g, new Rectangle(x + i * 156, ty, 148, 32), "cofreTab:" + tabs[i], labels[i], active);
            }
            g.DrawLine(Pn(A(Line, 180), 1f), x, ty + 40, w - 48, ty + 40);

            if (_account == null) return;
            int cy = ty + 58;
            if (_cofreTab.Equals("Banners", StringComparison.OrdinalIgnoreCase)) DrawCofreBanners(g, x, cy, w - x - 48, h);
            else if (_cofreTab.Equals("Icons", StringComparison.OrdinalIgnoreCase)) DrawCofreIcons(g, x, cy, w - x - 48, h);
            else DrawCofreProfile(g, x, cy, w - x - 48, h);
        }

        private void DrawCofreProfile(Graphics g, int x, int y, int w, int h)
        {
            var acc = _account!;
            var ban = new Rectangle(x, y, w, 140);
            DrawBanner(g, ban, AccountStore.Banner(acc.BannerId));
            Spaced(g, "BANNER EQUIPADO", FTiny, Color.White, x + 24, y + 24, 3f);
            Txt(g, AccountStore.Banner(acc.BannerId).Name, VName, Color.White, new RectangleF(x + 24, y + 34, 300, 30));

            var icon = AccountStore.Icon(acc.IconId);
            DrawAccountIcon(g, new RectangleF(x + 24, y + 152, 92, 92), icon);
            Txt(g, acc.Nick.ToUpperInvariant(), VSub, Ink, new RectangleF(x + 134, y + 156, 420, 46));
            Txt(g, acc.DisplayName, FMid, A(Ink, 210), new RectangleF(x + 134, y + 194, 420, 22));
            g.FillEllipse(Br(Green), x + 136, y + 224, 8, 8);
            Spaced(g, "ONLINE • " + acc.Region.ToUpperInvariant(), FTiny, Green, x + 150, y + 228, 2f);

            int cardW = (w - 2 * 18) / 3, cardH = 64;
            int gx = x, gy = y + 266;
            CofreStat(g, new Rectangle(gx, gy, cardW, cardH), "USERID", acc.UserId, Red);
            CofreStat(g, new Rectangle(gx + cardW + 18, gy, cardW, cardH), "ACCOUNT CREATED", AccountStore.CreationText(acc), Blue);
            CofreStat(g, new Rectangle(gx + 2 * (cardW + 18), gy, cardW, cardH), "ACCOUNT AGE", AccountStore.AgeText(acc.CreatedAt), Green);
            gy += cardH + 16;
            CofreStat(g, new Rectangle(gx, gy, cardW, cardH), "DEAD COINS", acc.DeadCoins.ToString("N0"), Amber);
            CofreStat(g, new Rectangle(gx + cardW + 18, gy, cardW, cardH), "ICON", icon.Name, IconPurple);
            CofreStat(g, new Rectangle(gx + 2 * (cardW + 18), gy, cardW, cardH), "BANNER", AccountStore.Banner(acc.BannerId).Name, Ink);
        }

        private void CofreStat(Graphics g, Rectangle r, string label, string value, Color accent)
        {
            using (var p = Chamfer(r, 10, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 220)), p);
                g.DrawPath(Pn(A(Line, 180), 1f), p);
            }
            g.FillRectangle(Br(accent), r.X + 12, r.Y + 12, 3, 18);
            Spaced(g, label, FTiny, Dim, r.X + 24, r.Y + 16, 2f);
            Txt(g, value, FMid, accent, new RectangleF(r.X + 18, r.Y + 34, r.Width - 36, 24));
        }

        private void DrawCofreBanners(Graphics g, int x, int y, int w, int h)
        {
            int cols = 3, gap = 18;
            int cw = (w - (cols - 1) * gap) / cols, chh = 150;
            for (int i = 0; i < AccountStore.Banners.Length; i++)
            {
                var b = AccountStore.Banners[i];
                int col = i % cols, row = i / cols;
                var r = new Rectangle(x + col * (cw + gap), y + row * (chh + gap), cw, chh);
                if (r.Bottom > h - 20) break;
                bool eq = string.Equals(_account!.BannerId, b.Id, StringComparison.OrdinalIgnoreCase);
                bool owned = _account.OwnedBanners.Contains(b.Id, StringComparer.OrdinalIgnoreCase);
                if (!owned) continue; // só mostra itens que o jogador conquistou/comprou
                string id = eq ? "" : "equipBanner:" + b.Id;
                if (id.Length > 0) Reg(r, id);
                float hv = id.Length > 0 ? Hv(id) : 0f;
                using (var p = Chamfer(r, 12, true, false, true, false))
                {
                    g.FillPath(Br(Mix(A(Panel, 235), A(PanelHi, 245), hv)), p);
                    g.DrawPath(Pn(Mix(A(Line, 190), owned ? Red : A(Dim, 200), hv), 1f), p);
                }
                var prev = new Rectangle(r.X + 10, r.Y + 10, r.Width - 20, 80);
                DrawBanner(g, prev, b);
                if (!owned) g.FillRectangle(Br(A(Color.Black, 125)), prev);
                Txt(g, b.Name, FMid, owned ? Ink : Dim, new RectangleF(r.X + 14, r.Y + 98, r.Width - 28, 22));
                if (eq) Spaced(g, "EQUIPADO", FTiny, Green, r.X + 14, r.Y + 128, 2f);
                else Spaced(g, "EQUIP", FTiny, A(Ink, 190), r.X + 14, r.Y + 128, 2f);
            }
        }

        private void DrawCofreIcons(Graphics g, int x, int y, int w, int h)
        {
            int cols = 3, gap = 18;
            int cw = (w - (cols - 1) * gap) / cols, chh = 150;
            for (int i = 0; i < AccountStore.Icons.Length; i++)
            {
                var icon = AccountStore.Icons[i];
                int col = i % cols, row = i / cols;
                var r = new Rectangle(x + col * (cw + gap), y + row * (chh + gap), cw, chh);
                if (r.Bottom > h - 20) break;
                bool eq = string.Equals(_account!.IconId, icon.Id, StringComparison.OrdinalIgnoreCase);
                bool owned = _account.OwnedIcons.Contains(icon.Id, StringComparer.OrdinalIgnoreCase);
                if (!owned) continue; // só mostra itens que o jogador conquistou/comprou
                string id = eq ? "" : "equipIcon:" + icon.Id;
                if (id.Length > 0) Reg(r, id);
                float hv = id.Length > 0 ? Hv(id) : 0f;
                using (var p = Chamfer(r, 12, true, false, true, false))
                {
                    g.FillPath(Br(Mix(A(Panel, 235), A(PanelHi, 245), hv)), p);
                    g.DrawPath(Pn(Mix(A(Line, 190), owned ? Red : A(Dim, 200), hv), 1f), p);
                }
                DrawAccountIcon(g, new RectangleF(r.X + 22, r.Y + 18, 64, 64), icon, !owned);
                Txt(g, icon.Name, FMid, owned ? Ink : Dim, new RectangleF(r.X + 100, r.Y + 28, r.Width - 110, 24));
                if (eq) Spaced(g, "EQUIPADO", FTiny, Green, r.X + 100, r.Y + 58, 2f);
                else Spaced(g, "EQUIP", FTiny, A(Ink, 190), r.X + 100, r.Y + 58, 2f);
            }
        }

        // ---------- Dead Royal ----------
        private void DrawDeadRoyal(Graphics g, int w, int h)
        {
            int x = 250, y = BarH + 28;
            Spaced(g, "STORE", FLabel, Red, x, y, 4f);
            Txt(g, "DEAD ROYAL", VSub, Ink, new RectangleF(x, y + 12, 420, 44));
            Txt(g, "Personalize seu arsenal e seu perfil.", FSmall, Dim,
                new RectangleF(x, y + 54, 520, 18));

            // botão de fechar alinhado com o saldo no canto superior direito
            DrawPageClose(g, new Rectangle(w - 48 - 36, y + 8, 36, 36), "shopClose");
            if (_account != null)
                DrawCoinsChip(g, new Rectangle(w - 48 - 36 - 12 - 238, y + 8, 238, 46), "shopReload");

            var items = AccountStore.Shop;
            int cols = 3, gap = 14;
            int cw = (w - x - 48 - (cols - 1) * gap) / cols;
            int chh = 168;
            int gy = BarH + 116;
            var view = new Rectangle(x, gy, w - x - 48, Math.Max(80, h - gy - 136));
            int rows = (items.Count + cols - 1) / cols;
            int total = Math.Max(0, rows * (chh + gap) - gap);
            float max = Math.Max(0f, total - view.Height);
            _shopScroll = Math.Clamp(_shopScroll, 0f, max);

            g.SetClip(view);
            for (int i = 0; i < items.Count; i++)
            {
                int col = i % cols, row = i / cols;
                var r = new Rectangle(x + col * (cw + gap), gy + row * (chh + gap) - (int)_shopScroll, cw, chh);
                if (r.Bottom > view.Top && r.Top < view.Bottom) DrawShopCard(g, r, items[i], i);
            }
            g.ResetClip();

            if (max > 0)
            {
                float th = Math.Max(30f, view.Height * (view.Height / (float)total));
                float ty = view.Y + (view.Height - th) * (_shopScroll / max);
                g.FillRectangle(Br(A(Ink, 40)), view.Right - 6, ty, 3, th);
            }
        }

        private void DrawPageClose(Graphics g, Rectangle r, string id)
        {
            Reg(r, id);
            float hv = Hv(id);
            using (var p = RoundRect(r, 8))
            {
                g.FillPath(Br(Mix(A(Panel, 220), A(Red, 45), hv)), p);
                g.DrawPath(Pn(Mix(A(Line, 200), Red, hv), 1.2f), p);
            }
            var pen = Pn(Mix(Dim, Ink, hv), 1.8f);
            g.DrawLine(pen, r.X + 12, r.Y + 12, r.Right - 12, r.Bottom - 12);
            g.DrawLine(pen, r.Right - 12, r.Y + 12, r.X + 12, r.Bottom - 12);
        }

        private void DrawShopCard(Graphics g, Rectangle r, ShopItem item, int idx)
        {
            var rarity = item.Rarity switch
            {
                "Legendary" => Amber,
                "Epic" => IconPurple,
                "Rare" => Blue,
                _ => Dim
            };
            using (var p = Chamfer(r, 10, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 238)), p);
                g.DrawPath(Pn(A(Line, 210), 1f), p);
            }
            g.FillRectangle(Br(rarity), r.X, r.Y, r.Width, 3);
            Spaced(g, item.Rarity.ToUpperInvariant(), FTiny, rarity, r.X + 16, r.Y + 12, 2f);

            var art = new Rectangle(r.X + 14, r.Y + 26, r.Width - 28, 52);
            using (var p = Chamfer(art, 8, true, false, true, false))
            {
                using (var lb = new LinearGradientBrush(art, A(rarity, 26), A(Panel, 120), 90f))
                    g.FillPath(lb, p);
                g.DrawPath(Pn(A(rarity, 80), 1f), p);
            }
            DrawShopArt(g, art, item, rarity);

            Txt(g, item.Name.ToUpperInvariant(), FMid, Ink, new RectangleF(r.X + 16, r.Y + 84, r.Width - 32, 22));
            string type = item.Type == "WeaponSkin" ? "WEAPON SKIN" : item.Type == "Icon" ? "ICON" : "BANNER";
            Spaced(g, type, FTiny, Dim, r.X + 16, r.Y + 107, 2f);

            bool owned = _account != null && AccountStore.Owns(_account, item);
            bool equipped = IsShopItemEquipped(item);
            int by = r.Bottom - 38;
            if (equipped)
            {
                using var p = Chamfer(new Rectangle(r.X + 14, by, r.Width - 28, 30), 8, true, false, true, false);
                g.FillPath(Br(A(Green, 26)), p);
                g.DrawPath(Pn(Green, 1.5f), p);
                Spaced(g, "EQUIPADO", FLabel, Green, r.X + r.Width / 2f, by + 15, 3f, StringAlignment.Center);
            }
            else if (owned)
            {
                Button(g, new Rectangle(r.X + 14, by, r.Width - 28, 30), "shopEquip:" + item.Id, "EQUIP", true, FLabel, 2.5f);
            }
            else
            {
                Button(g, new Rectangle(r.X + 14, by, r.Width - 28, 30), "shopBuy:" + item.Id,
                    "BUY  •  " + item.Price.ToString("N0"), false, FLabel, 2f);
            }
        }

        private void DrawShopArt(Graphics g, Rectangle r, ShopItem item, Color rarity)
        {
            if (item.Type.Equals("WeaponSkin", StringComparison.OrdinalIgnoreCase))
            {
                var gun = _guns.FirstOrDefault(x => string.Equals(x.GunId, item.WeaponId, StringComparison.OrdinalIgnoreCase));
                var skin = gun?.Skins.FirstOrDefault(s => string.Equals(s.SkinId, item.SkinId, StringComparison.OrdinalIgnoreCase));
                if (gun != null && skin != null)
                {
                    var img = Preview(gun, skin);
                    if (img != null)
                    {
                        float k = Math.Min((r.Width - 20f) / img.Width, (r.Height - 10f) / img.Height);
                        float dw = img.Width * k, dh = img.Height * k;
                        g.DrawImage(img, r.X + (r.Width - dw) / 2f, r.Y + (r.Height - dh) / 2f, dw, dh);
                        return;
                    }
                    // silhueta de arma
                    var pen = Pn(A(rarity, 220), 5f);
                    float cy = r.Y + r.Height / 2f;
                    g.DrawLine(pen, r.X + 26, cy, r.Right - 26, cy);
                    g.DrawLine(pen, r.Right - 52, cy, r.Right - 38, cy - 15);
                    g.DrawLine(pen, r.X + 44, cy, r.X + 54, cy + 14);
                    g.DrawLine(pen, r.X + 40, cy - 6, r.X + 70, cy - 6);
                    return;
                }
            }
            else if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase))
            {
                DrawAccountIcon(g, new RectangleF(r.X + r.Width / 2f - 22, r.Y + 4, 44, 44),
                    AccountStore.Icon(item.IconId));
                return;
            }
            else if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase))
            {
                DrawBanner(g, new Rectangle(r.X + 12, r.Y + 8, r.Width - 24, 36),
                    AccountStore.Banner(item.BannerId));
                return;
            }
            Txt(g, string.IsNullOrEmpty(item.Icon) ? "✦" : item.Icon, VTitle, rarity, r, StringAlignment.Center);
        }

        private bool IsShopItemEquipped(ShopItem item)
        {
            if (_account == null) return false;
            if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase))
                return string.Equals(_account.IconId, item.IconId, StringComparison.OrdinalIgnoreCase);
            if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase))
                return string.Equals(_account.BannerId, item.BannerId, StringComparison.OrdinalIgnoreCase);
            return _equipped.Any(kv => string.Equals(AccountStore.GunId(kv.Key), AccountStore.GunId(item.WeaponId), StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(kv.Value, item.SkinId, StringComparison.OrdinalIgnoreCase));
        }

        private void BuyShopItem(string id)
        {
            if (_account == null) return;
            var item = AccountStore.Shop.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            if (AccountStore.Buy(_account, item, out var err))
            {
                ShowToast("Comprado: " + item.Name);
                SaveEquippedToAccount();
            }
            else ShowToast(err);
        }

        private void EquipShopItem(string id)
        {
            if (_account == null) return;
            var item = AccountStore.Shop.FirstOrDefault(i => string.Equals(i.Id, id, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            if (!AccountStore.Owns(_account, item)) { ShowToast("Compre este item primeiro."); return; }
            if (item.Type.Equals("Icon", StringComparison.OrdinalIgnoreCase))
            {
                _account.IconId = AccountStore.Icon(item.IconId).Id;
                AccountStore.SaveAccounts();
            }
            else if (item.Type.Equals("Banner", StringComparison.OrdinalIgnoreCase))
            {
                _account.BannerId = AccountStore.Banner(item.BannerId).Id;
                AccountStore.SaveAccounts();
            }
            else
            {
                var gun = _guns.FirstOrDefault(g => string.Equals(AccountStore.GunId(g.GunId), AccountStore.GunId(item.WeaponId), StringComparison.OrdinalIgnoreCase));
                _equipped[gun?.GunId ?? item.WeaponId] = item.SkinId;
                SaveEquippedToAccount();
            }
            ShowToast("Equipado: " + item.Name);
        }

        private void SaveEquippedToAccount()
        {
            if (_account == null) return;
            _account.EquippedSkins = new Dictionary<string, string>(_equipped, StringComparer.OrdinalIgnoreCase);
            AccountStore.SaveAccounts();
        }

        private void LoadEquippedFromAccount()
        {
            _equipped.Clear();
            if (_account == null) return;
            foreach (var kv in _account.EquippedSkins)
                _equipped[kv.Key] = kv.Value;
        }

        private void InjectShopSkins()
        {
            foreach (var item in AccountStore.Shop.Where(i => i.Type.Equals("WeaponSkin", StringComparison.OrdinalIgnoreCase)))
            {
                var gun = _guns.FirstOrDefault(g => string.Equals(AccountStore.GunId(g.GunId), AccountStore.GunId(item.WeaponId), StringComparison.OrdinalIgnoreCase));
                if (gun == null) continue;
                if (!gun.Skins.Any(s => string.Equals(s.SkinId, item.SkinId, StringComparison.OrdinalIgnoreCase)))
                    gun.Skins.Add(new Skin { SkinId = item.SkinId, Name = item.Name });
            }
        }

        // ---------- Notificações ----------
        private void DrawNotifications(Graphics g, int w, int h)
        {
            if (!_notifOpen || _account == null) return;
            g.FillRectangle(Br(A(Color.Black, 110)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "notifBackdrop");

            int pw = 400, ph = Math.Min(470, h - BarH - 30);
            var r = new Rectangle(w - pw - 20, BarH + 12, pw, ph);
            Reg(r, "notifPanel");
            using (var p = Chamfer(r, 14, true, false, false, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "NOTIFICATIONS", FNav, Ink, r.X + 20, r.Y + 28, 3f);
            var close = new Rectangle(r.Right - 44, r.Y + 12, 30, 30);
            Reg(close, "notifClose");
            var xp = Pn(Mix(Dim, Ink, Hv("notifClose")), 1.8f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);
            g.DrawLine(Pn(A(Line, 160), 1f), r.X, r.Y + 54, r.Right, r.Y + 54);

            var list = AccountStore.NotificationsFor(_account);
            int maxItems = Math.Max(1, (ph - 78) / 72);
            int y = r.Y + 66;
            if (list.Count == 0)
            {
                Spaced(g, "NO NOTIFICATIONS", FLabel, Dim, r.X + r.Width / 2f, r.Y + 120, 3f, StringAlignment.Center);
            }
            foreach (var n in list.Take(maxItems))
            {
                var nr = new Rectangle(r.X + 14, y, r.Width - 28, 66);
                using (var p = Chamfer(nr, 8, true, false, true, false))
                {
                    g.FillPath(Br(A(Ink, 8)), p);
                    g.DrawPath(Pn(A(Line, 140), 1f), p);
                }
                bool unread = !_account.ReadNotifications.Contains(n.Id);
                if (unread) g.FillEllipse(Br(Red), nr.X + 10, nr.Y + 12, 6, 6);
                Txt(g, n.Message, FSmall, Ink, new RectangleF(nr.X + 22, nr.Y + 8, nr.Width - 32, 34));
                Spaced(g, $"{n.Author.ToUpperInvariant()}  •  {n.Target.ToUpperInvariant()}  •  {n.CreatedAt.ToLocalTime():dd/MM HH:mm}",
                    FTiny, Dim, nr.X + 22, nr.Y + 50, 1.2f);
                y += 72;
            }
        }

        // ---------- Adicionar amigo ----------
        private void DrawAddFriend(Graphics g, int w, int h)
        {
            if (!_addFriendOpen || _account == null) return;
            g.FillRectangle(Br(A(Color.Black, 160)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "addFriendBackdrop");

            int pw = 600, ph = 500;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 16, (h - ph) / 2), pw, ph);
            Reg(r, "addFriendPanel");
            using (var p = Chamfer(r, 16, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 16, r.Y, r.Width - 16, 3);
            Spaced(g, "FRIENDS", FLabel, Red, r.X + 28, r.Y + 30, 4f);
            Txt(g, "ADD FRIEND", VSub, Ink, new RectangleF(r.X + 28, r.Y + 42, pw - 56, 44));
            Txt(g, "Search by UserID or DisplayName of another local account.", FSmall, Dim,
                new RectangleF(r.X + 28, r.Y + 84, pw - 56, 20));

            var sf = new Rectangle(r.X + 28, r.Y + 112, pw - 56, 56);
            UiField(g, sf, "USERID OU DISPLAYNAME", _tbFriendSearch);

            int y = r.Y + 182;
            if ((_tbFriendSearch.Text ?? "").Trim().Length < 2)
            {
                Spaced(g, "TYPE AT LEAST 2 CHARACTERS", FLabel, Dim, r.X + pw / 2f, y + 60, 3f, StringAlignment.Center);
            }
            else if (_friendResults.Count == 0)
            {
                Spaced(g, "NO LOCAL ACCOUNT FOUND", FLabel, Dim, r.X + pw / 2f, y + 6, 3f, StringAlignment.Center);
                string query = (_tbFriendSearch.Text ?? "").Trim();
                var row = new Rectangle(r.X + 28, y + 26, pw - 56, 52);
                using (var p = Chamfer(row, 8, true, false, true, false))
                {
                    g.FillPath(Br(A(Ink, 8)), p);
                    g.DrawPath(Pn(A(Line, 140), 1f), p);
                }
                Txt(g, query, FMid, Ink, new RectangleF(row.X + 14, row.Y + 6, row.Width - 150, 24));
                Spaced(g, "ADD BY ID / GMAIL", FTiny, Dim, row.X + 14, row.Y + 32, 2f);
                Button(g, new Rectangle(row.Right - 118, row.Y + 10, 104, 32), "addRemote:" + query,
                    "ADD", true, FTiny, 1.5f);
            }
            else
            {
                foreach (var a in _friendResults.Take(4))
                {
                    var row = new Rectangle(r.X + 28, y, pw - 56, 52);
                    using (var p = Chamfer(row, 8, true, false, true, false))
                    {
                        g.FillPath(Br(A(Ink, 8)), p);
                        g.DrawPath(Pn(A(Line, 140), 1f), p);
                    }
                    DrawAccountIcon(g, new RectangleF(row.X + 10, row.Y + 8, 36, 36), AccountStore.Icon(a.IconId));
                    Txt(g, a.Nick, FMid, Ink, new RectangleF(row.X + 56, row.Y + 6, 200, 20));
                    Txt(g, a.DisplayName + "  •  " + a.UserId, FSmall, Dim,
                        new RectangleF(row.X + 56, row.Y + 27, 300, 18));
                    bool already = _account.Friends.Contains(a.UserId, StringComparer.OrdinalIgnoreCase);
                    bool pending = _account.SentFriendRequests.Contains(a.UserId, StringComparer.OrdinalIgnoreCase) ||
                                   a.FriendRequests.Contains(_account.UserId, StringComparer.OrdinalIgnoreCase);
                    if (already)
                        Spaced(g, "ALREADY A FRIEND", FTiny, Green, row.Right - 20, row.Y + 26, 2f, StringAlignment.Far);
                    else if (pending)
                        Spaced(g, "REQUEST SENT", FTiny, Amber, row.Right - 20, row.Y + 26, 2f, StringAlignment.Far);
                    else
                        Button(g, new Rectangle(row.Right - 108, row.Y + 10, 94, 32), "addFriendResult:" + a.UserId,
                            "ADD", true, FTiny, 1.5f);
                    y += 60;
                }
            }

            Button(g, new Rectangle(r.X + 28, r.Bottom - 54, 140, 40), "addFriendClose", "CLOSE", false, FLabel, 2.5f);
        }

        // ---------- Painel admin ----------
        private void ToggleAdmin()
        {
            if (_adminOpen) { CloseAdmin(); return; }
            if (_account == null || !AccountStore.IsAdminAllowed(_account))
            {
                ShowToast("Acesso administrativo restrito.");
                return;
            }
            _adminOpen = true;
            _adminError = "";
            _tbAdminPass.Text = "";
            if (_adminLog.Count == 0)
            {
                _adminLog.Add("Deadzone Admin Console v" + AppInfo.Versao);
                _adminLog.Add("Digite 'help' para ver os comandos.");
            }
        }

        private void CloseAdmin()
        {
            _adminOpen = false;
            _notifyUiOpen = false;
            _announceUiOpen = false;
            _giveOpen = false;
            _adminGmailOpen = false;
            _tagsToGive.Clear();
            _adminAuthed = false;
            _adminError = "";
            _tbAdminPass.Text = "";
            _tbAdminCmd.Text = "";
        }

        private void DrawAdminPanel(Graphics g, int w, int h)
        {
            if (!_adminOpen) return;
            g.FillRectangle(Br(A(Color.Black, 195)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "adminBackdrop");

            if (!_adminAuthed)
            {
                int pw = 520, ph = 382;
                var r = new Rectangle((w - pw) / 2, (h - ph) / 2, pw, ph);
                Reg(r, "adminPanel");
                using (var p = Chamfer(r, 14, true, false, true, false))
                {
                    g.FillPath(Br(Color.FromArgb(10, 16, 20)), p);
                    g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
                }
                g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), r.X + 14, r.Y, r.Width - 14, 3);
                Spaced(g, "ACESSO RESTRITO", FLabel, Color.FromArgb(40, 220, 140), r.X + 28, r.Y + 30, 4f);
                Txt(g, "ADMIN CONSOLE", VSub, Ink, new RectangleF(r.X + 28, r.Y + 44, pw - 56, 44));
                Txt(g, "Somente contas autorizadas. Informe as credenciais de administrador.",
                    FSmall, Dim, new RectangleF(r.X + 28, r.Y + 88, pw - 56, 20));

                var uf = new Rectangle(r.X + 28, r.Y + 126, pw - 56, 54);
                var pf = new Rectangle(r.X + 28, r.Y + 190, pw - 56, 54);
                UiField(g, uf, "ADMIN USER", _tbAdminUser);
                UiField(g, pf, "ADMIN PASSWORD", _tbAdminPass);
                if (!string.IsNullOrEmpty(_adminError))
                    Txt(g, _adminError, FSmall, HpRed, new RectangleF(r.X + 28, r.Y + 252, pw - 56, 20));

                Button(g, new Rectangle(r.X + 28, r.Bottom - 62, pw - 56, 44), "adminAuth", "AUTENTICAR", true, FLabel, 3f);
                var authClose = new Rectangle(r.Right - 42, r.Y + 12, 28, 28);
                Reg(authClose, "adminClose");
                var authXp = Pn(A(Ink, 180), 1.6f);
                g.DrawLine(authXp, authClose.X + 8, authClose.Y + 8, authClose.Right - 8, authClose.Bottom - 8);
                g.DrawLine(authXp, authClose.Right - 8, authClose.Y + 8, authClose.X + 8, authClose.Bottom - 8);
                return;
            }

            int cw = Math.Min(920, w - 60), chh = Math.Min(620, h - BarH - 26);
            var cr = new Rectangle((w - cw) / 2, Math.Max(BarH + 10, (h - chh) / 2), cw, chh);
            Reg(cr, "adminPanel");
            using (var p = Chamfer(cr, 12, true, false, true, false))
            {
                g.FillPath(Br(Color.FromArgb(7, 12, 10)), p);
                g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
            }
            g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), cr.X + 14, cr.Y, cr.Width - 14, 3);

            Spaced(g, "DEADZONE ADMIN // " + _account!.Nick.ToUpperInvariant(), FNav,
                Color.FromArgb(60, 230, 150), cr.X + 24, cr.Y + 26, 2f);
            Spaced(g, "ACTION PANEL", FTiny, Color.FromArgb(40, 160, 110), cr.X + 24, cr.Y + 48, 2f);
            Button(g, new Rectangle(cr.Right - 620, cr.Y + 14, 138, 36), "adminGmailUi", "ADMIN GMAIL", false, FTiny, 1.5f);
            Button(g, new Rectangle(cr.Right - 472, cr.Y + 14, 130, 36), "adminGiveUi", "GIVE", false, FLabel, 2f);
            Button(g, new Rectangle(cr.Right - 330, cr.Y + 14, 130, 36), "adminNotifyUi", "NOTIFY", false, FLabel, 2f);
            Button(g, new Rectangle(cr.Right - 188, cr.Y + 14, 140, 36), "adminAnnounceUi", "ANNOUNCE", false, FLabel, 2f);
            var close = new Rectangle(cr.Right - 44, cr.Y + 12, 28, 28);
            Reg(close, "adminClose");
            var xp = Pn(A(Ink, 170), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);
            g.DrawLine(Pn(Color.FromArgb(30, 80, 60), 1f), cr.X, cr.Y + 64, cr.Right, cr.Y + 64);

            var green = Color.FromArgb(90, 235, 160);
            var dim = Color.FromArgb(60, 130, 100);
            int maxLines = Math.Max(4, (chh - 150) / 18);
            int start = Math.Max(0, _adminLog.Count - maxLines);
            int ly = cr.Y + 82;
            for (int i = start; i < _adminLog.Count; i++)
            {
                var line = _adminLog[i];
                var c = line.StartsWith("> ", StringComparison.Ordinal) ? Color.FromArgb(230, 245, 235)
                    : line.StartsWith("[erro]", StringComparison.OrdinalIgnoreCase) ? HpRed
                    : green;
                g.DrawString(line, FMono, Br(c), cr.X + 24, ly, StringFormat.GenericTypographic);
                ly += 18;
            }

            g.DrawLine(Pn(Color.FromArgb(30, 80, 60), 1f), cr.X, cr.Bottom - 64, cr.Right, cr.Bottom - 64);
            g.DrawString(">", FMonoB, Br(green), cr.X + 24, cr.Bottom - 48, StringFormat.GenericTypographic);
            var input = new Rectangle(cr.X + 48, cr.Bottom - 48, cr.Width - 80, 22);
            if (_tbAdminCmd.Location != input.Location) _tbAdminCmd.Location = input.Location;
            if (_tbAdminCmd.Width != input.Width) _tbAdminCmd.Width = input.Width;
            if (_tbAdminCmd.BackColor != Color.FromArgb(7, 12, 10)) _tbAdminCmd.BackColor = Color.FromArgb(7, 12, 10);
            if (_tbAdminCmd.ForeColor != green) _tbAdminCmd.ForeColor = green;
            if (_tbAdminCmd.Font != FMono) _tbAdminCmd.Font = FMono;

            Spaced(g, "ENTER EXECUTA  •  ESC FECHA  •  'help' PARA COMANDOS", FTiny, dim,
                cr.X + 24, cr.Bottom - 22, 1.5f);
        }

        // ---------- Telas de ação do admin ----------
        private void DrawNotifyUi(Graphics g, int w, int h)
        {
            if (!_notifyUiOpen || !_adminAuthed) return;
            g.FillRectangle(Br(A(Color.Black, 150)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "notifyUiBackdrop");

            int pw = 640, ph = 430;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 14, (h - ph) / 2), pw, ph);
            Reg(r, "notifyUiPanel");
            using (var p = Chamfer(r, 14, true, false, true, false))
            {
                g.FillPath(Br(Color.FromArgb(9, 18, 13)), p);
                g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
            }
            g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "NOTIFY", FLabel, Color.FromArgb(60, 230, 150), r.X + 28, r.Y + 24, 4f);
            Txt(g, "SEND NOTIFICATION", VSub, Ink, new RectangleF(r.X + 28, r.Y + 38, pw - 100, 44));
            var close = new Rectangle(r.Right - 46, r.Y + 14, 28, 28);
            Reg(close, "notifyUiClose");
            var xp = Pn(A(Ink, 180), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            Spaced(g, "DESTINO", FTiny, Dim, r.X + 28, r.Y + 96, 3f);
            string[] targets = { "all", "Brazil", "USA", "Spain", "Portugal", "Mexico", "Argentina" };
            string[] names = { "ALL", "BRAZIL", "USA", "SPAIN", "PORTUGAL", "MEXICO", "ARGENTINA" };
            int bw = 82, gap = 8, per = 4;
            for (int i = 0; i < targets.Length; i++)
            {
                int col = i % per, row = i / per;
                bool sel = string.Equals(_notifyTarget, targets[i], StringComparison.OrdinalIgnoreCase);
                SizeBtn(g, new Rectangle(r.X + 28 + col * (bw + gap), r.Y + 112 + row * 38, bw, 30),
                    "notifyRegion:" + targets[i], names[i], sel);
            }

            var mf = new Rectangle(r.X + 28, r.Y + 198, pw - 56, 112);
            UiField(g, mf, "MESSAGE", _tbNotifyMsg);
            Button(g, new Rectangle(r.X + 28, r.Bottom - 62, 200, 42), "notifySend", "SEND NOTIFICATION", true, FLabel, 2.5f);
            Button(g, new Rectangle(r.Right - 28 - 140, r.Bottom - 62, 140, 42), "notifyUiClose", "BACK", false, FLabel, 2.5f);
        }

        private void DrawAnnounceUi(Graphics g, int w, int h)
        {
            if (!_announceUiOpen || !_adminAuthed) return;
            g.FillRectangle(Br(A(Color.Black, 150)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "announceUiBackdrop");

            int pw = 720, ph = 572;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 10, (h - ph) / 2), pw, ph);
            Reg(r, "announceUiPanel");
            using (var p = Chamfer(r, 14, true, false, true, false))
            {
                g.FillPath(Br(Color.FromArgb(9, 18, 13)), p);
                g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
            }
            g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "ANNOUNCE", FLabel, Color.FromArgb(60, 230, 150), r.X + 28, r.Y + 24, 4f);
            Txt(g, "PUBLISH ANNOUNCEMENT", VSub, Ink, new RectangleF(r.X + 28, r.Y + 38, pw - 100, 44));
            var close = new Rectangle(r.Right - 46, r.Y + 14, 28, 28);
            Reg(close, "announceUiClose");
            var xp = Pn(A(Ink, 180), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            Spaced(g, "CATEGORIA", FTiny, Dim, r.X + 28, r.Y + 92, 3f);
            string[] cats = { "Updates", "News", "Events", "Patch Notes" };
            string[] labels = { "UPDATES", "NEWS", "EVENTS", "PATCH NOTES" };
            int cw2 = 156, gap = 8;
            for (int i = 0; i < cats.Length; i++)
            {
                bool sel = string.Equals(_annCategory, cats[i], StringComparison.OrdinalIgnoreCase);
                SizeBtn(g, new Rectangle(r.X + 28 + i * (cw2 + gap), r.Y + 108, cw2, 30),
                    "announceCat:" + cats[i], labels[i], sel);
            }

            UiField(g, new Rectangle(r.X + 28, r.Y + 150, pw - 56, 48), "TITLE", _tbAnnTitle);
            UiField(g, new Rectangle(r.X + 28, r.Y + 206, pw - 56, 138), "TEXTO", _tbAnnBody);
            UiField(g, new Rectangle(r.X + 28, r.Y + 352, pw - 56, 46), "ANEXO (CAMINHO DO ARQUIVO, OPCIONAL)", _tbAnnFile);
            UiField(g, new Rectangle(r.X + 28, r.Y + 406, 280, 46), "AUTOR", _tbAnnAuthor);

            Button(g, new Rectangle(r.X + 28, r.Bottom - 62, 210, 42), "announcePublish", "PUBLISH ANNOUNCEMENT", true, FLabel, 2.5f);
            Button(g, new Rectangle(r.Right - 28 - 140, r.Bottom - 62, 140, 42), "announceUiClose", "BACK", false, FLabel, 2.5f);
        }

        // ---------- Give: itens, status e tags ----------
        private void DrawGiveUi(Graphics g, int w, int h)
        {
            if (!_giveOpen || !_adminAuthed) return;
            g.FillRectangle(Br(A(Color.Black, 150)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "giveBackdrop");

            int pw = 720, ph = 520;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 10, (h - ph) / 2), pw, ph);
            Reg(r, "givePanel");
            using (var p = Chamfer(r, 14, true, false, true, false))
            {
                g.FillPath(Br(Color.FromArgb(9, 18, 13)), p);
                g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
            }
            g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "GIVE", FLabel, Color.FromArgb(60, 230, 150), r.X + 28, r.Y + 22, 4f);
            Txt(g, "ITENS • STATUS • TAGS", VSub, Ink, new RectangleF(r.X + 28, r.Y + 36, pw - 100, 42));
            var close = new Rectangle(r.Right - 46, r.Y + 14, 28, 28);
            Reg(close, "giveClose");
            var xp = Pn(A(Ink, 180), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            UiField(g, new Rectangle(r.X + 28, r.Y + 88, pw - 56, 48), "GMAIL / USERID / PLAYER NICK", _tbGiveTarget);
            string[] tabs = { "ITEM", "STATUS", "TAG" };
            for (int i = 0; i < tabs.Length; i++)
                SizeBtn(g, new Rectangle(r.X + 28 + i * 118, r.Y + 146, 110, 30),
                    "giveTab:" + i, tabs[i], _giveTab == i);

            if (_giveTab == 0)
            {
                UiField(g, new Rectangle(r.X + 28, r.Y + 188, pw - 56, 46), "SEARCH ITEM (ID OR NAME)", _tbGiveQuery);
                string q = (_tbGiveQuery.Text ?? "").Trim();
                var items = AccountStore.Shop.Where(i => q.Length < 2 ||
                    i.Id.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    i.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).Take(5).ToList();
                int y = r.Y + 246;
                if (items.Count == 0)
                    Spaced(g, "NENHUM ITEM ENCONTRADO", FLabel, Dim, r.X + pw / 2f, y + 20, 3f, StringAlignment.Center);
                foreach (var item in items)
                {
                    string id = "giveItem:" + item.Id;
                    bool sel = string.Equals(_giveItemSel, item.Id, StringComparison.OrdinalIgnoreCase);
                    SizeBtn(g, new Rectangle(r.X + 28, y, pw - 56, 34), id,
                        item.Name.ToUpperInvariant() + "  •  " + item.Type.ToUpperInvariant(), sel);
                    y += 40;
                }
                Button(g, new Rectangle(r.X + 28, r.Bottom - 62, 220, 42), "giveItemGo", "DAR ITEM", true, FLabel, 2.5f);
            }
            else if (_giveTab == 1)
            {
                int fw = (pw - 56 - 16) / 2;
                UiField(g, new Rectangle(r.X + 28, r.Y + 190, fw, 50), "DEAD COINS (+/-)", _tbGiveCoins);
                UiField(g, new Rectangle(r.X + 28 + fw + 16, r.Y + 190, fw, 50), "LEVEL", _tbGiveLevel);
                UiField(g, new Rectangle(r.X + 28, r.Y + 250, fw, 50), "WINS (+/-)", _tbGiveWins);
                UiField(g, new Rectangle(r.X + 28 + fw + 16, r.Y + 250, fw, 50), "KILLS (+/-)", _tbGiveKills);
                Button(g, new Rectangle(r.X + 28, r.Bottom - 62, 220, 42), "giveStatsGo", "DAR STATUS", true, FLabel, 2.5f);
            }
            else
            {
                Spaced(g, "AVAILABLE TAGS", FLabel, Dim, r.X + 28, r.Y + 200, 3f);
                for (int i = 0; i < AccountStore.AdminTags.Length; i++)
                {
                    string tag = AccountStore.AdminTags[i];
                    SizeBtn(g, new Rectangle(r.X + 28 + i * 162, r.Y + 228, 150, 40),
                        "giveTag:" + tag, tag.ToUpperInvariant(), false);
                }
                Button(g, new Rectangle(r.X + 28, r.Bottom - 62, 220, 42), "giveTagGo", "DAR TAG", true, FLabel, 2.5f);
            }
        }

        private void DrawAdminGmailUi(Graphics g, int w, int h)
        {
            if (!_adminGmailOpen || !_adminAuthed) return;
            g.FillRectangle(Br(A(Color.Black, 150)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "adminGmailBackdrop");
            int pw = 520, ph = 300;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 14, (h - ph) / 2), pw, ph);
            Reg(r, "adminGmailPanel");
            using (var p = Chamfer(r, 14, true, false, true, false))
            {
                g.FillPath(Br(Color.FromArgb(9, 18, 13)), p);
                g.DrawPath(Pn(Color.FromArgb(40, 190, 120), 1.4f), p);
            }
            g.FillRectangle(Br(Color.FromArgb(40, 220, 140)), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "ADMIN GMAIL", FLabel, Color.FromArgb(60, 230, 150), r.X + 28, r.Y + 24, 4f);
            Txt(g, "ADD ADMINISTRATOR", VSub, Ink, new RectangleF(r.X + 28, r.Y + 38, pw - 80, 42));
            var close = new Rectangle(r.Right - 46, r.Y + 14, 28, 28);
            Reg(close, "adminGmailClose");
            var xp = Pn(A(Ink, 180), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);
            UiField(g, new Rectangle(r.X + 28, r.Y + 104, pw - 56, 52), "GMAIL ALLOWED TO OPEN ADMIN", _tbAdminGmail);
            Button(g, new Rectangle(r.X + 28, r.Bottom - 66, pw - 56, 44), "adminGmailAdd", "ADD", true, FLabel, 3f);
        }

        private void DrawLangConfirm(Graphics g, int w, int h)
        {
            if (!_langConfirmOpen) return;
            g.FillRectangle(Br(A(Color.Black, 170)), 0, 0, w, h);
            Reg(new Rectangle(0, 0, w, h), "langBackdrop");
            int pw = 520, ph = 300;
            var r = new Rectangle((w - pw) / 2, Math.Max(BarH + 14, (h - ph) / 2), pw, ph);
            Reg(r, "langPanel");
            using (var p = Chamfer(r, 16, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 252)), p);
                g.DrawPath(Pn(Line, 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 18, r.Y, r.Width - 18, 3);
            Spaced(g, "CONFIRM LANGUAGE CHANGE", FLabel, Red, r.X + 32, r.Y + 30, 4f);
            Txt(g, Loc.Languages[_langSelected], VSub, Ink, new RectangleF(r.X + 32, r.Y + 46, pw - 64, 44));
            Txt(g, _langConfirmLeft > 0f
                    ? "Confirm available in " + _langConfirmLeft.ToString("0.0") + "s"
                    : "You can confirm now.",
                FSmall, _langConfirmLeft > 0f ? Amber : Green,
                new RectangleF(r.X + 32, r.Y + 108, pw - 64, 22));
            Button(g, new Rectangle(r.X + 32, r.Bottom - 72, 130, 44), "langCancel", "CANCEL", false, FLabel, 2f);
            Button(g, new Rectangle(r.X + 174, r.Bottom - 72, 130, 44), "langRevert", "REVERT", false, FLabel, 2f);
            using (var p = Chamfer(new Rectangle(r.Right - 32 - 150, r.Bottom - 72, 150, 44), 12, true, false, true, false))
            {
                bool ready = _langConfirmLeft <= 0f;
                g.FillPath(Br(ready ? Red : A(Ink, 35)), p);
                g.DrawPath(Pn(A(Line, 200), 1f), p);
            }
            Reg(new Rectangle(r.Right - 32 - 150, r.Bottom - 72, 150, 44), "langConfirm");
            Spaced(g, "CONFIRM", FLabel, Color.White, r.Right - 32 - 75, r.Bottom - 50, 3f, StringAlignment.Center);
        }

        private void DrawChat(Graphics g, int w, int h)
        {
            if (!_chatOpen) return;
            int px = _page == 4 ? 32 : 250;
            int ph = 248, pw = 430;
            var r = new Rectangle(px, h - ph - 24, pw, ph);
            Reg(r, "chatPanel");
            using (var p = Chamfer(r, 12, true, false, true, false))
            {
                g.FillPath(Br(A(Panel, 246)), p);
                g.DrawPath(Pn(A(Line, 220), 1f), p);
            }
            g.FillRectangle(Br(Red), r.X + 14, r.Y, r.Width - 14, 3);
            Spaced(g, "CHAT", FLabel, Red, r.X + 20, r.Y + 24, 3f);
            SizeBtn(g, new Rectangle(r.X + 88, r.Y + 14, 86, 28), "chatScope:0", "TEAM", _chatScope == 0);
            SizeBtn(g, new Rectangle(r.X + 180, r.Y + 14, 86, 28), "chatScope:1", "ALL", _chatScope == 1);
            var close = new Rectangle(r.Right - 42, r.Y + 12, 28, 28);
            Reg(close, "chatClose");
            var xp = Pn(Mix(Dim, Ink, Hv("chatClose")), 1.6f);
            g.DrawLine(xp, close.X + 8, close.Y + 8, close.Right - 8, close.Bottom - 8);
            g.DrawLine(xp, close.Right - 8, close.Y + 8, close.X + 8, close.Bottom - 8);

            var view = new Rectangle(r.X + 10, r.Y + 48, r.Width - 20, 128);
            var team = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_account != null)
                foreach (var id in _account.Friends) team.Add(id);
            string scope = _chatScope == 1 ? "all" : "team";
            var list = ChatStore.For(scope, _account?.UserId ?? "", team);
            int y = view.Bottom - 8;
            int count = 0;
            for (int i = list.Count - 1; i >= 0 && count < 7; i--, count++)
            {
                var m = list[i];
                y -= 22;
                if (y < view.Top) break;
                var col = m.Scope == "all" ? Amber : Green;
                Spaced(g, m.Author.ToUpperInvariant(), FTiny, col, view.X + 6, y + 6, 1.5f);
                Txt(g, m.Text, FSmall, Ink, new RectangleF(view.X + 86, y, view.Width - 92, 18));
            }

            UiField(g, new Rectangle(r.X + 10, r.Y + 180, r.Width - 20, 44), "MESSAGE (ENTER TO SEND)", _tbChat);
            Button(g, new Rectangle(r.Right - 100, r.Bottom - 46, 88, 32), "chatSend", "SEND", true, FTiny, 1.5f);
        }

        private void AdminLogin()
        {
            if (AccountStore.VerifyAdmin(_tbAdminUser.Text, _tbAdminPass.Text))
            {
                _adminAuthed = true;
                _adminError = "";
                _tbAdminPass.Text = "";
                _adminLog.Add("Admin session started by " + _account?.Nick + ".");
                _adminLog.Add("help to see available commands.");
                _tbAdminCmd.Focus();
            }
            else
            {
                _adminError = "Invalid admin username or password.";
            }
        }

        private void ExecuteAdminCommand(string raw)
        {
            raw = (raw ?? "").Trim();
            if (raw.Length == 0) return;
            _adminLog.Add("> " + raw);
            var parts = raw.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLowerInvariant();

            // Notify e Announce não executam mais comando: abrem a tela correspondente.
            if (cmd == "notify" || cmd == "announce")
            {
                _adminLog.Add("Opening interface for " + (cmd == "notify" ? "notification" : "announcement") + ".");
                if (cmd == "notify")
                {
                    _announceUiOpen = false;
                    _notifyUiOpen = true;
                    _notifyTarget = parts.Length > 1 ? (AccountStore.NormalizeRegion(parts[1]) ?? "all") : "all";
                    _tbNotifyMsg.Text = parts.Length > 2 ? parts[2] : "";
                    BeginInvoke((Action)(() => { if (_notifyUiOpen) _tbNotifyMsg.Focus(); }));
                }
                else
                {
                    _notifyUiOpen = false;
                    _announceUiOpen = true;
                    _annCategory = parts.Length > 1 ? AccountStore.NormalizeCategory(parts[1]) : "Updates";
                    _tbAnnTitle.Text = "";
                    _tbAnnBody.Text = "";
                    _tbAnnFile.Text = "";
                    _tbAnnAuthor.Text = _account?.Nick ?? "sr.idev";
                    BeginInvoke((Action)(() => { if (_announceUiOpen) _tbAnnTitle.Focus(); }));
                }
                return;
            }

            switch (cmd)
            {
                case "help":
                    _adminLog.Add("notify all <message>              - notify everyone");
                    _adminLog.Add("notify brazil <message>            - notify a region");
                    _adminLog.Add("  regions: brazil, usa, spain, portugal, mexico, argentina");
                    _adminLog.Add("announce update <title> | <body> | <file>");
                    _adminLog.Add("  categories: update, news, event, patch");
                    _adminLog.Add("coins <amount>                      - add Dead Coins to current account");
                    _adminLog.Add("accounts                           — lista contas locais");
                    _adminLog.Add("version | clear | logout");
                    break;

                case "version":
                    _adminLog.Add("Deadzone " + AppInfo.Versao + " • " + AppInfo.Tema + " • " + AppInfo.EmpresaFundadora + " INC");
                    break;

                case "clear":
                    _adminLog.Clear();
                    _adminLog.Add("Console limpo.");
                    break;

                case "logout":
                    _adminAuthed = false;
                    _tbAdminPass.Text = "";
                    _adminLog.Add("Session closed.");
                    break;

                case "admin":
                {
                    if (parts.Length < 2) { _adminLog.Add("[erro] uso: admin <gmail>"); break; }
                    if (!AccountStore.AddAdminGmail(parts[1], out var aerr)) { _adminLog.Add("[erro] " + aerr); break; }
                    _adminLog.Add("Administrador adicionado: " + parts[1]);
                    break;
                }

                case "accounts":
                    _adminLog.Add($"{AccountStore.Accounts.Count} local account(s):");
                    foreach (var a in AccountStore.Accounts.Take(12))
                        _adminLog.Add($"  {a.Nick}  {a.UserId}  {a.Region}  {a.DeadCoins} DC");
                    break;

                case "coins":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out var amount) || amount <= 0)
                    {
                        _adminLog.Add("[erro] uso: coins <quantidade>");
                        break;
                    }
                    if (_account == null) { _adminLog.Add("[error] no account logged in."); break; }
                    _account.DeadCoins += amount;
                    AccountStore.SaveAccounts();
                    _adminLog.Add($"Adicionados {amount} Dead Coins. Saldo: {_account.DeadCoins}.");
                    break;

                case "notify":
                {
                    if (parts.Length < 3)
                    {
                        _adminLog.Add("[error] usage: notify <all|brazil|usa|spain|...> <message>");
                        break;
                    }
                    string targetRaw = parts[1];
                    var target = AccountStore.NormalizeRegion(targetRaw);
                    if (target == null)
                    {
                        _adminLog.Add("[error] invalid region. Use all, brazil, usa, spain, portugal, mexico or argentina.");
                        break;
                    }
                    string msg = parts[2].Trim();
                    if (msg.Length == 0) { _adminLog.Add("[error] empty message."); break; }
                    var n = AccountStore.AddNotification(target, msg, _account?.Nick ?? "sr.idev");
                    _adminLog.Add($"Notification sent to {(target == "all" ? "ALL" : target)}: {n.Message}");
                    ShowToast("Notification sent.");
                    break;
                }

                case "announce":
                {
                    if (parts.Length < 3)
                    {
                        _adminLog.Add("[error] usage: announce <update|news|event|patch> <title> | <body> | <file>");
                        break;
                    }
                    string cat = AccountStore.NormalizeCategory(parts[1]);
                    string rest = parts[2];
                    var chunks = rest.Split('|');
                    string title = chunks.Length > 0 ? chunks[0].Trim() : "";
                    string body = chunks.Length > 1 ? chunks[1].Trim() : "";
                    string file = chunks.Length > 2 ? chunks[2].Trim().Trim('"') : "";
                    if (title.Length == 0) { _adminLog.Add("[error] enter a title."); break; }
                    var ann = AccountStore.AddAnnouncement(cat, title, body, _account?.Nick ?? "sr.idev",
                        string.IsNullOrWhiteSpace(file) ? null : file);
                    _adminLog.Add($"Announcement published in {ann.Category}: {ann.Title}");
                    if (!string.IsNullOrEmpty(ann.AttachmentName)) _adminLog.Add("Anexo: " + ann.AttachmentName);
                    else if (!string.IsNullOrWhiteSpace(file)) _adminLog.Add("[error] attachment file not found.");
                    ShowToast("Announcement published.");
                    break;
                }

                default:
                    _adminLog.Add("[erro] comando desconhecido. Digite 'help'.");
                    break;
            }
            if (_adminLog.Count > 200) _adminLog.RemoveRange(0, _adminLog.Count - 200);
        }

        // ---------- Amigos / conta ----------
        private void LoadFriendsFromAccount()
        {
            _friendList.Clear();
            if (_account == null) return;
            foreach (var id in _account.Friends)
            {
                var a = AccountStore.FindByUserId(id);
                if (a == null) continue;
                _friendList.Add(new Friend(a.Nick, a.DisplayName + "  •  " + a.UserId, FStatus.Online, a.UserId));
            }
            foreach (var remote in _account.RemoteContacts)
                _friendList.Add(new Friend(remote, "Remote  •  " + remote, FStatus.Offline, remote));
            _friendRequests.Clear();
            _friendRequests.AddRange(_account.FriendRequests);
        }

        private void AddFriendByUserId(string userId)
        {
            if (_account == null) return;
            if (_online && OnlineClient.Connected)
            {
                OnlineClient.Send("friend:add", new { value = userId });
                ShowToast("Friend request sent online.");
                return;
            }
            var other = AccountStore.FindByUserId(userId);
            if (other == null || other.UserId == _account.UserId) return;
            if (!AccountStore.SendFriendRequest(_account, other, out var err))
            {
                ShowToast(err);
                return;
            }
            LoadFriendsFromAccount();
            SetFriendResults(AccountStore.SearchAccounts(_tbFriendSearch.Text, _account.UserId));
            ShowToast("Pedido enviado para " + other.Nick + ".");
        }

        private void SetFriendResults(List<Account> list)
        {
            _friendResults.Clear();
            _friendResults.AddRange(list);
        }

        // ---------- Lobby helpers ----------
        private void DrawCoinsChip(Graphics g, Rectangle r, string id)
        {
            Reg(r, id);
            float hv = Hv(id);
            using (var p = Chamfer(r, 9, true, false, true, false))
            {
                g.FillPath(Br(Mix(A(Panel, 240), A(PanelHi, 250), hv)), p);
                g.DrawPath(Pn(Mix(Line, Amber, hv), 1f), p);
            }
            g.FillEllipse(Br(A(Amber, 40)), r.X + 10, r.Y + 9, 28, 28);
            g.DrawEllipse(Pn(Amber, 1.6f), r.X + 10, r.Y + 9, 28, 28);
            Txt(g, "D", FSmallB, Amber, new RectangleF(r.X + 10, r.Y + 9, 28, 28), StringAlignment.Center);
            Spaced(g, "DEAD COINS", FTiny, Dim, r.X + 50, r.Y + 15, 2f);
            Txt(g, (_account?.DeadCoins ?? 0).ToString("N0"), FName, Amber,
                new RectangleF(r.X + 50, r.Y + 24, r.Width - 60, 20));
        }

        private void DrawSquadRow(Graphics g, int w, int y)
        {
            if (!_squadOpen) return;
            int gap = 16, cw = 190, ch = 200;
            int total = cw * 4 + gap * 3;
            int x = (w - total) / 2; // grupo sempre centralizado
            for (int i = 0; i < 4; i++)
            {
                var r = new Rectangle(x + i * (cw + gap), y, cw, ch);
                bool filled = _groupMembers.Count > i;
                using (var p = Chamfer(r, 14, true, false, true, false))
                {
                    if (filled)
                    {
                        using var lb = new LinearGradientBrush(r, A(PanelHi, 245), A(Panel, 245), 90f);
                        g.FillPath(lb, p);
                        g.DrawPath(Pn(i == 0 ? Red : A(Line, 230), 1.4f), p);
                    }
                    else
                    {
                        Reg(r, "invite");
                        float hv = Hv("invite");
                        g.FillPath(Br(A(Panel, (int)(130 + 60 * hv))), p);
                        using var dp = new Pen(Mix(Line, Ink, hv), 1.4f) { DashStyle = DashStyle.Dash };
                        g.DrawPath(dp, p);
                    }
                }
                if (!filled)
                {
                    float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f - 12;
                    var pen = Pn(Mix(Dim, Ink, Hv("invite")), 2f);
                    g.DrawLine(pen, cx - 12, cy, cx + 12, cy);
                    g.DrawLine(pen, cx, cy - 12, cx, cy + 12);
                    Spaced(g, "INVITE", FTiny, Dim, cx, cy + 28, 2f, StringAlignment.Center);
                    continue;
                }

                // card estilo antigo: silhueta + placa com o nome
                g.FillRectangle(Br(i == 0 ? Red : Line), r.X + 16, r.Y, r.Width - 16, 3);
                g.SetClip(new Rectangle(r.X + 1, r.Y + 4, r.Width - 2, r.Height - 8));
                float hd = r.Width * .34f;
                var sc = i == 0 ? Red : Ink;
                g.FillEllipse(Br(A(sc, 95)), r.X + (r.Width - hd) / 2f, r.Y + 34, hd, hd);
                float sw = r.Width * .84f;
                g.FillEllipse(Br(A(sc, 75)), r.X + (r.Width - sw) / 2f, r.Y + 34 + hd + 12, sw, sw);
                g.ResetClip();

                var plate = new Rectangle(r.X, r.Bottom - 62, r.Width, 62);
                g.FillRectangle(Br(A(Color.Black, 145)), plate);
                Txt(g, _groupMembers[i].ToUpperInvariant(), VName, Ink,
                    new RectangleF(plate.X + 16, plate.Y + 8, plate.Width - 32, 28));
                Spaced(g, i == 0 ? "LEADER" : "MEMBER", FTiny, i == 0 ? Red : Green, plate.X + 16, plate.Y + 40, 2f);
                if (i == 0 && _account != null)
                {
                    var rk = RankStore.ForXp(_account.Xp);
                    Spaced(g, rk.Name.ToUpperInvariant(), FTiny, RankStore.Parse(rk.Color),
                        plate.Right - 16, plate.Y + 40, 2f, StringAlignment.Far);
                }
            }
            Button(g, new Rectangle(x + total / 2 - 48, y + ch + 12, 96, 34), "squadLeave", "LEAVE", false, FTiny, 1.5f);
        }
    }
}
