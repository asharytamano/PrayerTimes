using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PrayerTimes.Services
{
    /// <summary>
    /// Offline location catalog loader + search (CLEAN v1).
    /// GUARANTEE: even if JSON is missing or malformed, Search() will return a small seed list.
    /// </summary>
    public class LocationCatalogService
    {
        private readonly string _jsonPath;

        public LocationCatalogService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _jsonPath = Path.Combine(baseDir, "Assets", "Data", "locations.json");
        }

        public List<LocationItem> LoadLocations()
        {
            try
            {
                Debug.WriteLine($"[LocationCatalogService] JSON Path: {_jsonPath}");

                if (!File.Exists(_jsonPath))
                {
                    Debug.WriteLine("[LocationCatalogService] File not found. Using seed list.");
                    return SeedLocations();
                }

                var json = File.ReadAllText(_jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var data = JsonSerializer.Deserialize<List<LocationItem>>(json, options);

                var list = data ?? new List<LocationItem>();
                Debug.WriteLine($"[LocationCatalogService] Loaded locations count: {list.Count}");

                // If deserialization yields empty list, keep UI usable
                return list.Count == 0 ? SeedLocations() : list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocationCatalogService] Load failed: {ex.Message}. Using seed list.");
                return SeedLocations();
            }
        }

        public List<LocationItem> Search(string keyword)
        {
            var all = LoadLocations();

            if (string.IsNullOrWhiteSpace(keyword))
                return all;

            keyword = keyword.Trim();

            var results = all
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.DisplayName) && x.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.Province) && x.Province.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(x.Region) && x.Region.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                )
                .OrderBy(x => x.DisplayName)
                .ToList();

            Debug.WriteLine($"[LocationCatalogService] Search '{keyword}' results: {results.Count}");
            return results;
        }

        private static List<LocationItem> SeedLocations()
        {
            return new List<LocationItem>
            {
                new LocationItem { DisplayName="Manila, NCR", Province="Metro Manila", Region="NCR", Latitude=14.5995, Longitude=120.9842 },
                new LocationItem { DisplayName="Quezon City, NCR", Province="Metro Manila", Region="NCR", Latitude=14.6760, Longitude=121.0437 },
                new LocationItem { DisplayName="Marikina, NCR", Province="Metro Manila", Region="NCR", Latitude=14.6507, Longitude=121.1029 },
                new LocationItem { DisplayName="Pasig, NCR", Province="Metro Manila", Region="NCR", Latitude=14.5764, Longitude=121.0851 },
                new LocationItem { DisplayName="Taguig, NCR", Province="Metro Manila", Region="NCR", Latitude=14.5176, Longitude=121.0509 },
                new LocationItem { DisplayName="Makati, NCR", Province="Metro Manila", Region="NCR", Latitude=14.5547, Longitude=121.0244 },
                new LocationItem { DisplayName="Muntinlupa, NCR", Province="Metro Manila", Region="NCR", Latitude=14.4081, Longitude=121.0415 },
                new LocationItem { DisplayName="Pateros, NCR", Province="Metro Manila", Region="NCR", Latitude=14.5442, Longitude=121.0686 },
                new LocationItem { DisplayName="Marawi City, Lanao del Sur", Province="Lanao del Sur", Region="BARMM", Latitude=8.0034, Longitude=124.2839 },
                new LocationItem { DisplayName="Tugaya, Lanao del Sur", Province="Lanao del Sur", Region="BARMM", Latitude=7.8878, Longitude=124.1486 },
            };
        }
    }

    public class LocationItem
    {
        public string DisplayName { get; set; } = "";
        public string Province { get; set; } = "";
        public string Region { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
