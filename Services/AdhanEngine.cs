// FILE: AdhanEngine.cs  (QM-style engine, UPDATED)
// PURPOSE:
//   - Master enable/disable Adhan
//   - Master enable/disable Notifications
//   - Per-wakt enable/disable + per-wakt mp3
//   - Sunrise NEVER plays Adhan (hard-block)
//   - Anti-double-fire via adhan_state.json saved in your AppData folder
//
// DEPENDS ON:
//   - AdhanSettings.cs   (namespace PrayerTimesApp.Settings)
//   - AdhanStateStore.cs (namespace PrayerTimesApp.Services)
//   - Your existing SettingsStore.cs (only to provide load/save delegates)
//
// NOTE:
//   This class is ONLY the engine. It does not decide where to live in your app.
//   You will instantiate it from MainWindow.xaml.cs or your ViewModel.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using PrayerTimesApp.Settings;

namespace PrayerTimesApp.Services
{
    public sealed class AdhanEngine : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly AdhanStateStore _stateStore;

        private readonly IAdhanAudioPlayer _audio;
        private readonly IToastService _toast;

        // Must return LOCAL DateTime values for "today"
        private readonly Func<Dictionary<WaktName, DateTime>> _getTodayTimes;

        // You provide these using your existing SettingsStore.cs
        private readonly Func<AdhanSettings> _loadSettings;
        private readonly Action<AdhanSettings> _saveSettings;

        // Persisted anti-double-fire state: key = "yyyy-MM-dd:WaktName"
        private Dictionary<string, DateTime> _state = new();

        // Current settings snapshot (reloadable)
        private AdhanSettings _settings = new();

        // Optional: keep engine from firing if prayer times are stale (e.g., yesterday's)
        public bool RequireTodayTimesOnly { get; set; } = true;

        public AdhanEngine(
            Func<Dictionary<WaktName, DateTime>> getTodayTimes,
            IAdhanAudioPlayer audio,
            IToastService toast,
            string appFolder,
            Func<AdhanSettings> loadSettings,
            Action<AdhanSettings> saveSettings)
        {
            _getTodayTimes = getTodayTimes ?? throw new ArgumentNullException(nameof(getTodayTimes));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _toast = toast ?? new NullToastService();

            if (string.IsNullOrWhiteSpace(appFolder))
                throw new ArgumentException("appFolder must be a valid folder path.", nameof(appFolder));

            _loadSettings = loadSettings ?? throw new ArgumentNullException(nameof(loadSettings));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));

            _stateStore = new AdhanStateStore(appFolder);

            _settings = SafeLoadSettings();
            _state = _stateStore.Load();

            _timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        public AdhanSettings Settings => _settings;

        public void ReloadSettings()
        {
            _settings = SafeLoadSettings();
        }

        public void SaveSettings()
        {
            try { _saveSettings(_settings); } catch { /* ignore */ }
        }

        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();

        private AdhanSettings SafeLoadSettings()
        {
            try { return _loadSettings() ?? new AdhanSettings(); }
            catch { return new AdhanSettings(); }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_settings.QuietMode) return;

            // If both are off, do nothing
            if (!_settings.AdhanEnabled && !_settings.NotificationsEnabled) return;

            var now = DateTime.Now;

            var times = _getTodayTimes.Invoke();
            if (times == null || times.Count == 0) return;

            if (RequireTodayTimesOnly)
            {
                // If the dictionary looks stale (e.g., all entries not today), skip
                // We only require that at least one time is for today.
                if (!times.Values.Any(t => t.Date == now.Date))
                    return;
            }

            // No Sunrise adhan - hard block
            var eligible = times
                .Where(kv => kv.Key != WaktName.Sunrise)
                .OrderBy(kv => kv.Value)
                .ToList();

            foreach (var kv in eligible)
            {
                var wakt = kv.Key;
                var scheduled = kv.Value;

                // If a scheduled time is not today (rare), skip that entry.
                if (RequireTodayTimesOnly && scheduled.Date != now.Date)
                    continue;

                var window = TimeSpan.FromSeconds(Math.Max(3, _settings.TriggerWindowSeconds));

                // Trigger only when now is within +/- window seconds of scheduled time
                if (now < scheduled - window || now > scheduled + window) continue;

                // Prevent duplicates for the same date+wakt
                var key = AdhanStateStore.Key(now.Date, wakt);
                if (_state.ContainsKey(key)) continue;

                // Mark fired first to avoid double-fire if audio blocks
                _state[key] = now;
                _stateStore.Save(_state);

                // Notification (QM-style enable/disable)
                if (_settings.NotificationsEnabled)
                {
                    _toast.ShowPrayerTime(wakt.ToString(), scheduled, now);
                }

                // Adhan per-wakt enable/disable
                if (_settings.AdhanEnabled && _settings.PerWakt.TryGetValue(wakt, out var per))
                {
                    if (per.Enabled && !string.IsNullOrWhiteSpace(per.AudioFile))
                    {
                        _audio.PlayFromAssets(per.AudioFile);
                    }
                }

                // Only one trigger per tick
                break;
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }
    }

    // -------- Interfaces you can map to your existing implementation --------

    public interface IAdhanAudioPlayer
    {
        // Expects file name in /Assets/Audio/ (Build Action: Resource)
        void PlayFromAssets(string fileName);
        void Stop();
    }

    public interface IToastService
    {
        void ShowPrayerTime(string prayerName, DateTime scheduledTime, DateTime nowLocal);
    }

    public sealed class NullToastService : IToastService
    {
        public void ShowPrayerTime(string prayerName, DateTime scheduledTime, DateTime nowLocal) { }
    }
}
