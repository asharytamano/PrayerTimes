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
using System.Windows.Media;
using System.Windows.Documents;
using PrayerTimes.ViewModels;

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

        // Add DetectStatus property for diagnostics
        private string _detectStatus = "";
        public string DetectStatus
        {
            get => _detectStatus;
            private set
            {
                if (_detectStatus != value)
                {
                    _detectStatus = value;
                    OnPropertyChanged();
                    Log($"DetectStatus: {value}");
                }
            }
        }

        private void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[PrayerTimes] {DateTime.Now:HH:mm:ss.fff}: {message}");
        }

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

        public ICommand TestAdhanCommand { get; }
        public ICommand StopAdhanCommand { get; }

        public bool IsAdhanPlaying
        {
            get => _isAdhanPlaying;
            private set => Set(ref _isAdhanPlaying, value);
        }
        private bool _isAdhanPlaying;

        public string PlayingAdhanText
        {
            get => _playingAdhanText;
            private set => Set(ref _playingAdhanText, value);
        }
        private string _playingAdhanText = "";

        // -------------------- Monthly Print (NEW) --------------------
        public int PrintYear { get => _printYear; set => Set(ref _printYear, value); }
        private int _printYear = DateTime.Now.Year;

        public int PrintMonth { get => _printMonth; set => Set(ref _printMonth, value); }
        private int _printMonth = DateTime.Now.Month; // 1-12

        public string PrintPagePreset { get => _printPagePreset; set => Set(ref _printPagePreset, value); }
        private string _printPagePreset = "LegalUS"; // or "A3"

        public ICommand GenerateMonthlyCommand { get; }
        public ICommand PrintMonthlyCommand { get; }

        private FixedDocument? _monthlyDocument;
        private string? _monthlyXpsPath;
        private string? _monthlyPdfPath;


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



        // ----------------------------
        // ADHAN SETTINGS (bindings for the right panel)
        // ----------------------------
        public IReadOnlyList<string> AzanVoiceOptions { get; } = new[]
        {
            "Hamza Al Majale",
            "Rabeh Al Jazairi",
            "Mishary Al-Afasy",
            "Mishary-Fajr",
            "Abdussamad-Fajr",
            "Mullah-Makkah",
            "Qassas-Madinah"
        };

        public bool AzanEnabled
        {
            get => _settings.AzanEnabled;
            set
            {
                if (_settings.AzanEnabled == value) return;
                _settings.AzanEnabled = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }

        public string DefaultAzanVoice
        {
            get => _settings.DefaultAzanVoice;
            set
            {
                var v = (value ?? "").Trim();
                if (string.Equals(_settings.DefaultAzanVoice, v, StringComparison.Ordinal)) return;
                _settings.DefaultAzanVoice = v;
                SaveSettings();
                OnPropertyChanged();
            }
        }

        public bool FajrAzanEnabled { get => _settings.FajrAzanEnabled; set { if (_settings.FajrAzanEnabled == value) return; _settings.FajrAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string FajrAzanVoice { get => _settings.FajrAzanVoice; set { var v = (value ?? ""); if (_settings.FajrAzanVoice == v) return; _settings.FajrAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }

        public bool SunriseAzanEnabled { get => _settings.SunriseAzanEnabled; set { if (_settings.SunriseAzanEnabled == value) return; _settings.SunriseAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string SunriseAzanVoice { get => _settings.SunriseAzanVoice; set { var v = (value ?? ""); if (_settings.SunriseAzanVoice == v) return; _settings.SunriseAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }

        public bool DhuhrAzanEnabled { get => _settings.DhuhrAzanEnabled; set { if (_settings.DhuhrAzanEnabled == value) return; _settings.DhuhrAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string DhuhrAzanVoice { get => _settings.DhuhrAzanVoice; set { var v = (value ?? ""); if (_settings.DhuhrAzanVoice == v) return; _settings.DhuhrAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }

        public bool AsrAzanEnabled { get => _settings.AsrAzanEnabled; set { if (_settings.AsrAzanEnabled == value) return; _settings.AsrAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string AsrAzanVoice { get => _settings.AsrAzanVoice; set { var v = (value ?? ""); if (_settings.AsrAzanVoice == v) return; _settings.AsrAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }

        public bool MaghribAzanEnabled { get => _settings.MaghribAzanEnabled; set { if (_settings.MaghribAzanEnabled == value) return; _settings.MaghribAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string MaghribAzanVoice { get => _settings.MaghribAzanVoice; set { var v = (value ?? ""); if (_settings.MaghribAzanVoice == v) return; _settings.MaghribAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }

        public bool IshaAzanEnabled { get => _settings.IshaAzanEnabled; set { if (_settings.IshaAzanEnabled == value) return; _settings.IshaAzanEnabled = value; SaveSettings(); OnPropertyChanged(); } }
        public string IshaAzanVoice { get => _settings.IshaAzanVoice; set { var v = (value ?? ""); if (_settings.IshaAzanVoice == v) return; _settings.IshaAzanVoice = v; SaveSettings(); OnPropertyChanged(); } }
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


        // Adhan playback
        private readonly MediaPlayer _azanPlayer = new();
        private readonly HashSet<string> _azanFiredKeys = new();
        private string? _lastAzanDateKey;
        public MainViewModel()
        {
            Log("MainViewModel constructor starting");
            _settings = _store.Load();
            _tz = ResolveTimeZone(_settings.TimeZoneId);

            SeedDraftFromSettings();
            InitializeLocations();

            OpenSettingsCommand = new SimpleCommand(OpenSettings);

            TestAdhanCommand = new SimpleCommand(TestAdhan);
            StopAdhanCommand = new SimpleCommand(StopAdhan);

            GenerateMonthlyCommand = new SimpleCommand(GenerateMonthly);
            PrintMonthlyCommand = new SimpleCommand(PrintMonthly);

            HookAzanPlayerEvents();


            Refresh();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
            Log("MainViewModel constructor completed");
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

            CheckAzanTriggers(r);
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



        private void TestAdhan()
        {
            Log("TestAdhan button clicked");
            // Always allow manual testing, regardless of AzanEnabled
            DetectStatus = "Testing Adhan audio...";
            var voice = ResolveVoice(null);
            Log($"Using voice: {voice}");
            PlayAzan(voice);
        }

        private void HookAzanPlayerEvents()
        {
            // Avoid multiple subscriptions; MediaPlayer allows duplicates if called repeatedly.
            _azanPlayer.MediaEnded -= AzanPlayer_MediaEnded;
            _azanPlayer.MediaFailed -= AzanPlayer_MediaFailed;

            _azanPlayer.MediaEnded += AzanPlayer_MediaEnded;
            _azanPlayer.MediaFailed += AzanPlayer_MediaFailed;
        }

        private void AzanPlayer_MediaEnded(object? sender, EventArgs e)
        {
            IsAdhanPlaying = false;
            PlayingAdhanText = "";
        }

        private void AzanPlayer_MediaFailed(object? sender, ExceptionEventArgs e)
        {
            IsAdhanPlaying = false;
            PlayingAdhanText = "";
            DetectStatus = $"Adhan error: {e.ErrorException?.Message}";
        }

        private void StopAdhan()
        {
            try
            {
                _azanPlayer.Stop();
            }
            catch { }

            IsAdhanPlaying = false;
            PlayingAdhanText = "";
            DetectStatus = "Stopped Adhan.";
        }

        private void GenerateMonthly()
        {
            try
            {
                if (PrintMonth < 1 || PrintMonth > 12) { DetectStatus = "Invalid month."; return; }
                if (PrintYear < 1900 || PrintYear > 2200) { DetectStatus = "Invalid year."; return; }
                if (_settings.Latitude == 0 || _settings.Longitude == 0) { DetectStatus = "Please set a location first."; return; }

                var vm = new MonthlyPrintViewModel(_service)
                {
                    Year = PrintYear,
                    Month = PrintMonth,
                    PagePreset = PrintPagePreset,
                    LocationName = string.IsNullOrWhiteSpace(_settings.CityName) ? "Selected Location" : _settings.CityName,
                    FooterLeft = "PrayerTimes Windows App",
                    FooterRight = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}"
                };

                _monthlyDocument = vm.BuildDocument(
                    _settings.Latitude,
                    _settings.Longitude,
                    _settings.Method,
                    _settings.Madhhab,
                    _tz
                );

                var outDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrayerTimes");
                var city = PrintService.SanitizeFilePart(string.IsNullOrWhiteSpace(_settings.CityName) ? "Location" : _settings.CityName);
                var baseName = $"PrayerTimes_{city}_{PrintYear:0000}-{PrintMonth:00}";

                _monthlyXpsPath = PrintService.TrySaveAsXps(_monthlyDocument, outDir, baseName);

                if (!string.IsNullOrWhiteSpace(_monthlyXpsPath))
                {
                    var pdfPath = System.IO.Path.Combine(outDir, baseName + ".pdf");
                    _monthlyPdfPath = PrintService.TryConvertXpsToPdf(_monthlyXpsPath, pdfPath);

                    if (_monthlyPdfPath != null)
                        DetectStatus = $"Monthly generated: XPS + PDF saved to {outDir}";
                    else
                        DetectStatus = $"Monthly generated: XPS saved to {outDir} (PDF export not available)";
                }
                else
                {
                    DetectStatus = "Monthly generated (in-memory). XPS export not available.";
                }
            }
            catch (Exception ex)
            {
                DetectStatus = $"Monthly generation failed: {ex.Message}";
            }
        }

        private void PrintMonthly()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_monthlyXpsPath) && System.IO.File.Exists(_monthlyXpsPath))
                {
                    if (PrintService.TryPrintFromXps(_monthlyXpsPath, "Prayer Times Monthly"))
                    {
                        DetectStatus = "Print job sent (from XPS).";
                        return;
                    }
                }

                if (_monthlyDocument == null)
                {
                    DetectStatus = "Please click Generate first.";
                    return;
                }

                PrintService.Print(_monthlyDocument);
                DetectStatus = "Print job sent.";
            }
            catch (Exception ex)
            {
                DetectStatus = $"Printing failed: {ex.Message}";
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

        private void CheckAzanTriggers(PrayerTimesResult r)
        {
            // Play within a small window after the exact time, once per prayer per day.
            // Sunrise is OFF by default but can be enabled by the user.
            if (!_settings.AzanEnabled) return;

            var now = DateTime.Now;
            var dateKey = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);


            // Prevent unbounded growth and ensure correct behavior across midnight
            if (!string.Equals(_lastAzanDateKey, dateKey, StringComparison.Ordinal))
            {
                _azanFiredKeys.Clear();
                _lastAzanDateKey = dateKey;
            }

            TryFireAzan(dateKey, "Fajr", r.Fajr, _settings.FajrAzanEnabled, ResolveVoice(_settings.FajrAzanVoice));
            TryFireAzan(dateKey, "Sunrise", r.Sunrise, _settings.SunriseAzanEnabled, ResolveVoice(_settings.SunriseAzanVoice));
            TryFireAzan(dateKey, "Dhuhr", r.Dhuhr, _settings.DhuhrAzanEnabled, ResolveVoice(_settings.DhuhrAzanVoice));
            TryFireAzan(dateKey, "Asr", r.Asr, _settings.AsrAzanEnabled, ResolveVoice(_settings.AsrAzanVoice));
            TryFireAzan(dateKey, "Maghrib", r.Maghrib, _settings.MaghribAzanEnabled, ResolveVoice(_settings.MaghribAzanVoice));
            TryFireAzan(dateKey, "Isha", r.Isha, _settings.IshaAzanEnabled, ResolveVoice(_settings.IshaAzanVoice));
        }

        private void TryFireAzan(string dateKey, string prayerName, DateTime at, bool enabled, string voice)
        {
            if (!enabled) return;

            var now = DateTime.Now;
            if (now < at) return;
            if (now > at.AddSeconds(25)) return; // avoid late triggers

            var key = $"{dateKey}:{prayerName}:azan";
            if (_azanFiredKeys.Contains(key)) return;

            _azanFiredKeys.Add(key);
            PlayAzan(voice);
        }

        private string ResolveVoice(string? perPrayerVoice)
        {
            var v = (perPrayerVoice ?? "").Trim();
            if (v.Length > 0) return v;

            var d = (_settings.DefaultAzanVoice ?? "").Trim();
            return d.Length > 0 ? d : "Mishary Al-Afasy";
        }

        private void PlayAzan(string voice)
        {
            try
            {
                Log($"PlayAzan called with voice: {voice}");
                var file = VoiceToFileName(voice);
                Log($"Audio file name: {file}");

                // Build a correct pack URI to locate the resource
                var asm = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "";
                Log($"Assembly name: {asm}");

                var safeFile = Uri.EscapeDataString(file);

                // Use the pack URI to find the resource
                var packUri = new Uri($"pack://application:,,,/{asm};component/Assets/Audio/{safeFile}", UriKind.Absolute);
                Log($"Looking for resource at: {packUri}");

                // Get the resource stream
                var info = System.Windows.Application.GetResourceStream(packUri);
                if (info?.Stream == null)
                {
                    var errorMsg = $"Adhan audio not found: {file}. Check if file exists in Assets/Audio folder.";
                    Log(errorMsg);
                    DetectStatus = errorMsg;
                    return;
                }

                Log($"SUCCESS: Found resource, stream length: {info.Stream.Length} bytes");

                // ALWAYS extract to temp file - MediaPlayer cannot play pack URIs directly
                // This is the fix for "Only site-of-origin pack URIs are supported for media"
                Log("Extracting audio to temp file...");
                var cacheDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PrayerTimes", "AdhanCache");
                System.IO.Directory.CreateDirectory(cacheDir);
                var tempFile = System.IO.Path.Combine(cacheDir, file);
                Log($"Temp file path: {tempFile}");

                // Check if file already exists and is up-to-date
                bool needsExtract = true;
                if (System.IO.File.Exists(tempFile))
                {
                    var fileInfo = new System.IO.FileInfo(tempFile);
                    if (fileInfo.Length == info.Stream.Length)
                    {
                        Log("Temp file already exists with correct size, reusing...");
                        needsExtract = false;
                    }
                    else
                    {
                        Log($"Temp file size mismatch: {fileInfo.Length} vs {info.Stream.Length}, re-extracting...");
                    }
                }

                if (needsExtract)
                {
                    // Reset stream position to beginning
                    info.Stream.Position = 0;

                    using (var fs = new System.IO.FileStream(tempFile, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read))
                    {
                        info.Stream.CopyTo(fs);
                        Log($"Copied {fs.Length} bytes to temp file");
                    }
                }

                DetectStatus = $"Playing Adhan: {voice}";

                // Set up event handlers for debugging
                _azanPlayer.MediaOpened += (s, e) => Log("MediaPlayer: MediaOpened event fired");
                _azanPlayer.MediaFailed += (s, e) => Log($"MediaPlayer: MediaFailed - {e.ErrorException?.Message}");
                _azanPlayer.MediaEnded += (s, e) => Log("MediaPlayer: MediaEnded event fired");

                // Play from temp file
                Log($"Opening and playing from temp file: {tempFile}");
                _azanPlayer.Stop();
                _azanPlayer.Open(new Uri(tempFile, UriKind.Absolute));
                _azanPlayer.Volume = 1.0; // Full volume
                _azanPlayer.Play();
                IsAdhanPlaying = true;
                PlayingAdhanText = $"Playing: {voice}";
                Log("Play command sent to MediaPlayer");
            }
            catch (Exception ex)
            {
                DetectStatus = $"Adhan play failed: {ex.Message}";
                Log(DetectStatus);
                Log($"Full error: {ex}");
            }
        }

        private static string VoiceToFileName(string voice)
        {
            return voice switch
            {
                "Hamza Al Majale" => "Hamza_Al_Majale.mp3",
                "Rabeh Al Jazairi" => "Rabeh_Al_Jazairi.mp3",
                "Mishary Al-Afasy" => "Mishary_Al-Afasy.mp3",
                "Mishary-Fajr" => "Mishary-Fajr.mp3",
                "Abdussamad-Fajr" => "Abdussamad-Fajr.mp3",
                "Mullah-Makkah" => "Mullah-Makkah.mp3",
                "Qassas-Madinah" => "Qassas-Madinah.mp3",
                _ => "Mishary_Al-Afasy.mp3"
            };
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

        private void SaveSettings()
        {
            try { _store.Save(_settings); }
            catch { }
        }

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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