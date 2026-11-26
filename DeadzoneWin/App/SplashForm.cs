// SplashForm.cs — Splash inicial do Deadzone: logo, nome do jogo e barra de carregamento fina.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Deadzone2D
{
    internal sealed class SplashForm : Form
    {
        private const int W = 720, H = 240;
        private readonly System.Windows.Forms.Timer _anim;
        private float _t;
        private Image? _icon;
        private readonly Font _fHead, _fMark, _fSmall, _fTiny;

        private static readonly Color Bg = Color.FromArgb(15, 25, 35);
        private static readonly Color BgDeep = Color.FromArgb(9, 15, 22);
        private static readonly Color Ink = Color.FromArgb(236, 232, 225);
        private static readonly Color Dim = Color.FromArgb(139, 151, 159);
        private static readonly Color Red = Color.FromArgb(255, 70, 85);
        private static readonly Color Line = Color.FromArgb(48, 62, 78);

        private static string PickFamily(params string[] names)
        {
            using var c = new InstalledFontCollection();
            var have = c.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return names.FirstOrDefault(n => have.Contains(n)) ?? "Segoe UI";
        }

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
            Opacity = 0;

            string head = PickFamily("Bahnschrift SemiBold Condensed", "Bahnschrift Condensed", "Bahnschrift", "Segoe UI");
            var headStyle = head.Contains("SemiBold") ? FontStyle.Regular : FontStyle.Bold;
            string body = PickFamily("Bahnschrift", "Segoe UI");
            _fHead = new Font(head, 70, headStyle, GraphicsUnit.Pixel);
            _fMark = new Font(head, 46, headStyle, GraphicsUnit.Pixel);
            _fSmall = new Font(body, 12, FontStyle.Bold, GraphicsUnit.Pixel);
            _fTiny = new Font(body, 11, FontStyle.Bold, GraphicsUnit.Pixel);

            _anim = new System.Windows.Forms.Timer { Interval = 16 };
            _anim.Tick += (s, e) =>
            {
                _t += .012f;
                // aparece suave no começo e some suave no fim
                Opacity = Math.Clamp(Math.Min(_t / .10f, (1f - _t) / .12f), 0f, 1f);
                if (_t >= 1f) { _anim.Stop(); Close(); return; }
                Invalidate();
            };
            FormClosed += (s, e) =>
            {
                _anim.Stop();
                _icon?.Dispose();
                _fHead.Dispose(); _fMark.Dispose(); _fSmall.Dispose(); _fTiny.Dispose();
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                var path = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? "", "Assets", "Splash.png");
                if (File.Exists(path))
                {
                    using var fs = File.OpenRead(path);
                    using var tmp = Image.FromStream(fs);
                    _icon = new Bitmap(tmp);      // cópia: não trava o arquivo no disco
                }
            }
            catch { }
            _anim.Start();
        }

        private static Color A(Color c, int a) => Color.FromArgb(Math.Clamp(a, 0, 255), c);

        private static GraphicsPath Chamfer(RectangleF r, float c)
        {
            var p = new GraphicsPath();
            p.AddPolygon(new[]
            {
                new PointF(r.Left + c, r.Top), new PointF(r.Right, r.Top),
                new PointF(r.Right, r.Bottom - c), new PointF(r.Right - c, r.Bottom),
                new PointF(r.Left, r.Bottom), new PointF(r.Left, r.Top + c),
            });
            return p;
        }

        // texto em caixa alta com espaçamento entre letras
        private static float Spaced(Graphics g, string s, Font f, Color c, float x, float cy, float sp)
        {
            using var br = new SolidBrush(c);
            float py = cy - f.GetHeight(g) / 2f, px = x;
            foreach (var ch in s)
            {
                float cw = ch == ' ' ? f.Size * .32f : g.MeasureString(ch.ToString(), f, PointF.Empty, StringFormat.GenericTypographic).Width;
                g.DrawString(ch.ToString(), f, br, px, py, StringFormat.GenericTypographic);
                px += cw + sp;
            }
            return px - x - sp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            // fundo
            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, W, H), Bg, BgDeep, 90f))
                g.FillRectangle(bg, 0, 0, W, H);
            using (var b1 = new SolidBrush(A(Red, 18)))
                g.FillPolygon(b1, new[] { new PointF(W * .52f, 0), new PointF(W * .80f, 0), new PointF(W * .56f, H), new PointF(W * .28f, H) });
            using (var b2 = new SolidBrush(A(Red, 10)))
                g.FillPolygon(b2, new[] { new PointF(W * .78f, 0), new PointF(W * .88f, 0), new PointF(W * .64f, H), new PointF(W * .54f, H) });
            using (var lp = new Pen(A(Ink, 6), 1f))
                for (int x = -H; x < W; x += 40) g.DrawLine(lp, x, H, x + H, 0);
            using (var vl = new LinearGradientBrush(new Rectangle(0, 0, 140, H), A(Color.Black, 120), A(Color.Black, 0), 0f))
                g.FillRectangle(vl, 0, 0, 140, H);
            using (var border = new Pen(Line, 1f)) g.DrawRectangle(border, 0, 0, W - 1, H - 1);

            // logo (imagem do jogo, se existir; senão a marca vermelha)
            const int lx = 52, ly = 56, ls = 92;
            if (_icon != null)
            {
                float k = Math.Min((float)ls / _icon.Width, (float)ls / _icon.Height);
                float dw = _icon.Width * k, dh = _icon.Height * k;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_icon, lx + (ls - dw) / 2f, ly + (ls - dh) / 2f, dw, dh);
            }
            else
            {
                using var mark = Chamfer(new RectangleF(lx, ly, ls, ls), 24);
                using (var br = new SolidBrush(Red)) g.FillPath(br, mark);
                using var inner = Chamfer(new RectangleF(lx + 8, ly + 8, ls - 16, ls - 16), 17);
                using (var ip = new Pen(A(Color.White, 90), 1.4f)) g.DrawPath(ip, inner);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var wb = new SolidBrush(Color.White)) g.DrawString("D", _fMark, wb, new RectangleF(lx, ly, ls, ls), sf);
            }

            // nome + subtítulo
            int tx = lx + ls + 36;
            Spaced(g, "DEADZONE", _fHead, Ink, tx, 84, 10f);
            Spaced(g, "MATA-MATA TÁTICO • DEADTHEME", _fTiny, Dim, tx + 2, 128, 4f);
            using (var lb = new SolidBrush(Red)) g.FillRectangle(lb, tx + 2, 144, 46, 3);

            // barra de carregamento fina
            float p = Math.Clamp(_t, 0f, 1f);
            float eased = 1f - (float)Math.Pow(1f - p, 2);
            int bx = tx + 2, by = H - 46, bw = W - bx - 52;
            using (var tr = new SolidBrush(A(Ink, 28))) g.FillRectangle(tr, bx, by, bw, 3);
            int fill = (int)(bw * eased);
            if (fill > 0)
            {
                using (var glow = new SolidBrush(A(Red, 50))) g.FillRectangle(glow, bx, by - 3, fill, 9);
                using (var fb = new SolidBrush(Red)) g.FillRectangle(fb, bx, by, fill, 3);
                using (var tip = new SolidBrush(Color.White)) g.FillRectangle(tip, bx + fill - 2, by - 2, 3, 7);
            }
            string status = p < .35f ? "INICIANDO" : p < .75f ? "CARREGANDO ARQUIVOS" : "QUASE LÁ";
            Spaced(g, status, _fTiny, Dim, bx, by + 20, 3f);
            using (var pb = new SolidBrush(Ink))
            using (var sfr = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center })
                g.DrawString($"{(int)(eased * 100)}%", _fSmall, pb, new RectangleF(bx, by + 10, bw, 20), sfr);
        }
    }
}