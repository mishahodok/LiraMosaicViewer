using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;


namespace LiraMosaicViewer.Core
{
    public static class LoadCasesCsvReader
    {
        public static List<LoadCaseItem> ReadFolder(string folder)
        {
            var result = new List<LoadCaseItem>();

            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return result;

            // Ищем файл словаря (точное имя + fallback)
            string? path = Path.Combine(folder, "loadcases.csv");
            if (!File.Exists(path))
            {
                path = Directory.EnumerateFiles(folder, "*loadcase*.csv").FirstOrDefault()
                    ?? Directory.EnumerateFiles(folder, "*loadcases*.csv").FirstOrDefault();
            }
            if (path == null || !File.Exists(path))
                return result;

            using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var header = sr.ReadLine();
            if (header == null) return result;

            char delim = header.Contains(';') ? ';' : header.Contains(',') ? ',' : '\t';

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(delim);
                if (parts.Length < 2) continue;

                if (!TryParseLc(parts[0], out int lc)) continue;

                var name = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(name)) name = $"LC={lc}";

                result.Add(new LoadCaseItem { Lc = lc, Name = name });
            }

            return result.OrderBy(x => x.Lc).ToList();
        }

        private static bool TryParseLc(string token, out int lc)
        {
            lc = 0;
            token = (token ?? "").Trim();

            // на случай "LC=2" / "LC 2" / "2"
            var m = Regex.Match(token, @"-?\d+");
            if (!m.Success) return false;

            return int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lc);
        }

    }
}
