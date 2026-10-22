using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Finds surfaces in the open scene that lie on top of each other: same plane, facing the
// same way, overlapping. These flicker ("z-fight") in WebGL builds, whose depth buffer is
// far less precise than the Metal renderer the editor uses, so they can look fine here.
public static class OverlappingSurfaceFinder {
    // How far apart two surfaces can be and still count as overlapping, in world units.
    const float PlaneTolerance = 0.002f;
    // Cosine of the largest angle between two surfaces that still counts as parallel.
    const float ParallelCos = 0.9995f;

    class Tri {
        public Renderer renderer;
        public Vector3 a, b, c;
        public Vector3 facing;   // the side that is drawn
        public Vector3 geometric; // winding normal, for point-in-triangle tests
        public float d, area;
    }

    [MenuItem("Tools/Interactive Narrative/Find Overlapping Surfaces")]
    static void Find () {
        var tris = new List<Tri>();
        foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
            var renderer = filter.GetComponent<MeshRenderer>();
            var mesh = filter.sharedMesh;
            if (renderer == null || !renderer.enabled || !filter.gameObject.activeInHierarchy || mesh == null)
                continue;
            var m = filter.transform.localToWorldMatrix;
            // A mirrored transform reverses the winding, and Unity flips culling to match.
            var mirrored = m.determinant < 0;
            var v = mesh.vertices.Select(p => m.MultiplyPoint3x4(p)).ToArray();
            for (int s = 0; s < mesh.subMeshCount; s++) {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                var t = mesh.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3) {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    var cross = Vector3.Cross(b - a, c - a);
                    if (cross.sqrMagnitude < 1e-12f) continue;
                    var g = cross.normalized;
                    var facing = mirrored ? -g : g;
                    tris.Add(new Tri { renderer = renderer, a = a, b = b, c = c, facing = facing, geometric = g,
                                       d = Vector3.Dot(facing, a), area = cross.magnitude * 0.5f });
                }
            }
        }

        // Bucket by rounded facing direction and plane offset so only likely pairs are compared.
        const float cell = PlaneTolerance * 4;
        var buckets = new Dictionary<(int, int, int, long), List<Tri>>();
        foreach (var t in tris) {
            var key = (Mathf.RoundToInt(t.facing.x * 50), Mathf.RoundToInt(t.facing.y * 50),
                       Mathf.RoundToInt(t.facing.z * 50), (long)Mathf.Floor(t.d / cell));
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Tri>();
            list.Add(t);
        }

        var pairs = new Dictionary<(Renderer, Renderer), (int count, float area, float gap)>();
        foreach (var kv in buckets) {
            var (nx, ny, nz, dk) = kv.Key;
            foreach (var p in kv.Value) {
                for (long k = dk; k <= dk + 1; k++) {
                    if (!buckets.TryGetValue((nx, ny, nz, k), out var others)) continue;
                    foreach (var q in others) {
                        if (ReferenceEquals(p, q)) continue;
                        // Visit each unordered pair once.
                        if (k == dk && p.GetHashCode() > q.GetHashCode()) continue;
                        if (Vector3.Dot(p.facing, q.facing) < ParallelCos) continue;
                        var gap = Mathf.Abs(p.d - q.d);
                        if (gap > PlaneTolerance) continue;
                        if (!Overlaps(p, q) && !Overlaps(q, p)) continue;
                        var key = p.renderer.GetInstanceID() <= q.renderer.GetInstanceID()
                            ? (p.renderer, q.renderer) : (q.renderer, p.renderer);
                        pairs.TryGetValue(key, out var acc);
                        pairs[key] = (acc.count + 1, acc.area + Mathf.Min(p.area, q.area), Mathf.Max(acc.gap, gap));
                    }
                }
            }
        }

        if (pairs.Count == 0) {
            Debug.Log("[Overlaps] No overlapping surfaces found among " + tris.Count + " triangles.");
            return;
        }
        Debug.Log("[Overlaps] " + pairs.Count + " pairs of objects have overlapping surfaces (" + tris.Count + " triangles checked). Largest first:");
        foreach (var kv in pairs.OrderByDescending(x => x.Value.area)) {
            var (r1, r2) = kv.Key;
            var (count, area, gap) = kv.Value;
            var who = r1 == r2 ? Path(r1.transform) + " (overlaps itself)" : Path(r1.transform) + "  <->  " + Path(r2.transform);
            Debug.Log(string.Format("[Overlaps] {0}: {1} triangle pairs, {2:0.##} sq units, gap up to {3:0.#####}", who, count, area, gap));
        }
        Selection.objects = pairs.Keys.SelectMany(k => new Object[] { k.Item1.gameObject, k.Item2.gameObject }).Distinct().ToArray();
    }

    // True if p's interior pokes into q, sampled at p's centroid and near each corner.
    static bool Overlaps (Tri p, Tri q) {
        var centre = (p.a + p.b + p.c) / 3f;
        return Inside(centre, q) || Inside(Vector3.Lerp(centre, p.a, 0.9f), q)
            || Inside(Vector3.Lerp(centre, p.b, 0.9f), q) || Inside(Vector3.Lerp(centre, p.c, 0.9f), q);
    }

    static bool Inside (Vector3 pt, Tri q) {
        const float margin = 1e-5f;
        return Side(q.a, q.b, pt, q.geometric) > margin && Side(q.b, q.c, pt, q.geometric) > margin
            && Side(q.c, q.a, pt, q.geometric) > margin;
    }

    // Signed distance of pt from the edge e0->e1, positive on the triangle's inside.
    static float Side (Vector3 e0, Vector3 e1, Vector3 pt, Vector3 n) {
        var edge = e1 - e0;
        return Vector3.Dot(Vector3.Cross(edge, pt - e0), n) / edge.magnitude;
    }

    static string Path (Transform t) {
        var path = t.name;
        for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
        return path;
    }
}
