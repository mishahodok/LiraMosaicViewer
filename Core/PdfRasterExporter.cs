using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiraMosaicViewer.Core
{
    public static class PdfRasterExporter
    {
        /// <summary>
        /// Собрать один PDF из набора фабрик страниц (каждая фабрика возвращает PNG bytes).
        /// В памяти одновременно держим только одну страницу.
        /// </summary>
        public static string ExportSinglePdf(string outputFolder, string baseFileName, IEnumerable<Func<byte[]>> pageFactories)
        {
            Directory.CreateDirectory(outputFolder);

            string outPath = EnsureUniquePath(Path.Combine(outputFolder, baseFileName));

            var doc = new PdfDocument();

            foreach (var makePng in pageFactories)
            {
                byte[] png = makePng();
                AddA4Page(doc, png);
            }

            doc.Save(outPath);
            return outPath;
        }

        /// <summary>
        /// Экспорт отдельными PDF (по одному листу).
        /// fileNameFactory(sheetNumber) должен вернуть имя файла (с .pdf).
        /// </summary>
        public static List<string> ExportSeparatePdfs(string outputFolder, IEnumerable<(int sheetNumber, Func<byte[]> makePng, Func<string> fileName)> pages)
        {
            Directory.CreateDirectory(outputFolder);

            var result = new List<string>();

            foreach (var p in pages)
            {
                string outPath = EnsureUniquePath(Path.Combine(outputFolder, p.fileName()));

                var doc = new PdfDocument();
                byte[] png = p.makePng();
                AddA4Page(doc, png);
                doc.Save(outPath);

                result.Add(outPath);
            }

            return result;
        }

        private static void AddA4Page(PdfDocument doc, byte[] pngBytes)
        {
            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            using var gfx = XGraphics.FromPdfPage(page);

            using var ms = new MemoryStream(pngBytes);
            using var img = XImage.FromStream(() => ms);

            // Растягиваем на весь A4
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

            // совсем крайний случай
            return Path.Combine(dir, $"{name}_{Guid.NewGuid():N}{ext}");
        }
    }
}
