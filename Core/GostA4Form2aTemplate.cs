using System;
using System.Windows;

namespace LiraMosaicViewer.Core
{
    public sealed class GostA4Form2aTemplate
    {
        // A4 portrait
        public const double PageWmm = 210;
        public const double PageHmm = 297;

        // ГОСТ поля рамки
        public const double MarginLeftMm = 20;
        public const double MarginTopMm = 5;
        public const double MarginRightMm = 5;
        public const double MarginBottomMm = 5;

        // Штамп форма 2а: один ряд 15 мм, ширина 185 мм
        public const double StampHeightMm = 15;
        public const double StampWidthMm = 185;

        // Разбивка по X (мм) внутри 185 мм
                // 7 + 10 + 23 + 15 + 10 + 110 + 10 = 185
        public static readonly double[] StampColsMm = { 7, 10, 23, 15, 10, 110, 10 };

        // Правый блок "Лист" по высоте: 7 (заголовок) + 8 (номер) = 15
        public const double StampSheetHeaderMm = 7;
        public const double StampSheetNumberMm = 8;


        // “Слоты” компоновки (можно потом подстроить)
        public const double HeaderHeightMm = 22;     // 3 строки
        public const double CaptionHeightMm = 10;    // подпись под рисунком
        public const double LegendBarWidthMm = 8;    // цветная шкала
        public const double LegendTotalWidthMm = 28; // шкала + подписи

        public Rect FrameRectMm =>
            new Rect(
                MarginLeftMm,
                MarginTopMm,
                PageWmm - MarginLeftMm - MarginRightMm,
                PageHmm - MarginTopMm - MarginBottomMm);

        public Rect StampRectMm
        {
            get
            {
                var fr = FrameRectMm;
                // штамп в правом нижнем углу внутри рамки, ширина 185, высота 15
                double x = fr.Right - StampWidthMm;
                double y = fr.Bottom - StampHeightMm;
                return new Rect(x, y, StampWidthMm, StampHeightMm);
            }
        }

        public Rect ContentRectMm
        {
            get
            {
                var fr = FrameRectMm;
                var st = StampRectMm;
                // всё внутри рамки, но выше штампа
                return new Rect(fr.X, fr.Y, fr.Width, st.Y - fr.Y);
            }
        }

        public Rect HeaderRectMm
        {
            get
            {
                var cr = ContentRectMm;
                return new Rect(cr.X, cr.Y, cr.Width, HeaderHeightMm);
            }
        }

        public Rect CaptionRectMm
        {
            get
            {
                var cr = ContentRectMm;
                return new Rect(cr.X, cr.Bottom - CaptionHeightMm, cr.Width, CaptionHeightMm);
            }
        }

        public Rect FigureAreaRectMm
        {
            get
            {
                var cr = ContentRectMm;
                var hr = HeaderRectMm;
                var cap = CaptionRectMm;

                double top = hr.Bottom;
                double bottom = cap.Y;

                return new Rect(cr.X, top, cr.Width, bottom - top);
            }
        }

        public Rect LegendRectMm
        {
            get
            {
                var fa = FigureAreaRectMm;
                // слева внутри области рисунка — вертикальная легенда
                return new Rect(fa.X, fa.Y, LegendTotalWidthMm, fa.Height);
            }
        }

        public Rect MosaicRectMm
        {
            get
            {
                var fa = FigureAreaRectMm;
                var lg = LegendRectMm;

                // справа от легенды
                double x = lg.Right;
                return new Rect(x, fa.Y, fa.Right - x, fa.Height);
            }
        }

        // мм -> WPF DIU (1/96")
        public static double MmToDiu(double mm) => mm * 96.0 / 25.4;

        public static Rect MmRectToDiu(Rect mmRect) =>
            new Rect(MmToDiu(mmRect.X), MmToDiu(mmRect.Y), MmToDiu(mmRect.Width), MmToDiu(mmRect.Height));
    }
}
