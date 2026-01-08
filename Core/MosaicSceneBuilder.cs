using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace LiraMosaicViewer.Core
{
    public sealed class MosaicSceneBuilder
    {
        /// <summary>
        /// Текущий режим (как было): значения заданы ПО ЭЛЕМЕНТАМ (moments Mx/My/Mxy и т.п.)
        /// </summary>
        public MosaicScene? Build(
            string baseName,
            string geomPath,
            string momentsPath,
            int lc,
            MomentField field,
            Dictionary<int, Element2D> geometryByElement,
            Dictionary<int, double> valuesByElement)
        {
            _ = geomPath;
            _ = momentsPath;

            string titleRight = $"Мозаика напряжений по {field} | LC={lc}";
            return BuildCore(baseName, titleRight, geometryByElement, valuesByElement);
        }

        /// <summary>
        /// Новый режим (для перемещений): значения заданы ПО УЗЛАМ (nodeId -> value),
        /// а цвет элемента считаем как среднее по его узлам (NodeIds).
        /// </summary>
        public MosaicScene? BuildFromNodeValues(
            string baseName,
            string geomPath,
            string displacementsPath,
            int rsn,
            string fieldName, // "Ux" / "Uy" / "Uz" — только для подписи справа
            Dictionary<int, Element2D> geometryByElement,
            IReadOnlyDictionary<int, double> valuesByNode)
        {
            _ = geomPath;
            _ = displacementsPath;

            // Пересчёт узловых значений в элементные (1 цвет на элемент)
            var valuesByElement = new Dictionary<int, double>(geometryByElement.Count);

            foreach (var kvp in geometryByElement)
            {
                int elementId = kvp.Key;
                var el = kvp.Value;

                if (el.Points == null || el.Points.Length < 3)
                    continue;

                if (el.NodeIds == null || el.NodeIds.Length < 3)
                    continue; // нет узлов — не можем посчитать элементное значение

                double sum = 0;
                int cnt = 0;

                for (int i = 0; i < el.NodeIds.Length; i++)
                {
                    int nodeId = el.NodeIds[i];
                    if (valuesByNode.TryGetValue(nodeId, out var v))
                    {
                        sum += v;
                        cnt++;
                    }
                }

                if (cnt == 0)
                    continue; // ни одного узла с данными

                valuesByElement[elementId] = sum / cnt;
            }

            string titleRight = $"Изополя относительных перемещений по {fieldName} | РСН={rsn}";
            return BuildCore(baseName, titleRight, geometryByElement, valuesByElement);
        }

        private static MosaicScene? BuildCore(
            string baseName,
            string titleRight,
            Dictionary<int, Element2D> geometryByElement,
            IReadOnlyDictionary<int, double> valuesByElement)
        {
            // Собираем только те элементы, у которых есть и геометрия, и значение
            var elements = new List<Element2D>(geometryByElement.Count);
            var values = new List<double>(geometryByElement.Count);

            Rect? bounds = null;

            foreach (var kvp in geometryByElement)
            {
                int elementId = kvp.Key;

                if (!valuesByElement.TryGetValue(elementId, out var v))
                    continue; // нет значения — не рисуем

                var el = kvp.Value;
                if (el.Points == null || el.Points.Length < 3)
                    continue;

                // Валидация координат (на всякий)
                bool ok = true;
                foreach (var p in el.Points)
                {
                    if (double.IsNaN(p.X) || double.IsNaN(p.Y) ||
                        double.IsInfinity(p.X) || double.IsInfinity(p.Y))
                    {
                        ok = false;
                        break;
                    }
                }
                if (!ok) continue;

                elements.Add(el);
                values.Add(v);

                // bounds
                var elBounds = ComputeBounds(el.Points);
                bounds = bounds == null ? elBounds : Rect.Union(bounds.Value, elBounds);
            }

            if (elements.Count == 0 || bounds == null)
                return null;

            // Легенда/бины
            var legend = LegendBuilder.Build(values, binCount: 12);

            // elementId -> bin
            var elementToBin = new Dictionary<int, int>(elements.Count);
            for (int i = 0; i < elements.Count; i++)
            {
                int id = elements[i].ElementId;
                double v = values[i]; // важно: берём из списка, чтобы не зависеть от словаря
                int bin = LegendBuilder.GetBinIndex(v, legend.Min, legend.Max, legend.BinCount);
                elementToBin[id] = bin;
            }

            return new MosaicScene
            {
                TitleLeft = baseName,
                TitleRight = titleRight,
                Elements = elements,
                ElementToBin = elementToBin,
                WorldBounds = bounds.Value,
                Legend = legend
            };
        }

        private static Rect ComputeBounds(Point[] pts)
        {
            double minX = pts[0].X, maxX = pts[0].X;
            double minY = pts[0].Y, maxY = pts[0].Y;

            for (int i = 1; i < pts.Length; i++)
            {
                var p = pts[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }
    }
}
