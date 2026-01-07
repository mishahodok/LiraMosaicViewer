using System;
using System.Windows.Media;

namespace LiraMosaicViewer.Core
{
    public static class Palette
    {
        // Приближение “как в ЛИРЕ” (синий -> голубой -> зелёный -> жёлтый)
        // Дискретные 12 цветов получаем выборкой по градиенту.
        private static readonly (double t, Color c)[] Stops = new[]
        {
            (0.00, Color.FromRgb(  0,   0, 120)), // тёмно-синий
            (0.20, Color.FromRgb(  0,  70, 255)), // синий
            (0.40, Color.FromRgb(  0, 220, 255)), // бирюзовый
            (0.60, Color.FromRgb(120, 255, 200)), // зеленоватый
            (0.80, Color.FromRgb(255, 255, 140)), // светло-жёлтый
            (1.00, Color.FromRgb(255, 230,   0)), // жёлтый
        };

        public static Color ColorAt(double t)
        {
            t = Math.Clamp(t, 0, 1);

            for (int i = 0; i < Stops.Length - 1; i++)
            {
                var (t0, c0) = Stops[i];
                var (t1, c1) = Stops[i + 1];

                if (t >= t0 && t <= t1)
                {
                    var u = (t - t0) / (t1 - t0);
                    byte r = (byte)Math.Round(c0.R + (c1.R - c0.R) * u);
                    byte g = (byte)Math.Round(c0.G + (c1.G - c0.G) * u);
                    byte b = (byte)Math.Round(c0.B + (c1.B - c0.B) * u);
                    return Color.FromRgb(r, g, b);
                }
            }

            return Stops[^1].c;
        }

        public static SolidColorBrush[] CreateDiscreteBrushes(int bins)
        {
            var arr = new SolidColorBrush[bins];
            for (int i = 0; i < bins; i++)
            {
                // берём центр интервала
                double t = (i + 0.5) / bins;
                var brush = new SolidColorBrush(ColorAt(t));
                brush.Freeze();
                arr[i] = brush;
            }
            return arr;
        }
    }
}
