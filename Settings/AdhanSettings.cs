// FILE: AdhanSettings.cs
// PURPOSE: Persisted settings for QM-style adhan controls (global + per-wakt).
// NOTE: Adjust namespace to match your project.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PrayerTimesApp.Settings
{
    public enum WaktName
    {
        Fajr,
        Sunrise,   // MUST NEVER PLAY ADHAN
        Dhuhr,
        Asr,
        Maghrib,
        Isha
    }

    public sealed class AdhanPerWaktSetting
    {
        public bool Enabled { get; set; } = true;

        // MP3 file name (relative) in /Assets/Audio/
        // Example: "Mishary_Al-Afasy.mp3"
        public string AudioFile { get; set; } = "Mishary_Al-Afasy.mp3";
    }

    public sealed class AdhanSettings
    {
        // Master switch (Enable/Disable all adhan)
        public bool AdhanEnabled { get; set; } = true;

        // QM-style toggle: if false, we won't show toast notifications either.
        public bool NotificationsEnabled { get; set; } = true;

        // Optional: a “quiet mode” master toggle for adhan + notifications.
        public bool QuietMode { get; set; } = false;

        // Avoid multiple plays due to timer tick jitter (seconds window)
        public int TriggerWindowSeconds { get; set; } = 20;

        // Per-wakt controls
        public Dictionary<WaktName, AdhanPerWaktSetting> PerWakt { get; set; } = CreateDefault();

        private static Dictionary<WaktName, AdhanPerWaktSetting> CreateDefault()
        {
            var d = new Dictionary<WaktName, AdhanPerWaktSetting>();

            d[WaktName.Fajr] = new AdhanPerWaktSetting { Enabled = true, AudioFile = "Mishary_Al-Afasy.mp3" };
            d[WaktName.Sunrise] = new AdhanPerWaktSetting { Enabled = false, AudioFile = "" }; // never
            d[WaktName.Dhuhr] = new AdhanPerWaktSetting { Enabled = true, AudioFile = "Hamza_Al_Majale.mp3" };
            d[WaktName.Asr] = new AdhanPerWaktSetting { Enabled = true, AudioFile = "Rabeh_Al_Jazairi.mp3" };
            d[WaktName.Maghrib] = new AdhanPerWaktSetting { Enabled = true, AudioFile = "Mishary_Al-Afasy.mp3" };
            d[WaktName.Isha] = new AdhanPerWaktSetting { Enabled = true, AudioFile = "Rabeh_Al_Jazairi.mp3" };

            return d;
        }
    }
}
