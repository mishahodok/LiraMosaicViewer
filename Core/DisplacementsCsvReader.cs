using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiraMosaicViewer.Core
{
    public sealed class DisplacementsTable
    {
        // RSN -> NodeId -> (Ux,Uy,Uz)
        private readonly Dictionary<int, Dictionary<int, (double Ux, double Uy, double Uz)>> _data = new();

        public HashSet<int> AvailableRsns { get; } = new();

        public void Add(int nodeId, int rsn, double ux, double uy, double uz)
        {
            if (!_data.TryGetValue(rsn, out var map))
            {
                map = new Dictionary<int, (double, double, double)>();
                _data[rsn] = map;
            }

            map[nodeId] = (ux, uy, uz);
            AvailableRsns.Add(rsn);
        }

        public bool TryGetValue(int nodeId, int rsn, DisplacementField field, out double v)
        {
            v = 0;
            if (!_data.TryGetValue(rsn, out var map)) return false;
            if (!map.TryGetValue(nodeId, out var tup)) return false;

            v = field switch
            {
                DisplacementField.Ux => tup.Ux,
                DisplacementField.Uy => tup.Uy,
                DisplacementField.Uz => tup.Uz,
                _ => tup.Uz
            };
            return true;
        }

        public Dictionary<int, double> GetValuesByNode(int rsn, DisplacementField field)
        {
            var result = new Dictionary<int, double>();

            if (!_data.TryGetValue(rsn, out var map))
                return result;

            foreach (var kvp in map)
            {
                var (ux, uy, uz) = kvp.Value;
                double v = field switch
                {
                    DisplacementField.Ux => ux,
                    DisplacementField.Uy => uy,
                    DisplacementField.Uz => uz,
                    _ => uz
                };

                result[kvp.Key] = v;
            }

            return result;
        }
    }

    public static class DisplacementsCsvReader
    {
        // Ожидаемые колонки: Node, RSN, RSN_Table, Ux, Uy, Uz
        public static DisplacementsTable ReadTable(string path)
        {
            var table = new DisplacementsTable();
            if (!File.Exists(path)) return table;

            using var sr = new StreamReader(path);
            bool headerSkipped = false;

            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // пропускаем заголовок
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                var parts = line.Split(';');          // у тебя CSV из Excel — разделитель ; 
                if (parts.Length < 6) continue;


                if (!CsvParsing.TryParseInt(parts[0], out var nodeId)) continue;
                if (!CsvParsing.TryParseInt(parts[1], out var rsn)) continue;

                if (!CsvParsing.TryParseDouble(parts[3], out var ux)) continue;
                if (!CsvParsing.TryParseDouble(parts[4], out var uy)) continue;
                if (!CsvParsing.TryParseDouble(parts[5], out var uz)) continue;

                table.Add(nodeId, rsn, ux, uy, uz);
            }

            return table;
        }
    }
}
