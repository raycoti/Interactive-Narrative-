using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

// Marks a box or mesh as a wall that only stops a flying player (see the fly mode in
// RigidbodyFirstPersonController). Walking players pass through it, and it's hidden
// in play. In the Scene view its collider is outlined in green. Add it to any object
// with a solid (non-trigger) collider, or use GameObject > Interactive Narrative >
// Invisible Wall.
[RequireComponent(typeof(Collider))]
public class InvisibleWall : MonoBehaviour {
    const string LayerName = RigidbodyFirstPersonController.InvisibleWallLayer;

    void Reset () {
        UseWallLayer();
        GetComponent<Collider>().isTrigger = false;
    }

    void Awake () {
        UseWallLayer();
        var meshRenderer = GetComponent<Renderer>();
        if (meshRenderer != null) meshRenderer.enabled = false;
    }

    void UseWallLayer () {
        var layer = LayerMask.NameToLayer(LayerName);
        if (layer >= 0) gameObject.layer = layer;
        else Debug.LogWarning("Add a layer named " + LayerName + " in Project Settings > Tags and Layers.", this);
    }

    void OnDrawGizmos () {
        var wall = GetComponent<Collider>();
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.8f);
        Gizmos.matrix = transform.localToWorldMatrix;
        var box = wall as BoxCollider;
        var mesh = wall as MeshCollider;
        if (box != null) {
            Gizmos.DrawWireCube(box.center, box.size);
        } else if (mesh != null && mesh.sharedMesh != null) {
            Gizmos.DrawWireMesh(mesh.sharedMesh);
        } else {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireCube(wall.bounds.center, wall.bounds.size);
        }
    }
}
