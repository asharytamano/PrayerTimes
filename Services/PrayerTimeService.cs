using System;
using System.Linq;
using System.Reflection;
using Batoulapps.Adhan;

namespace PrayerTimes.Services
{
    public enum CalcMethod { MuslimWorldLeague, UmmAlQura, NorthAmerica }
    public enum AsrMadhhab { Shafi, Hanafi }

    public sealed class PrayerTimesResult
    {
        public DateTime DateLocal { get; init; }
        public DateTime Fajr { get; init; }
        public DateTime Sunrise { get; init; }
        public DateTime Dhuhr { get; init; }
        public DateTime Asr { get; init; }
        public DateTime Maghrib { get; init; }
        public DateTime Isha { get; init; }
        public string NextPrayerName { get; init; } = "";
        public TimeSpan TimeToNextPrayer { get; init; }
    }

    public sealed class PrayerTimeService
    {
        public PrayerTimesResult GetToday(double latitude, double longitude, CalcMethod method, AsrMadhhab madhhab, TimeZoneInfo tz)
        {
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var coords = new Coordinates(latitude, longitude);

            // CalculationMethod enum exists in your DLL :contentReference[oaicite:2]{index=2}
            var calcMethod = ResolveCalcMethod(method);

            // Extension exists: CalculationMethodExtensions.GetParameters :contentReference[oaicite:3]{index=3}
            var parameters = CalculationMethodExtensions.GetParameters(calcMethod);

            SetMadhab(parameters, madhhab);

            // PrayerTimes type exists, but constructor expects Internal.DateComponents in your build :contentReference[oaicite:4]{index=4}
            object dateComponents = CreateDateComponents(nowLocal.Date);
            object pt = CreatePrayerTimes(coords, dateComponents, parameters);

            var pFajr = ResolvePrayer("Fajr");
            var pSunrise = ResolvePrayer("Sunrise");
            var pDhuhr = ResolvePrayer("Dhuhr");
            var pAsr = ResolvePrayer("Asr");
            var pMaghrib = ResolvePrayer("Maghrib");
            var pIsha = ResolvePrayer("Isha");

            DateTime fajr = ToLocal(InvokeTimeForPrayer(pt, pFajr), tz);
            DateTime sunrise = ToLocal(InvokeTimeForPrayer(pt, pSunrise), tz);
            DateTime dhuhr = ToLocal(InvokeTimeForPrayer(pt, pDhuhr), tz);
            DateTime asr = ToLocal(InvokeTimeForPrayer(pt, pAsr), tz);
            DateTime maghrib = ToLocal(InvokeTimeForPrayer(pt, pMaghrib), tz);
            DateTime isha = ToLocal(InvokeTimeForPrayer(pt, pIsha), tz);

            // IMPORTANT: Do NOT rely on Adhan's NextPrayer() here.
            // In some builds/ports it can get "stuck" (e.g., always returns Fajr).
            // We compute the next upcoming prayer using the actual local times we just calculated.
            string nextName;
            DateTime nextTime;

            if (nowLocal < fajr)
            {
                nextName = "Fajr";
                nextTime = fajr;
            }
            else if (nowLocal < dhuhr)
            {
                nextName = "Dhuhr";
                nextTime = dhuhr;
            }
            else if (nowLocal < asr)
            {
                nextName = "Asr";
                nextTime = asr;
            }
            else if (nowLocal < maghrib)
            {
                nextName = "Maghrib";
                nextTime = maghrib;
            }
            else if (nowLocal < isha)
            {
                nextName = "Isha";
                nextTime = isha;
            }
            else
            {
                // All prayers for today passed -> next is tomorrow's Fajr
                var tomorrowNoon = nowLocal.Date.AddDays(1).AddHours(12);
                object dc2 = CreateDateComponents(tomorrowNoon.Date);
                object pt2 = CreatePrayerTimes(coords, dc2, parameters);

                nextName = "Fajr";
                nextTime = ToLocal(InvokeTimeForPrayer(pt2, pFajr), tz);
            }

            var remaining = nextTime - nowLocal;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            return new PrayerTimesResult
            {
                DateLocal = nowLocal.Date,
                Fajr = fajr,
                Sunrise = sunrise,
                Dhuhr = dhuhr,
                Asr = asr,
                Maghrib = maghrib,
                Isha = isha,
                NextPrayerName = nextName,
                TimeToNextPrayer = remaining
            };
        }

        // ---------- Adhan port helpers (reflection where needed) ----------

        private static object CreateDateComponents(DateTime localDate)
        {
            // Type exists: Batoulapps.Adhan.Internal.DateComponents :contentReference[oaicite:6]{index=6}
            var t = typeof(CalculationMethod).Assembly.GetType("Batoulapps.Adhan.Internal.DateComponents", throwOnError: true)!;

            // Try ctor(int year, int month, int day)
            var ctorYmd = t.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                           binder: null,
                                           types: new[] { typeof(int), typeof(int), typeof(int) },
                                           modifiers: null);
            if (ctorYmd != null)
                return ctorYmd.Invoke(new object[] { localDate.Year, localDate.Month, localDate.Day });

            // Try any ctor with 3 ints (fallback)
            var any3Int = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          .FirstOrDefault(c =>
                          {
                              var ps = c.GetParameters();
                              return ps.Length == 3 && ps.All(p => p.ParameterType == typeof(int));
                          });
            if (any3Int != null)
                return any3Int.Invoke(new object[] { localDate.Year, localDate.Month, localDate.Day });

            throw new InvalidOperationException("Cannot construct Batoulapps.Adhan.Internal.DateComponents (no suitable constructor found).");
        }

