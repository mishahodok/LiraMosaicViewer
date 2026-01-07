using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiraMosaicViewer.Core; // если не видит DisplacementsCsvReader (обычно не нужно)


namespace LiraMosaicViewer.Core
{
    public sealed class MosaicSceneBuilder
    {

        public MosaicScene? Build(
            string baseName,
            string geomPath,
            string momentsPath,
            int lc,
            MomentField field,
            Dictionary<int, Element2D> geometryByElement,
            Dictionary<int, double> valuesByElement)
        {
            // Собираем только те элементы, у которых есть и геометрия, и значение
            var elements = new List<Element2D>(geometryByElement.Count);
            var values = new List<double>();

            Rect? bounds = null;

            foreach (var kvp in geometryByElement)
            {
                int elementId = kvp.Key;

                if (!valuesByElement.TryGetValue(elementId, out var v))
                    continue; // нет значения — не рисуем

                var el = kvp.Value;
                if (el.Points.Length < 3) continue;

                // Валидация координат (на всякий)
                bool ok = true;
                foreach (var p in el.Points)
                {
                    if (double.IsNaN(p.X) || double.IsNaN(p.Y) || double.IsInfinity(p.X) || double.IsInfinity(p.Y))
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
                double v = valuesByElement[id];
                int bin = LegendBuilder.GetBinIndex(v, legend.Min, legend.Max, legend.BinCount);
                elementToBin[id] = bin;
            }

            return new MosaicScene
            {
                TitleLeft = baseName,
                TitleRight = $"Мозаика напряжений по {field} | LC={lc}",
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
