using System;

namespace PrayerTimes.Settings
{
    public sealed class AppSettings
    {
        public string CityName { get; set; } = "Marawi City";
        public double Latitude { get; set; } = 8.0034;
        public double Longitude { get; set; } = 124.2839;

        // Engine options
        public Services.CalcMethod Method { get; set; } = Services.CalcMethod.MuslimWorldLeague;
        public Services.AsrMadhhab Madhhab { get; set; } = Services.AsrMadhhab.Shafi;

        // Feature toggles
        public bool NotificationsEnabled { get; set; } = false;
        public int ReminderMinutesBefore { get; set; } = 5;
        public bool AzanEnabled { get; set; } = false;

        // Timezone (Windows ID). Default is PH (UTC+8).
        public string TimeZoneId { get; set; } = "Singapore Standard Time";
    }
}
