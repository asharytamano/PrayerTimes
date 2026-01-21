using System;
using System.Printing;
using System.Windows;
using System.Windows.Documents;

namespace PrayerTimesApp.Services
{
    public enum PagePreset
    {
        LegalUS, // 8.5 x 14 in
        A3       // 297 x 420 mm
    }

    public static class PrintService
    {
        public static void Print(FixedDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var dlg = new PrintDialog();
            if (dlg.ShowDialog() != true) return;

            dlg.PrintDocument(doc.DocumentPaginator, "Monthly Prayer Times");
        }
    }
}
