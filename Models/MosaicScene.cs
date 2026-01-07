using System.Collections.Generic;
using System.Windows;

namespace LiraMosaicViewer.Models
{
    public sealed class MosaicScene
    {
        public required string TitleLeft { get; init; }
        public required string TitleRight { get; init; }

        public required IReadOnlyList<Element2D> Elements { get; init; }
        public required IReadOnlyDictionary<int, int> ElementToBin { get; init; } // elementId -> bin index
        public required Rect WorldBounds { get; init; }

        public required LegendModel Legend { get; init; }

        public int RenderedElementCount => Elements.Count;
    }
}
