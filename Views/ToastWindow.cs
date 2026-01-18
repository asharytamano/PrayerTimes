using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrayerTimes.Views
{
    // Simple in-app toast popup (bottom-right) for reliable notifications.
    // NOTE: This project references WinForms/System.Drawing for tray icon,
    // so we fully-qualify a few types to avoid ambiguity (Brushes, Color, Orientation, Application).
    public sealed class ToastWindow : Window
    {
        private ToastWindow(string title, string message, int seconds)
        {
            Width = 360;
            Height = 110;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;

            var border = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(235, 30, 30, 30)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 0,
                    Opacity = 0.45
                }
            };

            var stack = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 4)
            });

            stack.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220)),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            });

            border.Child = stack;
            Content = border;

            Loaded += (_, __) =>
            {
                var work = SystemParameters.WorkArea;
                Left = work.Right - Width - 18;
                Top = work.Bottom - Height - 18;

                var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
                t.Tick += (s, e) => { t.Stop(); Close(); };
                t.Start();
            };
        }

        public static void ShowToast(string title, string message, int seconds = 4)
        {
            // Fully-qualify Application to avoid ambiguity with WinForms Application
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                new ToastWindow(title, message, seconds).Show();
            });
        }
    }
}
