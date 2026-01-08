using System.Collections.Generic;
using System.Linq;

namespace LiraMosaicViewer.Models
{
    public sealed class FilePairItem
    {
        public string BaseName { get; set; } = "";
        public string GeomPath { get; set; } = "";

        // Старый режим (если вдруг где-то остался один файл моментов на плиту)
        public string MomentsPath { get; set; } = "";

        // Новый режим: моменты разнесены по файлам на каждый LC
        // LC -> путь к csv
        public Dictionary<int, string> MomentsByLc { get; } = new();

        // Новый режим: перемещения разнесены по файлам на каждый RSN
        // RSN -> путь к csv
        public Dictionary<int, string> DisplacementsByRsn { get; } = new();

        // Удобные read-only представления (если нужно для биндинга/кеша)
        public IReadOnlyDictionary<int, string> MomentsPaths => MomentsByLc;
        public IReadOnlyDictionary<int, string> DisplacementsPaths => DisplacementsByRsn;

        // Чтобы не ловить пустоту/несовместимость: отдаём список путей, который
        // понимает MomentsCsvReader.ReadTables(IEnumerable<string>)
        public IEnumerable<string> EnumerateMomentsPaths()
        {
            if (MomentsByLc.Count > 0) return MomentsByLc.Values;
            if (!string.IsNullOrWhiteSpace(MomentsPath)) return new[] { MomentsPath };
            return Enumerable.Empty<string>();
        }
        // Любой moments-файл (нужен для Build(...) как "опорный" путь/диагностика)
        public string AnyMomentsPath => EnumerateMomentsPaths().FirstOrDefault() ?? "";

        public IEnumerable<string> EnumerateDisplacementsPaths()
        {
            if (DisplacementsByRsn.Count > 0) return DisplacementsByRsn.Values;
            return Enumerable.Empty<string>();
        }

        public bool HasAnyMoments => MomentsByLc.Count > 0 || !string.IsNullOrWhiteSpace(MomentsPath);
        public bool HasAnyDisplacements => DisplacementsByRsn.Count > 0;

        public override string ToString() => BaseName;
    }
}
