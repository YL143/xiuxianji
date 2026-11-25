using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Xiuxian
{
    public enum LogKind
    {
        Info,
        Good,
        Gold,
        Bad,
        Sys
    }

    public class LogEntry
    {
        public string Stamp;
        public string Text;
        public LogKind Kind;

        public LogEntry(string stamp, string text, LogKind kind)
        {
            Stamp = stamp;
            Text = text;
            Kind = kind;
        }
    }

    public class OfflineReport
    {
        public double Seconds;
        public double Exp;
        public int Levels;
        public bool Capped;
    }

    /// <summary>一世的修行状态（可整体存档）</summary>
    public class Player
    {
        public string Name = "无名散修";
        public int Realm = 0;
        public int Layer = 1;
        public double Exp = 0;
        public long Stones = 0;
        public int Insight = 0;
        public int Reincarnation = 0;
        public double TotalSeconds = 0;   // 本世修行时长
        public double LifeSeconds = 0;    // 累计游戏时长
        public double BuffLeft = 0;       // 聚气丹剩余秒数
        public int BreakPills = 0;
        public double WoundLeft = 0;
        public double HarvestCd = 0;
        public double AdventureCd = 0;
        public bool Meditating = true;
        public bool Dead = false;
        public bool Ascended = false;
        public int BestRealm = 0;
    }

    /// <summary>
    /// 游戏核心：数值结算、行为、存读档。UI 只读它、调它，不直接改状态。
    /// </summary>
    public class Engine
    {
        public Player P = new Player();
        public List<LogEntry> Logs = new List<LogEntry>();
        public OfflineReport Offline = null;
        public string SavePath = "";
        public string SaveError = "";
        public DateTime LastSaveUtc = DateTime.UtcNow;
        public Random Rnd = new Random();
        public double Flash = 0; // 升级闪光，供 UI 读取

        int bottleneckLogged = -1;
        bool loaded = false;

        public int Index { get { return G.Index(P.Realm, P.Layer); } }
        public double Need { get { return G.Need(Index); } }
        public double AgeYears { get { return 16.0 + P.TotalSeconds / 3600.0; } }
        public double LifeSpan { get { return G.BaseLife + G.LifeBonus[P.Realm]; } }
        public bool EndGame { get { return P.Dead || P.Ascended; } }
        public bool BreakReady { get { return !EndGame && P.Layer == 9 && P.Exp >= Need; } }
        public bool IsMaxRealm { get { return P.Realm == G.RealmNames.Length - 1; } }
        public bool Loaded { get { return loaded; } }

        /// <summary>当前每秒修为（含所有增益）</summary>
        public double Rate
        {
            get
            {
                double r = G.BaseRate(Index);
                r *= 1.0 + P.Insight * 0.02 + P.Reincarnation * 0.06;
                if (P.Meditating) r *= G.MeditateBonus;
                if (P.BuffLeft > 0) r *= 1.5;
                if (P.WoundLeft > 0) r *= 0.5;
                return r;
            }
        }

        /// <summary>不含打坐/丹药的裸速率（离线用）</summary>
        public double BaseRateNow
        {
            get
            {
                double r = G.BaseRate(Index);
                r *= 1.0 + P.Insight * 0.02 + P.Reincarnation * 0.06;
                return r;
            }
        }

        /// <summary>突破成功率（含已备破境丹）</summary>
        public double BreakChance
        {
            get
            {
                double c = 0.58 + P.Insight * 0.01 - P.Realm * 0.02;
                if (P.BreakPills > 0) c += G.PillBonusPct;
                if (c < 0.15) c = 0.15;
                if (c > 0.95) c = 0.95;
                return c;
            }
        }

        // ================= 存档 =================

        public void Init(string savePath)
        {
            SavePath = savePath;
            if (!Load()) NewGame(true);
            loaded = true;
            ApplyOffline();
        }

        void NewGame(bool intro)
        {
            P = new Player();
            Logs.Clear();
            LastSaveUtc = DateTime.UtcNow;
            if (intro)
            {
                Log("你在青云山脚的破庙中醒来，识海深处浮着一枚残破玉简。", LogKind.Sys);
                Log("玉简上只剩四个字：炼气，长生。", LogKind.Sys);
                Log("【提示】点中央「打坐」凝气，修为满后会自动晋入下一层。", LogKind.Info);
                Log("【提示】九层圆满方可「冲击瓶颈」，失败会折损半数修为。", LogKind.Info);
                Log("【提示】关窗也会继续修行，单次离线最长结算 8 小时。", LogKind.Info);
            }
            Save();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("version=1");
                sb.AppendLine("name=" + P.Name);
                sb.AppendLine("realm=" + P.Realm);
                sb.AppendLine("layer=" + P.Layer);
                sb.AppendLine("exp=" + P.Exp.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("stones=" + P.Stones);
                sb.AppendLine("insight=" + P.Insight);
                sb.AppendLine("reinc=" + P.Reincarnation);
                sb.AppendLine("total=" + P.TotalSeconds.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("life=" + P.LifeSeconds.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("buff=" + P.BuffLeft.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("pillb=" + P.BreakPills);
                sb.AppendLine("wound=" + P.WoundLeft.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("hcd=" + P.HarvestCd.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("acd=" + P.AdventureCd.ToString("R", CultureInfo.InvariantCulture));
                sb.AppendLine("meditate=" + (P.Meditating ? 1 : 0));
                sb.AppendLine("dead=" + (P.Dead ? 1 : 0));
                sb.AppendLine("asc=" + (P.Ascended ? 1 : 0));
                sb.AppendLine("best=" + P.BestRealm);
                sb.AppendLine("saved=" + DateTime.UtcNow.Ticks);

                File.WriteAllText(SavePath, sb.ToString(), new UTF8Encoding(false));
                LastSaveUtc = DateTime.UtcNow;
                SaveError = "";
            }
            catch (Exception ex)
            {
                SaveError = ex.Message;
            }
        }

        bool Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return false;
                Dictionary<string, string> d = new Dictionary<string, string>();
                string[] lines = File.ReadAllLines(SavePath, Encoding.UTF8);
                foreach (string line in lines)
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    d[line.Substring(0, i)] = line.Substring(i + 1);
                }
                if (d.Count == 0) return false;

                Player np = new Player();
                if (d.ContainsKey("name") && d["name"].Trim().Length > 0) np.Name = d["name"].Trim();
                np.Realm = ClampI(D(d, "realm", 0), 0, G.RealmNames.Length - 1);
                np.Layer = ClampI(D(d, "layer", 1), 1, 9);
                np.Exp = D(d, "exp", 0);
                np.Stones = (long)D(d, "stones", 0);
                np.Insight = ClampI(D(d, "insight", 0), 0, 99);
                np.Reincarnation = ClampI(D(d, "reinc", 0), 0, 999);
                np.TotalSeconds = D(d, "total", 0);
                np.LifeSeconds = D(d, "life", 0);
                np.BuffLeft = D(d, "buff", 0);
                np.BreakPills = ClampI(D(d, "pillb", 0), 0, 99);
                np.WoundLeft = D(d, "wound", 0);
                np.HarvestCd = D(d, "hcd", 0);
                np.AdventureCd = D(d, "acd", 0);
                np.Meditating = D(d, "meditate", 1) > 0.5;
                np.Dead = D(d, "dead", 0) > 0.5;
                np.Ascended = D(d, "asc", 0) > 0.5;
                np.BestRealm = ClampI(D(d, "best", np.Realm), 0, G.RealmNames.Length - 1);
                long ticks = (long)D(d, "saved", 0);
                if (ticks > 0 && ticks < DateTime.UtcNow.Ticks)
                    LastSaveUtc = new DateTime(ticks, DateTimeKind.Utc);

                P = np;
                Log("神识归位，你重新睁开双眼。", LogKind.Sys);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static double D(Dictionary<string, string> d, string k, double def)
        {
            string v;
            if (d.TryGetValue(k, out v))
            {
                double r;
                if (double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out r)) return r;
            }
            return def;
        }

        static int ClampI(double v, int lo, int hi)
        {
            int i = (int)Math.Round(v);
            if (i < lo) i = lo;
            if (i > hi) i = hi;
            return i;
        }

        // ================= 离线 =================

        public void ApplyOffline()
        {
            if (EndGame) return;
            double elapsed = (DateTime.UtcNow - LastSaveUtc).TotalSeconds;
            if (elapsed < 120) return;

            double sec = Math.Min(elapsed, G.OfflineCap);
            double gain = BaseRateNow * G.OfflineRate * sec;
            int before = Index;

            P.TotalSeconds += sec;
            P.LifeSeconds += sec;
            DecreaseTimers(sec);
            AddExp(gain, true);

            if (AgeYears >= LifeSpan) Die();

            Offline = new OfflineReport();
            Offline.Seconds = sec;
            Offline.Exp = gain;
            Offline.Levels = Math.Max(0, Index - before);
            Offline.Capped = elapsed > G.OfflineCap;

            string head = string.Format("闭关归来：历时{0}，静悟修为 {1}", G.Dur(sec), G.Num(gain));
            if (Offline.Levels > 0) head += string.Format("，连晋 {0} 层", Offline.Levels);
            head += "。";
            Log(head, LogKind.Gold);
        }

        void DecreaseTimers(double dt)
        {
            if (P.BuffLeft > 0) P.BuffLeft = Math.Max(0, P.BuffLeft - dt);
            if (P.WoundLeft > 0) P.WoundLeft = Math.Max(0, P.WoundLeft - dt);
            if (P.HarvestCd > 0) P.HarvestCd = Math.Max(0, P.HarvestCd - dt);
            if (P.AdventureCd > 0) P.AdventureCd = Math.Max(0, P.AdventureCd - dt);
        }

        // ================= 主循环 =================

        public void Tick(double dt)
        {
            if (EndGame) return;
            P.TotalSeconds += dt;
            P.LifeSeconds += dt;
            DecreaseTimers(dt);
            if (Flash > 0) Flash = Math.Max(0, Flash - dt * 2.0);

            AddExp(Rate * dt, false);

            if (AgeYears >= LifeSpan) Die();
        }

        public void AddExp(double amount, bool silent)
        {
            if (EndGame || amount <= 0) return;
            P.Exp += amount;

            int guard = 0;
            while (guard++ < 300)
            {
                double need = Need;
                if (P.Layer >= 9)
                {
                    if (P.Exp > need) P.Exp = need;
                    if (P.Exp >= need && bottleneckLogged != Index)
                    {
                        bottleneckLogged = Index;
                        if (!silent)
                            Log(G.Title(P.Realm, P.Layer) + "圆满，丹田如满月流转，可冲击瓶颈。", LogKind.Gold);
                    }
                    break;
                }
                if (P.Exp < need) break;
                P.Exp -= need;
                P.Layer++;
                Flash = 1.0;
                if (P.Layer == 9)
                {
                    bottleneckLogged = -1;
                    if (!silent)
                        Log("修为精进，已至" + G.Title(P.Realm, P.Layer) + "。", LogKind.Good);
                }
                else if (!silent)
                {
                    Log("修为精进，晋入" + G.Title(P.Realm, P.Layer) + "。", LogKind.Good);
                }
            }
        }

        void Die()
        {
            if (P.Dead) return;
            P.Dead = true;
            Log("寿元枯竭，你于蒲团上阖目坐化，享年 " + (int)AgeYears + " 岁。", LogKind.Bad);
            Save();
        }

        // ================= 玩家行为 =================

        /// <summary>点击打坐：按冷却给一段修为</summary>
        public bool Strike()
        {
            if (EndGame) return false;
            AddExp(Rate * G.ClickSeconds, false);
            return true;
        }

        public void ToggleMeditate()
        {
            P.Meditating = !P.Meditating;
            Log(P.Meditating ? "你收拢心神，进入打坐状态（吐纳 ×1.6）。" : "你松开盘坐，转入散修状态（吐纳 ×1.0）。", LogKind.Sys);
        }

        public bool TryBreak()
        {
            if (!BreakReady) return false;

            double need = Need;
            double chance = BreakChance;
            bool usedPill = false;
            if (P.BreakPills > 0)
            {
                P.BreakPills--;
                usedPill = true;
            }

            P.Exp = 0;
            Log(string.Format("你盘坐九日，引天地灵气冲击{0}瓶颈……（成功率 {1:0}%{2}）",
                IsMaxRealm ? "天门" : G.NextRealm(P.Realm),
                chance * 100,
                usedPill ? "，已服破境丹" : ""), LogKind.Info);

            bool ok = Rnd.NextDouble() < chance;
            if (ok)
            {
                if (IsMaxRealm)
                {
                    P.Ascended = true;
                    Log("九霄雷云散尽，天门洞开。你踏光而上，白日飞升。", LogKind.Gold);
                    Save();
                    return true;
                }
                P.Realm++;
                P.Layer = 1;
                P.BestRealm = Math.Max(P.BestRealm, P.Realm);
                bottleneckLogged = -1;
                P.BreakPills = 0;
                P.WoundLeft = 0;
                long bonus = 30 + P.Realm * 60;
                P.Stones += bonus;
                Flash = 1.0;
                Log("瓶颈崩碎！你踏入" + G.Title(P.Realm, 1) + "，寿元大增，气机焕然。", LogKind.Gold);
                Log("天地灵气倒灌，灵石 +" + bonus + " 枚。", LogKind.Good);
                Save();
            }
            else
            {
                P.Exp = need * 0.5;
                P.WoundLeft = 90;
                Log("冲击失败，气机逆冲，你喷出一口淤血——修为折损半数，需静养 90 秒。", LogKind.Bad);
            }
            return ok;
        }

        public bool Harvest()
        {
            if (EndGame || P.HarvestCd > 0) return false;
            int idx = Index;
            double raw = (8 + idx * 6) * (0.85 + Rnd.NextDouble() * 0.3);
            long gain = (long)Math.Round(raw);
            if (gain < 1) gain = 1;
            P.Stones += gain;
            P.HarvestCd = 45;
            string[] txt = {
                "你入后山采得几株带露的灵草，",
                "你在崖畔寻到一丛紫芝，",
                "你以玉锄掘出数枚灵石矿胚，",
            };
            Log(txt[Rnd.Next(txt.Length)] + "换得灵石 " + gain + " 枚。", LogKind.Good);
            return true;
        }

        public bool Adventure()
        {
            if (EndGame || P.AdventureCd > 0) return false;
            P.AdventureCd = 150;
            int idx = Index;
            double roll = Rnd.NextDouble();

            if (roll < 0.34)
            {
                long st = (long)Math.Round(20 + idx * 10 + Rnd.NextDouble() * (20 + idx * 8));
                P.Stones += st;
                Log("你于坊市替人驱邪，得灵石 " + st + " 枚。", LogKind.Good);
            }
            else if (roll < 0.64)
            {
                double exp = Rate * (90.0 + Rnd.NextDouble() * 60.0);
                AddExp(exp, false);
                Log("你在古洞中得到半卷残篇，参悟修为 " + G.Num(exp) + "。", LogKind.Good);
            }
            else if (roll < 0.74)
            {
                P.Insight++;
                Log("月下观瀑，忽有所悟——悟性 +1。", LogKind.Gold);
            }
            else if (roll < 0.88)
            {
                double loss = P.Exp * 0.15;
                P.Exp -= loss;
                P.WoundLeft = 90;
                Log("你误闯妖兽巢穴，负伤而归，" + (loss > 0 ? "修为折损 " + G.Num(loss) : "气血翻涌") + "，需静养 90 秒。", LogKind.Bad);
            }
            else
            {
                P.BreakPills++;
                Log("你救下一名老道，他赠你一枚破境丹。", LogKind.Gold);
            }

            if (AgeYears >= LifeSpan) Die();
            return true;
        }

        public long PillGasCost { get { return 80; } }
        public long PillBreakCost { get { return 120 + P.Realm * 80; } }
        public long TeaCost { get { return (long)Math.Round(150 * Math.Pow(2, Math.Min(P.Insight, 20))); } }

        public bool BuyGasPill()
        {
            if (EndGame || P.Stones < PillGasCost) return false;
            P.Stones -= PillGasCost;
            P.BuffLeft = Math.Max(P.BuffLeft, 0) + 720;
            Log("你服下一枚聚气丹，药力化开，吐纳 ×1.5，持续 12 分。", LogKind.Good);
            return true;
        }

        public bool BuyBreakPill()
        {
            if (EndGame || P.Stones < PillBreakCost) return false;
            P.Stones -= PillBreakCost;
            P.BreakPills++;
            Log("你收下一枚破境丹，下次冲击瓶颈成功率 +20%。", LogKind.Good);
            return true;
        }

        public bool BuyTea()
        {
            if (EndGame || P.Stones < TeaCost) return false;
            P.Stones -= TeaCost;
            P.Insight++;
            Log("一盏悟道茶入喉，心神澄明——悟性 +1（永久）。", LogKind.Gold);
            return true;
        }

        public void Rename(string name)
        {
            name = name.Trim();
            if (name.Length == 0) return;
            if (name.Length > 8) name = name.Substring(0, 8);
            P.Name = name;
            Log("你以神识刻下道号：" + name + "。", LogKind.Sys);
            Save();
        }

        public void Rebirth()
        {
            int oldRealm = P.Realm;
            P.Reincarnation++;
            P.Dead = false;
            P.Ascended = false;
            P.Realm = 0;
            P.Layer = 1;
            P.Exp = 0;
            P.TotalSeconds = 0;
            P.BuffLeft = 0;
            P.WoundLeft = 0;
            P.HarvestCd = 0;
            P.AdventureCd = 0;
            P.BreakPills = 0;
            P.Meditating = true;
            P.Stones = 60 + oldRealm * 40;
            bottleneckLogged = -1;
            Flash = 1.0;
            Log(string.Format("你散尽旧躯，踏入轮回。第 {0} 世，道韵 +{1}% 永久吐纳。", P.Reincarnation, P.Reincarnation * 6), LogKind.Gold);
            Log("轮回馈赠：灵石 +" + P.Stones + " 枚，前世" + G.RealmNames[Math.Min(oldRealm, G.RealmNames.Length - 1)] + "感悟犹存。", LogKind.Good);
            Save();
        }

        public void ResetAll()
        {
            NewGame(true);
        }

        // ================= 日志 =================

        public void Log(string text, LogKind kind)
        {
            int day = (int)(P.TotalSeconds / 360.0) + 1;
            int sc = (int)((P.TotalSeconds % 360.0) / 30.0);
            if (sc < 0) sc = 0;
            if (sc > 11) sc = 11;
            string stamp = string.Format("{0:00}日·{1}", day, G.ShiChen[sc]);
            Logs.Insert(0, new LogEntry(stamp, text, kind));
            while (Logs.Count > 60) Logs.RemoveAt(Logs.Count - 1);
        }
    }
}
