using System;

namespace Xiuxian
{
    /// <summary>
    /// 全局数值与文案配置。所有可调参数集中在这里，后续加玩法只需在此扩展数据表。
    /// </summary>
    public static class G
    {
        public const int LayersPerRealm = 9;
        public static readonly string[] RealmNames = { "炼气", "筑基", "结丹", "元婴", "化神", "炼虚", "合体", "大乘", "渡劫" };
        public static readonly string[] LayerNames = { "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        public static readonly string[] ShiChen = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

        // 寿元：基础 80 载，每提升一个大境界加成一档
        public const double BaseLife = 80;
        public static readonly double[] LifeBonus = { 0, 60, 150, 300, 520, 800, 1150, 1550, 2000 };

        // 修为曲线：第 n 级所需 / 每秒产出
        public const double NeedBase = 100;
        public const double NeedGrowth = 1.36;
        public const double RateBase = 1.1;
        public const double RateGrowth = 1.32;

        // 多倍率
        public const double MeditateBonus = 1.6;   // 打坐加持
        public const double PillBonusPct = 0.20;   // 破境丹加成功率
        public const double OfflineRate = 0.8;     // 离线效率
        public const double OfflineCap = 8 * 3600; // 离线上限（秒）
        public const double ClickSeconds = 4.0;    // 一次凝气相当于多少秒的修为

        public static int Index(int realm, int layer)
        {
            return realm * LayersPerRealm + layer - 1;
        }

        public static double Need(int index)
        {
            return NeedBase * Math.Pow(NeedGrowth, index);
        }

        public static double BaseRate(int index)
        {
            return RateBase * Math.Pow(RateGrowth, index);
        }

        public static string Title(int realm, int layer)
        {
            return RealmNames[realm] + LayerNames[layer - 1] + "层";
        }

        public static string NextRealm(int realm)
        {
            return realm + 1 < RealmNames.Length ? RealmNames[realm + 1] : "仙界";
        }

        /// <summary>数字缩写：万 / 亿 / 兆 / 京</summary>
        public static string Num(double v)
        {
            if (v < 0) return "0";
            if (v < 10000) return Trim(v);
            if (v < 1e8) return Trim(v / 1e4) + "万";
            if (v < 1e12) return Trim(v / 1e8) + "亿";
            if (v < 1e16) return Trim(v / 1e12) + "兆";
            return Trim(v / 1e16) + "京";
        }

        static string Trim(double v)
        {
            if (Math.Abs(v - Math.Round(v)) < 0.05) return Math.Round(v).ToString("#,0");
            if (v < 10) return v.ToString("0.0#");
            if (v < 100) return v.ToString("0.#");
            return Math.Round(v).ToString("0");
        }

        /// <summary>时长：秒 -> 中文</summary>
        public static string Dur(double seconds)
        {
            if (seconds < 1) return "0秒";
            long s = (long)Math.Round(seconds);
            if (s < 60) return s + "秒";
            if (s < 3600) return (s / 60) + "分" + (s % 60 > 0 ? (s % 60) + "秒" : "");
            long h = s / 3600;
            long m = (s % 3600) / 60;
            if (h < 24) return h + "时" + (m > 0 ? m + "分" : "");
            return (h / 24) + "日" + (h % 24 > 0 ? (h % 24) + "时" : "");
        }
    }
}
