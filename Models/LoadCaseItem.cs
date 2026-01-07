namespace LiraMosaicViewer.Models
{
    public sealed class LoadCaseItem
    {
        public int Lc { get; set; }
        public string Name { get; set; } = "";

        public string Display => $"{Lc} — {Name}";
    }
}
