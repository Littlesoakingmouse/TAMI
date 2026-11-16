using System.Globalization;

namespace Tami
{
    /// <summary>Hiển thị số lớn dạng 12,5K; 340M (GDD mục 5.6) và thời gian.</summary>
    public static class NumberFormat
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

        public static string Short(double value)
        {
            if (value < 0) return "-" + Short(-value);
            if (value < 1000) return System.Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);

            int i = 0;
            while (value >= 1000 && i < Suffixes.Length - 1)
            {
                value /= 1000;
                i++;
            }
            string format = value >= 100 ? "0" : value >= 10 ? "0.#" : "0.##";
            return value.ToString(format, CultureInfo.InvariantCulture).Replace('.', ',') + Suffixes[i];
        }

        /// <summary>Đồng hồ đếm ngược dạng 4:05.</summary>
        public static string Clock(float seconds)
        {
            int s = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        /// <summary>Khoảng thời gian dạng "2 giờ 15 phút".</summary>
        public static string Duration(double seconds)
        {
            long total = (long)System.Math.Max(0, seconds);
            long hours = total / 3600, minutes = total % 3600 / 60;
            if (hours > 0) return hours + " giờ " + minutes + " phút";
            if (minutes > 0) return minutes + " phút";
            return total + " giây";
        }
    }
}
