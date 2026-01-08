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
            _ = sr.ReadLine();

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

                // Собираем узлы: (nodeId + координаты)
                var nodes = new List<(int nodeId, Point pt)>(nodesCount);

                // Node1 начинается с индекса 7: Node;X;Y;Z -> шаг 4
                for (int i = 1; i <= nodesCount; i++)
                {
                    int baseIdx = 7 + (i - 1) * 4; // NodeId
                    int xIdx = baseIdx + 1;
                    int yIdx = baseIdx + 2;

                    if (baseIdx >= parts.Length || xIdx >= parts.Length || yIdx >= parts.Length)
                        break;

                    // nodeId — best-effort (чтобы не ломать моменты при неожиданном формате)
                    int nodeId = 0;
                    CsvParsing.TryParseInt(parts[baseIdx], out nodeId);

                    if (!CsvParsing.TryParseDouble(parts[xIdx], out var x)) break;
                    if (!CsvParsing.TryParseDouble(parts[yIdx], out var y)) break;

                    nodes.Add((nodeId, new Point(x, y)));
                }

                // Если недобрали точки — пропускаем элемент (не падаем)
                if (nodes.Count < 3) continue;

                // Упорядочиваем по углу (как раньше), но сохраняем и NodeIds в том же порядке
                var orderedPts = OrderByAngle(nodes, out var orderedNodeIds);

                result[elementId] = new Element2D
                {
                    ElementId = elementId,
                    Points = orderedPts,
                    NodeIds = orderedNodeIds
                };
            }

            return result;
        }

        private static Point[] OrderByAngle(List<(int nodeId, Point pt)> nodes, out int[] nodeIds)
        {
            double cx = 0, cy = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                cx += nodes[i].pt.X;
                cy += nodes[i].pt.Y;
            }
            cx /= nodes.Count;
            cy /= nodes.Count;

            nodes.Sort((a, b) =>
            {
                double aa = Math.Atan2(a.pt.Y - cy, a.pt.X - cx);
                double bb = Math.Atan2(b.pt.Y - cy, b.pt.X - cx);
                return aa.CompareTo(bb);
            });

            var pts = new Point[nodes.Count];
            nodeIds = new int[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                pts[i] = nodes[i].pt;
                nodeIds[i] = nodes[i].nodeId;
            }

            return pts;
        }
    }
}
