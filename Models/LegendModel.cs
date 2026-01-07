using System.Windows.Media;

namespace LiraMosaicViewer.Models
{
    public sealed class LegendModel
    {
        public int BinCount { get; init; } = 12;
        public double Min { get; init; }
        public double Max { get; init; }

        // Длины: Boundaries = BinCount+1, Colors = BinCount, PercentText = BinCount
        public double[] Boundaries { get; init; } = [];
        public SolidColorBrush[] Colors { get; init; } = [];
        public string[] PercentText { get; init; } = [];
    }
}
