using LiraMosaicViewer.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LiraMosaicViewer.View
{
    public sealed class MosaicView : FrameworkElement
    {
        public MosaicScene? Scene
        {
            get => (MosaicScene?)GetValue(SceneProperty);
            set => SetValue(SceneProperty, value);
        }

        public static readonly DependencyProperty SceneProperty =
            DependencyProperty.Register(nameof(Scene), typeof(MosaicScene), typeof(MosaicView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowMesh { get; set; } = true;

        // Внешний вид “как в ЛИРЕ”
        private const double LegendHeight = 80;
        private const double LeftTextWidth = 220;
        private const double Padding = 10;

        private static readonly Pen MeshPen = CreatePen(Color.FromRgb(0, 140, 0), 0.6);
        private static readonly Pen LegendBorderPen = CreatePen(Color.FromRgb(0, 0, 0), 0.8);

        private static Pen CreatePen(Color c, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(c), thickness);
            pen.Freeze();
            return pen;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (Scene == null)
            {
                DrawNoData(dc);
                return;
            }

            DrawLegend(dc, Scene);

            var mosaicRect = new Rect(
                Padding,
                LegendHeight + Padding,
                Math.Max(0, ActualWidth - 2 * Padding),
                Math.Max(0, ActualHeight - LegendHeight - 2 * Padding));

            if (mosaicRect.Width < 10 || mosaicRect.Height < 10)
                return;

            // Трансформ: мир -> экран (с сохранением пропорций) + переворот Y
            var world = Scene.WorldBounds;
            if (world.Width <= 0 || world.Height <= 0) return;

            double scaleX = mosaicRect.Width / world.Width;
            double scaleY = mosaicRect.Height / world.Height;
            double scale = Math.Min(scaleX, scaleY);

            // Центрируем
            double drawW = world.Width * scale;
            double drawH = world.Height * scale;
            Pen? meshPen = null;
            if (ShowMesh)
            {
                var b = new SolidColorBrush(Color.FromRgb(0, 140, 0));
                b.Freeze();

                // 0.6px на экране
                double thicknessInWorld = 0.6 / scale;

                meshPen = new Pen(b, thicknessInWorld)
                {
                    LineJoin = PenLineJoin.Miter,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat
                };
                meshPen.Freeze();
            }


            double offsetX = mosaicRect.X + (mosaicRect.Width - drawW) / 2.0;
            double offsetY = mosaicRect.Y + (mosaicRect.Height - drawH) / 2.0;

            // В WPF Y вниз, в расчётной плоскости обычно Y вверх: делаем flip по Y
            var tg = new TransformGroup();
            tg.Children.Add(new TranslateTransform(-world.X, -world.Y));
            tg.Children.Add(new ScaleTransform(scale, -scale));
            tg.Children.Add(new TranslateTransform(offsetX, offsetY + drawH));
            dc.PushTransform(tg);


            // Рисуем элементы
            var legend = Scene.Legend;

            for (int i = 0; i < Scene.Elements.Count; i++)
            {
                var el = Scene.Elements[i];

                if (!Scene.ElementToBin.TryGetValue(el.ElementId, out int bin))
                    continue;

                if (bin < 0 || bin >= legend.Colors.Length)
                    continue;

                var brush = legend.Colors[bin];

                var geom = BuildPolygonGeometry(el.Points);
                if (geom == null) continue;

                dc.DrawGeometry(brush, meshPen, geom);

            }

            dc.Pop(); // transform
        }

        private static Geometry? BuildPolygonGeometry(Point[] pts)
        {
            if (pts.Length < 3) return null;

            var sg = new StreamGeometry();
            using (var ctx = sg.Open())
            {
                ctx.BeginFigure(pts[0], isFilled: true, isClosed: true);
                for (int i = 1; i < pts.Length; i++)
                    ctx.LineTo(pts[i], isStroked: true, isSmoothJoin: false);
            }
            sg.Freeze();
            return sg;
        }

        private void DrawNoData(DrawingContext dc)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var ft = new FormattedText(
                "Нет данных для отображения",
                CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                16,
                Brushes.Black,
                dpi);

            dc.DrawText(ft, new Point(20, 20));
        }

        private void DrawLegend(DrawingContext dc, MosaicScene scene)
        {
            var legend = scene.Legend;
            int bins = legend.BinCount;

            // фон легенды
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, LegendHeight));

            // Тексты слева (как в ЛИРЕ — область под подписи)
            DrawText(dc, scene.TitleLeft, 12, FontWeights.SemiBold, new Point(Padding, 10));
            DrawText(dc, scene.TitleRight, 12, FontWeights.Normal, new Point(Padding, 30));
            DrawText(dc, "Единицы измерения - (т*м)/м", 11, FontWeights.Normal, new Point(Padding, 50));

            // Полоса шкалы справа от текстовой области
            double barX = LeftTextWidth;
            double barY = 12;
            double barW = Math.Max(0, ActualWidth - barX - Padding);
            double barH = 18;

            var barRect = new Rect(barX, barY, barW, barH);

            if (barRect.Width < 50) return;

            double segW = barRect.Width / bins;

            // сегменты
            for (int i = 0; i < bins; i++)
            {
                var r = new Rect(barRect.X + i * segW, barRect.Y, segW, barRect.Height);
                dc.DrawRectangle(legend.Colors[i], LegendBorderPen, r);

                // проценты над сегментом
                string pct = (i < legend.PercentText.Length) ? legend.PercentText[i] : "";
                if (!string.IsNullOrWhiteSpace(pct))
                {
                    var p = new Point(r.X + r.Width / 2 - 12, barRect.Y - 14);
                    DrawTextCentered(dc, pct, 10, FontWeights.Normal, new Point(r.X + r.Width / 2, barRect.Y - 12));
                }
            }

            // подписи границ (bins+1)
            // выводим как в лире: значения под линией
            for (int i = 0; i <= bins; i++)
            {
                double x = barRect.X + i * segW;
                dc.DrawLine(LegendBorderPen, new Point(x, barRect.Bottom), new Point(x, barRect.Bottom + 6));

                string s = FormatLegendNumber(legend.Boundaries[i]);
                DrawTextCentered(dc, s, 10, FontWeights.Normal, new Point(x, barRect.Bottom + 16));
            }
        }

        private static string FormatLegendNumber(double v)
        {
            // ЛИРА в примерах часто показывает с точкой; используем Invariant.
            // Кол-во знаков адаптивно (до 4), чтобы мелкие числа не терялись.
            var inv = CultureInfo.InvariantCulture;
            double av = Math.Abs(v);

            string fmt = av switch
            {
                >= 1000 => "0",
                >= 100 => "0.0",
                >= 10 => "0.00",
                >= 1 => "0.00",
                >= 0.1 => "0.000",
                _ => "0.0000"
            };

            return v.ToString(fmt, inv);
        }

        private void DrawText(DrawingContext dc, string text, double size, FontWeight weight, Point p)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var ft = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
                size,
                Brushes.Black,
                dpi);

            dc.DrawText(ft, p);
        }

        private void DrawTextCentered(DrawingContext dc, string text, double size, FontWeight weight, Point center)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var ft = new FormattedText(
                text,
                CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
                size,
                Brushes.Black,
                dpi);

            dc.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
        }
    }
}
