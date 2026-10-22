using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shrinks the open scene's environment around the player. The models were built far
// larger than the 1.6-unit-tall player, and the extra size also costs depth precision,
// which shows up as flickering surfaces in WebGL builds.
public class EnvironmentScaler : EditorWindow {
    const string EnvironmentName = "Environment";
    const string PlayerPrefabPath = "Assets/Standard Assets/networkfirst.prefab";

    // The player prefab's values at full size. Set from these rather than multiplied,
    // so applying a scale is the same however many times it has been applied before.
    const float ForwardSpeed = 12, BackwardSpeed = 10, StrafeSpeed = 12;
    const float FogStartDistance = 20, FarClipPlane = 2010;

    float scale = 0.4f;

    [MenuItem("Tools/Interactive Narrative/Set Environment Scale...")]
    static void Open () {
        var window = GetWindow<EnvironmentScaler>(true, "Environment Scale");
        var env = FindEnvironment(SceneManager.GetActiveScene());
        if (env != null) window.scale = env.transform.localScale.x;
        window.minSize = window.maxSize = new Vector2(320, 110);
    }

    void OnGUI () {
        var env = FindEnvironment(SceneManager.GetActiveScene());
        EditorGUILayout.LabelField("Current scale", env != null ? env.transform.localScale.x.ToString("0.###") : "1 (not scaled)");
        scale = EditorGUILayout.Slider("New scale", scale, 0.1f, 1f);
        EditorGUILayout.HelpBox("Scales everything except the player. 1 is the original size.", MessageType.None);
        if (GUILayout.Button("Apply")) Apply(scale);
    }

    static GameObject FindEnvironment (Scene scene) {
        return scene.GetRootGameObjects().FirstOrDefault(go => go.name == EnvironmentName);
    }

    static void Apply (float target) {
        var scene = SceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Scale Environment");

        // Gather everything except the player and its spawner under one parent at the origin,
        // so scaling the parent shrinks positions towards the origin where the player spawns.
        var env = FindEnvironment(scene);
        if (env == null) {
            env = new GameObject(EnvironmentName);
            Undo.RegisterCreatedObjectUndo(env, "Create Environment");
            foreach (var root in scene.GetRootGameObjects()) {
                if (root == env || IsPlayer(root)) continue;
                Undo.SetTransformParent(root.transform, env.transform, "Move into Environment");
            }
        }
        var current = env.transform.localScale.x;
        var ratio = target / current;
        if (Mathf.Approximately(ratio, 1)) return;

        Undo.RecordObject(env.transform, "Scale Environment");
        env.transform.localScale = Vector3.one * target;

        // Light ranges are in world units and ignore the parent's scale.
        foreach (var light in env.GetComponentsInChildren<Light>(true)) {
            if (light.type == LightType.Directional) continue;
            Undo.RecordObject(light, "Scale light range");
            light.range *= ratio;
        }

        foreach (var mover in env.GetComponentsInChildren<moveplayer>(true)) {
            Undo.RecordObject(mover, "Scale teleport lift");
            mover.lift *= ratio;
        }
        foreach (var trees in env.GetComponentsInChildren<creator>(true)) {
            Undo.RecordObject(trees, "Scale trees");
            trees.scale *= ratio;
        }

        // Fog thickens per unit of distance, so a smaller world needs denser fog.
        var renderSettings = typeof(RenderSettings)
            .GetMethod("GetRenderSettings", BindingFlags.NonPublic | BindingFlags.Static)?
            .Invoke(null, null) as Object;
        if (renderSettings != null) Undo.RecordObject(renderSettings, "Scale fog");
        RenderSettings.fogDensity /= ratio;
        RenderSettings.fogStartDistance *= ratio;
        RenderSettings.fogEndDistance *= ratio;

        EditorSceneManager.MarkSceneDirty(scene);
        ScalePlayerPrefab(target);

        Debug.Log("[Scale] Scaled " + scene.name + " from " + current + "x to " + target + "x. Save the scene to keep it.");
    }

    static bool IsPlayer (GameObject root) {
        return root.GetComponentInChildren<LocalPlayerSpawner>(true) != null
            || root.GetComponentInChildren<SetUpLocalPlayer>(true) != null;
    }

    static void ScalePlayerPrefab (float factor) {
        var player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try {
            var controller = new SerializedObject(player.GetComponent<UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController>());
            controller.FindProperty("movementSettings.ForwardSpeed").floatValue = ForwardSpeed * factor;
            controller.FindProperty("movementSettings.BackwardSpeed").floatValue = BackwardSpeed * factor;
            controller.FindProperty("movementSettings.StrafeSpeed").floatValue = StrafeSpeed * factor;
            controller.ApplyModifiedPropertiesWithoutUndo();

            var camera = player.GetComponentInChildren<Camera>(true);
            camera.farClipPlane = FarClipPlane * factor;

            // GlobalFog is internal to the game scripts, so set it through its serialized fields.
            var fog = camera.GetComponent("GlobalFog");
            if (fog != null) {
                var so = new SerializedObject(fog);
                so.FindProperty("startDistance").floatValue = FogStartDistance * factor;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        } finally {
            PrefabUtility.UnloadPrefabContents(player);
        }
    }
}
