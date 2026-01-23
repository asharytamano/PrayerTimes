using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace PrayerTimes
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ✅ Prevent app from exiting when SplashWindow closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashWindow();
            splash.Show();

            var sb = new Storyboard();

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.Zero
            };
            Storyboard.SetTarget(fadeIn, splash);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(Window.OpacityProperty));
            sb.Children.Add(fadeIn);

            // ✅ Visible for 5 seconds AFTER fade-in (0.6s + 5s = 5.6s)
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.6),
                BeginTime = TimeSpan.FromSeconds(5.6)
            };
            Storyboard.SetTarget(fadeOut, splash);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Window.OpacityProperty));
            sb.Children.Add(fadeOut);

            sb.Completed += (_, __) =>
            {
                // ✅ Create and show MainWindow FIRST
                var main = new MainWindow();
                MainWindow = main;      // important
                main.Show();

                // ✅ Now close splash
                splash.Close();

                // ✅ Switch shutdown mode to normal behavior
                ShutdownMode = ShutdownMode.OnMainWindowClose;
            };

            sb.Begin();
        }
    }
}
