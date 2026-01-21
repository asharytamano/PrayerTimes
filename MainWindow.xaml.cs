using System.Windows;

namespace PrayerTimes
{
    public partial class MainWindow : Window
    {
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
