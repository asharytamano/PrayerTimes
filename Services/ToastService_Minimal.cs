// FILE: ToastService_Minimal.cs  (FIXED - resolves ambiguous Application/MessageBox)
// PURPOSE: Minimal “notification” fallback using WPF MessageBox.
// NOTE: If your project references System.Windows.Forms somewhere, this file will still compile
//       because we fully-qualify WPF Application + MessageBox.

using System;

namespace PrayerTimesApp.Services
{
    public sealed class ToastService_Minimal : IToastService
    {
        public void ShowPrayerTime(string prayerName, DateTime scheduledTime, DateTime nowLocal)
        {
            // Non-blocking (queued on WPF dispatcher)
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                System.Windows.MessageBox.Show(
                    $"{prayerName} time ({scheduledTime:hh:mm tt})",
                    "Prayer Time",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information
                );
            }));
        }
    }
}
