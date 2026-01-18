using System;
using System.Globalization;
using System.Windows;
using PrayerTimes.ViewModels;

namespace PrayerTimes.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly MainViewModel _vm;

        public SettingsWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            DataContext = _vm;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _vm.DiscardSettingsDraft();
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(_vm.DraftLatitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            {
                System.Windows.MessageBox.Show("Invalid Latitude. Example: 8.0034", "Settings");
                return;
            }

            if (!double.TryParse(_vm.DraftLongitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                System.Windows.MessageBox.Show("Invalid Longitude. Example: 124.2839", "Settings");
                return;
            }

            if (lat < -90 || lat > 90)
            {
                System.Windows.MessageBox.Show("Latitude must be between -90 and 90.", "Settings");
                return;
            }

            if (lon < -180 || lon > 180)
            {
                System.Windows.MessageBox.Show("Longitude must be between -180 and 180.", "Settings");
                return;
            }

            var minutes = 5;
            if (!string.IsNullOrWhiteSpace(_vm.DraftReminderMinutes))
            {
                if (!int.TryParse(_vm.DraftReminderMinutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
                {
                    System.Windows.MessageBox.Show("Reminder minutes must be a whole number. Example: 5", "Settings");
                    return;
                }
            }
            if (minutes < 0 || minutes > 60)
            {
                System.Windows.MessageBox.Show("Reminder minutes must be between 0 and 60.", "Settings");
                return;
            }

            _vm.ApplySettingsDraft(lat, lon, minutes);
            Close();
        }
    }
}
