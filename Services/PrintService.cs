// FILE: PrintService.cs (v2 FIX)
// FOLDER: /Services
// NOTE: Uses WPF PrintDialog explicitly (avoids WinForms ambiguity)

using System;

namespace PrayerTimes.Services
{
    public enum PagePreset
    {
        LegalUS, // 8.5 x 14 in (portrait)
        A3       // 297 x 420 mm (portrait)
    }

    public static class PrintService
    {
        public static void Print(System.Windows.Documents.FixedDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var dlg = new System.Windows.Controls.PrintDialog(); // force WPF
            if (dlg.ShowDialog() != true) return;

            dlg.PrintDocument(doc.DocumentPaginator, "Monthly Prayer Times");
        }
    }
}
