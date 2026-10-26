using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Builds Sandbox.unity: an empty version of test44 with the same fog and ambient
// light, a floor, and the first-person player, for trying things out away from
// the main scene.
public static class SandboxSceneCreator {
    const string ScenePath = "Assets/Sandbox.unity";
    const string PlayerPrefabPath = "Assets/Standard Assets/networkfirst.prefab";
    const string FloorMaterialPath = "Assets/Materials/concrete.mat";

    [MenuItem("Tools/Interactive Narrative/Create Sandbox Scene")]
    static void Create () {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Sandbox scene exists", ScenePath + " already exists. Replace it?", "Replace", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Match test44's atmosphere.
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.01f;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.23f;

        // test44 is lit only by its spotlights, so give the empty scene a sun.
        var sun = new GameObject("Directional Light").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);

        // The plane mesh is 10x10 units, so this makes a 100x100 floor.
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(10, 1, 10);
        var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>(FloorMaterialPath);
        if (floorMaterial != null)
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

        // Placed directly rather than through LocalPlayerSpawner so it can be moved
        // in the editor. The capsule is 1.6 tall and centred on the root, so y = 1
        // rests it just above the floor.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        player.transform.SetPositionAndRotation(new Vector3(0, 1, 0), Quaternion.identity);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Created " + ScenePath);
    }
}
