using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Documents;
using PrayerTimes.Models;
using PrayerTimes.Services;

namespace PrayerTimes.ViewModels
{
    public sealed class MonthlyPrintViewModel
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month; // 1-12

        // "LegalUS" or "A3"
        public string PagePreset { get; set; } = "LegalUS";

        public string LocationName { get; set; } = "Your Location";
        public string SubTitle { get; set; } = "";
        public string FooterLeft { get; set; } = "";
        public string FooterRight { get; set; } = "";

        public ObservableCollection<DailyPrayerTimesRow> Rows { get; } = new();

        // Provide actual per-day times using your PrayerTimeService.GetForDate(...)
        public Func<DateTime, (DateTime fajr, DateTime sunrise, DateTime dhuhr, DateTime asr, DateTime maghrib, DateTime isha)>? GetTimesForDate { get; set; }

        // Provide hijri string per day if you have a precise source; fallback uses arithmetic HijriCalendar.
        public Func<DateTime, string>? GetHijriText { get; set; }

        public void Generate()
        {
            Rows.Clear();

            if (GetTimesForDate == null)
                throw new InvalidOperationException("GetTimesForDate is not set.");

            int days = DateTime.DaysInMonth(Year, Month);
            for (int d = 1; d <= days; d++)
            {
                var date = new DateTime(Year, Month, d);
                var times = GetTimesForDate(date);
                Rows.Add(BuildRow(date, times));
            }
        }

        public FixedDocument BuildDocument()
        {
            if (Rows.Count == 0) Generate();

            var model = new MonthlyPrayerTimesPrintModel
            {
                LocationName = LocationName,
                MonthTitle = $"PRAYER TIMES — {new DateTime(Year, Month, 1):MMMM yyyy}",
                HijriMonthTitle = BuildHijriMonthTitle(new List<DailyPrayerTimesRow>(Rows)),
                SubTitle = SubTitle,
                FooterLeft = FooterLeft,
                FooterRight = FooterRight,
                Rows = new List<DailyPrayerTimesRow>(Rows)
            };

            return MonthlyPrayerTimesGenerator.BuildFixedDocument(model, PagePreset);
        }

        private DailyPrayerTimesRow BuildRow(DateTime date, (DateTime fajr, DateTime sunrise, DateTime dhuhr, DateTime asr, DateTime maghrib, DateTime isha) t)
        {
            string fmt(DateTime x) => x.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            string hijri = GetHijriText?.Invoke(date) ?? DefaultHijriText(date);
            string day = date.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();

            return new DailyPrayerTimesRow
            {
                Date = date,
                HijriText = hijri,
                DayName = day,
                Fajr = fmt(t.fajr),
                Sunrise = fmt(t.sunrise),
                Dhuhr = fmt(t.dhuhr),
                Asr = fmt(t.asr),
                Maghrib = fmt(t.maghrib),
                Isha = fmt(t.isha)
            };
        }

        private static string DefaultHijriText(DateTime greg)
        {
            // NOTE: This is arithmetic HijriCalendar. If you need Umm al-Qura or local sighting alignment,
            // inject GetHijriText from your app's existing hijri logic.
            var hc = new HijriCalendar();
            int hy = hc.GetYear(greg);
            int hm = hc.GetMonth(greg);
            int hd = hc.GetDayOfMonth(greg);

            string[] months =
            {
                "Muharram","Safar","Rabi' al-Awwal","Rabi' al-Thani","Jumada al-Ula","Jumada al-Akhirah",
                "Rajab","Sha'ban","Ramadan","Shawwal","Dhu al-Qi'dah","Dhu al-Hijjah"
            };

            string m = (hm >= 1 && hm <= 12) ? months[hm - 1] : $"M{hm}";
            return $"{hd} {m} {hy}";
        }

        private static string BuildHijriMonthTitle(List<DailyPrayerTimesRow> rows)
        {
            // Expect HijriText format: "10 Sha'ban 1447"
            if (rows == null || rows.Count == 0) return "";

            static (string month, string year) ParseMonthYear(string? hijri)
            {
                if (string.IsNullOrWhiteSpace(hijri)) return ("", "");
                var parts = hijri.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) return ("", "");
                return (parts[1], parts[2]);
            }

            var (m1, y1) = ParseMonthYear(rows[0].HijriText);
            var (m2, y2) = ParseMonthYear(rows[^1].HijriText);

            if (string.IsNullOrWhiteSpace(m1) || string.IsNullOrWhiteSpace(y1)) return "";

            m1 = m1.ToUpperInvariant();
            y1 = y1.ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(m2) && !string.IsNullOrWhiteSpace(y2))
            {
                m2 = m2.ToUpperInvariant();
                y2 = y2.ToUpperInvariant();

                if (m1 == m2 && y1 == y2) return $"{m1} {y1} AH";
                if (y1 == y2) return $"{m1}–{m2} {y1} AH";
                return $"{m1} {y1} AH – {m2} {y2} AH";
            }

            return $"{m1} {y1} AH";
        }
    }
}
