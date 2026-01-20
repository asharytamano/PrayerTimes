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

        // Default voice (used when a prayer-specific voice is blank)
        public string DefaultAzanVoice { get; set; } = "Mishary Al-Afasy";

        // Per-prayer switches + optional per-prayer voice overrides
        public bool FajrAzanEnabled { get; set; } = true;
        public string FajrAzanVoice { get; set; } = "";

        // Sunrise: no Adhan by default (common practice)
        public bool SunriseAzanEnabled { get; set; } = false;
        public string SunriseAzanVoice { get; set; } = "";

        public bool DhuhrAzanEnabled { get; set; } = true;
        public string DhuhrAzanVoice { get; set; } = "";

        public bool AsrAzanEnabled { get; set; } = true;
        public string AsrAzanVoice { get; set; } = "";

        public bool MaghribAzanEnabled { get; set; } = true;
        public string MaghribAzanVoice { get; set; } = "";

        public bool IshaAzanEnabled { get; set; } = true;
        public string IshaAzanVoice { get; set; } = "";


        // Timezone (Windows ID). Default is PH (UTC+8).
        public string TimeZoneId { get; set; } = "Singapore Standard Time";

        // IPR Decision A: Windows Location Services only
        // If enabled, the app will try Windows location on startup and overwrite Lat/Lon (offline, no IP fallback).
        public bool UseWindowsLocationOnStartup { get; set; } = false;
    }
}
