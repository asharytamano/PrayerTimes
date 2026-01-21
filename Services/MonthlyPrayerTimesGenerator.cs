using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

// WPF aliases to avoid System.Drawing and WinForms ambiguities
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

using PrayerTimes.Models;

namespace PrayerTimes.Services
{
    public static class MonthlyPrayerTimesGenerator
    {
        private static readonly WpfBrush HeaderBg = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#1F6B3A"));
        private static readonly WpfBrush FooterBg = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#C9A23A"));
        private static readonly WpfBrush HeaderFg = WpfBrushes.White;
        private static readonly WpfBrush BodyFg = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#1B1B1B"));

        // pagePreset: "LegalUS" or "A3"
        public static FixedDocument BuildFixedDocument(MonthlyPrayerTimesPrintModel model, string pagePreset)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            System.Windows.Size pageSize = GetPageSize(pagePreset);

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = pageSize;

            var fixedPage = new FixedPage
            {
                Width = pageSize.Width,
                Height = pageSize.Height,
                Background = WpfBrushes.White
            };

            double margin = 36;
            double contentWidth = pageSize.Width - (margin * 2);

            const double headerHeight = 120;
            const double footerHeight = 72;
            const double gap = 18;

            var header = BuildHeader(model, contentWidth, headerHeight);
            FixedPage.SetLeft(header, margin);
            FixedPage.SetTop(header, margin);
            fixedPage.Children.Add(header);

            double footerTop = pageSize.Height - margin - footerHeight;
            var footer = BuildFooter(model, contentWidth, footerHeight);
            FixedPage.SetLeft(footer, margin);
            FixedPage.SetTop(footer, footerTop);
            fixedPage.Children.Add(footer);

            double tableTop = margin + headerHeight + gap;
            double tableBottom = footerTop - gap;
            double tableHeight = Math.Max(120, tableBottom - tableTop);

            var table = BuildTable(model, contentWidth, tableHeight);
            FixedPage.SetLeft(table, margin);
            FixedPage.SetTop(table, tableTop);
            fixedPage.Children.Add(table);

            var pageContent = new PageContent { Child = fixedPage };
            doc.Pages.Add(pageContent);

            return doc;
        }

        private static System.Windows.Size GetPageSize(string preset)
        {
            preset = (preset ?? "").Trim().ToUpperInvariant();
            if (preset == "A3")
                return new System.Windows.Size(11.69 * 96, 16.54 * 96); // A3 portrait

            return new System.Windows.Size(8.5 * 96, 14.0 * 96); // Legal US portrait
        }

        private static Border BuildHeader(MonthlyPrayerTimesPrintModel model, double width, double height)
        {
            var grid = new Grid { Width = width, Height = height };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = model.MonthTitle,
                Foreground = HeaderFg,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(18, 14, 18, 0)
            };

            var location = new TextBlock
            {
                Text = model.LocationName,
                Foreground = HeaderFg,
                FontSize = 14,
                Opacity = 0.95,
                Margin = new Thickness(18, 4, 18, 0)
            };

            var hijri = new TextBlock
            {
                Text = model.HijriMonthTitle,
                Foreground = HeaderFg,
                FontSize = 13,
                Opacity = 0.92,
                Margin = new Thickness(18, 4, 18, 14)
            };

            grid.Children.Add(title); Grid.SetRow(title, 0);
            grid.Children.Add(location); Grid.SetRow(location, 1);
            grid.Children.Add(hijri); Grid.SetRow(hijri, 2);

            return new Border
            {
                Background = HeaderBg,
                CornerRadius = new CornerRadius(10),
                Child = grid
            };
        }

        private static Border BuildFooter(MonthlyPrayerTimesPrintModel model, double width, double height)
        {
            var grid = new Grid { Width = width, Height = height };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var left = new TextBlock
            {
                Text = model.FooterLeft,
                Foreground = WpfBrushes.White,
                FontSize = 12,
                Margin = new Thickness(14, 10, 14, 10),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

            var right = new TextBlock
            {
                Text = model.FooterRight,
                Foreground = WpfBrushes.White,
                FontSize = 12,
                Margin = new Thickness(14, 10, 14, 10),
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };

            grid.Children.Add(left); Grid.SetColumn(left, 0);
            grid.Children.Add(right); Grid.SetColumn(right, 1);

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
                BorderBrush = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#D4D4D4")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Background = WpfBrushes.White
            };

            var grid = new Grid();
            outer.Child = grid;

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // AH
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) }); // AD
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) }); // DAY
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // FAJR
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // SUN
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // DHUHR
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // ASR
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // MAGHRIB
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) }); // ISHA

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
                Foreground = WpfBrushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(6, 6, 6, 6)
            };

            var b = new Border
            {
                Background = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#2C7A49")),
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
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(6, 4, 6, 4)
            };

            var b = new Border
            {
                BorderBrush = new WpfSolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#E6E6E6")),
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
