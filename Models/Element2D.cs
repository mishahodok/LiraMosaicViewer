using System;
using System.Windows;

namespace LiraMosaicViewer.Models
{
    public sealed class Element2D
    {
        public int ElementId { get; set; }

        // ВАЖНО: нужно для узловых результатов (перемещения Ux/Uy/Uz).
        // Для моментов (по элементам) можно не заполнять — ничего не сломается.
        public int[] NodeIds { get; set; } = Array.Empty<int>();

        public Point[] Points { get; set; } = Array.Empty<Point>();
    }
}
