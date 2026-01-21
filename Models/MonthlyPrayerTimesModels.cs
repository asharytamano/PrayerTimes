using System;
using System.Collections.Generic;

namespace PrayerTimes.Models
{
    public sealed class DailyPrayerTimesRow
    {
        public DateTime Date { get; set; }
        public string HijriText { get; set; } = "";
        public string DayName { get; set; } = "";
        public string Fajr { get; set; } = "";
        public string Sunrise { get; set; } = "";
        public string Dhuhr { get; set; } = "";
        public string Asr { get; set; } = "";
        public string Maghrib { get; set; } = "";
        public string Isha { get; set; } = "";
    }

    public sealed class MonthlyPrayerTimesPrintModel
    {
        public string LocationName { get; set; } = "";
        public string MonthTitle { get; set; } = "";
        public string HijriMonthTitle { get; set; } = "";
        public string SubTitle { get; set; } = "";
        public string FooterLeft { get; set; } = "";
        public string FooterRight { get; set; } = "";
        public List<DailyPrayerTimesRow> Rows { get; set; } = new();
    }
}
