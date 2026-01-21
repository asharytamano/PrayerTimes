using System;
using System.Collections.Generic;

namespace PrayerTimesApp.Models
{
    public sealed class DailyPrayerTimesRow
    {
        public DateTime Date { get; set; }               // Local date
        public string HijriText { get; set; } = "";      // e.g., "10 Sha'ban 1447"
        public string DayName { get; set; } = "";        // e.g., "Wed"
        public string Fajr { get; set; } = "";
        public string Sunrise { get; set; } = "";
        public string Dhuhr { get; set; } = "";
        public string Asr { get; set; } = "";
        public string Maghrib { get; set; } = "";
        public string Isha { get; set; } = "";
    }

    public sealed class MonthlyPrayerTimesPrintModel
    {
        public string LocationName { get; set; } = "";     // e.g., "Quezon City, NCR"
        public string MonthTitle { get; set; } = "";       // e.g., "PRAYER TIMES — March 2026"
        public string SubTitle { get; set; } = "";         // e.g., "Method: MWL (Shafi)"
        public string FooterLeft { get; set; } = "";       // e.g., "Prepared by: ..."
        public string FooterRight { get; set; } = "";      // e.g., "Contact: ..."
        public List<DailyPrayerTimesRow> Rows { get; set; } = new();
    }
}
