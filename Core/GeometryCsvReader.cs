using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;

namespace LiraMosaicViewer.Core
{
    public static class GeometryCsvReader
    {
        public static Dictionary<int, Element2D> ReadElements(string geomPath)
        {
            var result = new Dictionary<int, Element2D>();

            using var sr = new StreamReader(geomPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            // header

            var header = sr.ReadLine();
            if (header == null) return result;

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';'); // важно: пустые поля сохраняются
                if (parts.Length < 15) continue;

                // Element id
                if (!CsvParsing.TryParseInt(parts[0], out var elementId)) continue;

                // NodesCount
                if (!CsvParsing.TryParseInt(parts[2], out var nodesCount)) continue;
                if (nodesCount < 3 || nodesCount > 8) continue;

                var pts = new List<Point>(nodesCount);

                // Node1 начинается с индекса 7: Node;X;Y;Z -> шаг 4
                for (int i = 1; i <= nodesCount; i++)
                {
                    int baseIdx = 7 + (i - 1) * 4;
                    int xIdx = baseIdx + 1;
                    int yIdx = baseIdx + 2;

                    if (xIdx >= parts.Length || yIdx >= parts.Length) break;

                    if (!CsvParsing.TryParseDouble(parts[xIdx], out var x)) break;
                    if (!CsvParsing.TryParseDouble(parts[yIdx], out var y)) break;

                    pts.Add(new Point(x, y));
                }

                // Если недобрали точки — пропускаем элемент (не падаем)
                if (pts.Count < 3) continue;

                // Простейшая защита от дубликата
                result[elementId] = new Element2D
                {
                    ElementId = elementId,
                    Points = OrderByAngle(pts)

                };
            }

            return result;
        }
        private static Point[] OrderByAngle(List<Point> pts)
        {
            double cx = 0, cy = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                cx += pts[i].X;
                cy += pts[i].Y;
            }
            cx /= pts.Count;
            cy /= pts.Count;

            pts.Sort((a, b) =>
            {
                double aa = Math.Atan2(a.Y - cy, a.X - cx);
                double bb = Math.Atan2(b.Y - cy, b.X - cx);
                return aa.CompareTo(bb);
            });

            return pts.ToArray();
        }

    }
}
