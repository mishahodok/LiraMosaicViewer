using System.Collections.Generic;
using System.Linq;

namespace LiraMosaicViewer.Models
{
    public sealed class FilePairItem
    {
        public required string BaseName { get; init; }   // например "Z=-6.15"
        public required string GeomPath { get; init; }   // geom_*.csv

        // moments_Z=..._LC=2.csv
        public Dictionary<int, string> MomentsByLc { get; } = new();

        // plates_Z=..._displacements_rsn3.csv
        public Dictionary<int, string> DisplacementsByRsn { get; } = new();

        public IEnumerable<int> AvailableLCs => MomentsByLc.Keys.OrderBy(x => x);
        public IEnumerable<int> AvailableRsns => DisplacementsByRsn.Keys.OrderBy(x => x);

        public bool TryGetMomentsPath(int lc, out string path) => MomentsByLc.TryGetValue(lc, out path!);
        public bool TryGetDisplacementsPath(int rsn, out string path) => DisplacementsByRsn.TryGetValue(rsn, out path!);

        public string DisplayName => BaseName;
    }
}
