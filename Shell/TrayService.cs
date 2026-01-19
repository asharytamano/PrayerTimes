using System;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using PrayerTimes.ViewModels;

namespace PrayerTimes.Shell
{
    /// <summary>
    /// System tray integration (NotifyIcon) for WPF.
    /// - Minimizing closes to tray (hide window)
    /// - Right-click menu (Open / Settings placeholder / Exit)
    /// - Tooltip shows next prayer + countdown
    /// </summary>
    public sealed class TrayService : IDisposable
    {
        private readonly Window _window;
        private readonly MainViewModel _vm;

        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _openItem;
        private readonly ToolStripMenuItem _settingsItem;
        private readonly ToolStripMenuItem _exitItem;

        private bool _allowClose;
        private bool _shownHintOnce;

        public TrayService(Window window, MainViewModel vm)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));

            _openItem = new ToolStripMenuItem("Open");
            _settingsItem = new ToolStripMenuItem("Settings");
            _exitItem = new ToolStripMenuItem("Exit");

            _openItem.Click += (_, __) => ShowWindow();
            _settingsItem.Click += (_, __) => OpenSettings();
            _exitItem.Click += (_, __) => ExitApp();

            var menu = new ContextMenuStrip();
            menu.Items.Add(_openItem);
            menu.Items.Add(_settingsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_exitItem);

            _notifyIcon = new NotifyIcon
            {
                Icon = TryGetAppIcon() ?? SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = menu,
                Text = BuildTooltipText()
            };

            _notifyIcon.DoubleClick += (_, __) => ShowWindow();

            _window.StateChanged += OnWindowStateChanged;
            _window.Closing += OnWindowClosing;

            _vm.PropertyChanged += OnVmPropertyChanged;
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.NextPrayer) ||
                e.PropertyName == nameof(MainViewModel.Countdown))
            {
                SafeSetTooltip(BuildTooltipText());
            }
        }

        private string BuildTooltipText()
        {
            // NotifyIcon.Text max is 63 characters.
            // Keep it short and always valid.
            var next = string.IsNullOrWhiteSpace(_vm.NextPrayer) ? "Next: --" : _vm.NextPrayer.Trim();
            var cd = string.IsNullOrWhiteSpace(_vm.Countdown) ? "" : _vm.Countdown.Trim();
            var text = next;
            if (!string.IsNullOrWhiteSpace(cd)) text = $"{next} • {cd}";

            if (text.Length > 63) text = text.Substring(0, 63);
            if (string.IsNullOrWhiteSpace(text)) text = "Prayer Times";
            return text;
        }

        private void SafeSetTooltip(string text)
        {
            try { _notifyIcon.Text = text; } catch { /* ignore */ }
        }

        private void OnWindowStateChanged(object? sender, EventArgs e)
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                HideToTray();
            }
        }

        private void OnWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_allowClose) return;

            // During debugging (F5), do NOT keep the process running in tray.
            // This prevents "taskkill/bin/obj deletion" issues.
            if (Debugger.IsAttached)
            {
                _allowClose = true;
                return;
            }

            // Normal behavior: Intercept "X" close -> hide to tray
            e.Cancel = true;
            HideToTray();
        }

        private void HideToTray()
        {
            _window.Hide();

            if (!_shownHintOnce)
            {
                _shownHintOnce = true;
                try
                {
                    _notifyIcon.BalloonTipTitle = "Prayer Times";
                    _notifyIcon.BalloonTipText = "Running in the system tray. Double-click the icon to open.";
                    _notifyIcon.ShowBalloonTip(1500);
                }
                catch { /* ignore */ }
            }
        }


        private void OpenSettings()
        {
            try
            {
                // Run on WPF UI thread
                var disp = System.Windows.Application.Current?.Dispatcher;
                if (disp != null)
                {
                    disp.Invoke(() =>
                    {
                        if (_vm.OpenSettingsCommand?.CanExecute(null) == true)
                            _vm.OpenSettingsCommand.Execute(null);
                    });
                }
                else
                {
                    if (_vm.OpenSettingsCommand?.CanExecute(null) == true)
                        _vm.OpenSettingsCommand.Execute(null);
                }
            }
            catch { /* ignore */ }
        }

        private static Icon? TryGetAppIcon()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetEntryAssembly();
                if (asm == null) return null;

                var path = asm.Location;
                if (string.IsNullOrWhiteSpace(path)) return null;

                return Icon.ExtractAssociatedIcon(path);
            }
            catch { return null; }
        }

        private void ShowWindow()
        {
            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;

            _window.Activate();
        }

        private void ExitApp()
        {
            _allowClose = true;

            try { _notifyIcon.Visible = false; } catch { /* ignore */ }

            // Close the main window then shutdown application
            try { _window.Close(); } catch { /* ignore */ }
            try { System.Windows.Application.Current.Shutdown(); } catch { /* ignore */ }
        }

        public void Dispose()
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _window.StateChanged -= OnWindowStateChanged;
            _window.Closing -= OnWindowClosing;

            try
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            catch { /* ignore */ }
        }
    }
}
