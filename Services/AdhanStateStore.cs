// FILE: AdhanStateStore.cs
// PURPOSE: Prevent duplicate adhan plays; stores last-fired per-wakt timestamps (per-day).
// NOTE: Adjust namespace to match your project.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PrayerTimesApp.Settings;

namespace PrayerTimesApp.Services
{
    public sealed class AdhanStateStore
    {
        private readonly string _path;

        public AdhanStateStore(string appFolder)
        {
            Directory.CreateDirectory(appFolder);
            _path = Path.Combine(appFolder, "adhan_state.json");
        }

        public Dictionary<string, DateTime> Load()
        {
            try
            {
                if (!File.Exists(_path)) return new Dictionary<string, DateTime>();
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json) ?? new Dictionary<string, DateTime>();
            }
            catch
            {
                return new Dictionary<string, DateTime>();
            }
        }

        public void Save(Dictionary<string, DateTime> state)
        {
            try
            {
                var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_path, json);
            }
            catch { /* ignore */ }
        }

        public static string Key(DateTime dateLocal, WaktName wakt) =>
            $"{dateLocal:yyyy-MM-dd}:{wakt}";
    }
}
