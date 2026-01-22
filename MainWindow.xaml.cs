using System.Windows;

namespace PrayerTimes
{
    public partial class MainWindow : Window
    {
        private void MonthlyGenerateComingSoon_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Monthly Generate is coming in the next update, in sha Allah.\n\nFor now, this build focuses on core prayer times + adhan reliability (IPR).",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void MonthlyPrintComingSoon_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Monthly Print is coming in the next update, in sha Allah.\n\nFor now, this build focuses on core prayer times + adhan reliability (IPR).",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        private void ScrollToMonthlyPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Works even when the named element is inside a different namescope/template
                var target = LogicalTreeHelper.FindLogicalNode(this, "MonthlyPrintSection") as FrameworkElement;
                target?.BringIntoView();
            }
            catch
            {
                // no-op
            }
        }
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
