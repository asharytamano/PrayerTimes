using System;
using System.Windows;
using PrayerTimes.Shell;
using PrayerTimes.ViewModels;

namespace PrayerTimes
{
    public partial class App : System.Windows.Application
    {
        private TrayService? _tray;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var main = new MainWindow();

                // Ensure DataContext exists (some setups set it in XAML; others do it in code-behind).
                var vm = main.DataContext as MainViewModel;
                if (vm == null)
                {
                    vm = new MainViewModel();
                    main.DataContext = vm;
                }

                // Wire tray behavior (minimize/close to tray + menu + tooltip).
                _tray = new TrayService(main, vm);

                MainWindow = main;
                main.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "OnStartup crash");
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _tray?.Dispose(); } catch { /* ignore */ }
            base.OnExit(e);
        }
    }
}
