// MonthlyPrayerTimesGenerator.cs
// NOTE: WPF-only types are explicitly aliased to avoid System.Drawing ambiguities.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

// Aliases (in case any other file pulls System.Drawing into the compilation unit)
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using WpfSize = System.Windows.Size;

namespace PrayerTimes.Services
{
    /// <summary>
    /// Monthly prayer times printable document builder.
    /// Reflection-based: avoids compile-time dependency on Models to prevent namespace/model-shape mismatches.
    ///
    /// Columns: AH | AD | DAY | FAJR | DHUHR | ASR | MAGHRIB | ISHA  (NO SUNRISE)
    /// Friday rows: highlighted.
    /// </summary>
    public sealed class MonthlyPrayerTimesGenerator
    {
        // Theme
        private static readonly MediaBrush HeaderBg = (MediaBrush)new BrushConverter().ConvertFromString("#F2E6C8")!; // parchment
        private static readonly MediaBrush FooterBg = (MediaBrush)new BrushConverter().ConvertFromString("#145A32")!; // deep green
        private static readonly MediaBrush Gold = (MediaBrush)new BrushConverter().ConvertFromString("#C9A24A")!; // gold
        private static readonly MediaBrush FridayBg = (MediaBrush)new BrushConverter().ConvertFromString("#FFF2C9")!; // soft highlight
        private static readonly MediaBrush CellBg = (MediaBrush)new BrushConverter().ConvertFromString("#FBF4E6")!; // light parchment
        private static readonly MediaBrush TextDark = (MediaBrush)new BrushConverter().ConvertFromString("#1B2A22")!;
        private static readonly MediaBrush HeaderTxt = (MediaBrush)new BrushConverter().ConvertFromString("#0F6B3A")!;

        /// <summary>
        /// Static wrapper: call this as MonthlyPrayerTimesGenerator.BuildFixedDocument(model)
        /// </summary>
        public static FixedDocument BuildFixedDocument(object model)
            => new MonthlyPrayerTimesGenerator().BuildFixedDocumentInstance(model);

        /// <summary>
        /// Instance builder.
        /// </summary>
        public FixedDocument BuildFixedDocumentInstance(object model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var doc = new FixedDocument();
            var page = CreateFixedPage(model);

            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(page);
            doc.Pages.Add(pageContent);

            return doc;
        }

        private FixedPage CreateFixedPage(object model)
        {
            string preset = GetString(model, "PagePreset", "LegalUS");
            WpfSize pageSize = preset.Equals("A3", StringComparison.OrdinalIgnoreCase)
                ? new WpfSize(1122, 1587)   // A3 @ 96 DPI approx
                : new WpfSize(816, 1344);   // US Legal 8.5x14 @ 96 DPI

            var page = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = MediaBrushes.White
            };

            double outerMargin = 36;

            var frame = new Border
            {
                Width = pageSize.Width - (outerMargin * 2),
                Height = pageSize.Height - (outerMargin * 2),
                Background = MediaBrushes.White,
                BorderBrush = Gold,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(22)
            };
            FixedPage.SetLeft(frame, outerMargin);
            FixedPage.SetTop(frame, outerMargin);
            page.Children.Add(frame);

            var root = new Grid();
            frame.Child = root;

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // table
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // footer

            var header = BuildHeader(model);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var table = BuildTable(model);
            Grid.SetRow(table, 2);
            root.Children.Add(table);

            var footer = BuildFooter(model);
            Grid.SetRow(footer, 4);
            root.Children.Add(footer);

            return page;
        }

