using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Threading;
using System.Threading.Tasks;

namespace LiraMosaicViewer.Core
{
    public sealed record ExportProgress(
        int Done,
        int Total,
        double Percent,
        TimeSpan Elapsed,
        TimeSpan? Remaining,
        string CurrentTitle);

    public sealed record ExportResult(
        bool OnePdf,
        string? SinglePdfPath,
        IReadOnlyList<string> PdfPaths);

    public static class ExportRunner
    {
        /// <summary>
        /// Экспорт в PDF (растровый). Рендер страниц делегатом renderPngAsync(sheetNo,dpi) -> PNG bytes.
        /// Прогресс: 1 раз на страницу.
        /// </summary>
        public static async Task<ExportResult> ExportRasterPdfAsync<TItem>(
            IReadOnlyList<TItem> items,
            int startSheetNumber,
            int dpi,
            string outputFolder,
            bool onePdf,
            string onePdfFileName,
            Func<TItem, int, int, CancellationToken, Task<byte[]>> renderPngAsync,
Func<TItem, int, string> separatePdfFileName,

            IProgress<ExportProgress>? progress,
            CancellationToken ct)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            Directory.CreateDirectory(outputFolder);

            var sw = Stopwatch.StartNew();
            int total = items.Count;

            void report(int done, string title)
            {
                double percent = total <= 0 ? 100 : (double)done * 100.0 / total;
                TimeSpan elapsed = sw.Elapsed;

                TimeSpan? remaining = null;
                if (done > 0 && total > done)
                {
                    double secPerItem = elapsed.TotalSeconds / done;
                    remaining = TimeSpan.FromSeconds(secPerItem * (total - done));
                }

                progress?.Report(new ExportProgress(
                    Done: done,
                    Total: total,
                    Percent: percent,
                    Elapsed: elapsed,
                    Remaining: remaining,
                    CurrentTitle: title));
            }

            report(0, "Подготовка…");

            if (onePdf)
            {
                string outPath = EnsureUniquePath(Path.Combine(outputFolder, onePdfFileName));

                var doc = new PdfDocument();

                for (int i = 0; i < total; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    int sheetNo = startSheetNumber + i;
                    var item = items[i];

                    byte[] png = await renderPngAsync(item, sheetNo, dpi, ct).ConfigureAwait(false);
                    AddA4Page(doc, png);

                    report(i + 1, $"Лист {sheetNo} / {startSheetNumber + total - 1}");
                }

                doc.Save(outPath);

                report(total, "Готово");
                return new ExportResult(true, outPath, Array.Empty<string>());
            }
            else
            {
                var paths = new List<string>(capacity: total);

                for (int i = 0; i < total; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    int sheetNo = startSheetNumber + i;
                    var item = items[i];

                    byte[] png = await renderPngAsync(item, sheetNo, dpi, ct).ConfigureAwait(false);

                    string fileName = separatePdfFileName(item, sheetNo);
                    string outPath = EnsureUniquePath(Path.Combine(outputFolder, fileName));

                    var doc = new PdfDocument();
                    AddA4Page(doc, png);
                    doc.Save(outPath);

                    paths.Add(outPath);

                    report(i + 1, $"Лист {sheetNo} / {startSheetNumber + total - 1}");
                }

                report(total, "Готово");
                return new ExportResult(false, null, paths);
            }
        }

        private static void AddA4Page(PdfDocument doc, byte[] pngBytes)
        {
            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            using var gfx = XGraphics.FromPdfPage(page);

            // Важно: поток должен жить пока жив XImage
            using var ms = new MemoryStream(pngBytes);
            using var img = XImage.FromStream(() => ms);

            gfx.DrawImage(img, 0, 0, page.Width, page.Height);
        }

        private static string EnsureUniquePath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
                return desiredPath;

            string dir = Path.GetDirectoryName(desiredPath) ?? "";
            string name = Path.GetFileNameWithoutExtension(desiredPath);
            string ext = Path.GetExtension(desiredPath);

            for (int i = 1; i < 10_000; i++)
            {
                string candidate = Path.Combine(dir, $"{name}_{i:000}{ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
        }
    }
}
