using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiraMosaicViewer.Core
{
    public sealed class MomentsTable
    {
        // LC -> (ElementId -> (Mx,My,Mxy))
        private readonly Dictionary<int, Dictionary<int, (double Mx, double My, double Mxy)>> _data
            = new();

        public HashSet<int> AvailableLCs { get; } = new();

        public void Add(int elementId, int lc, double mx, double my, double mxy)
        {
            AvailableLCs.Add(lc);

            if (!_data.TryGetValue(lc, out var byElem))
            {
                byElem = new Dictionary<int, (double Mx, double My, double Mxy)>();
                _data[lc] = byElem;
            }

            // Если дубликаты — перезапишем последним
            byElem[elementId] = (mx, my, mxy);
        }

        public Dictionary<int, double> GetValuesByElement(int lc, MomentField field)
        {
            var result = new Dictionary<int, double>();

            if (!_data.TryGetValue(lc, out var byElem))
                return result;

            foreach (var kvp in byElem)
            {
                var (mx, my, mxy) = kvp.Value;
                double v = field switch
                {
                    MomentField.Mx => mx,
                    MomentField.My => my,
                    MomentField.Mxy => mxy,
                    _ => my
                };
                result[kvp.Key] = v;
            }

            return result;
        }
    }

    public static class MomentsCsvReader
    {
        public static MomentsTable ReadTable(string momentsPath)
        {
            var table = new MomentsTable();

            using var sr = new StreamReader(momentsPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            var header = sr.ReadLine();
            if (header == null) return table;

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length < 8) continue;

                // Element;CS;LC;Shape;History;Mx;My;Mxy
                if (!CsvParsing.TryParseInt(parts[0], out var elementId)) continue;
                if (!CsvParsing.TryParseInt(parts[2], out var lc)) continue;

                if (!CsvParsing.TryParseDouble(parts[5], out var mx)) continue;
                if (!CsvParsing.TryParseDouble(parts[6], out var my)) continue;
                if (!CsvParsing.TryParseDouble(parts[7], out var mxy)) continue;

                table.Add(elementId, lc, mx, my, mxy);
            }

            return table;
        }
        public static MomentsTable ReadTables(IEnumerable<string> momentsPaths)
        {
            var table = new MomentsTable();

            foreach (var path in momentsPaths)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    continue;

                // читаем один файл и добавляем в общий table
                using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

                var header = sr.ReadLine();
                if (header == null) continue;

                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(';');
                    if (parts.Length < 8) continue;

                    // Element;CS;LC;Shape;History;Mx;My;Mxy
                    if (!CsvParsing.TryParseInt(parts[0], out var elementId)) continue;
                    if (!CsvParsing.TryParseInt(parts[2], out var lc)) continue;

                    if (!CsvParsing.TryParseDouble(parts[5], out var mx)) continue;
                    if (!CsvParsing.TryParseDouble(parts[6], out var my)) continue;
                    if (!CsvParsing.TryParseDouble(parts[7], out var mxy)) continue;

                    table.Add(elementId, lc, mx, my, mxy);
                }
            }

            return table;
        }

    }
}
