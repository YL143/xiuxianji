using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Xiuxian
{
    /// <summary>配色、字体、绘制工具</summary>
    public static class Theme
    {
        public const float W = 1180f;
        public const float H = 760f;

        public static readonly Color BgTop = Color.FromArgb(12, 13, 18);
        public static readonly Color BgBottom = Color.FromArgb(27, 22, 16);

        public static readonly Color Gold = Color.FromArgb(214, 179, 106);
        public static readonly Color GoldBright = Color.FromArgb(247, 219, 156);
        public static readonly Color GoldDim = Color.FromArgb(139, 116, 68);
        public static readonly Color GoldDeep = Color.FromArgb(94, 76, 42);

        public static readonly Color Ink = Color.FromArgb(233, 225, 208);
        public static readonly Color Muted = Color.FromArgb(156, 148, 131);
        public static readonly Color Faint = Color.FromArgb(110, 103, 90);
        public static readonly Color Line = Color.FromArgb(62, 55, 41);

        public static readonly Color CardTop = Color.FromArgb(33, 34, 41);
        public static readonly Color CardBottom = Color.FromArgb(17, 18, 23);

        public static readonly Color Jade = Color.FromArgb(124, 195, 168);
        public static readonly Color JadeDeep = Color.FromArgb(56, 106, 90);
        public static readonly Color Crimson = Color.FromArgb(201, 99, 86);

        public static readonly Font FBrand = new Font("KaiTi", 25f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBrandSub = new Font("Microsoft YaHei", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FName = new Font("KaiTi", 18f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBadge = new Font("Microsoft YaHei", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FLabel = new Font("Microsoft YaHei", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FValue = new Font("Microsoft YaHei", 12f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FTiny = new Font("Microsoft YaHei", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FLog = new Font("Microsoft YaHei", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FLogStamp = new Font("Microsoft YaHei", 7.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FBtn = new Font("KaiTi", 12.5f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBtnSub = new Font("Microsoft YaHei", 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FAltar = new Font("KaiTi", 30f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FAltarSub = new Font("Microsoft YaHei", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FModal = new Font("KaiTi", 22f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FModalBody = new Font("Microsoft YaHei", 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FHint = new Font("Microsoft YaHei", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font FAvatarChar = new Font("KaiTi", 26f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBreakTitle = new Font("KaiTi", 18f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FBreakIdle = new Font("KaiTi", 16f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FShopTitle = new Font("KaiTi", 11f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FLogTitle = new Font("KaiTi", 14f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FStatIcon = new Font("KaiTi", 10f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FFloating = new Font("Microsoft YaHei", 11f, FontStyle.Bold, GraphicsUnit.Point);
        public static readonly Font FVerse = new Font("KaiTi", 11f, FontStyle.Regular, GraphicsUnit.Point);

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = rad * 2f;
            if (rad <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, RectangleF r, float rad, Brush b)
        {
            GraphicsPath p = Round(r, rad);
            g.FillPath(b, p);
            p.Dispose();
        }

        public static void StrokeRound(Graphics g, RectangleF r, float rad, Color c, float width)
        {
            GraphicsPath p = Round(r, rad);
            using (Pen pen = new Pen(c, width))
            {
                pen.Alignment = PenAlignment.Inset;
                g.DrawPath(pen, p);
            }
            p.Dispose();
        }

        public static void Text(Graphics g, string s, Font f, Color c, RectangleF r,
            StringAlignment h, StringAlignment v)
        {
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = h;
                sf.LineAlignment = v;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                using (SolidBrush b = new SolidBrush(c))
                {
                    g.DrawString(s, f, b, r, sf);
                }
            }
        }

        public static void TextCenter(Graphics g, string s, Font f, Color c, RectangleF r)
        {
            Text(g, s, f, c, r, StringAlignment.Center, StringAlignment.Center);
        }

        public static void TextLeft(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment v)
        {
            Text(g, s, f, c, r, StringAlignment.Near, v);
        }

        public static void TextRight(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment v)
        {
            Text(g, s, f, c, r, StringAlignment.Far, v);
        }

        /// <summary>带金色辉光的文字</summary>
        public static void Glow(Graphics g, string s, Font f, Color main, Color glow, RectangleF r,
            StringAlignment h, StringAlignment v, float spread)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, glow)))
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        RectangleF rr = new RectangleF(r.X + dx * spread, r.Y + dy * spread, r.Width, r.Height);
                        using (StringFormat sf = new StringFormat())
                        {
                            sf.Alignment = h;
                            sf.LineAlignment = v;
                            sf.FormatFlags = StringFormatFlags.NoWrap;
                            g.DrawString(s, f, b, rr, sf);
                        }
                    }
                }
            }
            Text(g, s, f, main, r, h, v);
        }

        public static Color Alpha(Color c, int a)
        {
            return Color.FromArgb(a, c.R, c.G, c.B);
        }

        /// <summary>细金线</summary>
        public static void HairLine(Graphics g, float x1, float y1, float x2, float y2, int alpha)
        {
            using (Pen pen = new Pen(Color.FromArgb(alpha, GoldDim), 1f))
            {
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }
    }
}
