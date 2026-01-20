using PrayerTimes.Services;
using PrayerTimes.Settings;
using PrayerTimes.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrayerTimes.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly PrayerTimeService _service = new();
        private readonly DispatcherTimer _timer;
        private readonly SettingsStore _store = new();
        private readonly LocationCatalogService _locationCatalog = new();

        private AppSettings _settings;
        private PrayerTimesResult? _last;

        private string _todayLabel = "";
        private string _nextPrayer = "";
        private string _countdown = "";

        private string _fajr = "";
        private string _sunrise = "";
        private string _dhuhr = "";
        private string _asr = "";
        private string _maghrib = "";
        private string _isha = "";

        public string TodayLabel { get => _todayLabel; private set => Set(ref _todayLabel, value); }
        public string NextPrayer { get => _nextPrayer; private set => Set(ref _nextPrayer, value); }
        public string Countdown { get => _countdown; private set => Set(ref _countdown, value); }

        public string Fajr { get => _fajr; private set => Set(ref _fajr, value); }
        public string Sunrise { get => _sunrise; private set => Set(ref _sunrise, value); }
        public string Dhuhr { get => _dhuhr; private set => Set(ref _dhuhr, value); }
        public string Asr { get => _asr; private set => Set(ref _asr, value); }
        public string Maghrib { get => _maghrib; private set => Set(ref _maghrib, value); }
        public string Isha { get => _isha; private set => Set(ref _isha, value); }

        public ICommand OpenSettingsCommand { get; }

        // Draft values for Settings window (strings for safe input)
        public string DraftCityName { get => _draftCityName; set => Set(ref _draftCityName, value); }
        public string DraftLatitude { get => _draftLatitude; set => Set(ref _draftLatitude, value); }
        public string DraftLongitude { get => _draftLongitude; set => Set(ref _draftLongitude, value); }

        // Offline location picker (searchable)
        public string LocationQuery
        {
            get => _locationQuery;
            set
            {
                Set(ref _locationQuery, value);
                UpdateLocationMatches();
            }
        }

        // NOTE: LocationCatalogService should expose a public model type like LocationEntry or LocationItem.
        // In CLEAN v1 we bind to whatever the service returns from Search().
        public ObservableCollection<LocationItem> LocationMatches { get; } = new();

        public LocationItem? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                Set(ref _selectedLocation, value);
                if (value != null)
                {
                    DraftCityName = value.DisplayName;
                    DraftLatitude = value.Latitude.ToString(CultureInfo.InvariantCulture);
                    DraftLongitude = value.Longitude.ToString(CultureInfo.InvariantCulture);
                }
            }
        }

        public bool DraftNotificationsEnabled { get => _draftNotificationsEnabled; set => Set(ref _draftNotificationsEnabled, value); }
        public string DraftReminderMinutes { get => _draftReminderMinutes; set => Set(ref _draftReminderMinutes, value); }

        public CalcMethod DraftMethod { get => _draftMethod; set => Set(ref _draftMethod, value); }
        public AsrMadhhab DraftMadhhab { get => _draftMadhhab; set => Set(ref _draftMadhhab, value); }

        public IReadOnlyList<CalcMethod> Methods { get; } =
            new[] { CalcMethod.MuslimWorldLeague, CalcMethod.UmmAlQura, CalcMethod.NorthAmerica };

        public IReadOnlyList<AsrMadhhab> Madhhabs { get; } =
            new[] { AsrMadhhab.Shafi, AsrMadhhab.Hanafi };

        private string _draftCityName = "";
        private string _draftLatitude = "";
        private string _draftLongitude = "";

        // Offline location search support
        private string _locationQuery = "";
        private LocationItem? _selectedLocation;
        private bool _draftNotificationsEnabled;
        private string _draftReminderMinutes = "5";
        private CalcMethod _draftMethod;
        private AsrMadhhab _draftMadhhab;

        private TimeZoneInfo _tz;

        // Notifications (v1 uses WinForms NotifyIcon balloon)
        private System.Windows.Forms.NotifyIcon? _notify;
        private string? _lastNotifiedKey;

        public MainViewModel()
        {
            _settings = _store.Load();
            _tz = ResolveTimeZone(_settings.TimeZoneId);

            SeedDraftFromSettings();
            InitializeLocations();

            OpenSettingsCommand = new SimpleCommand(OpenSettings);

            Refresh();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
        }

        private void Refresh()
        {
            var r = _service.GetToday(_settings.Latitude, _settings.Longitude, _settings.Method, _settings.Madhhab, _tz);
            _last = r;

            TodayLabel = $"{_settings.CityName} • {r.DateLocal:dddd, MMM dd, yyyy}";
            Fajr = r.Fajr.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Sunrise = r.Sunrise.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Dhuhr = r.Dhuhr.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Asr = r.Asr.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Maghrib = r.Maghrib.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Isha = r.Isha.ToString("hh:mm tt", CultureInfo.InvariantCulture);

            var next = ComputeNextPrayer(r);
            NextPrayer = $"Next: {next.Name}";
            Countdown = $"In: {FormatCountdown(next.Remaining)}";

            // For IPR we keep notifications optional; this call is safe even if disabled
            NotifyIfNeeded(next.Name, next.Remaining);
        }

        private void OpenSettings()
        {
            try
            {
                SeedDraftFromSettings();
                InitializeLocations();

                var win = new SettingsWindow(this)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Settings");
            }
        }

        private void SeedDraftFromSettings()
        {
            DraftCityName = _settings.CityName;
            DraftLatitude = _settings.Latitude.ToString(CultureInfo.InvariantCulture);
            DraftLongitude = _settings.Longitude.ToString(CultureInfo.InvariantCulture);
            DraftMethod = _settings.Method;
            DraftMadhhab = _settings.Madhhab;
            DraftNotificationsEnabled = _settings.NotificationsEnabled;
            DraftReminderMinutes = _settings.ReminderMinutesBefore.ToString(CultureInfo.InvariantCulture);

            _draftMethod = DraftMethod;
            _draftMadhhab = DraftMadhhab;
        }

        public void DiscardSettingsDraft()
        {
            SeedDraftFromSettings();
            InitializeLocations();
        }

        public void ApplySettingsDraft(double parsedLat, double parsedLon, int reminderMinutes)
        {
            _settings.CityName = string.IsNullOrWhiteSpace(DraftCityName) ? "Custom Location" : DraftCityName.Trim();
            _settings.Latitude = parsedLat;
            _settings.Longitude = parsedLon;
            _settings.Method = DraftMethod;
            _settings.Madhhab = DraftMadhhab;
            _settings.NotificationsEnabled = DraftNotificationsEnabled;
            _settings.ReminderMinutesBefore = reminderMinutes;

            _store.Save(_settings);

            // reset notify state so next cycle can notify correctly
            _lastNotifiedKey = null;

            Refresh();

            // Quick sanity check: if notifications are enabled, show a small test balloon right after Save.
            if (_settings.NotificationsEnabled)
            {
                ShowTestNotification(
                    "Notifications enabled",
                    "Test popup: if you don't see this, Windows is likely suppressing balloons for this app."
                );
            }
        }

        private void ShowTestNotification(string title, string message)
        {
            try
            {
                EnsureNotifyIcon();
                _notify!.BalloonTipTitle = title;
                _notify!.BalloonTipText = message;
                _notify!.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                _notify!.ShowBalloonTip(5000);
            }
            catch { }
        }

        private void NotifyIfNeeded(string nextPrayerName, TimeSpan remaining)
        {
            if (!_settings.NotificationsEnabled)
            {
                DisposeNotifyIcon();
                _lastNotifiedKey = null;
                return;
            }

            EnsureNotifyIcon();

            var reminder = TimeSpan.FromMinutes(Math.Max(0, _settings.ReminderMinutesBefore));

            // _last can be null during startup edge-cases; keep key stable anyway
            var dateKey = (_last?.DateLocal ?? DateTime.Now).ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var key = $"{dateKey}:{nextPrayerName}";
            if (_lastNotifiedKey == key) return;

            // Notify when we're within the reminder window (including exactly at prayer time).
            if (remaining <= reminder && remaining >= TimeSpan.Zero)
            {
                var title = "Prayer Time Reminder";
                var msg = $"{nextPrayerName} in {FormatCountdown(remaining)}";

                try
                {
                    _notify!.BalloonTipTitle = title;
                    _notify!.BalloonTipText = msg;
                    _notify!.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                    _notify!.ShowBalloonTip(5000);
                    _lastNotifiedKey = key;
                }
                catch { }
            }
        }

        private void EnsureNotifyIcon()
        {
            if (_notify != null) return;

            try
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly()!.Location)
                           ?? System.Drawing.SystemIcons.Application;

                _notify = new System.Windows.Forms.NotifyIcon
                {
                    Icon = icon,
                    Visible = true,
                    Text = "Prayer Times Notifications"
                };
            }
            catch
            {
                _notify = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Application,
                    Visible = true,
                    Text = "Prayer Times Notifications"
                };
            }
        }

        private void DisposeNotifyIcon()
        {
            try
            {
                if (_notify == null) return;
                _notify.Visible = false;
                _notify.Dispose();
                _notify = null;
            }
            catch { }
        }

        private static TimeZoneInfo ResolveTimeZone(string tzId)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch { return TimeZoneInfo.Local; }
        }

        private static string FormatCountdown(TimeSpan ts)
        {
            if (ts.TotalSeconds < 1) return "00:00:00";
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

        // ----------------------------
        // NEXT-PRAYER COMPUTATION
        // ----------------------------
        private readonly struct NextPrayerInfo
        {
            public string Name { get; }
            public DateTime Time { get; }
            public TimeSpan Remaining { get; }

            public NextPrayerInfo(string name, DateTime time, TimeSpan remaining)
            {
                Name = name;
                Time = time;
                Remaining = remaining;
            }
        }

        private NextPrayerInfo ComputeNextPrayer(PrayerTimesResult r)
        {
            var now = DateTime.Now;
            // Build ordered schedule
            var schedule = new (string Name, DateTime Time)[]
            {
                ("Fajr", r.Fajr),
                ("Sunrise", r.Sunrise),
                ("Dhuhr", r.Dhuhr),
                ("Asr", r.Asr),
                ("Maghrib", r.Maghrib),
                ("Isha", r.Isha)
            };

            // Find first time strictly after now
            foreach (var item in schedule)
            {
                if (item.Time > now)
                    return new NextPrayerInfo(item.Name, item.Time, item.Time - now);
            }

            // If all passed, compute tomorrow Fajr (assume service returns "today" times; add 1 day)
            var tomorrowFajr = r.Fajr.AddDays(1);
            return new NextPrayerInfo("Fajr", tomorrowFajr, tomorrowFajr - now);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private sealed class SimpleCommand : ICommand
        {
            private readonly Action _run;
            public SimpleCommand(Action run) => _run = run;

            public bool CanExecute(object? parameter) => true;
            public void Execute(object? parameter) => _run();

            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }

        // ----------------------------
        // OFFLINE LOCATIONS
        // ----------------------------
        private void InitializeLocations()
        {
            try
            {
                // CLEAN v1: Load from JSON on demand; service can internally cache
                UpdateLocationMatches();
            }
            catch
            {
                // ignore - app can still run with manual lat/lon entry
            }
        }

        private void UpdateLocationMatches()
        {
            LocationMatches.Clear();

            var q = (LocationQuery ?? "").Trim();
            if (q.Length < 2) return;

            foreach (var item in _locationCatalog.Search(q).Take(50))
                LocationMatches.Add(item);
        }
    }
}
