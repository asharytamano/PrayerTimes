using System;
using System.IO;
using System.Text.Json;

namespace PrayerTimes.Settings
{
    public sealed class SettingsStore
    {
        private const string AppFolderName = "PrayerTimes";
        private const string FileName = "settings.json";

        public string SettingsPath { get; }

        public SettingsStore()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppFolderName);

            Directory.CreateDirectory(dir);
            SettingsPath = Path.Combine(dir, FileName);
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return s ?? new AppSettings();
            }
            catch
            {
                // If anything goes wrong, fall back to defaults.
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsPath, json);
        }
    }
}
