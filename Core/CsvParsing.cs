using System.Globalization;

namespace LiraMosaicViewer.Core
{
    public static class CsvParsing
    {
        public static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

        public static bool TryParseInt(string? s, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || int.TryParse(s, NumberStyles.Integer, Ru, out value);
        }

        public static bool TryParseDouble(string? s, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim();

            // Часто в данных запятая. Но на всякий поддержим и точку.
            if (double.TryParse(s, NumberStyles.Float, Ru, out value)) return true;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;

            // Иногда встречаются пробелы/неразрывные пробелы
            s = s.Replace(" ", "").Replace("\u00A0", "");
            if (double.TryParse(s, NumberStyles.Float, Ru, out value)) return true;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return true;

            return false;
        }
    }
}
