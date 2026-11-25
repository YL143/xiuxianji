using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Xiuxian
{
    class Ember
    {
        public float X, Y, Size, Speed, Alpha, Drift;
    }

    class FloatText
    {
        public string Text;
        public float X, Y;
        public double Life;
        public Color Color;
    }

    class Ripple
    {
        public float X, Y;
        public double Life;
    }

    /// <summary>
    /// 整窗自绘的游戏画面：背景、面板、按钮、动画与全部交互命中。
    /// 所有坐标用 1180x760 的逻辑空间，绘制时统一乘以缩放系数 S。
    /// </summary>
    public class GameCanvas : Control
    {
        Engine E;
        public float S = 1f;
        Bitmap bg;

        public double T = 0;
        public string Hover = null;
        string pressed = null;

        // 拖拽标题栏
        bool dragging;
        Point dragCursor;
        Point dragForm;

        float clickCd;

        // 弹窗：0无 1离线归来 2坐化 3飞升 4重开确认
        public int Modal = 0;

        List<Ember> embers = new List<Ember>();
        List<FloatText> floats = new List<FloatText>();
        List<Ripple> ripples = new List<Ripple>();
        Random rnd = new Random();

        // ===== 布局（逻辑坐标） =====
        RectangleF closeBtn = new RectangleF(1138, 12, 32, 28);
        RectangleF minBtn = new RectangleF(1098, 12, 32, 28);
        RectangleF cardL = new RectangleF(24, 82, 292, 638);
        RectangleF cardR = new RectangleF(864, 82, 292, 638);
        PointF avatarC = new PointF(170, 168);
        float avatarR = 40;
        RectangleF nameRect = new RectangleF(40, 206, 260, 32);
        RectangleF barRect = new RectangleF(48, 292, 244, 13);
        RectangleF breakBtn = new RectangleF(360, 470, 464, 58);
        RectangleF act1 = new RectangleF(360, 548, 148, 54);
        RectangleF act2 = new RectangleF(518, 548, 148, 54);
        RectangleF act3 = new RectangleF(676, 548, 148, 54);
        RectangleF shop1 = new RectangleF(360, 616, 148, 46);
        RectangleF shop2 = new RectangleF(518, 616, 148, 46);
        RectangleF shop3 = new RectangleF(676, 616, 148, 46);
        RectangleF renameBtn = new RectangleF(48, 668, 84, 28);
        RectangleF resetBtn = new RectangleF(142, 668, 100, 28);
        PointF altarC = new PointF(592, 306);
        float altarR = 128;
        RectangleF modalCard = new RectangleF(290, 200, 600, 340);

        public GameCanvas(Engine engine, float scale)
        {
            E = engine;
            S = scale;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Theme.BgTop;
            TabStop = true;
            Bounds = new Rectangle(0, 0, (int)Math.Round(Theme.W * S), (int)Math.Round(Theme.H * S));
            InitEmbers();
        }

        void InitEmbers()
        {
            embers.Clear();
            for (int i = 0; i < 40; i++)
            {
                Ember e = new Ember();
                e.X = (float)(rnd.NextDouble() * Theme.W);
                e.Y = (float)(rnd.NextDouble() * Theme.H);
                e.Size = (float)(0.8 + rnd.NextDouble() * 1.8);
                e.Speed = (float)(6 + rnd.NextDouble() * 16);
                e.Alpha = (float)(0.2 + rnd.NextDouble() * 0.5);
                e.Drift = (float)(rnd.NextDouble() * 2 - 1) * 6f;
                embers.Add(e);
            }
        }

        // ================= 动画推进 =================

        public void Advance(double dt)
        {
            T += dt;
            if (clickCd > 0) clickCd = (float)Math.Max(0, clickCd - dt);
            if (Shake > 0) Shake = Math.Max(0, Shake - dt * 2.2);

            // 坐化 / 飞升时自动弹出结算
            if (E.EndGame && Modal == 0) Modal = E.P.Ascended ? 3 : 2;

            foreach (Ember e in embers)
            {
                e.Y -= e.Speed * (float)dt;
                e.X += e.Drift * (float)dt;
                if (e.Y < -8) { e.Y = Theme.H + 8; e.X = (float)(rnd.NextDouble() * Theme.W); }
                if (e.X < -8) e.X = Theme.W + 8;
                if (e.X > Theme.W + 8) e.X = -8;
            }

            for (int i = floats.Count - 1; i >= 0; i--)
            {
                FloatText f = floats[i];
                f.Y -= (float)(42 * dt);
                f.Life -= dt;
                if (f.Life <= 0) floats.RemoveAt(i);
            }

            for (int i = ripples.Count - 1; i >= 0; i--)
            {
                ripples[i].Life += dt;
                if (ripples[i].Life > 0.9) ripples.RemoveAt(i);
            }
        }

        // ================= 绘画 =================

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintAll(e.Graphics);
        }

        /// <summary>把整幅画面画到任意 Graphics，便于无窗口渲染预览</summary>
        public void PaintAll(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (bg == null || bg.Width != Width || bg.Height != Height) BuildBackground();

            g.DrawImageUnscaled(bg, 0, 0);
            g.ScaleTransform(S, S);

            if (Shake > 0)
            {
                float amp = (float)(Math.Sin(T * 72) * Shake * 4.5);
                g.TranslateTransform(amp, amp * 0.4f);
            }

            DrawEmbers(g);
            DrawTopBar(g);
            DrawLeft(g);
            DrawRight(g);
            DrawCenter(g);
            DrawFooter(g);
            DrawFloats(g);

            if (E.Flash > 0)
            {
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    new RectangleF(0, 0, Theme.W, Theme.H),
                    Color.FromArgb((int)(E.Flash * 26), 247, 219, 156),
                    Color.FromArgb(0, 247, 219, 156), 90f))
                {
                    g.FillRectangle(lb, 0, 0, Theme.W, Theme.H);
                }
            }

            if (Modal != 0) DrawModal(g);
        }

        void BuildBackground()
        {
            if (bg != null) bg.Dispose();
            bg = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            Graphics g = Graphics.FromImage(bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(S, S);

            RectangleF full = new RectangleF(0, 0, Theme.W, Theme.H);
            using (LinearGradientBrush lb = new LinearGradientBrush(full, Theme.BgTop, Theme.BgBottom, 80f))
            {
                g.FillRectangle(lb, full);
            }

            // 丹炉大面积暖光
            Radial(g, altarC.X, altarC.Y, 480, Color.FromArgb(52, 214, 179, 106));
            Radial(g, 170, 240, 330, Color.FromArgb(22, 214, 179, 106));

            // 底部云纹弧线
            using (Pen pen = new Pen(Color.FromArgb(18, 214, 179, 106), 1f))
            {
                for (int i = 0; i < 5; i++)
                {
                    float r = 260 + i * 90;
                    g.DrawArc(pen, Theme.W / 2 - r, Theme.H - r * 0.55f, r * 2, r * 0.9f, 190, 160);
                }
            }

            // 细颗粒，做出纸面/石面的粗糙感
            Random nr = new Random(20261125);
            for (int i = 0; i < 5200; i++)
            {
                int a = 4 + nr.Next(7);
                float x = (float)(nr.NextDouble() * Theme.W);
                float y = (float)(nr.NextDouble() * Theme.H);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 214, 179, 106)))
                {
                    g.FillRectangle(b, x, y, 1.1f, 1.1f);
                }
            }

            // 暗角
            GraphicsPath vp = new GraphicsPath();
            vp.AddRectangle(full);
            using (PathGradientBrush vb = new PathGradientBrush(vp))
            {
                vb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                vb.SurroundColors = new Color[] { Color.FromArgb(105, 0, 0, 0) };
                vb.CenterPoint = new PointF(Theme.W / 2, Theme.H / 2);
                g.FillPath(vb, vp);
            }
            vp.Dispose();

            g.Dispose();
        }

        static void Radial(Graphics g, float cx, float cy, float radius, Color color)
        {
            GraphicsPath p = new GraphicsPath();
            p.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            using (PathGradientBrush b = new PathGradientBrush(p))
            {
                b.CenterColor = color;
                b.SurroundColors = new Color[] { Color.FromArgb(0, color.R, color.G, color.B) };
                b.CenterPoint = new PointF(cx, cy);
                g.FillPath(b, p);
            }
            p.Dispose();
        }

        void DrawEmbers(Graphics g)
        {
            foreach (Ember e in embers)
            {
                int a = (int)(e.Alpha * 255 * 0.55);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 230, 196, 130)))
                {
                    g.FillEllipse(b, e.X, e.Y, e.Size, e.Size);
                }
            }
        }

        // ---------- 顶栏 ----------

        void DrawTopBar(Graphics g)
        {
            RectangleF bar = new RectangleF(0, 0, Theme.W, 64);
            using (LinearGradientBrush lb = new LinearGradientBrush(bar,
                Color.FromArgb(30, 31, 38), Color.FromArgb(17, 18, 23), 90f))
            {
                g.FillRectangle(lb, bar);
            }
            Theme.HairLine(g, 0, 63.5f, Theme.W, 63.5f, 34);

            // 左侧金色竖条
            using (LinearGradientBrush lb = new LinearGradientBrush(
                new RectangleF(32, 15, 3, 32), Theme.GoldBright, Theme.GoldDeep, 90f))
            {
                g.FillRectangle(lb, 32, 15, 3, 32);
            }

            Theme.TextLeft(g, "修仙纪", Theme.FBrand, Theme.GoldBright,
                new RectangleF(44, 10, 180, 36), StringAlignment.Center);
            Theme.TextLeft(g, "云海问道 · 单机修真", Theme.FBrandSub, Theme.Faint,
                new RectangleF(46, 42, 220, 18), StringAlignment.Near);

            // 右侧状态字
            Theme.TextRight(g, "本地存档 · 无需联网", Theme.FTiny, Theme.Muted,
                new RectangleF(900, 22, 190, 20), StringAlignment.Center);

            // 窗口按钮
            DrawWinButton(g, minBtn, false);
            DrawWinButton(g, closeBtn, true);
        }

        void DrawWinButton(Graphics g, RectangleF r, bool close)
        {
            bool hot = Hover == (close ? "close" : "min");
            if (hot)
            {
                using (SolidBrush b = new SolidBrush(close ? Color.FromArgb(150, 130, 50, 45) : Color.FromArgb(60, 247, 219, 156)))
                {
                    Theme.FillRound(g, r, 6, b);
                }
            }
            using (Pen pen = new Pen(Color.FromArgb(close && hot ? 230 : 110, 214, 179, 106), 1f))
            {
                if (close)
                {
                    g.DrawLine(pen, r.X + 11, r.Y + 10, r.Right - 11, r.Bottom - 10);
                    g.DrawLine(pen, r.Right - 11, r.Y + 10, r.X + 11, r.Bottom - 10);
                }
                else
                {
                    g.DrawLine(pen, r.X + 11, r.Y + r.Height / 2, r.Right - 11, r.Y + r.Height / 2);
                }
            }
        }

        // ---------- 面板底 ----------

        void Panel(Graphics g, RectangleF r)
        {
            RectangleF rr = new RectangleF(r.X, r.Y - 2, r.Width, r.Height + 4);
            using (LinearGradientBrush lb = new LinearGradientBrush(rr, Theme.CardTop, Theme.CardBottom, 90f))
            {
                Theme.FillRound(g, r, 14, lb);
            }
            using (Pen pen = new Pen(Color.FromArgb(120, 247, 219, 156), 1f))
            {
                g.DrawLine(pen, r.X + 20, r.Y + 1.2f, r.Right - 20, r.Y + 1.2f);
            }
            Theme.StrokeRound(g, r, 14, Color.FromArgb(255, 58, 52, 39), 1f);

            // 四角小饰
            using (Pen pen = new Pen(Color.FromArgb(46, 214, 179, 106), 1f))
            {
                float L = 12;
                g.DrawLine(pen, r.X + 9, r.Y + 9, r.X + 9 + L, r.Y + 9);
                g.DrawLine(pen, r.X + 9, r.Y + 9, r.X + 9, r.Y + 9 + L);
                g.DrawLine(pen, r.Right - 9, r.Y + 9, r.Right - 9 - L, r.Y + 9);
                g.DrawLine(pen, r.Right - 9, r.Y + 9, r.Right - 9, r.Y + 9 + L);
                g.DrawLine(pen, r.X + 9, r.Bottom - 9, r.X + 9 + L, r.Bottom - 9);
                g.DrawLine(pen, r.X + 9, r.Bottom - 9, r.X + 9, r.Bottom - 9 - L);
                g.DrawLine(pen, r.Right - 9, r.Bottom - 9, r.Right - 9 - L, r.Bottom - 9);
                g.DrawLine(pen, r.Right - 9, r.Bottom - 9, r.Right - 9, r.Bottom - 9 - L);
            }
        }

        // ---------- 左：修士 ----------

        void DrawLeft(Graphics g)
        {
            Panel(g, cardL);
            Player p = E.P;

            // 头像
            float pulse = (float)(Math.Sin(T * 1.6) * 0.5 + 0.5);
            if (p.Meditating && !E.EndGame)
            {
                using (Pen pen = new Pen(Color.FromArgb((int)(40 + pulse * 40), 214, 179, 106), 3f))
                {
                    g.DrawEllipse(pen, avatarC.X - avatarR - 5, avatarC.Y - avatarR - 5,
                        (avatarR + 5) * 2, (avatarR + 5) * 2);
                }
            }

            RectangleF ac = new RectangleF(avatarC.X - avatarR, avatarC.Y - avatarR, avatarR * 2, avatarR * 2);
            GraphicsPath cp = new GraphicsPath();
            cp.AddEllipse(ac);
            using (PathGradientBrush pb = new PathGradientBrush(cp))
            {
                pb.CenterColor = Color.FromArgb(255, 45, 37, 24);
                pb.SurroundColors = new Color[] { Color.FromArgb(255, 15, 14, 12) };
                pb.CenterPoint = new PointF(avatarC.X, avatarC.Y - 8);
                g.FillPath(pb, cp);
            }
            using (Pen pen = new Pen(Color.FromArgb(200, 214, 179, 106), 1.6f))
            {
                g.DrawEllipse(pen, ac);
            }
            using (Pen pen = new Pen(Color.FromArgb(70, 214, 179, 106), 1f))
            {
                g.DrawEllipse(pen, ac.X + 6, ac.Y + 6, ac.Width - 12, ac.Height - 12);
            }
            string ch = p.Name.Length > 0 ? p.Name.Substring(0, 1) : "道";
            Theme.Glow(g, ch, Theme.FAvatarChar, Theme.GoldBright,
                Theme.Gold, new RectangleF(ac.X, ac.Y - 2, ac.Width, ac.Height),
                StringAlignment.Center, StringAlignment.Center, 1.2f);

            // 道号
            Theme.TextCenter(g, p.Name, Theme.FName, Theme.Ink, nameRect);

            // 境界徽章
            string title = G.Title(p.Realm, p.Layer);
            float tw = Measure(g, title, Theme.FBadge) + 30;
            RectangleF badge = new RectangleF(170 - tw / 2, 241, tw, 22);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 30, 27, 20)))
            {
                Theme.FillRound(g, badge, 11, b);
            }
            Theme.StrokeRound(g, badge, 11, Color.FromArgb(150, 214, 179, 106), 1f);
            Theme.TextCenter(g, title, Theme.FBadge, Theme.Gold, badge);
            if (p.Reincarnation > 0)
            {
                Theme.TextCenter(g, "第 " + (p.Reincarnation + 1) + " 世", Theme.FTiny,
                    Theme.Faint, new RectangleF(badge.Right + 4, 243, 60, 18));
            }

            // 修为
            double need = E.Need;
            double pct = need > 0 ? Math.Min(1.0, p.Exp / need) : 1;
            Theme.TextLeft(g, "修为", Theme.FLabel, Theme.Muted, new RectangleF(48, 272, 100, 18), StringAlignment.Near);
            Theme.TextRight(g, (pct * 100).ToString("0.0") + "%", Theme.FLabel,
                p.Layer == 9 && pct >= 1 ? Theme.Crimson : Theme.Gold,
                new RectangleF(196, 272, 96, 18), StringAlignment.Near);

            RectangleF barBg = barRect;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 34, 30, 23)))
            {
                Theme.FillRound(g, barBg, 6.5f, b);
            }
            float w = (float)(pct * barBg.Width);
            if (w > 2)
            {
                RectangleF fill = new RectangleF(barBg.X, barBg.Y, w, barBg.Height);
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    new RectangleF(barBg.X, barBg.Y, barBg.Width, barBg.Height),
                    Theme.GoldBright, Color.FromArgb(255, 146, 110, 52), 0f))
                {
                    Theme.FillRound(g, fill, 6.5f, lb);
                }
                using (Pen pen = new Pen(Color.FromArgb(70, 255, 244, 214), 1f))
                {
                    g.DrawLine(pen, fill.X + 4, fill.Y + 2, Math.Max(fill.X + 4, fill.Right - 4), fill.Y + 2);
                }
            }
            Theme.StrokeRound(g, barBg, 6.5f, Color.FromArgb(90, 214, 179, 106), 1f);

            Theme.TextLeft(g, G.Num(p.Exp) + " / " + G.Num(need), Theme.FTiny, Theme.Muted,
                new RectangleF(48, 309, 150, 18), StringAlignment.Near);
            string remain = "";
            if (E.BreakReady) remain = "瓶颈已满";
            else if (E.Rate > 0)
            {
                double sec = Math.Max(0, (need - p.Exp) / E.Rate);
                remain = "剩 " + G.Dur(sec);
            }
            Theme.TextRight(g, remain, Theme.FTiny,
                E.BreakReady ? Theme.Gold : Theme.Faint,
                new RectangleF(190, 309, 102, 18), StringAlignment.Near);

            Theme.HairLine(g, 48, 340, 268, 340, 40);

            // 属性四行
            float y = 352;
            Stat(g, ref y, "石", "灵石", G.Num(p.Stones) + " 枚");
            Stat(g, ref y, "悟", "悟性", p.Insight + " 点");
            Stat(g, ref y, "轮", "轮回", p.Reincarnation == 0 ? "初世" : "第 " + (p.Reincarnation + 1) + " 世");
            Stat(g, ref y, "寿", "年岁", ((int)E.AgeYears) + " / " + ((int)E.LifeSpan) + " 载");

            Theme.HairLine(g, 48, 522, 268, 522, 40);

            // 状态
            Theme.TextLeft(g, "状态", Theme.FLabel, Theme.Muted, new RectangleF(48, 532, 100, 18), StringAlignment.Near);

            float cx = 48;
            float cy = 554;
            if (p.Dead) Chip(g, ref cx, ref cy, "已坐化", Theme.Crimson, true);
            else if (p.Ascended) Chip(g, ref cx, ref cy, "已飞升", Theme.GoldBright, true);
            else if (p.Meditating) Chip(g, ref cx, ref cy, "打坐 ×1.6", Theme.Gold, true);
            else Chip(g, ref cx, ref cy, "散修 ×1.0", Theme.Muted, false);

            if (p.BuffLeft > 0) Chip(g, ref cx, ref cy, "丹药 ×1.5 " + Cd(p.BuffLeft), Theme.Jade, true);
            if (p.WoundLeft > 0) Chip(g, ref cx, ref cy, "带伤 ×0.5 " + Cd(p.WoundLeft), Theme.Crimson, true);
            if (p.BreakPills > 0) Chip(g, ref cx, ref cy, "破境丹 ×" + p.BreakPills, Theme.Gold, true);

            // 底部题句
            Theme.TextCenter(g, "大道无形，生育天地", Theme.FVerse, Color.FromArgb(105, 214, 179, 106),
                new RectangleF(48, 616, 220, 22));
            Theme.TextCenter(g, "大道无情，运行日月", Theme.FVerse, Color.FromArgb(105, 214, 179, 106),
                new RectangleF(48, 638, 220, 22));

            // 底部小按钮
            GhostButton(g, renameBtn, "改道号", Hover == "rename", pressed == "rename");
            GhostButton(g, resetBtn, "重开一世", Hover == "reset", pressed == "reset");
        }

        void Stat(Graphics g, ref float y, string ico, string label, string value)
        {
            RectangleF row = new RectangleF(48, y, 220, 34);
            RectangleF ic = new RectangleF(row.X, row.Y + 6, 22, 22);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 32, 29, 22)))
            {
                g.FillEllipse(b, ic);
            }
            using (Pen pen = new Pen(Color.FromArgb(110, 214, 179, 106), 1f))
            {
                g.DrawEllipse(pen, ic);
            }
            Theme.TextCenter(g, ico, Theme.FStatIcon, Theme.Gold,
                new RectangleF(ic.X, ic.Y - 1, ic.Width, ic.Height));

            Theme.TextLeft(g, label, Theme.FLabel, Theme.Muted,
                new RectangleF(row.X + 32, row.Y + 2, 80, 26), StringAlignment.Center);
            Theme.TextRight(g, value, Theme.FValue, Theme.Ink,
                new RectangleF(row.X + 100, row.Y + 2, 120, 26), StringAlignment.Center);

            y += 42;
        }

        /// <summary>小胶囊标签；换行时自动落到下一行</summary>
        void Chip(Graphics g, ref float x, ref float y, string text, Color color, bool strong)
        {
            float w = Measure(g, text, Theme.FTiny) + 18;
            if (x + w > 268 && x > 48) { x = 48; y += 24; }
            RectangleF r = new RectangleF(x, y, w, 20);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(strong ? 26 : 16, color.R, color.G, color.B)))
            {
                Theme.FillRound(g, r, 10, b);
            }
            Theme.StrokeRound(g, r, 10, Color.FromArgb(strong ? 130 : 70, color.R, color.G, color.B), 1f);
            Theme.TextCenter(g, text, Theme.FTiny, color, r);
            x += w + 6;
        }

        void GhostButton(Graphics g, RectangleF r, string text, bool hot, bool down)
        {
            Color c = hot ? Theme.Gold : Theme.Faint;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(hot ? 26 : 14, 214, 179, 106)))
            {
                Theme.FillRound(g, r, 8, b);
            }
            Theme.StrokeRound(g, r, 8, Color.FromArgb(hot ? 120 : 55, 214, 179, 106), 1f);
            Theme.TextCenter(g, text, Theme.FTiny, down ? Theme.GoldBright : c, r);
        }

        // ---------- 右：日志 ----------

        void DrawRight(Graphics g)
        {
            Panel(g, cardR);
            Theme.TextLeft(g, "修行日志", Theme.FLogTitle, Theme.Ink,
                new RectangleF(886, 96, 150, 24), StringAlignment.Center);

            float dot = (float)(Math.Sin(T * 2) * 0.5 + 0.5);
            using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(120 + dot * 100), 124, 195, 168)))
            {
                g.FillEllipse(b, 1086, 103, 6, 6);
            }
            Theme.TextLeft(g, "实时", Theme.FTiny, Theme.Faint, new RectangleF(1096, 96, 40, 20), StringAlignment.Center);

            Theme.HairLine(g, 886, 128, 1134, 128, 40);

            float y = 136;
            int shown = 0;
            foreach (LogEntry le in E.Logs)
            {
                if (shown >= 11) break;
                Color c;
                switch (le.Kind)
                {
                    case LogKind.Good: c = Theme.Jade; break;
                    case LogKind.Gold: c = Theme.GoldBright; break;
                    case LogKind.Bad: c = Theme.Crimson; break;
                    case LogKind.Sys: c = Color.FromArgb(158, 168, 180); break;
                    default: c = Theme.Muted; break;
                }
                using (SolidBrush b = new SolidBrush(Color.FromArgb(200, c.R, c.G, c.B)))
                {
                    g.FillEllipse(b, 886, y + 4, 4, 4);
                }
                Theme.TextLeft(g, le.Stamp, Theme.FLogStamp, Theme.Faint,
                    new RectangleF(896, y - 1, 90, 14), StringAlignment.Near);
                DrawWrapped(g, le.Text, Theme.FLog, c, new RectangleF(896, y + 14, 238, 32), 2);
                y += 52;
                shown++;
            }

            if (shown == 0)
            {
                Theme.TextCenter(g, "尚无记载", Theme.FLog, Theme.Faint, new RectangleF(886, 320, 250, 30));
            }
        }

        static Dictionary<string, List<string>> wrapCache = new Dictionary<string, List<string>>();

        void DrawWrapped(Graphics g, string text, Font f, Color c, RectangleF r, int maxLines)
        {
            string key = text + "|" + maxLines + "|" + (int)r.Width;
            List<string> lines;
            if (!wrapCache.TryGetValue(key, out lines))
            {
                lines = Wrap(g, text, f, r.Width, maxLines);
                if (wrapCache.Count > 300) wrapCache.Clear();
                wrapCache[key] = lines;
            }
            float lh = f.GetHeight(g) + 0.5f;
            for (int i = 0; i < lines.Count; i++)
            {
                Theme.TextLeft(g, lines[i], f, c,
                    new RectangleF(r.X, r.Y + i * lh, r.Width, lh + 2), StringAlignment.Near);
            }
        }

        static List<string> Wrap(Graphics g, string s, Font f, float width, int maxLines)
        {
            List<string> res = new List<string>();
            int i = 0;
            while (i < s.Length && res.Count < maxLines)
            {
                int n = 1;
                while (i + n <= s.Length && g.MeasureString(s.Substring(i, n), f).Width <= width)
                    n++;
                n--;
                if (n < 1) n = 1;
                // 尽量在标点处断行，避免把数字/词拆开
                for (int k = n; k > Math.Max(1, n - 6); k--)
                {
                    if (i + k - 1 < s.Length && "，。、；：！？）".IndexOf(s[i + k - 1]) >= 0)
                    {
                        n = k;
                        break;
                    }
                }
                if (res.Count == maxLines - 1 && i + n < s.Length)
                {
                    string rest = s.Substring(i);
                    while (rest.Length > 0 && g.MeasureString(rest + "…", f).Width > width)
                        rest = rest.Substring(0, rest.Length - 1);
                    res.Add(rest + "…");
                    return res;
                }
                res.Add(s.Substring(i, n));
                i += n;
            }
            return res;
        }

        // ---------- 中：丹田 ----------

        void DrawCenter(Graphics g)
        {
            // 顶部状态胶囊
            float cx = 592;
            List<string> chips = new List<string>();
            List<Color> colors = new List<Color>();
            chips.Add("吐纳 +" + G.Num(E.Rate) + "/秒");
            colors.Add(Theme.Gold);
            if (E.P.Meditating) { chips.Add("打坐 ×1.6"); colors.Add(Theme.Gold); }
            if (E.P.BuffLeft > 0) { chips.Add("聚气 ×1.5 " + Cd(E.P.BuffLeft)); colors.Add(Theme.Jade); }
            if (E.P.Reincarnation > 0) { chips.Add("道韵 +" + (E.P.Reincarnation * 6) + "%"); colors.Add(Theme.Jade); }
            if (E.P.WoundLeft > 0) { chips.Add("带伤 ×0.5 " + Cd(E.P.WoundLeft)); colors.Add(Theme.Crimson); }
            if (E.P.BreakPills > 0) { chips.Add("破境丹 ×" + E.P.BreakPills); colors.Add(Theme.Gold); }

            float total = 0;
            for (int i = 0; i < chips.Count; i++) total += Measure(g, chips[i], Theme.FTiny) + 24;
            float x = cx - total / 2;
            float y = 92;
            for (int i = 0; i < chips.Count; i++)
            {
                float w = Measure(g, chips[i], Theme.FTiny) + 18;
                if (x + w > 824) { x = 360; y += 25; }
                RectangleF r = new RectangleF(x, y, w, 22);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(20, colors[i].R, colors[i].G, colors[i].B)))
                {
                    Theme.FillRound(g, r, 11, b);
                }
                Theme.StrokeRound(g, r, 11, Color.FromArgb(100, colors[i].R, colors[i].G, colors[i].B), 1f);
                Theme.TextCenter(g, chips[i], Theme.FTiny, colors[i], r);
                x += w + 6;
            }

            DrawAltar(g);

            // 突破
            DrawBreak(g);

            // 行动
            bool h1 = Hover == "harvest", h2 = Hover == "adventure", h3 = Hover == "toggle";
            bool p1 = pressed == "harvest", p2 = pressed == "adventure", p3 = pressed == "toggle";
            DrawAction(g, act1, "采药", E.P.HarvestCd > 0 ? "还需 " + Cd(E.P.HarvestCd) : "灵石 +" + G.Num(8 + E.Index * 6) + " 上下",
                E.P.HarvestCd <= 0 && !E.EndGame, h1, p1, false, E.P.HarvestCd > 0 ? 1 - E.P.HarvestCd / 45.0 : 1);
            DrawAction(g, act2, "历练", E.P.AdventureCd > 0 ? "还需 " + Cd(E.P.AdventureCd) : "机缘与风险并存",
                E.P.AdventureCd <= 0 && !E.EndGame, h2, p2, false, E.P.AdventureCd > 0 ? 1 - E.P.AdventureCd / 150.0 : 1);
            DrawAction(g, act3, "打坐加持", E.P.Meditating ? "已开启 · ×1.6" : "未开启 · ×1.0",
                !E.EndGame, h3, p3, E.P.Meditating, 1);

            // 商铺
            DrawShop(g, shop1, "聚气丹", G.Num(E.PillGasCost) + " 灵石 · ×1.5/12分",
                E.P.Stones >= E.PillGasCost && !E.EndGame, "pill_gas");
            DrawShop(g, shop2, "破境丹", G.Num(E.PillBreakCost) + " 灵石 · 破境 +20%",
                E.P.Stones >= E.PillBreakCost && !E.EndGame, "pill_break");
            DrawShop(g, shop3, "悟道茶", G.Num(E.TeaCost) + " 灵石 · 悟性 +1",
                E.P.Stones >= E.TeaCost && !E.EndGame, "tea");

            Theme.TextCenter(g, "空格打坐 · B 键突破 · 关窗后离线仍会结算", Theme.FHint, Theme.Faint,
                new RectangleF(360, 680, 464, 20));
        }

        void DrawAltar(Graphics g)
        {
            PointF c = altarC;
            float scale = 1f;
            if (pressed == "meditate") scale = 0.965f;
            float R = altarR * scale;

            float breathe = (float)(Math.Sin(T * 1.8) * 0.5 + 0.5);
            int glowA = E.P.Meditating ? (int)(26 + breathe * 22) : 12;
            Radial(g, c.X, c.Y, R * 1.65f, Color.FromArgb(glowA, 214, 179, 106));

            // 三圈符环
            float[] radii = { R, R - 16, R - 34 };
            float[] speeds = { 0.16f, -0.11f, 0.07f };
            int[] alphas = { 70, 50, 40 };
            for (int i = 0; i < radii.Length; i++)
            {
                float rr = radii[i];
                float ang = (float)(T * speeds[i]);
                int n = i == 0 ? 48 : (i == 1 ? 36 : 24);
                using (Pen pen = new Pen(Color.FromArgb(alphas[i], 214, 179, 106), i == 0 ? 1.4f : 1f))
                {
                    for (int k = 0; k < n; k++)
                    {
                        float a0 = ang + k * (360f / n);
                        float a1 = a0 + (360f / n) * (i == 0 ? 0.55f : 0.4f);
                        g.DrawArc(pen, c.X - rr, c.Y - rr, rr * 2, rr * 2, a0, a1 - a0);
                    }
                }
            }

            // 八方符纹：每方三道短划，缓速流转
            for (int k = 0; k < 8; k++)
            {
                double ang = k * Math.PI / 4 + T * 0.05;
                float rr = R - 7;
                float px0 = c.X + (float)Math.Cos(ang) * rr;
                float py0 = c.Y + (float)Math.Sin(ang) * rr;
                float tx = (float)Math.Cos(ang + Math.PI / 2);
                float ty = (float)Math.Sin(ang + Math.PI / 2);
                using (Pen pen = new Pen(Color.FromArgb(95, 214, 179, 106), 1.1f))
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        float px = px0 + tx * j * 3.2f;
                        float py = py0 + ty * j * 3.2f;
                        g.DrawLine(pen, px - tx * 5f, py - ty * 5f, px + tx * 5f, py + ty * 5f);
                    }
                }
            }

            // 内盘
            RectangleF disc = new RectangleF(c.X - R + 34, c.Y - R + 34, (R - 34) * 2, (R - 34) * 2);
            GraphicsPath dp = new GraphicsPath();
            dp.AddEllipse(disc);
            using (PathGradientBrush pb = new PathGradientBrush(dp))
            {
                pb.CenterColor = Color.FromArgb(255, 43, 36, 24);
                pb.SurroundColors = new Color[] { Color.FromArgb(255, 14, 13, 12) };
                pb.CenterPoint = new PointF(c.X, c.Y - 6);
                g.FillPath(pb, dp);
            }
            float rr2 = (R - 34);
            using (Pen pen = new Pen(Color.FromArgb(190, 214, 179, 106), 1.6f))
            {
                g.DrawEllipse(pen, c.X - rr2, c.Y - rr2, rr2 * 2, rr2 * 2);
            }
            using (Pen pen = new Pen(Color.FromArgb(60, 247, 219, 156), 1f))
            {
                g.DrawEllipse(pen, c.X - rr2 + 9, c.Y - rr2 + 9, (rr2 - 9) * 2, (rr2 - 9) * 2);
            }

            // 中心字与提示
            bool dead = E.EndGame;
            Color main = dead ? Theme.Faint : Theme.GoldBright;
            Theme.Glow(g, dead ? "已止" : "打坐", Theme.FAltar, main, Theme.Gold,
                new RectangleF(c.X - 90, c.Y - 40, 180, 46), StringAlignment.Center, StringAlignment.Center, 1.4f);
            Theme.TextCenter(g, dead ? "此生已尽" : "点击凝聚灵气", Theme.FAltarSub, Theme.Muted,
                new RectangleF(c.X - 90, c.Y + 8, 180, 20));

            // 点击涟漪
            foreach (Ripple rp in ripples)
            {
                float a = (float)(1 - rp.Life / 0.9);
                float radius = (float)(76 + rp.Life * 90);
                using (Pen pen = new Pen(Color.FromArgb((int)(a * 110), 247, 219, 156), 2f))
                {
                    g.DrawEllipse(pen, rp.X - radius, rp.Y - radius, radius * 2, radius * 2);
                }
            }

            // 打坐时的灵气流转
            if (E.P.Meditating && !dead)
            {
                for (int i = 0; i < 3; i++)
                {
                    float ang = (float)(T * 0.9 + i * Math.PI * 2 / 3);
                    float rad = R - 22;
                    float px = c.X + (float)Math.Cos(ang) * rad;
                    float py = c.Y + (float)Math.Sin(ang) * rad * 0.42f;
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 247, 219, 156)))
                    {
                        g.FillEllipse(b, px - 2.5f, py - 2.5f, 5, 5);
                    }
                }
            }
        }

        void DrawBreak(Graphics g)
        {
            bool ready = E.BreakReady;
            bool hot = Hover == "break";
            bool down = pressed == "break";
            RectangleF r = breakBtn;
            bool maxed = E.IsMaxRealm && ready;

            if (ready)
            {
                Color top = hot ? Color.FromArgb(255, 240, 205, 133) : Theme.GoldBright;
                Color bot = hot ? Color.FromArgb(255, 176, 133, 62) : Color.FromArgb(255, 146, 110, 52);
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2), top, bot, 90f))
                {
                    Theme.FillRound(g, r, 12, lb);
                }
                using (Pen pen = new Pen(Color.FromArgb(220, 255, 240, 200), 1f))
                {
                    g.DrawPath(pen, Theme.Round(r, 12));
                }
                int a = (int)(18 + (Math.Sin(T * 2.4) * 0.5 + 0.5) * 26);
                using (Pen pen = new Pen(Color.FromArgb(a, 247, 219, 156), 6f))
                {
                    g.DrawPath(pen, Theme.Round(new RectangleF(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), 15));
                }

                string title = maxed ? "飞升 · 天门将开" : "冲击 " + G.NextRealm(E.P.Realm) + "境";
                Theme.TextCenter(g, title, Theme.FBreakTitle, Color.FromArgb(255, 42, 31, 16),
                    new RectangleF(r.X, r.Y + 6, r.Width, 28));
                string sub = string.Format("成功率 {0:0}% · 失败折损半数修为{1}", E.BreakChance * 100,
                    E.P.BreakPills > 0 ? " · 已含破境丹" : "");
                Theme.TextCenter(g, sub, Theme.FBtnSub, Color.FromArgb(230, 62, 46, 24),
                    new RectangleF(r.X, r.Y + 34, r.Width, 18));
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 26, 24, 20)))
                {
                    Theme.FillRound(g, r, 12, b);
                }
                Theme.StrokeRound(g, r, 12, Color.FromArgb(90, 214, 179, 106), 1f);
                string title, sub;
                if (E.EndGame) { title = "此生已尽"; sub = "转世重修，重开道途"; }
                else if (E.P.Layer == 9)
                {
                    double left = Math.Max(0, E.Need - E.P.Exp);
                    title = "九层圆满方可破境";
                    sub = "尚需 " + G.Num(left) + " 修为 · 约 " + G.Dur(left / Math.Max(0.001, E.Rate));
                }
                else
                {
                    title = "破境 · 需九层圆满";
                    sub = "当前 " + G.Title(E.P.Realm, E.P.Layer) + " · 继续吐纳自会晋层";
                }
                Theme.TextCenter(g, title, Theme.FBreakIdle, Theme.GoldDim,
                    new RectangleF(r.X, r.Y + 10, r.Width, 26));
                Theme.TextCenter(g, sub, Theme.FBtnSub, Theme.Faint,
                    new RectangleF(r.X, r.Y + 37, r.Width, 16));
            }
        }

        void DrawAction(Graphics g, RectangleF r, string title, string sub, bool enabled,
            bool hot, bool down, bool active, double progress)
        {
            using (LinearGradientBrush lb = new LinearGradientBrush(
                new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2),
                Color.FromArgb(255, 34, 33, 38), Color.FromArgb(255, 21, 21, 25), 90f))
            {
                Theme.FillRound(g, r, 10, lb);
            }
            Color border = active ? Color.FromArgb(200, 214, 179, 106) : (hot && enabled ? Color.FromArgb(180, 214, 179, 106) : Color.FromArgb(70, 214, 179, 106));
            Theme.StrokeRound(g, r, 10, border, 1f);
            if (active)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(26, 214, 179, 106)))
                {
                    Theme.FillRound(g, r, 10, b);
                }
            }
            Color tc = enabled ? (active ? Theme.GoldBright : Theme.Ink) : Theme.Faint;
            Theme.TextCenter(g, title, Theme.FBtn, tc, new RectangleF(r.X, r.Y + 7, r.Width, 22));
            Theme.TextCenter(g, sub, Theme.FBtnSub, enabled ? Theme.Muted : Theme.Faint,
                new RectangleF(r.X, r.Y + 30, r.Width, 16));

            if (progress < 1 && progress > 0)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(90, 214, 179, 106)))
                {
                    Theme.FillRound(g, new RectangleF(r.X + 8, r.Bottom - 5, (r.Width - 16) * (float)progress, 2), 1, b);
                }
            }
        }

        void DrawShop(Graphics g, RectangleF r, string title, string sub, bool enabled, string id)
        {
            bool hot = Hover == id;
            bool down = pressed == id;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 22, 22, 26)))
            {
                Theme.FillRound(g, r, 9, b);
            }
            Theme.StrokeRound(g, r, 9, Color.FromArgb(hot && enabled ? 160 : 55, 214, 179, 106), 1f);
            if (hot && enabled && !down)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(22, 214, 179, 106)))
                {
                    Theme.FillRound(g, r, 9, b);
                }
            }
            Theme.TextLeft(g, title, Theme.FShopTitle, enabled ? Theme.Gold : Theme.Faint,
                new RectangleF(r.X + 12, r.Y + 5, r.Width - 20, 18), StringAlignment.Near);
            Theme.TextLeft(g, sub, Theme.FTiny, enabled ? Theme.Muted : Theme.Faint,
                new RectangleF(r.X + 12, r.Y + 24, r.Width - 16, 16), StringAlignment.Near);
        }

        void DrawFooter(Graphics g)
        {
            string left = "存档  " + (E.SavePath.Length > 0 ? E.SavePath : "save\\xiuxian.sav");
            if (E.SaveError.Length > 0) left = "存档异常：" + E.SaveError;
            Theme.TextLeft(g, left, Theme.FHint, E.SaveError.Length > 0 ? Theme.Crimson : Theme.Faint,
                new RectangleF(24, 732, 700, 18), StringAlignment.Near);
            Theme.TextRight(g, "离线结算 ×0.8 · 上限 8 小时 · 全部数据仅存本机", Theme.FHint, Theme.Faint,
                new RectangleF(760, 732, 396, 18), StringAlignment.Near);
        }

        void DrawFloats(Graphics g)
        {
            foreach (FloatText f in floats)
            {
                int a = (int)(Math.Min(1.0, f.Life / 0.6) * 255);
                Color c = Color.FromArgb(a, f.Color.R, f.Color.G, f.Color.B);
                Theme.TextCenter(g, f.Text, Theme.FFloating, c,
                    new RectangleF(f.X - 90, f.Y, 180, 24));
            }
        }

        // ---------- 弹窗 ----------

        void DrawModal(Graphics g)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(185, 4, 4, 6)))
            {
                g.FillRectangle(b, 0, 0, Theme.W, Theme.H);
            }

            RectangleF r = modalCard;
            using (LinearGradientBrush lb = new LinearGradientBrush(r, Theme.CardTop, Color.FromArgb(13, 13, 17), 90f))
            {
                Theme.FillRound(g, r, 16, lb);
            }
            Theme.StrokeRound(g, r, 16, Color.FromArgb(190, 214, 179, 106), 1.4f);

            string title = "";
            string[] lines = new string[0];
            string btn = "";
            Color titleColor = Theme.GoldBright;

            if (Modal == 1)
            {
                title = "闭关归来";
                titleColor = Theme.GoldBright;
                OfflineReport o = E.Offline;
                List<string> ls = new List<string>();
                if (o != null)
                {
                    ls.Add("你离开洞府 " + G.Dur(o.Seconds));
                    ls.Add("静悟所得修为 " + G.Num(o.Exp));
                    if (o.Levels > 0) ls.Add("连晋 " + o.Levels + " 层");
                    if (o.Capped) ls.Add("离线结算以 8 小时为限");
                }
                else ls.Add("神识归位，继续修行。");
                lines = ls.ToArray();
                btn = "收入丹田";
            }
            else if (Modal == 2)
            {
                title = "红尘坐化";
                titleColor = Theme.Crimson;
                lines = new string[]
                {
                    "寿元枯竭，你于蒲团上阖目而坐，享年 " + (int)E.AgeYears + " 岁。",
                    "此世修至 " + G.Title(E.P.Realm, E.P.Layer) + "，共历 " + G.Dur(E.P.TotalSeconds) + "。",
                    "转世重修：道韵 +" + ((E.P.Reincarnation + 1) * 6) + "%，吐纳永久加快，悟性保留。"
                };
                btn = "转世重修";
            }
            else if (Modal == 3)
            {
                title = "白日飞升";
                titleColor = Theme.GoldBright;
                lines = new string[]
                {
                    "九霄雷云散尽，天门洞开，金光垂落如瀑。",
                    "你以 " + G.Title(E.P.Realm, E.P.Layer) + " 之身踏光而上，位列仙班。",
                    "此世历时 " + G.Dur(E.P.TotalSeconds) + "，共历 " + (E.P.Reincarnation + 1) + " 世。",
                    "转世重修可再走一遭，道韵 +" + ((E.P.Reincarnation + 1) * 6) + "%。"
                };
                btn = "转世重修";
            }
            else if (Modal == 4)
            {
                title = "重开一世";
                titleColor = Theme.Crimson;
                lines = new string[]
                {
                    "将清空修为、境界、灵石、悟性与轮回道韵。",
                    "此操作不可撤销。",
                };
                btn = "确定重开";
            }

            Theme.TextCenter(g, title, Theme.FModal, titleColor,
                new RectangleF(r.X, r.Y + 26, r.Width, 40));

            // 标题下的分隔装饰
            Theme.HairLine(g, r.X + 150, r.Y + 78, r.X + r.Width / 2 - 18, r.Y + 78, 60);
            Theme.HairLine(g, r.X + r.Width / 2 + 18, r.Y + 78, r.Right - 150, r.Y + 78, 60);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 214, 179, 106)))
            {
                g.FillEllipse(b, r.X + r.Width / 2 - 3, r.Y + 75, 6, 6);
            }

            float y = r.Y + 100;
            foreach (string s in lines)
            {
                Theme.TextCenter(g, s, Theme.FModalBody, Color.FromArgb(214, 206, 190),
                    new RectangleF(r.X + 40, y, r.Width - 80, 24));
                y += 27;
            }

            if (Modal == 4)
            {
                RectangleF cancel = new RectangleF(600, 462, 150, 44);
                DrawModalButton(g, cancel, "取消", false, Hover == "modal_cancel", pressed == "modal_cancel");
                DrawDangerButton(g, new RectangleF(430, 462, 150, 44), btn, Hover == "modal_ok");
            }
            else
            {
                DrawModalButton(g, new RectangleF(490, 462, 200, 44), btn, true,
                    Hover == "modal_ok", pressed == "modal_ok");
            }
        }

        void DrawDangerButton(Graphics g, RectangleF r, string text, bool hot)
        {
            using (LinearGradientBrush lb = new LinearGradientBrush(
                new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2),
                hot ? Color.FromArgb(255, 172, 88, 74) : Color.FromArgb(255, 142, 70, 58),
                Color.FromArgb(255, 104, 48, 41), 90f))
            {
                Theme.FillRound(g, r, 10, lb);
            }
            Theme.StrokeRound(g, r, 10, Color.FromArgb(150, 220, 140, 120), 1f);
            Theme.TextCenter(g, text, Theme.FBtn, Color.FromArgb(255, 253, 240, 230), r);
        }

        void DrawModalButton(Graphics g, RectangleF r, string text, bool primary, bool hot, bool down)
        {
            if (primary)
            {
                using (LinearGradientBrush lb = new LinearGradientBrush(
                    new RectangleF(r.X, r.Y - 1, r.Width, r.Height + 2),
                    hot ? Color.FromArgb(255, 240, 205, 133) : Theme.GoldBright,
                    Color.FromArgb(255, 152, 116, 56), 90f))
                {
                    Theme.FillRound(g, r, 10, lb);
                }
                Theme.TextCenter(g, text, Theme.FBtn, Color.FromArgb(255, 44, 33, 18), r);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 30, 29, 34)))
                {
                    Theme.FillRound(g, r, 10, b);
                }
                Theme.StrokeRound(g, r, 10, Color.FromArgb(hot ? 150 : 80, 214, 179, 106), 1f);
                Theme.TextCenter(g, text, Theme.FBtn, hot ? Theme.Gold : Theme.Muted, r);
            }
        }

        /// <summary>离屏渲染成一张图片，用于无窗口预览与截图</summary>
        public Bitmap RenderToBitmap()
        {
            Bitmap bmp = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Theme.BgTop);
                PaintAll(g);
            }
            return bmp;
        }

        // ================= 交互 =================

        PointF Logical(Point p)
        {
            return new PointF(p.X / S, p.Y / S);
        }

        public void ApplyModal()
        {
            Invalidate();
        }

        string HitTest(PointF p)
        {
            if (Modal != 0)
            {
                RectangleF ok = Modal == 4 ? new RectangleF(430, 462, 150, 44) : new RectangleF(490, 462, 200, 44);
                if (ok.Contains(p)) return "modal_ok";
                if (Modal == 4 && new RectangleF(600, 462, 150, 44).Contains(p)) return "modal_cancel";
                return null;
            }
            if (closeBtn.Contains(p)) return "close";
            if (minBtn.Contains(p)) return "min";
            if (E.EndGame)
            {
                // 结束后只允许转世：直接点中央
                float dx0 = p.X - altarC.X, dy0 = p.Y - altarC.Y;
                if (dx0 * dx0 + dy0 * dy0 <= altarR * altarR) return "meditate";
                if (renameBtn.Contains(p)) return "rename";
                if (resetBtn.Contains(p)) return "reset";
                return null;
            }
            if (renameBtn.Contains(p)) return "rename";
            if (resetBtn.Contains(p)) return "reset";
            if (breakBtn.Contains(p)) return "break";
            if (act1.Contains(p)) return "harvest";
            if (act2.Contains(p)) return "adventure";
            if (act3.Contains(p)) return "toggle";
            if (shop1.Contains(p)) return "pill_gas";
            if (shop2.Contains(p)) return "pill_break";
            if (shop3.Contains(p)) return "tea";
            float dx = p.X - altarC.X, dy = p.Y - altarC.Y;
            if (dx * dx + dy * dy <= altarR * altarR) return "meditate";
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            PointF p = Logical(e.Location);
            if (dragging)
            {
                Form f = FindForm();
                if (f != null)
                {
                    Point now = Cursor.Position;
                    f.Location = new Point(dragForm.X + (now.X - dragCursor.X), dragForm.Y + (now.Y - dragCursor.Y));
                }
                return;
            }
            string h = HitTest(p);
            if (h != Hover)
            {
                Hover = h;
                Cursor = h != null ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            PointF p = Logical(e.Location);
            if (Modal != 0)
            {
                pressed = HitTest(p);
                Invalidate();
                return;
            }
            if (p.Y < 62 && !closeBtn.Contains(p) && !minBtn.Contains(p))
            {
                dragging = true;
                dragCursor = Cursor.Position;
                Form f = FindForm();
                if (f != null) dragForm = f.Location;
                return;
            }
            pressed = HitTest(p);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            PointF p = Logical(e.Location);
            if (dragging) { dragging = false; pressed = null; return; }
            string id = HitTest(p);
            if (pressed != null && id == pressed) Activate(id);
            pressed = null;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (Hover != null) { Hover = null; Cursor = Cursors.Default; Invalidate(); }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Modal != 0)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { Activate("modal_ok"); e.Handled = true; }
                if (e.KeyCode == Keys.Escape && Modal == 4) { Modal = 0; Invalidate(); e.Handled = true; }
                return;
            }
            if (e.KeyCode == Keys.Space) { Activate("meditate"); e.Handled = true; }
            else if (e.KeyCode == Keys.B) { Activate("break"); e.Handled = true; }
        }

        void Activate(string id)
        {
            if (id == null) return;

            if (id == "min")
            {
                Form f = FindForm();
                if (f != null) f.WindowState = FormWindowState.Minimized;
                return;
            }
            if (id == "close")
            {
                Form f = FindForm();
                if (f != null) f.Close();
                return;
            }

            if (id == "modal_ok")
            {
                if (Modal == 1) { Modal = 0; E.Save(); }
                else if (Modal == 2 || Modal == 3) { E.Rebirth(); Modal = 0; }
                else if (Modal == 4) { E.ResetAll(); Modal = 0; }
                Hover = null;
                Invalidate();
                return;
            }
            if (id == "modal_cancel") { Modal = 0; Invalidate(); return; }

            if (id == "rename") { ShowNameEditor(); return; }
            if (id == "reset") { Modal = 4; Invalidate(); return; }

            if (id == "meditate")
            {
                if (E.EndGame) { Modal = E.P.Ascended ? 3 : 2; Invalidate(); return; }
                if (clickCd > 0) return;
                if (E.Strike())
                {
                    clickCd = 0.85f;
                    double gain = E.Rate * 4.0;
                    FloatText ft = new FloatText();
                    ft.Text = "+" + G.Num(gain) + " 修为";
                    ft.X = altarC.X + (float)(rnd.NextDouble() * 40 - 20);
                    ft.Y = altarC.Y - 46;
                    ft.Life = 1.3;
                    ft.Color = Theme.GoldBright;
                    floats.Add(ft);
                    Ripple rp = new Ripple();
                    rp.X = altarC.X;
                    rp.Y = altarC.Y;
                    rp.Life = 0.0;
                    ripples.Add(rp);
                }
                return;
            }
            if (id == "break")
            {
                if (!E.BreakReady)
                {
                    if (E.EndGame) { Modal = E.P.Ascended ? 3 : 2; Invalidate(); }
                    return;
                }
                bool ok = E.TryBreak();
                if (!ok) { ScreenShake(); }
                Invalidate();
                return;
            }
            if (id == "harvest")
            {
                E.Harvest();
                Invalidate();
                return;
            }
            if (id == "adventure")
            {
                E.Adventure();
                Invalidate();
                return;
            }
            if (id == "toggle") { E.ToggleMeditate(); return; }

            if (id == "pill_gas")
            {
                if (!E.BuyGasPill()) SpendFail();
                Invalidate();
                return;
            }
            if (id == "pill_break")
            {
                if (!E.BuyBreakPill()) SpendFail();
                Invalidate();
                return;
            }
            if (id == "tea")
            {
                if (!E.BuyTea()) SpendFail();
                Invalidate();
                return;
            }
        }

        void SpendFail()
        {
            E.Log("灵石不足，先去后山采药。", LogKind.Bad);
        }

        public double Shake = 0;

        void ScreenShake()
        {
            Shake = 1.0;
        }

        // ---------- 改名 ----------

        void ShowNameEditor()
        {
            TextBox tb = new TextBox();
            tb.Text = E.P.Name;
            tb.MaxLength = 8;
            tb.Font = new Font("Microsoft YaHei", 11f * S);
            tb.BackColor = Color.FromArgb(24, 24, 29);
            tb.ForeColor = Color.FromArgb(240, 230, 205);
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.TextAlign = HorizontalAlignment.Center;
            RectangleF nr = nameRect;
            tb.Bounds = new Rectangle((int)(nr.X * S), (int)(nr.Y * S) + 2,
                (int)(nr.Width * S), (int)(30 * S));
            bool done = false;

            EventHandler commit = null;
            commit = delegate(object s, EventArgs ev)
            {
                if (done) return;
                done = true;
                E.Rename(tb.Text);
                Controls.Remove(tb);
                tb.Dispose();
                Focus();
                Invalidate();
            };
            tb.KeyDown += delegate(object s, KeyEventArgs ev)
            {
                if (ev.KeyCode == Keys.Enter) { ev.SuppressKeyPress = true; commit(s, EventArgs.Empty); }
                else if (ev.KeyCode == Keys.Escape) { done = true; Controls.Remove(tb); tb.Dispose(); Focus(); Invalidate(); }
            };
            tb.LostFocus += commit;
            Controls.Add(tb);
            tb.SelectAll();
            tb.Focus();
        }

        float Measure(Graphics g, string s, Font f)
        {
            return g.MeasureString(s, f).Width;
        }

        string Cd(double seconds)
        {
            long s = (long)Math.Ceiling(seconds);
            if (s < 60) return s + "s";
            return (s / 60) + ":" + ((s % 60) < 10 ? "0" : "") + (s % 60);
        }
    }
}
