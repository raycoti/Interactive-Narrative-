using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the WebGL player into "web game/" so it can be run from the command line:
// Unity -batchmode -quit -projectPath . -executeMethod WebGLBuilder.Build
public static class WebGLBuilder {

    const string OutputPath = "web game";

    [MenuItem("Tools/Interactive Narrative/Build WebGL")]
    public static void Build () {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(scenes, OutputPath, BuildTarget.WebGL, BuildOptions.None);
        Debug.Log("WebGL build result: " + report.summary.result + ", " + report.summary.totalSize + " bytes");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
