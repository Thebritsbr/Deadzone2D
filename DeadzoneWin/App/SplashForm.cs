// SplashForm.cs — Splash inicial: nome do jogo, ícone e barra de carregamento.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace CrossfireWin
{
    internal sealed class SplashForm : Form
    {
        private const int W = 720, H = 150;
        private readonly System.Windows.Forms.Timer _anim;
        private float _t;
        private Image? _icon;

        private static readonly Color Bg = Color.FromArgb(12, 22, 31);
        private static readonly Color Ink = Color.FromArgb(236, 232, 225);
        private static readonly Color Dim = Color.FromArgb(139, 151, 159);
        private static readonly Color Amber = Color.FromArgb(245, 197, 66);
        private static readonly Color Line = Color.FromArgb(48, 62, 78);

        public SplashForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            Size = new Size(W, H);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg;

            _anim = new System.Windows.Forms.Timer { Interval = 24 };
            _anim.Tick += (s, e) =>
            {
                _t += .045f / 3.5f;
                if (_t >= 1f) { _anim.Stop(); Close(); }
                Invalidate();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                var path = System.IO.Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Assets", "Splash.png");
                if (File.Exists(path)) _icon = Image.FromFile(path);
            }
            catch { }
            _anim.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.SystemDefault;

            using (var b = new SolidBrush(Color.FromArgb(10, 20, 30))) g.FillRectangle(b, 0, 0, W, H);
            using (var top = new LinearGradientBrush(new Rectangle(0, 0, W, 90), Color.FromArgb(150, 10, 12, 18), Color.FromArgb(0, 10, 12, 18), 90f))
                g.FillRectangle(top, 0, 0, W, 90);

            // ícone + nome
            int ix = 38, iy = 40, iw = 96;
            if (_icon != null)
            {
                using (var k = new TextureBrush(_icon, new Rectangle(0, 0, _icon.Width, _icon.Height)))
                    g.FillRectangle(k, ix, iy - 4, iw, 64);
                using var b2 = new SolidBrush(Color.FromArgb(18, 28, 40));
                g.FillRectangle(b2, ix - 4, iy + 46, iw + 8, 12);
            }

            int tx = ix + (iw > 0 ? iw + 30 : 150);
            using (var f1 = new Font("Segoe UI", 20, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(Ink))
                    g.DrawString("Deadzone", f1, b, tx, 45);
            using (var f2 = new Font("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(Dim))
                    g.DrawString("Matar mata com estilo Valorant", f2, b, tx, 67);

            // barra de carregamento
            int bx = tx, by = H - 34, bw = W - tx - 30, bh = 5;
            using (var b = new SolidBrush(Color.FromArgb(40, 52, 66))) g.FillRectangle(b, bx, by, bw, bh);
            int fill = (int)(bw * Math.Clamp(_t, 0f, 1f));
            using (var b = new LinearGradientBrush(new Rectangle(bx, by, bw, bh), Color.FromArgb(255, 205, 80), Color.FromArgb(242, 200, 70), 0f))
                g.FillRectangle(b, bx, by, fill, bh);
            using (var p = new Pen(Amber, 1f))
                g.DrawRectangle(p, bx, by, bw, bh);
            using (var b = new SolidBrush(Amber))
                g.FillRectangle(b, bx + Math.Max(4, fill - 26), by - 4, 10, 13);
        }

        private static Color A(Color c, int a) => Color.FromArgb(Math.Clamp(a, 0, 255), c);
    }
}

