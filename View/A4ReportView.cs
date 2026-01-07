using LiraMosaicViewer.Core;
using LiraMosaicViewer.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace LiraMosaicViewer.View
{
    public sealed class A4ReportView : FrameworkElement
    {
        public MosaicScene? Scene
        {
            get => (MosaicScene?)GetValue(SceneProperty);
            set => SetValue(SceneProperty, value);
        }
        public static readonly DependencyProperty SceneProperty =
            DependencyProperty.Register(nameof(Scene), typeof(MosaicScene), typeof(A4ReportView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public ReportPageModel Page
        {
            get => (ReportPageModel)GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }
        public static readonly DependencyProperty PageProperty =
            DependencyProperty.Register(nameof(Page), typeof(ReportPageModel), typeof(A4ReportView),
                new FrameworkPropertyMetadata(new ReportPageModel(), FrameworkPropertyMetadataOptions.AffectsRender));

        public bool ShowMesh
        {
            get => (bool)GetValue(ShowMeshProperty);
            set => SetValue(ShowMeshProperty, value);
        }
        public static readonly DependencyProperty ShowMeshProperty =
            DependencyProperty.Register(nameof(ShowMesh), typeof(bool), typeof(A4ReportView),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        private readonly GostA4Form2aTemplate _t = new();

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // Базовый размер листа в DIU
            double pageW = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.PageWmm);
            double pageH = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.PageHmm);

            if (pageW <= 0 || pageH <= 0 || ActualWidth < 10 || ActualHeight < 10)
                return;

            // Вписываем лист в окно
            double scale = Math.Min(ActualWidth / pageW, ActualHeight / pageH);
            scale = Math.Max(0.01, scale);

            double drawW = pageW * scale;
            double drawH = pageH * scale;

            double ox = (ActualWidth - drawW) / 2.0;
            double oy = (ActualHeight - drawH) / 2.0;

            dc.PushTransform(new TranslateTransform(ox, oy));
            dc.PushTransform(new ScaleTransform(scale, scale));

            // Фон листа + тонкая обводка
            var pageRect = new Rect(0, 0, pageW, pageH);
            dc.DrawRectangle(Brushes.White, new Pen(Brushes.Black, 1.0 / scale), pageRect);

            // Рамка
            DrawFrame(dc, scale);

            // Штамп
            DrawStamp(dc, scale);

            // Заголовок (3 строки)
            DrawHeaderText(dc);

            // Подпись под рисунком
            DrawCaption(dc);

            // Мозаика + вертикальная легенда
            DrawFigure(dc, scale);

            dc.Pop(); // scale
            dc.Pop(); // translate
        }

        private void DrawFrame(DrawingContext dc, double viewScale)
        {
            var fr = GostA4Form2aTemplate.MmRectToDiu(_t.FrameRectMm);
            var pen = new Pen(Brushes.Black, 1.2 / viewScale);
            pen.Freeze();
            dc.DrawRectangle(null, pen, fr);
        }

        private void DrawStamp(DrawingContext dc, double viewScale)
        {
            var st = GostA4Form2aTemplate.MmRectToDiu(_t.StampRectMm);
            var pen = new Pen(Brushes.Black, 1.0 / viewScale);
            pen.Freeze();

            dc.DrawRectangle(null, pen, st);

            // Вертикальные линии колонок
            double x = st.X;
            for (int i = 0; i < GostA4Form2aTemplate.StampColsMm.Length - 1; i++)
            {
                x += GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.StampColsMm[i]);
                dc.DrawLine(pen, new Point(x, st.Y), new Point(x, st.Bottom));
            }
            // Правая колонка "Лист" (последняя, 10 мм) — делим по высоте 7+8 мм (как на форме 2а)
            double rightColW = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.StampColsMm[^1]);
            double rightColX = st.Right - rightColW;

            double splitY = st.Y + GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.StampSheetHeaderMm);
            dc.DrawLine(pen, new Point(rightColX, splitY), new Point(st.Right, splitY));


            // Подписи ячеек (снизу внутри)
            // Минимально: названия слева + номер листа справа.
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tf = new Typeface(new FontFamily("Times New Roman"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);

            string[] labels = { "Изм.", "", "№ докум.", "Подп.", "Дата", "", "" };
            double colX = st.X;
            for (int i = 0; i < labels.Length; i++)
            {
                double w = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.StampColsMm[i]);
                if (!string.IsNullOrWhiteSpace(labels[i]))
                {
                    var ft = new FormattedText(labels[i], CultureInfo.GetCultureInfo("ru-RU"),
                        FlowDirection.LeftToRight, tf, 10, Brushes.Black, dpi);
                    dc.DrawText(ft, new Point(colX + 2, st.Bottom - ft.Height - 1));
                }
                colX += w;
            }
            // "Лист" и номер листа — в правом блоке (последняя колонка)
            var tfItalic = new Typeface(new FontFamily("Times New Roman"), FontStyles.Italic, FontWeights.Normal, FontStretches.Normal);
            var tfBold = new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            // Заголовок "Лист" (верхние 7 мм)
            var ftSheet = new FormattedText("Лист", CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight, tfItalic, 10, Brushes.Black, dpi);

            double headerH = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.StampSheetHeaderMm);
            dc.DrawText(ftSheet, new Point(
                rightColX + rightColW / 2 - ftSheet.Width / 2,
                st.Y + headerH / 2 - ftSheet.Height / 2));

            // Номер листа (нижние 8 мм)
            var ftNum = new FormattedText(Page?.SheetNumber ?? "1", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, tfBold, 12, Brushes.Black, dpi);

            double bottomY0 = splitY;
            double bottomH = st.Bottom - splitY;
            dc.DrawText(ftNum, new Point(
                rightColX + rightColW / 2 - ftNum.Width / 2,
                bottomY0 + bottomH / 2 - ftNum.Height / 2));

        }

        private void DrawHeaderText(DrawingContext dc)
        {
            var hr = GostA4Form2aTemplate.MmRectToDiu(_t.HeaderRectMm);

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tf = new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            double x = hr.X + 4;
            double y = hr.Y + 4;

            string l1 = Page?.LoadTitle ?? "Снег";
            string l2 = Page?.MosaicTitle ?? "Мозаика напряжений по My";
            string l3 = Page?.UnitsTitle ?? "Единицы измерения - т/м2";

            DrawLineText(dc, l1, tf, 12, x, y, dpi); y += 14;
            DrawLineText(dc, l2, tf, 12, x, y, dpi); y += 14;
            DrawLineText(dc, l3, tf, 12, x, y, dpi);
        }

        private void DrawCaption(DrawingContext dc)
        {
            var cap = GostA4Form2aTemplate.MmRectToDiu(_t.CaptionRectMm);

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tf = new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            string text = Page?.FigureCaption ?? "Рис. 1 ...";

            var ft = new FormattedText(text, CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight, tf, 12, Brushes.Black, dpi);

            // по центру
            dc.DrawText(ft, new Point(cap.X + cap.Width / 2 - ft.Width / 2, cap.Y + cap.Height / 2 - ft.Height / 2));
        }
        private void DrawNoData(DrawingContext dc, Rect rect, string message)
        {
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tf = new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var ft = new FormattedText(message, System.Globalization.CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight, tf, 16, Brushes.Black, dpi);

            dc.DrawText(ft, new Point(rect.X + rect.Width / 2 - ft.Width / 2, rect.Y + rect.Height / 2 - ft.Height / 2));
        }


        private void DrawFigure(DrawingContext dc, double viewScale)
        {
            var legendRect = GostA4Form2aTemplate.MmRectToDiu(_t.LegendRectMm);
            var mosaicRect = GostA4Form2aTemplate.MmRectToDiu(_t.MosaicRectMm);

            // Если нет сцены — пишем сообщение и не рисуем легенду/мозаику
            if (Scene == null)
            {
                var msg = Page?.NoDataText;
                if (!string.IsNullOrWhiteSpace(msg))
                    DrawNoData(dc, mosaicRect, msg);
                return;
            }

            DrawLegendVertical(dc, Scene.Legend, legendRect, viewScale);
            DrawMosaic(dc, Scene, mosaicRect, viewScale);
        }


        private void DrawLegendVertical(DrawingContext dc, LegendModel legend, Rect rect, double viewScale)
        {
            int bins = legend.BinCount;
            if (bins <= 0) return;

            // Полоса цвета слева
            double barW = GostA4Form2aTemplate.MmToDiu(GostA4Form2aTemplate.LegendBarWidthMm);
            var barRect = new Rect(rect.X + 2, rect.Y + 2, barW, rect.Height - 4);

            var pen = new Pen(Brushes.Black, 0.8 / viewScale);
            pen.Freeze();

            double segH = barRect.Height / bins;

            // Сверху — max, снизу — min (как обычно)
            for (int i = 0; i < bins; i++)
            {
                int colorIndex = bins - 1 - i; // чтобы верх был "макс"
                var r = new Rect(barRect.X, barRect.Y + i * segH, barRect.Width, segH);
                dc.DrawRectangle(legend.Colors[colorIndex], pen, r);
            }

            // Подписи значений справа от шкалы
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var tf = new Typeface(new FontFamily("Times New Roman"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            double textX = barRect.Right + 6;

            for (int i = 0; i <= bins; i++)
            {
                // boundary: i=0 -> min, i=bins -> max
                // нам нужно сверху max, поэтому инверсия:
                int bi = bins - i;
                bi = Math.Clamp(bi, 0, bins);

                double v = legend.Boundaries[bi];
                string s = FormatLegendNumber(v);

                double y = barRect.Y + i * segH;

                var ft = new FormattedText(s, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, tf, 11, Brushes.Black, dpi);

                dc.DrawText(ft, new Point(textX, y - ft.Height / 2));
            }
        }

        private void DrawMosaic(DrawingContext dc, MosaicScene scene, Rect targetRect, double viewScale)
        {
            var world = scene.WorldBounds;
            if (world.Width <= 0 || world.Height <= 0) return;

            // Принудительно стараемся сделать “вертикально”
            bool rotate = world.Width > world.Height;

            // Рамка области рисунка (можно убрать позже)
            // dc.DrawRectangle(null, new Pen(Brushes.LightGray, 0.6 / viewScale), targetRect);

            // Для расчёта bounds после поворота
            double cx = world.X + world.Width / 2;
            double cy = world.Y + world.Height / 2;

            Rect bounds = world;
            if (rotate)
            {
                // 90° вокруг центра: ширина/высота меняются местами
                bounds = new Rect(cx - world.Height / 2, cy - world.Width / 2, world.Height, world.Width);
            }

            double scaleX = targetRect.Width / bounds.Width;
            double scaleY = targetRect.Height / bounds.Height;
            double s = Math.Min(scaleX, scaleY);
            s = Math.Max(0.000001, s);

            double drawW = bounds.Width * s;
            double drawH = bounds.Height * s;

            double tx = targetRect.X + (targetRect.Width - drawW) / 2.0;
            double ty = targetRect.Y + (targetRect.Height - drawH) / 2.0;

            // Перо сетки: 0.6 px на экране
            Pen? meshPen = null;
            if (ShowMesh)
            {
                var b = new SolidColorBrush(Color.FromRgb(0, 140, 0));
                b.Freeze();
                double thicknessWorld = 0.6 / s; // в координатах мира до масштаба
                meshPen = new Pen(b, thicknessWorld)
                {
                    LineJoin = PenLineJoin.Miter,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat
                };
                meshPen.Freeze();
            }

            // Transform: мир -> targetRect, Y вверх
            var tg = new TransformGroup();

            // 1) (опционально) поворот вокруг центра
            if (rotate)
            {
                // x' = cx + (y - cy)
                // y' = cy - (x - cx)
                // Реализуем как матрицу вокруг центра:
                // [0  1]
                // [-1 0]
                // + переносы
                var m = new Matrix(0, -1, 1, 0, 0, 0);
                // вокруг (cx,cy): T(cx,cy) * R * T(-cx,-cy)
                m.Translate(-cx, -cy);
                m = Matrix.Multiply(m, new Matrix(0, -1, 1, 0, 0, 0));
                m.Translate(cx, cy);
                // В WPF MatrixTransform применяется “как есть” — проще сделать готовую матрицу вручную:
                // В итоге сделаем проще: отдельная функция вращения ниже, без MatrixTransform.
                // Поэтому rotate-ветку тут не используем.
            }

            // Мы пойдём проще: без MatrixTransform — будем трансформировать точки вручную.
            // Значит здесь только “вписать” bounds.
            tg.Children.Add(new TranslateTransform(-bounds.X, -(bounds.Y + bounds.Height)));
            tg.Children.Add(new ScaleTransform(s, -s));
            tg.Children.Add(new TranslateTransform(tx, ty));

            dc.PushTransform(tg);

            // Рисуем
            for (int i = 0; i < scene.Elements.Count; i++)
            {
                var el = scene.Elements[i];
                if (!scene.ElementToBin.TryGetValue(el.ElementId, out int bin)) continue;
                if (bin < 0 || bin >= scene.Legend.Colors.Length) continue;

                var brush = scene.Legend.Colors[bin];

                var pts = el.Points;

                // Если нужно — поворот точек 90° вокруг центра world
                if (rotate)
                {
                    var rotPts = new Point[pts.Length];
                    for (int k = 0; k < pts.Length; k++)
                    {
                        double x = pts[k].X;
                        double y = pts[k].Y;
                        double xr = cx + (y - cy);
                        double yr = cy - (x - cx);
                        rotPts[k] = new Point(xr, yr);
                    }
                    pts = rotPts;
                }

                var geom = BuildPolygonGeometry(pts);
                if (geom == null) continue;

                dc.DrawGeometry(brush, meshPen, geom);
            }

            dc.Pop();
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

        private static void DrawLineText(DrawingContext dc, string text, Typeface tf, double size, double x, double y, double dpi)
        {
            var ft = new FormattedText(text, CultureInfo.GetCultureInfo("ru-RU"),
                FlowDirection.LeftToRight, tf, size, Brushes.Black, dpi);
            dc.DrawText(ft, new Point(x, y));
        }

        private static string FormatLegendNumber(double v)
        {
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
    }
}
