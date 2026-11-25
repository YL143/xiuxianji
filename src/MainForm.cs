using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Xiuxian
{
    public class MainForm : Form
    {
        /// <summary>由外部宿主指定资源与存档目录（PowerShell 宿主时使用）</summary>
        public static string BaseDir = null;

        public Engine E = new Engine();
        GameCanvas canvas;
        Timer loop;
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        double lastSec;
        double saveAcc;
        float scale = 1f;

        static bool CanWriteDir(string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".write_probe");
                File.WriteAllText(probe, "1");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        public MainForm()
        {
            Text = "修仙纪";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgTop;
            DoubleBuffered = true;
            ShowInTaskbar = true;
            MaximizeBox = false;

            // 缩放适配：以 96 DPI 为基准，保证窗口不超出屏幕
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            using (Graphics g = CreateGraphics())
            {
                scale = g.DpiX / 96f;
            }
            if (scale < 0.85f) scale = 0.85f;
            if (scale > 1.6f) scale = 1.6f;
            float fitW = wa.Width * 0.95f / Theme.W;
            float fitH = wa.Height * 0.95f / Theme.H;
            if (scale > fitW) scale = fitW;
            if (scale > fitH) scale = fitH;

            // 存档与程序同目录，绿色便携
            string appDir = BaseDir != null && BaseDir.Length > 0 ? BaseDir : Path.GetDirectoryName(Application.ExecutablePath);
            try
            {
                string iconPath = Path.Combine(Path.Combine(appDir, "assets"), "app.ico");
                if (File.Exists(iconPath)) Icon = new Icon(iconPath);
            }
            catch { }
            // 存档默认与程序同目录；若目录不可写（如装在 Program Files），退到本机用户数据目录
            string saveDir = Path.Combine(appDir, "save");
            if (!CanWriteDir(saveDir))
                saveDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "修仙纪", "save");
            string savePath = Path.Combine(saveDir, "xiuxian.sav");
            try
            {
                E.Init(savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("存档初始化失败：" + ex.Message, "修仙纪");
            }

            canvas = new GameCanvas(E, scale);
            Controls.Add(canvas);
            ClientSize = new Size(canvas.Width, canvas.Height);

            // 圆角窗体
            GraphicsPath rp = Theme.Round(new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), 14 * scale);
            Region = new Region(rp);
            rp.Dispose();

            if (E.P.Dead) canvas.Modal = 2;
            else if (E.P.Ascended) canvas.Modal = 3;
            else if (E.Offline != null) canvas.Modal = 1;

            loop = new Timer();
            loop.Interval = 40;
            loop.Tick += LoopTick;
            sw.Start();
            lastSec = sw.Elapsed.TotalSeconds;
            loop.Start();

            FormClosing += OnClosing;
        }

        void LoopTick(object sender, EventArgs e)
        {
            double now = sw.Elapsed.TotalSeconds;
            double dt = now - lastSec;
            lastSec = now;
            if (dt < 0) dt = 0;
            if (dt > 0.5) dt = 0.5;

            E.Tick(dt);
            canvas.Advance(dt);
            canvas.Invalidate();

            saveAcc += dt;
            if (saveAcc >= 20)
            {
                saveAcc = 0;
                E.Save();
            }
        }

        void OnClosing(object sender, FormClosingEventArgs e)
        {
            E.Save();
            if (loop != null) loop.Stop();
        }
    }
}
