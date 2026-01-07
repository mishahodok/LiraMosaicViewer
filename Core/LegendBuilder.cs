using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LiraMosaicViewer.Core
{
    public static class LegendBuilder
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static LegendModel Build(IReadOnlyList<double> values, int binCount)
        {
            if (values.Count == 0)
            {
                return new LegendModel
                {
                    BinCount = binCount,
                    Min = 0,
                    Max = 0,
                    Boundaries = new double[binCount + 1],
                    Colors = Palette.CreateDiscreteBrushes(binCount),
                    PercentText = new string[binCount],
                };
            }

            var arr = values.ToArray();
            Array.Sort(arr);

            double rawMin = arr[0];
            double rawMax = arr[^1];

            double p01 = QuantileSorted(arr, 0.01);
            double p99 = QuantileSorted(arr, 0.99);

            double fullRange = rawMax - rawMin;
            double coreRange = p99 - p01;

            // если выбросов нет — не клиппим (это твой Mx)
            bool heavyOutliers = coreRange > 1e-12 && (fullRange / coreRange) > 10.0;

            double min = heavyOutliers ? QuantileSorted(arr, 0.001) : rawMin;
            double max = heavyOutliers ? QuantileSorted(arr, 0.994) : rawMax;



            // защита от max==min
            if (Math.Abs(max - min) < 1e-12)
            {
                max = min + 1.0;
            }

            var boundaries = new double[binCount + 1];
            for (int i = 0; i <= binCount; i++)
                boundaries[i] = min + (max - min) * i / binCount;

            var colors = Palette.CreateDiscreteBrushes(binCount);

            var counts = new int[binCount];
            foreach (var v in values)
            {
                int bin = GetBinIndex(v, min, max, binCount);
                if (bin >= 0 && bin < binCount) counts[bin]++;
            }

            var percentText = new string[binCount];
            for (int i = 0; i < binCount; i++)
            {
                double pct = 100.0 * counts[i] / values.Count;
                if (pct > 0 && pct < 1.0) percentText[i] = "<1%";
                else percentText[i] = Math.Round(pct).ToString("0", Invariant) + "%";
            }

            return new LegendModel
            {
                BinCount = binCount,
                Min = min,
                Max = max,
                Boundaries = boundaries,
                Colors = colors,
                PercentText = percentText
            };
        }

        public static int GetBinIndex(double v, double min, double max, int binCount)
        {
            if (max <= min) return 0;

            double t = (v - min) / (max - min);
            t = Math.Clamp(t, 0, 1);

            int bin = (int)Math.Floor(t * binCount);
            if (bin == binCount) bin = binCount - 1;
            return bin;
        }
        private static double Quantile(IReadOnlyList<double> values, double q)
        {
            if (values.Count == 0) return 0;

            q = Math.Clamp(q, 0, 1);

            var arr = values.ToArray();
            Array.Sort(arr);

            if (arr.Length == 1) return arr[0];

            double pos = (arr.Length - 1) * q;
            int i = (int)Math.Floor(pos);
            int j = (int)Math.Ceiling(pos);

            if (i == j) return arr[i];

            double w = pos - i;
            return arr[i] * (1 - w) + arr[j] * w;
        }
        private static double QuantileSorted(double[] sorted, double q)
        {
            q = Math.Clamp(q, 0, 1);
            if (sorted.Length == 0) return 0;
            if (sorted.Length == 1) return sorted[0];

            double pos = (sorted.Length - 1) * q;
            int i = (int)Math.Floor(pos);
            int j = (int)Math.Ceiling(pos);

            if (i == j) return sorted[i];

            double w = pos - i;
            return sorted[i] * (1 - w) + sorted[j] * w;
        }


    }
}
