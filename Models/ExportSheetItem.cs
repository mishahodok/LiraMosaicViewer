using System;
using System.Globalization;

namespace LiraMosaicViewer.Models
{
    /// <summary>
    /// Один лист в очереди экспорта (рецепт + готовые подписи).
    /// Сцену НЕ храним: на экспорт пересобираем из CSV.
    /// </summary>
    public sealed class ExportSheetItem
    {
        public required FilePairItem Pair { get; init; }

        public required int Lc { get; init; }
        public required string LcName { get; init; }  // как показывать в заголовке
        public required MomentField Field { get; init; }

        public required ReportPageModel Page { get; init; } // уже сформированные надписи (кроме номера листа)
        public required bool ShowMesh { get; init; }

        public bool IsNoData { get; init; }            // для подсветки очереди
        public string NoDataReason { get; init; } = ""; // опционально

        public string PlateTitle => Pair.BaseName;     // сейчас ключ вида "Z=..."
        public string ResultKindTitle => "Моменты";

        /// <summary>Короткая строка для очереди</summary>
        public string DisplayLine
        {
            get
            {
                // Пример: "LC=2 Пол | Z=-0.30 | Моменты: My"
                return $"LC={Lc} {LcName} | {PlateTitle} | {ResultKindTitle}: {Field}";
            }
        }

        /// <summary>Имя файла для раздельного экспорта</summary>
        public string BuildSuggestedFileName(int sheetNumber)
        {
            // Пример: "Лист_005__LC2_Пол__Z=-0.30__Moments_My.pdf"
            string sn = sheetNumber.ToString("000", CultureInfo.InvariantCulture);
            string safeLc = MakeSafe($"{Lc}_{LcName}");
            string safePlate = MakeSafe(PlateTitle);
            return $"Лист_{sn}__LC{safeLc}__{safePlate}__Moments_{Field}.pdf";
        }

        private static string MakeSafe(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "NA";
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            s = s.Replace(' ', '_');
            return s;
        }
    }
}
