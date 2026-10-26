using UnityEditor;
using UnityEngine;

// Adds an invisible wall (a cube that only stops flying players) to the open scene.
public static class InvisibleWallCreator {

    [MenuItem("GameObject/Interactive Narrative/Invisible Wall", false, 10)]
    static void Create (MenuCommand command) {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Invisible Wall";
        wall.transform.localScale = new Vector3(10f, 10f, 1f);
        GameObjectUtility.SetParentAndAlign(wall, command.context as GameObject);
        if (command.context == null && SceneView.lastActiveSceneView != null)
            wall.transform.position = SceneView.lastActiveSceneView.pivot;
        wall.AddComponent<InvisibleWall>();
        Undo.RegisterCreatedObjectUndo(wall, "Create Invisible Wall");
        Selection.activeObject = wall;
    }
}
