using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class VacDepthDiagnosticBuild
{
    [MenuItem("VAC/Build Depth Diagnostic APK")]
    private static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android ||
            string.IsNullOrEmpty(scene.path) || scene.isDirty ||
            Object.FindObjectsByType<VACExperiment.DepthJudgmentManager>(
                FindObjectsSortMode.None).Length != 1)
        {
            EditorUtility.DisplayDialog("VAC Depth Diagnostic",
                "Switch the build target to Android and open the saved Pilot scene with one active DepthJudgmentManager.",
                "OK");
            return;
        }

        string folder = Path.GetFullPath("Builds");
        Directory.CreateDirectory(folder);
        string apk = Path.Combine(folder, "VAC-Depth-Diagnostic.apk");
        var target = UnityEditor.Build.NamedBuildTarget.Android;
        string previousId = PlayerSettings.GetApplicationIdentifier(target);
        string previousName = PlayerSettings.productName;
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetApplicationIdentifier(target, "com.vacexperiment.depthdiagnostic");
            PlayerSettings.productName = "VAC Depth Diagnostic";
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scene.path },
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new System.InvalidOperationException("VAC diagnostic APK build failed. See Unity Console.");
            EditorUtility.RevealInFinder(apk);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
            PlayerSettings.SetApplicationIdentifier(target, previousId);
            PlayerSettings.productName = previousName;
        }
    }
}
