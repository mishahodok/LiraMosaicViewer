using LiraMosaicViewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiraMosaicViewer.Core
{
    public static class FilePairFinder
    {
        public static IEnumerable<FilePairItem> FindPairs(string folder)
        {
            if (!Directory.Exists(folder)) yield break;

            var files = Directory.EnumerateFiles(folder, "*.csv").ToList();

            // key = "Z=-6.15" или "plates_z_m6_15" (старый вариант тоже оставляем)
            var geomByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // key -> (lc -> path)
            var momentsByKey = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

            // key -> (rsn -> path)
            var dispByKey = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);

                // --- GEOM ---
                // Новый нейминг: geom_Z=-6.15.csv  => key = "Z=-6.15"
                if (name.StartsWith("geom_", StringComparison.OrdinalIgnoreCase))
                {
                    var key = name.Substring("geom_".Length);
                    if (!string.IsNullOrWhiteSpace(key))
                        geomByKey[key] = file;

                    continue;
                }

                // Старый нейминг: plates_xxx_geom.csv => key = "plates_xxx"
                if (name.EndsWith("_geom", StringComparison.OrdinalIgnoreCase))
                {
                    var key = name.Substring(0, name.Length - "_geom".Length);
                    if (!string.IsNullOrWhiteSpace(key))
                        geomByKey[key] = file;

                    continue;
                }

                // --- MOMENTS ---
                // moments_Z=-6.15_LC=2.csv
                if (name.StartsWith("moments_", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = name.Substring("moments_".Length);  // "Z=-6.15_LC=2"
                    var idx = rest.IndexOf("_LC=", StringComparison.OrdinalIgnoreCase);
                    if (idx <= 0) continue;

                    var key = rest.Substring(0, idx);             // "Z=-6.15"
                    var lcStr = rest.Substring(idx + "_LC=".Length);

                    if (!int.TryParse(lcStr, out var lc)) continue;

                    if (!momentsByKey.TryGetValue(key, out var map))
                    {
                        map = new Dictionary<int, string>();
                        momentsByKey[key] = map;
                    }

                    map[lc] = file;
                    continue;
                }

                // Старый нейминг: plates_xxx_moments_lc.csv (если у тебя ещё встречается)
                if (name.EndsWith("_moments_lc", StringComparison.OrdinalIgnoreCase))
                {
                    // считаем что это один LC=1 (как было в самом начале)
                    var key = name.Substring(0, name.Length - "_moments_lc".Length);
                    if (string.IsNullOrWhiteSpace(key)) continue;

                    if (!momentsByKey.TryGetValue(key, out var map))
                    {
                        map = new Dictionary<int, string>();
                        momentsByKey[key] = map;
                    }

                    map[1] = file;
                    continue;
                }

                // --- DISPLACEMENTS ---
                // plates_Z=-6.15_displacements_rsn3.csv
                if (name.StartsWith("plates_", StringComparison.OrdinalIgnoreCase))
                {
                    var rest = name.Substring("plates_".Length); // "Z=-6.15_displacements_rsn3"
                    const string mark = "_displacements_rsn";

                    var idx = rest.IndexOf(mark, StringComparison.OrdinalIgnoreCase);
                    if (idx <= 0) continue;

                    var key = rest.Substring(0, idx); // "Z=-6.15"
                    var rsnStr = rest.Substring(idx + mark.Length); // "3"

                    if (!int.TryParse(rsnStr, out var rsn)) continue;

                    if (!dispByKey.TryGetValue(key, out var map))
                    {
                        map = new Dictionary<int, string>();
                        dispByKey[key] = map;
                    }

                    map[rsn] = file;
                    continue;
                }
            }

            // Важно: возвращаем ВСЕ плиты, где есть геометрия,
            // даже если моментов/перемещений нет (чтобы UI не ломался и можно было увидеть "нет данных")
            foreach (var kv in geomByKey.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var key = kv.Key;
                var item = new FilePairItem
                {
                    BaseName = key,
                    GeomPath = kv.Value
                };

                if (momentsByKey.TryGetValue(key, out var m))
                {
                    foreach (var p in m) item.MomentsByLc[p.Key] = p.Value;
                }

                if (dispByKey.TryGetValue(key, out var d))
                {
                    foreach (var p in d) item.DisplacementsByRsn[p.Key] = p.Value;
                }

                yield return item;
            }
        }
    }
}
