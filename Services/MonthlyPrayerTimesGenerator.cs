using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PrayerTimesApp.Models;

namespace PrayerTimesApp.Services
{
    public static class MonthlyPrayerTimesGenerator
    {
        // Theme colors (you can tweak later)
        private static readonly Brush HeaderBg = (Brush)new BrushConverter().ConvertFromString("#1F6B3A")!;
        private static readonly Brush FooterBg = (Brush)new BrushConverter().ConvertFromString("#C9A23A")!;
        private static readonly Brush HeaderFg = Brushes.White;
        private static readonly Brush BodyFg = (Brush)new BrushConverter().ConvertFromString("#1B1B1B")!;

        public static FixedDocument BuildFixedDocument(MonthlyPrayerTimesPrintModel model, PagePreset preset)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var pageSize = GetPageSize(preset);

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = pageSize;

            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = Brushes.White
            };

            // Margins
            double margin = 36; // 0.5 inch at 72 dpi
            double contentWidth = pageSize.Width - margin * 2;

            // HEADER
            var header = BuildHeader(model, contentWidth);
            FixedPage.SetLeft(header, margin);
            FixedPage.SetTop(header, margin);
            fixedPage.Children.Add(header);

            // FOOTER
            var footer = BuildFooter(model, contentWidth);
            FixedPage.SetLeft(footer, margin);
            FixedPage.SetBottom(footer, margin);
            fixedPage.Children.Add(footer);

            // TABLE AREA
            double headerHeight = 120;
            double footerHeight = 72;
            double tableTop = margin + headerHeight + 18;
            double tableHeight = pageSize.Height - (margin + headerHeight + 18) - (margin + footerHeight);
            var table = BuildTable(model, contentWidth, tableHeight);

            FixedPage.SetLeft(table, margin);
            FixedPage.SetTop(table, tableTop);
            fixedPage.Children.Add(table);

            pageContent.Child = fixedPage;
            doc.Pages.Add(pageContent);

            return doc;
        }

        private static Size GetPageSize(PagePreset preset)
        {
            // WPF device-independent units: 96 units per inch
            return preset switch
            {
                PagePreset.LegalUS => new Size(8.5 * 96, 14.0 * 96), // 816 x 1344
                PagePreset.A3 => new Size(11.69 * 96, 16.54 * 96),   // ~1122 x 1588 (portrait)
                _ => new Size(8.5 * 96, 14.0 * 96)
            };
        }

        private static Border BuildHeader(MonthlyPrayerTimesPrintModel model, double width)
        {
            var grid = new Grid { Width = width, Height = 120 };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = model.MonthTitle,
                Foreground = HeaderFg,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(18, 14, 18, 4)
            };

            var sub = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(model.SubTitle) ? model.LocationName : $"{model.LocationName} • {model.SubTitle}",
                Foreground = HeaderFg,
                FontSize = 14,
                Opacity = 0.95,
                Margin = new Thickness(18, 0, 18, 14)
            };

            grid.Children.Add(title);
            Grid.SetRow(title, 0);

            grid.Children.Add(sub);
            Grid.SetRow(sub, 1);

            return new Border
            {
                Background = HeaderBg,
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        private static Border BuildFooter(MonthlyPrayerTimesPrintModel model, double width)
        {
            var grid = new Grid { Width = width, Height = 72 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new TextBlock
            {
                Text = model.FooterLeft,
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(14, 10, 14, 10),
                VerticalAlignment = VerticalAlignment.Center
            };

            var right = new TextBlock
            {
                Text = model.FooterRight,
                Foreground = Brushes.White,
                FontSize = 12,
                Margin = new Thickness(14, 10, 14, 10),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };

            grid.Children.Add(left);
            Grid.SetColumn(left, 0);

            grid.Children.Add(right);
            Grid.SetColumn(right, 1);

            return new Border
            {
                Background = FooterBg,
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        private static Border BuildTable(MonthlyPrayerTimesPrintModel model, double width, double height)
        {
            var outer = new Border
            {
                Width = width,
                Height = height,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#D4D4D4")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Background = Brushes.White
            };

            var grid = new Grid();
            outer.Child = grid;

            // Columns: AH | AD | DAY | FAJR | SUN | DHUHR | ASR | MAGHRIB | ISHA
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // AH
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) }); // AD
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) }); // DAY
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // Fajr
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // Sun
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // Dhuhr
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // Asr
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Maghrib
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // Isha

            // Header row
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddHeaderCell(grid, 0, "AH");
            AddHeaderCell(grid, 1, "AD");
            AddHeaderCell(grid, 2, "DAY");
            AddHeaderCell(grid, 3, "FAJR");
            AddHeaderCell(grid, 4, "SUN");
            AddHeaderCell(grid, 5, "DHUHR");
            AddHeaderCell(grid, 6, "ASR");
            AddHeaderCell(grid, 7, "MAGHRIB");
            AddHeaderCell(grid, 8, "ISHA");

            int r = 1;
            foreach (var row in model.Rows)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                AddBodyCell(grid, r, 0, row.HijriText);
                AddBodyCell(grid, r, 1, row.Date.ToString("dd"));
                AddBodyCell(grid, r, 2, row.DayName);
                AddBodyCell(grid, r, 3, row.Fajr);
                AddBodyCell(grid, r, 4, row.Sunrise);
                AddBodyCell(grid, r, 5, row.Dhuhr);
                AddBodyCell(grid, r, 6, row.Asr);
                AddBodyCell(grid, r, 7, row.Maghrib);
                AddBodyCell(grid, r, 8, row.Isha);

                r++;
            }

            return outer;
        }

        private static void AddHeaderCell(Grid g, int c, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 6, 6, 6)
            };

            var b = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString("#2C7A49")!,
                CornerRadius = new CornerRadius(6),
                Child = tb,
                Margin = new Thickness(2)
            };

            g.Children.Add(b);
            Grid.SetRow(b, 0);
            Grid.SetColumn(b, c);
        }

        private static void AddBodyCell(Grid g, int r, int c, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Foreground = BodyFg,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 4, 6, 4)
            };

            var b = new Border
            {
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E6E6E6")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Child = tb,
                Margin = new Thickness(2)
            };

            g.Children.Add(b);
            Grid.SetRow(b, r);
            Grid.SetColumn(b, c);
        }
    }
}