        private UIElement BuildHeader(object model)
        {
            string title = GetString(model, "HeaderTitle", GetString(model, "Title", "Monthly Prayer Times"));
            string subtitle = GetString(model, "HeaderSubtitle", "");
            string right = GetString(model, "HeaderRight", GetString(model, "MonthTitle", ""));

            string locationLine = GetString(model, "LocationName", GetString(model, "LocationLine", ""));
            if (string.IsNullOrWhiteSpace(subtitle) && !string.IsNullOrWhiteSpace(locationLine))
                subtitle = locationLine;

            var border = new Border
            {
                Background = HeaderBg,
                BorderBrush = Gold,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            border.Child = g;

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = HeaderTxt
            });

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                left.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Foreground = TextDark,
                    Opacity = 0.85,
                    Margin = new Thickness(0, 6, 0, 0)
                });
            }

            g.Children.Add(left);

            var rightTb = new TextBlock
            {
                Text = right ?? "",
                FontSize = 12,
                Foreground = TextDark,
                Opacity = 0.9,
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetColumn(rightTb, 1);
            g.Children.Add(rightTb);

            return border;
        }

        private UIElement BuildFooter(object model)
        {
            string left = GetString(model, "FooterLeft", "Generated by PrayerTimes");
            string right = GetString(model, "FooterRight", DateTime.Now.ToString("yyyy-MM-dd"));

            var border = new Border
            {
                Background = FooterBg,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            border.Child = g;

            g.Children.Add(new TextBlock
            {
                Text = left,
                Foreground = MediaBrushes.White,
                FontSize = 11,
                Opacity = 0.95,
                VerticalAlignment = VerticalAlignment.Center
            });

            var r = new TextBlock
            {
                Text = right,
                Foreground = MediaBrushes.White,
                FontSize = 11,
                Opacity = 0.95,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(r, 1);
            g.Children.Add(r);

            return border;
        }

        private UIElement BuildTable(object model)
        {
            var rowsObj = GetPropertyValue(model, "Rows")
                       ?? GetPropertyValue(model, "Days")
                       ?? GetPropertyValue(model, "Items");

            var rows = (rowsObj as IEnumerable)?.Cast<object>().ToList() ?? Enumerable.Empty<object>().ToList();

            // NO SUNRISE
            var headers = new[] { "AH", "AD", "DAY", "FAJR", "DHUHR", "ASR", "MAGHRIB", "ISHA" };

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // AH
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // AD
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // DAY
            for (int i = 0; i < 5; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // header
            for (int i = 0; i < rows.Count; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            for (int c = 0; c < headers.Length; c++)
                grid.Children.Add(Cell(headers[c], 0, c, isHeader: true, isFriday: false));

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                bool isFriday = IsFriday(row);

                grid.Children.Add(Cell(GetString(row, "HijriDateText", GetString(row, "Hijri", "")), i + 1, 0, false, isFriday));
                grid.Children.Add(Cell(GetGregorianShort(row), i + 1, 1, false, isFriday));
                grid.Children.Add(Cell(GetDayText(row), i + 1, 2, false, isFriday));

                grid.Children.Add(Cell(GetTimeText(row, "Fajr"), i + 1, 3, false, isFriday));
                grid.Children.Add(Cell(GetTimeText(row, "Dhuhr"), i + 1, 4, false, isFriday));
                grid.Children.Add(Cell(GetTimeText(row, "Asr"), i + 1, 5, false, isFriday));
                grid.Children.Add(Cell(GetTimeText(row, "Maghrib"), i + 1, 6, false, isFriday));
                grid.Children.Add(Cell(GetTimeText(row, "Isha"), i + 1, 7, false, isFriday));
            }

            return new Border
            {
                BorderBrush = Gold,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        private UIElement Cell(string text, int row, int col, bool isHeader, bool isFriday)
        {
            MediaBrush bg;
            MediaBrush fg;
            FontWeight fw;
            double fs;

            if (isHeader)
            {
                bg = FooterBg;
                fg = MediaBrushes.White;
                fw = FontWeights.SemiBold;
                fs = 12;
            }
            else
            {
                bg = isFriday ? FridayBg : CellBg;
                fg = TextDark;
                fw = FontWeights.Normal;
                fs = 11.5;
            }

            var b = new Border
            {
                Background = bg,
                BorderBrush = Gold,
                BorderThickness = new Thickness(0.6),
                Padding = new Thickness(6, 5, 6, 5),
                Child = new TextBlock
                {
                    Text = text ?? "",
                    Foreground = fg,
                    FontWeight = fw,
                    FontSize = fs,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                }
            };

            Grid.SetRow(b, row);
            Grid.SetColumn(b, col);
            return b;
        }

        private static object? GetPropertyValue(object obj, string propName)
        {
            var t = obj.GetType();
            var p = t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            return p?.GetValue(obj);
        }

        private static string GetString(object obj, string propName, string fallback)
        {
            var v = GetPropertyValue(obj, propName);
            return v?.ToString() ?? fallback;
        }

        private static string GetTimeText(object row, string key)
        {
            return GetString(row, key + "Text",
                   GetString(row, key,
                   GetString(row, key + "Time", "")));
        }

        private static string GetDayText(object row)
        {
            var day = GetString(row, "DayName", GetString(row, "Day", ""));
            if (!string.IsNullOrWhiteSpace(day)) return day.ToUpperInvariant();

            if (TryGetDate(row, out var dt))
                return dt.ToString("ddd", CultureInfo.InvariantCulture).ToUpperInvariant();

            return "";
        }

        private static string GetGregorianShort(object row)
        {
            var s = GetString(row, "GregorianDateText", GetString(row, "Gregorian", ""));
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                    return dt.ToString("MM-dd", CultureInfo.InvariantCulture);
                return s;
            }

            if (TryGetDate(row, out var dt2))
                return dt2.ToString("MM-dd", CultureInfo.InvariantCulture);

            return "";
        }

        private static bool IsFriday(object row)
        {
            var day = GetString(row, "DayName", GetString(row, "Day", ""));
            if (!string.IsNullOrWhiteSpace(day))
                return day.Trim().Equals("Friday", StringComparison.OrdinalIgnoreCase)
                    || day.Trim().Equals("Fri", StringComparison.OrdinalIgnoreCase);

            if (TryGetDate(row, out var dt))
                return dt.DayOfWeek == DayOfWeek.Friday;

            var g = GetString(row, "GregorianDateText", "");
            if (DateTime.TryParse(g, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt2))
                return dt2.DayOfWeek == DayOfWeek.Friday;

            return false;
        }

        private static bool TryGetDate(object row, out DateTime dt)
        {
            dt = default;

            var v = GetPropertyValue(row, "GregorianDate")
                 ?? GetPropertyValue(row, "Date")
                 ?? GetPropertyValue(row, "LocalDate");

            if (v is DateTime d) { dt = d; return true; }

            if (v != null && DateTime.TryParse(v.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                dt = parsed;
                return true;
            }
            return false;
        }
    }
}
