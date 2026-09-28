using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VACExperiment.SmokeTest;

namespace VACExperiment.EditorTools
{
    public static class VACMilestone1Setup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/M1_SmokeTest.unity";

        [MenuItem("VAC/Milestone 1/Create Smoke Test Scene")]
        public static void CreateSmokeTestScene()
        {
            string rigGuid = AssetDatabase.FindAssets("ML Rig t:Prefab").FirstOrDefault();

            if (string.IsNullOrEmpty(rigGuid))
            {
                EditorUtility.DisplayDialog(
                    "Magic Leap ML Rig sample not found",
                    "Import the Magic Leap SDK 'ML Rig & Inputs' sample first, then run this menu command again.\n\n" +
                    "Package Manager > Magic Leap SDK > Samples > ML Rig & Inputs",
                    "OK");
                return;
            }

            string rigPath = AssetDatabase.GUIDToAssetPath(rigGuid);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);

            if (rigPrefab == null)
            {
                Debug.LogError($"VAC Milestone 1: Could not load ML Rig prefab at {rigPath}.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject rig = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;
            if (rig == null)
            {
                Debug.LogError("VAC Milestone 1: Could not instantiate the ML Rig prefab.");
                return;
            }

            rig.name = "ML Rig";

            Camera rigCamera = rig.GetComponentInChildren<Camera>(true);
            if (rigCamera != null)
            {
                rigCamera.nearClipPlane = 0.25f;
            }
            else
            {
                Debug.LogWarning("VAC Milestone 1: No Camera was found under the ML Rig. Check the imported sample.");
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "M1_SmokeTest_Cube";

            M1SmokeTestCube controller = cube.AddComponent<M1SmokeTestCube>();
            if (rigCamera != null)
                controller.Configure(rigCamera.transform);

            // These are only initial visible defaults.
            // The tester changes them directly in the Inspector.
            cube.transform.position = new Vector3(0f, 0f, 1f);
            cube.transform.localScale = Vector3.one * 0.25f;

            GameObject lightObject = new GameObject("M1_DirectionalLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            if (!AssetDatabase.IsValidFolder(SceneDirectory))
                Directory.CreateDirectory(SceneDirectory);

            EditorSceneManager.SaveScene(scene, ScenePath);

            var existingScenes = EditorBuildSettings.scenes
                .Where(s => s.path != ScenePath)
                .ToList();

            existingScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existingScenes.ToArray();

            Selection.activeGameObject = cube;
            EditorGUIUtility.PingObject(cube);

            Debug.Log(
                "VAC Milestone 1 smoke-test scene created. " +
                "Select M1_SmokeTest_Cube to edit distance/size parameters in the Inspector.");
        }
    }
}
