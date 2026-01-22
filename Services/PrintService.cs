// FILE: PrintService.cs
// FOLDER: /Services
// NOTE: Keeps original Print(FixedDocument) signature.
// Adds XPS dump + best-effort XPS->PDF via external converter WITHOUT requiring ReachFramework at compile-time
// (uses reflection to load XpsDocument types if ReachFramework is available on the machine).

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;

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

            var dlg = new System.Windows.Controls.PrintDialog(); // WPF PrintDialog
            if (dlg.ShowDialog() != true) return;

            dlg.PrintDocument(doc.DocumentPaginator, "Monthly Prayer Times");
        }

        /// <summary>
        /// Save FixedDocument as XPS using reflection (no compile-time ReachFramework dependency).
        /// Returns XPS path if saved, otherwise null.
        /// </summary>
        public static string? TrySaveAsXps(System.Windows.Documents.FixedDocument doc, string outputDir, string baseFileNameNoExt)
        {
            try
            {
                if (doc == null) return null;
                if (string.IsNullOrWhiteSpace(outputDir) || string.IsNullOrWhiteSpace(baseFileNameNoExt)) return null;

                Directory.CreateDirectory(outputDir);
                var xpsPath = Path.Combine(outputDir, baseFileNameNoExt + ".xps");
                if (File.Exists(xpsPath))
                {
                    try { File.Delete(xpsPath); } catch { /* ignore */ }
                }

                // Type: System.Windows.Xps.Packaging.XpsDocument (ReachFramework)
                var xpsDocType = Type.GetType("System.Windows.Xps.Packaging.XpsDocument, ReachFramework");
                var xpsWriterType = Type.GetType("System.Windows.Xps.XpsDocument, ReachFramework"); // may be null in some builds
                var createWriterOwner = Type.GetType("System.Windows.Xps.XpsDocument, ReachFramework");

                if (xpsDocType == null)
                    return null; // ReachFramework not available

                // ctor: XpsDocument(string, FileAccess)
                var ctor = xpsDocType.GetConstructor(new[] { typeof(string), typeof(FileAccess) });
                if (ctor == null) return null;

                using (var xpsDoc = ctor.Invoke(new object[] { xpsPath, FileAccess.ReadWrite }) as IDisposable)
                {
                    if (xpsDoc == null) return null;

                    // Static: XpsDocument.CreateXpsDocumentWriter(XpsDocument)
                    var createWriter = xpsDocType.GetMethod("CreateXpsDocumentWriter", new[] { xpsDocType });
                    if (createWriter == null) return null;

                    var writer = createWriter.Invoke(null, new[] { xpsDoc });
                    if (writer == null) return null;

                    // writer.Write(FixedDocument)
                    var writeMethod = writer.GetType().GetMethod("Write", new[] { typeof(System.Windows.Documents.FixedDocument) });
                    if (writeMethod == null) return null;

                    writeMethod.Invoke(writer, new object[] { doc });
                }

                return File.Exists(xpsPath) ? xpsPath : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Print an XPS file by loading FixedDocumentSequence via reflection. Returns true if printed.
        /// </summary>
        public static bool TryPrintFromXps(string xpsPath, string jobName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xpsPath) || !File.Exists(xpsPath)) return false;

                var xpsDocType = Type.GetType("System.Windows.Xps.Packaging.XpsDocument, ReachFramework");
                if (xpsDocType == null) return false;

                var ctor = xpsDocType.GetConstructor(new[] { typeof(string), typeof(FileAccess) });
                if (ctor == null) return false;

                using (var xpsDoc = ctor.Invoke(new object[] { xpsPath, FileAccess.Read }) as IDisposable)
                {
                    if (xpsDoc == null) return false;

                    var getSeq = xpsDocType.GetMethod("GetFixedDocumentSequence");
                    if (getSeq == null) return false;

                    var seq = getSeq.Invoke(xpsDoc, null);
                    if (seq == null) return false;

                    // seq.DocumentPaginator
                    var paginatorProp = seq.GetType().GetProperty("DocumentPaginator");
                    var paginator = paginatorProp?.GetValue(seq);
                    if (paginator == null) return false;

                    var dlg = new System.Windows.Controls.PrintDialog();
                    if (dlg.ShowDialog() != true) return false;

                    // dlg.PrintDocument(DocumentPaginator, string)
                    dlg.PrintDocument((System.Windows.Documents.DocumentPaginator)paginator, string.IsNullOrWhiteSpace(jobName) ? "Prayer Times Monthly" : jobName);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Best-effort XPS->PDF conversion via external converter:
        /// xps2pdf.exe "&lt;input.xps&gt;" "&lt;output.pdf&gt;".
        /// Returns the PDF path if successful, otherwise null.
        /// </summary>
        public static string? TryConvertXpsToPdf(string xpsPath, string pdfPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xpsPath) || !File.Exists(xpsPath)) return null;
                if (string.IsNullOrWhiteSpace(pdfPath)) return null;

                var exeCandidates = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "xps2pdf.exe"),
                    Path.Combine(AppContext.BaseDirectory, "XpsToPdf.exe"),
                    "xps2pdf.exe",
                    "XpsToPdf.exe"
                };

                var exe = exeCandidates.FirstOrDefault(p =>
                {
                    try
                    {
                        if (Path.IsPathRooted(p)) return File.Exists(p);
                        return true; // PATH
                    }
                    catch { return false; }
                });

                if (string.IsNullOrWhiteSpace(exe)) return null;

                Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);
                if (File.Exists(pdfPath))
                {
                    try { File.Delete(pdfPath); } catch { /* ignore */ }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = $"\"{xpsPath}\" \"{pdfPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return null;

                p.WaitForExit(15000);

                if (File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0)
                    return pdfPath;

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string SanitizeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Location";
            var invalid = Path.GetInvalidFileNameChars();

            var chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                var ch = chars[i];
                if (invalid.Contains(ch))
                {
                    chars[i] = '_';
                    continue;
                }

                if (!(char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_'))
                    chars[i] = '_';
            }

            var cleaned = new string(chars).Trim();
            return cleaned.Length == 0 ? "Location" : cleaned;
        }
    }
}