        private static object CreatePrayerTimes(Coordinates coords, object dateComponents, CalculationParameters parameters)
        {
            // Type exists: Batoulapps.Adhan.PrayerTimes :contentReference[oaicite:7]{index=7}
            var t = typeof(CalculationMethod).Assembly.GetType("Batoulapps.Adhan.PrayerTimes", throwOnError: true)!;

            // Find ctor(Coordinates, DateComponents, CalculationParameters)
            var ctor = t.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(c =>
                        {
                            var ps = c.GetParameters();
                            return ps.Length == 3
                                   && ps[0].ParameterType == typeof(Coordinates)
                                   && ps[1].ParameterType.FullName == "Batoulapps.Adhan.Internal.DateComponents"
                                   && ps[2].ParameterType == typeof(CalculationParameters);
                        });

            if (ctor == null)
                throw new InvalidOperationException("Cannot find PrayerTimes(Coordinates, DateComponents, CalculationParameters) constructor.");

            return ctor.Invoke(new object[] { coords, dateComponents, parameters });
        }

        private static object InvokeTimeForPrayer(object pt, Prayer prayer)
        {
            var m = pt.GetType().GetMethod("TimeForPrayer", BindingFlags.Public | BindingFlags.Instance);
            if (m == null) throw new InvalidOperationException("PrayerTimes.TimeForPrayer(...) not found.");
            return m.Invoke(pt, new object[] { prayer }) ?? throw new InvalidOperationException("TimeForPrayer returned null.");
        }

        private static object InvokeNoArg(object obj, string methodName)
        {
            var m = obj.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi => mi.Name == methodName && mi.GetParameters().Length == 0);

            if (m == null)
                throw new InvalidOperationException($"No zero-parameter method found: {methodName}()");

            return m.Invoke(obj, null) ?? throw new InvalidOperationException($"{methodName} returned null.");
        }
        private static DateTime ToLocal(object timeValue, TimeZoneInfo tz)
        {
            // The Adhan 0.9.0 port you're using often returns "unspecified" DateTime values that are actually UTC.
            // So our safest rule is:
            // - Utc -> convert to tz
            // - Unspecified -> assume UTC, convert to tz
            // - Local -> return as-is

            if (timeValue is DateTime dt)
            {
                if (dt.Kind == DateTimeKind.Local) return dt;

                if (dt.Kind == DateTimeKind.Utc)
                    return TimeZoneInfo.ConvertTimeFromUtc(dt, tz);

                // Unspecified: treat as UTC
                var assumedUtc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(assumedUtc, tz);
            }

            if (timeValue is DateTimeOffset dto)
                return TimeZoneInfo.ConvertTimeFromUtc(dto.UtcDateTime, tz);

            // Internal time type: DateTimeExtensions.GetTime(x)
            var asm = typeof(CalculationMethod).Assembly;
            var ext = asm.GetType("Batoulapps.Adhan.Internal.DateTimeExtensions", throwOnError: false);

            if (ext != null)
            {
                var getTime = ext.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(mi => mi.Name == "GetTime" && mi.GetParameters().Length == 1);

                if (getTime != null)
                {
                    var r = getTime.Invoke(null, new[] { timeValue });
                    if (r is DateTime dt2)
                    {
                        if (dt2.Kind == DateTimeKind.Local) return dt2;

                        if (dt2.Kind == DateTimeKind.Utc)
                            return TimeZoneInfo.ConvertTimeFromUtc(dt2, tz);

                        // Unspecified: treat as UTC
                        var assumedUtc = DateTime.SpecifyKind(dt2, DateTimeKind.Utc);
                        return TimeZoneInfo.ConvertTimeFromUtc(assumedUtc, tz);
                    }
                }
            }

            throw new InvalidOperationException("Unsupported time value type from Adhan: " + timeValue.GetType().FullName);
        }
        private static CalculationMethod ResolveCalcMethod(CalcMethod method)
        {
            var all = Enum.GetValues(typeof(CalculationMethod)).Cast<CalculationMethod>().ToArray();

            string[] keys = method switch
            {
                CalcMethod.UmmAlQura => new[] { "umm", "qura" },
                CalcMethod.NorthAmerica => new[] { "north", "america", "isna" },
                _ => new[] { "muslim", "world", "league", "mwl" }
            };

            foreach (var v in all)
            {
                var n = v.ToString().ToLowerInvariant();
                if (keys.Any(k => n.Contains(k))) return v;
            }

            return all.Length > 0 ? all[0] : default;
        }

        private static Prayer ResolvePrayer(string keyword)
        {
            var all = Enum.GetValues(typeof(Prayer)).Cast<Prayer>().ToArray();
            var key = keyword.ToLowerInvariant();

            foreach (var v in all)
            {
                var n = v.ToString().ToLowerInvariant();
                if (n.Contains(key)) return v;
            }

            return all.Length > 0 ? all[0] : default;
        }

        private static void SetMadhab(CalculationParameters parameters, AsrMadhhab madhhab)
        {
            var all = Enum.GetValues(typeof(Madhab)).Cast<Madhab>().ToArray();
            string[] keys = (madhhab == AsrMadhhab.Hanafi) ? new[] { "hanafi" } : new[] { "shafi", "shafii" };

            var selected = all.Length > 0 ? all[0] : default;
            foreach (var v in all)
            {
                var n = v.ToString().ToLowerInvariant();
                if (keys.Any(k => n.Contains(k))) { selected = v; break; }
            }

            // set via reflection (CalculationParameters has no public props listed in dump) :contentReference[oaicite:9]{index=9}
            var p = parameters.GetType().GetProperty("Madhab", BindingFlags.Public | BindingFlags.Instance);
            if (p != null && p.CanWrite)
                p.SetValue(parameters, selected);
        }
    }
}
