using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace PrayerTimes.Services
{
    /// <summary>
    /// Lightweight adhan engine:
    /// - Plays an embedded MP3 (Build Action: Resource) via pack://application:,,,/
    /// - Plays once per prayer per day (prevents repeat)
    /// - Skips Sunrise by default (you can still enable it explicitly)
    /// </summary>
    public sealed class AdhanAudioService
    {
        private readonly MediaPlayer _player = new MediaPlayer();

        // Keeps: yyyy-MM-dd|PRAYER
        private string? _lastPlayedKey;

        // Slight tolerance to handle timer tick drift
        private static readonly TimeSpan TriggerWindow = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Checks if we should play adhan now, and plays it if needed.
        /// </summary>
        /// <param name="now">Current local time</param>
        /// <param name="isEnabled">Master enable switch (Enable Adhan)</param>
        /// <param name="prayerEnabled">Per-prayer enable lookup (e.g., "Fajr" => true)</param>
        /// <param name="resolveMuadhdhinFile">Returns mp3 filename for a prayer (override or default). Example: "Mishary_Al-Afasy.mp3"</param>
        /// <param name="prayerTimesToday">Prayer times for TODAY (DateTime with today date). Keys: Fajr, Sunrise, Dhuhr, Asr, Maghrib, Isha</param>
        public void Tick(
            DateTime now,
            bool isEnabled,
            Func<string, bool> prayerEnabled,
            Func<string, string?> resolveMuadhdhinFile,
            IReadOnlyDictionary<string, DateTime> prayerTimesToday)
        {
            if (!isEnabled) return;
            if (prayerTimesToday == null || prayerTimesToday.Count == 0) return;

            // Decide which prayer time we're inside (if any)
            // Priority: exact-time triggers only (within TriggerWindow)
            foreach (var kv in prayerTimesToday.OrderBy(k => k.Value))
            {
                var prayer = kv.Key;
                var t = kv.Value;

                // Default safety: Sunrise should be OFF unless explicitly enabled
                if (string.Equals(prayer, "Sunrise", StringComparison.OrdinalIgnoreCase) &&
                    !prayerEnabled("Sunrise"))
                {
                    continue;
                }

                if (!prayerEnabled(prayer))
                    continue;

                // Trigger when time is within [t, t + TriggerWindow]
                if (now < t) continue;
                if (now - t > TriggerWindow) continue;

                var dayKey = now.ToString("yyyy-MM-dd");
                var playedKey = $"{dayKey}|{prayer}";
                if (string.Equals(_lastPlayedKey, playedKey, StringComparison.Ordinal))
                    return;

                var file = resolveMuadhdhinFile(prayer);
                if (string.IsNullOrWhiteSpace(file))
                    return;

                PlayResourceMp3(file);
                _lastPlayedKey = playedKey;
                return;
            }
        }

        /// <summary>
        /// Plays an mp3 embedded as WPF Resource: /Assets/Audio/{fileName}
        /// </summary>
        public void PlayResourceMp3(string fileName)
        {
            // IMPORTANT: fileName must match the file in /Assets/Audio/
            // Example: "Mishary_Al-Afasy.mp3"
            var uri = new Uri($"pack://application:,,,/Assets/Audio/{Uri.EscapeDataString(fileName)}", UriKind.Absolute);

            try
            {
                _player.Stop();
                _player.Open(uri);
                _player.Volume = 1.0; // later we can add a setting for volume
                _player.Play();
            }
            catch
            {
                // Intentionally swallow to avoid crashing on audio errors
                // (Optional: expose an event/log callback if you want UI feedback)
            }
        }
    }
}
