using PrayerTimesApp.Models;
using PrayerTimesApp.Services;
using PrayerTimesApp.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Documents;

namespace PrayerTimesApp.ViewModels
{
    public sealed class MonthlyPrintViewModel
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month; // 1-12

        public PagePreset SelectedPagePreset { get; set; } = PagePreset.LegalUS;

        public string LocationName { get; set; } = "Your Location";
        public string SubTitle { get; set; } = "For community use • Verify before printing";

        public string FooterLeft { get; set; } = "Prepared by: ________";
        public string FooterRight { get; set; } = "Contact: ________";

        public ObservableCollection<DailyPrayerTimesRow> Rows { get; } = new();

        // REQUIRED: Provide a function that computes a day's prayer times for a given date.
        public Func<DateTime, Dictionary<WaktName, DateTime>>? GetTimesForDate { get; set; }

        // OPTIONAL: Provide hijri string
        public Func<DateTime, string>? GetHijriText { get; set; }

        public void Generate()
        {
            Rows.Clear();

            if (GetTimesForDate == null)
                throw new InvalidOperationException("GetTimesForDate is not set.");

            var days = DateTime.DaysInMonth(Year, Month);

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
                SubTitle = SubTitle,
                FooterLeft = FooterLeft,
                FooterRight = FooterRight,
                Rows = new List<DailyPrayerTimesRow>(Rows)
            };

            return MonthlyPrayerTimesGenerator.BuildFixedDocument(model, SelectedPagePreset);
        }

        private DailyPrayerTimesRow BuildRow(DateTime date, Dictionary<WaktName, DateTime> times)
        {
            string fmt(DateTime t) => t.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            string hijri = GetHijriText?.Invoke(date) ?? "";
            string day = date.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();

            return new DailyPrayerTimesRow
            {
                Date = date,
                HijriText = hijri,
                DayName = day,
                Fajr = times.TryGetValue(WaktName.Fajr, out var fajr) ? fmt(fajr) : "",
                Sunrise = times.TryGetValue(WaktName.Sunrise, out var sun) ? fmt(sun) : "",
                Dhuhr = times.TryGetValue(WaktName.Dhuhr, out var dhuhr) ? fmt(dhuhr) : "",
                Asr = times.TryGetValue(WaktName.Asr, out var asr) ? fmt(asr) : "",
                Maghrib = times.TryGetValue(WaktName.Maghrib, out var mag) ? fmt(mag) : "",
                Isha = times.TryGetValue(WaktName.Isha, out var isha) ? fmt(isha) : ""
            };
        }
    }
}