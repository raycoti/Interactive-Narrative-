using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The Maya models contain faces that sit exactly on top of other faces, often a copy
// with its normal flipped so it renders dark. The editor's Metal renderer always draws
// the same one of each pair, but WebGL alternates between them, which shows as black
// hatching on lit walls. This keeps one face of each pair when the models are imported.
// The .fbx files are not changed.
public class ModelOverlapCleaner : AssetPostprocessor {
    const string ModelFolder = "Assets/Models Animation/";
    // Distances below are relative to the model's overall size.
    const float PlaneTolerance = 5e-5f;
    const float ParallelCos = 0.9995f;

    public override uint GetVersion () { return 1; }

    class Tri {
        public int mesh, submesh, index;  // where it lives, and the order it is drawn in
        public Vector3 a, b, c, facing, geometric;
        public float d;
        public bool normalsAgree;         // false when its lighting normal points away from its front
    }

    [MenuItem("Tools/Interactive Narrative/Reimport Models Without Duplicate Faces")]
    static void Reimport () {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelFolder.TrimEnd('/') }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }

    void OnPostprocessModel (GameObject root) {
        if (!assetPath.StartsWith(ModelFolder)) return;

        // Each mesh once, with its transform relative to the model root.
        var meshes = new List<(Mesh mesh, Matrix4x4 toRoot)>();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) {
            if (filter.sharedMesh == null || meshes.Any(m => m.mesh == filter.sharedMesh)) continue;
            meshes.Add((filter.sharedMesh, root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix));
        }
        if (meshes.Count == 0) return;

        // Work in coordinates normalised to the model's size, so the tolerances suit any model.
        var all = meshes.SelectMany(m => m.mesh.vertices.Select(v => m.toRoot.MultiplyPoint3x4(v))).ToArray();
        var bounds = new Bounds(all[0], Vector3.zero);
        foreach (var p in all) bounds.Encapsulate(p);
        var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (size <= 0) return;

        var tris = new List<Tri>();
        for (int mi = 0; mi < meshes.Count; mi++) {
            var (mesh, toRoot) = meshes[mi];
            var mirrored = toRoot.determinant < 0;
            var v = mesh.vertices.Select(p => (toRoot.MultiplyPoint3x4(p) - bounds.center) / size).ToArray();
            var n = mesh.normals.Select(x => toRoot.MultiplyVector(x)).ToArray();
            for (int s = 0; s < mesh.subMeshCount; s++) {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                var t = mesh.GetTriangles(s);
                for (int i = 0; i < t.Length; i += 3) {
                    Vector3 a = v[t[i]], b = v[t[i + 1]], c = v[t[i + 2]];
                    var cross = Vector3.Cross(b - a, c - a);
                    if (cross.sqrMagnitude < 1e-16f) continue;
                    var g = cross.normalized;
                    var facing = mirrored ? -g : g;
                    var agree = n.Length == 0 || Vector3.Dot(n[t[i]] + n[t[i + 1]] + n[t[i + 2]], facing) > 0;
                    tris.Add(new Tri { mesh = mi, submesh = s, index = i, a = a, b = b, c = c, facing = facing,
                                       geometric = g, d = Vector3.Dot(facing, a), normalsAgree = agree });
                }
            }
        }

        const float cell = PlaneTolerance * 4;
        var buckets = new Dictionary<(int, int, int, long), List<Tri>>();
        foreach (var t in tris) {
            var key = (Mathf.RoundToInt(t.facing.x * 50), Mathf.RoundToInt(t.facing.y * 50),
                       Mathf.RoundToInt(t.facing.z * 50), (long)Mathf.Floor(t.d / cell));
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Tri>();
            list.Add(t);
        }

        var removed = new HashSet<Tri>();
        foreach (var kv in buckets) {
            var (nx, ny, nz, dk) = kv.Key;
            foreach (var p in kv.Value) {
                for (long k = dk; k <= dk + 1; k++) {
                    if (!buckets.TryGetValue((nx, ny, nz, k), out var others)) continue;
                    foreach (var q in others) {
                        if (ReferenceEquals(p, q) || removed.Contains(p) || removed.Contains(q)) continue;
                        if (Vector3.Dot(p.facing, q.facing) < ParallelCos) continue;
                        if (Mathf.Abs(p.d - q.d) > PlaneTolerance) continue;
                        if (!Overlaps(p, q) && !Overlaps(q, p)) continue;
                        removed.Add(Loser(p, q));
                    }
                }
            }
        }
        if (removed.Count == 0) return;

        for (int mi = 0; mi < meshes.Count; mi++) {
            var mesh = meshes[mi].mesh;
            for (int s = 0; s < mesh.subMeshCount; s++) {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                var drop = new HashSet<int>(removed.Where(r => r.mesh == mi && r.submesh == s).Select(r => r.index));
                if (drop.Count == 0) continue;
                var t = mesh.GetTriangles(s);
                var kept = new List<int>(t.Length);
                for (int i = 0; i < t.Length; i += 3)
                    if (!drop.Contains(i)) kept.AddRange(new[] { t[i], t[i + 1], t[i + 2] });
                mesh.SetTriangles(kept, s);
            }
        }
        Debug.Log("[ModelOverlapCleaner] " + assetPath + ": removed " + removed.Count + " duplicate faces.");
    }

    // Drop the face whose lighting is backwards; otherwise the one drawn first, which a later
    // face drawn at the same depth covers on desktop anyway.
    static Tri Loser (Tri p, Tri q) {
        if (p.normalsAgree != q.normalsAgree) return p.normalsAgree ? q : p;
        if (p.mesh != q.mesh) return p.mesh < q.mesh ? p : q;
        if (p.submesh != q.submesh) return p.submesh < q.submesh ? p : q;
        return p.index < q.index ? p : q;
    }

    // True if p's interior pokes into q, sampled at p's centroid and near each corner.
    static bool Overlaps (Tri p, Tri q) {
        var centre = (p.a + p.b + p.c) / 3f;
        return Inside(centre, q) || Inside(Vector3.Lerp(centre, p.a, 0.9f), q)
            || Inside(Vector3.Lerp(centre, p.b, 0.9f), q) || Inside(Vector3.Lerp(centre, p.c, 0.9f), q);
    }

    static bool Inside (Vector3 pt, Tri q) {
        const float margin = 1e-6f;
        return Side(q.a, q.b, pt, q.geometric) > margin && Side(q.b, q.c, pt, q.geometric) > margin
            && Side(q.c, q.a, pt, q.geometric) > margin;
    }

    // Signed distance of pt from the edge e0->e1, positive on the triangle's inside.
    static float Side (Vector3 e0, Vector3 e1, Vector3 pt, Vector3 n) {
        var edge = e1 - e0;
        return Vector3.Dot(Vector3.Cross(edge, pt - e0), n) / edge.magnitude;
    }
}
