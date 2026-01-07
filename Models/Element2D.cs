using System.Windows;

namespace LiraMosaicViewer.Models
{
    public sealed class Element2D
    {
        public int ElementId { get; init; }
        public Point[] Points { get; init; } = [];
    }
}
