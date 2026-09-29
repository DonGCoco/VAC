using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VACExperiment.Board;

namespace VACExperiment.EditorTools
{
    public static class VACMilestone1Setup
    {
        private const string SceneDirectory = "Assets/Scenes";
        private const string ScenePath = SceneDirectory + "/M1_SmokeTest.unity";

        [MenuItem("VAC/Milestone 1/Create Board Registration Scene")]
        public static void CreateBoardRegistrationScene()
        {
            string rigGuid = AssetDatabase.FindAssets("ML Rig t:Prefab").FirstOrDefault();

            if (string.IsNullOrEmpty(rigGuid))
            {
                EditorUtility.DisplayDialog(
                    "Magic Leap ML Rig sample not found",
                    "Import the Magic Leap SDK 'ML Rig & OpenXR Input' sample first.",
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
                rigCamera.nearClipPlane = 0.25f;

            XROrigin xrOrigin = rig.GetComponent<XROrigin>();
            if (xrOrigin == null)
                xrOrigin = rig.GetComponentInChildren<XROrigin>(true);

            if (xrOrigin == null)
            {
                Debug.LogError("VAC Milestone 1: No XROrigin was found on the official ML Rig.");
                return;
            }

            GameObject registrationObject = new("M1_BoardRegistration");
            BoardRegistration registration = registrationObject.AddComponent<BoardRegistration>();

            GameObject boardAnchor = new("BoardAnchor");

            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = "M1_BoardTestCube";
            testCube.transform.SetParent(boardAnchor.transform, false);
            testCube.transform.localPosition = new Vector3(0f, 0f, -0.015f);
            testCube.transform.localScale = new Vector3(0.06f, 0.06f, 0.01f);

            registration.Configure(xrOrigin, boardAnchor.transform);

            GameObject lightObject = new("M1_DirectionalLight");
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

            Selection.activeGameObject = registrationObject;
            EditorGUIUtility.PingObject(registrationObject);

            Debug.Log(
                "VAC Milestone 1 board-registration scene created. " +
                "Configure QR text, measured QR size and marker-to-board offset on M1_BoardRegistration.");
        }
    }
}
