#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace DoorPuzzle.Editor
{
    public static class ProjectSetup
    {
        private const string SceneFolder = "Assets/Scenes";
        private const string TitleScenePath = SceneFolder + "/Scene-1 Title.unity";
        private const string ScenePath = SceneFolder + "/Scene-TestLevel.unity";
        private const string TestScenePath = SceneFolder + "/Scene-TestLevel.unity";
        private const string RainAudioPath = "Assets/@SE/bgm_RainyAtmo.wav";
        private const string WetStreetMaterialPath = "Assets/@FX/Materials/Wet Street.mat";
        private const string RainyAlleyAtmospherePath = "Assets/@Prefabs/Environment/Rainy Alley Atmosphere.prefab";
        private const string LightingRigPath = "Assets/@Prefabs/Level/Lighting Rig.prefab";
        private const string TitleStreetPath = "Assets/@Prefabs/Environment/Title Street Environment.prefab";

        [MenuItem("Tools/Blues With You/Open Demo Scene", priority = 10)]
        public static void OpenDemoScene()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        [MenuItem("Tools/Blues With You/Open Title Scene", priority = 11)]
        public static void OpenTitleScene()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            }
        }

        [MenuItem("Tools/Blues With You/Validate Installation", priority = 20)]
        public static void ValidateInstallation()
        {
            var missing = new List<string>();
            var shaders = new[]
            {
                "Blues With You/Stencil Mask",
                "Blues With You/Stencil Lit",
                "Blues With You/Portal VFX",
                "Blues With You/Rain/Wet Street",
                "Blues With You/Rain/Rain Streak"
            };
            foreach (var shader in shaders)
            {
                if (Shader.Find(shader) == null) missing.Add(shader);
            }
            if (!File.Exists(ScenePath)) missing.Add(ScenePath);
            if (!File.Exists(TitleScenePath)) missing.Add(TitleScenePath);
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(RainAudioPath) == null) missing.Add(RainAudioPath);
            if (AssetDatabase.LoadAssetAtPath<PuzzleChunkCatalog>("Assets/Resources/PuzzleChunkCatalog.asset") == null)
                missing.Add("Assets/Resources/PuzzleChunkCatalog.asset");

            if (missing.Count == 0)
            {
                Debug.Log("[DoorPuzzle] Validation passed: demo scene and all five URP HLSL shaders are available.");
            }
            else
            {
                Debug.LogError("[DoorPuzzle] Validation failed. Missing: " + string.Join(", ", missing));
            }
        }

        [MenuItem("Tools/Blues With You/Apply Rendering Configuration", priority = 25)]
        public static void ApplyRenderingConfiguration()
        {
            EnsureStencilRendererFeature();
            AddSceneToBuildSettings();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Blues With You";
            AssetDatabase.SaveAssets();
            Debug.Log("[DoorPuzzle] Forward renderer and build scene configuration applied.");
        }

        [MenuItem("Tools/Blues With You/Run Solve Smoke Test", priority = 30)]
        public static void RunSolveSmokeTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[DoorPuzzle] Enter Play Mode before running the solve smoke test.");
                return;
            }

            var puzzle = Object.FindFirstObjectByType<PuzzleController>();
            if (puzzle == null)
            {
                Debug.LogError("[DoorPuzzle] No active PuzzleController was found.");
                return;
            }

            puzzle.DebugSolve();
            Debug.Log("[DoorPuzzle] Solve smoke test started.");
        }

        private static void EnsureDemoScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            if (!File.Exists(ScenePath))
            {
                var previousScene = SceneManager.GetActiveScene();
                if (previousScene.IsValid() && string.IsNullOrEmpty(previousScene.path))
                {
                    const string backupFolder = "Assets/Scenes";
                    if (!AssetDatabase.IsValidFolder(backupFolder))
                    {
                        AssetDatabase.CreateFolder("Assets", "Scenes");
                    }

                    var backupPath = AssetDatabase.GenerateUniqueAssetPath(backupFolder + "/Untitled_BeforeDemo.unity");
                    if (!EditorSceneManager.SaveScene(previousScene, backupPath))
                    {
                        Debug.LogError("[DoorPuzzle] Could not safely save the current Untitled scene. Demo generation was postponed.");
                        return;
                    }
                    Debug.Log("[DoorPuzzle] Preserved the current Untitled scene at " + backupPath);
                }

                var demoScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(demoScene);

                var experience = new GameObject("Blues With You Experience");
                experience.AddComponent<WorldBuilder>();
                EditorSceneManager.MarkSceneDirty(demoScene);
                EditorSceneManager.SaveScene(demoScene, ScenePath);
                EditorSceneManager.CloseScene(demoScene, true);

                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }

                Debug.Log("[DoorPuzzle] Created URP demo scene at " + ScenePath);
            }

            NormalizeDemoScene();
            ConfigureGameplayScene(ScenePath);
            ConfigureGameplayScene(TestScenePath);
            EnsureTitleScene();
            PrefabBaker.EnsureGenerated();
            EnsureStencilRendererFeature();
            AddSceneToBuildSettings();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "Blues With You";
            AssetDatabase.SaveAssets();
        }

        private static void EnsureTitleScene()
        {
            var previousScene = SceneManager.GetActiveScene();
            var titleScene = SceneManager.GetSceneByPath(TitleScenePath);
            var openedForUpdate = !titleScene.IsValid() || !titleScene.isLoaded;

            if (!File.Exists(TitleScenePath))
            {
                titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            }
            else if (openedForUpdate)
            {
                titleScene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Additive);
            }

            SceneManager.SetActiveScene(titleScene);
            TitleSequenceController controller = null;
            foreach (var root in titleScene.GetRootGameObjects())
            {
                controller = root.GetComponent<TitleSequenceController>();
                if (controller != null) break;
            }

            if (controller == null)
            {
                var root = new GameObject("Title Sequence");
                controller = root.AddComponent<TitleSequenceController>();
            }

            var settings = new SerializedObject(controller);
            settings.FindProperty("gameplaySceneName").stringValue = "Scene-TestLevel";
            settings.FindProperty("rainyAtmo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RainAudioPath);
            settings.FindProperty("wetStreetMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(WetStreetMaterialPath);
            settings.FindProperty("rainyAlleyAtmospherePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(RainyAlleyAtmospherePath);
            settings.FindProperty("lightingRigPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(LightingRigPath);
            settings.FindProperty("titleStreetPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(TitleStreetPath);
            settings.FindProperty("ambienceVolume").floatValue = 0.52f;
            settings.FindProperty("headsetPromptFadeDuration").floatValue = 0.75f;
            settings.FindProperty("headsetPromptHoldDuration").floatValue = 1.8f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(titleScene);
            EditorSceneManager.SaveScene(titleScene, TitleScenePath);

            if (openedForUpdate)
            {
                EditorSceneManager.CloseScene(titleScene, true);
            }
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }
        }

        private static void ConfigureGameplayScene(string path)
        {
            if (!File.Exists(path)) return;
            var scene = SceneManager.GetSceneByPath(path);
            var openedForUpdate = !scene.IsValid() || !scene.isLoaded;
            if (openedForUpdate)
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }

            var rainClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RainAudioPath);
            foreach (var root in scene.GetRootGameObjects())
            {
                var builder = root.GetComponent<WorldBuilder>();
                if (builder == null) continue;
                var settings = new SerializedObject(builder);
                settings.FindProperty("rainyAtmo").objectReferenceValue = rainClip;
                settings.FindProperty("rainyAtmoVolume").floatValue = 0.62f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                break;
            }

            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
            if (openedForUpdate) EditorSceneManager.CloseScene(scene, true);
        }

        private static void EnsureStencilRendererFeature()
        {
            var rendererPaths = new[]
            {
                "Assets/Settings/Mobile_Renderer.asset",
                "Assets/Settings/PC_Renderer.asset"
            };
            var portalLayerMask = (1 << PortalStencilRendererFeature.ContentLayer) |
                                  (1 << PortalStencilRendererFeature.MaskLayer);

            foreach (var rendererPath in rendererPaths)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
                if (rendererData == null) continue;

                var found = false;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature is PortalStencilRendererFeature)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var feature = ScriptableObject.CreateInstance<PortalStencilRendererFeature>();
                    feature.name = "Blues With You Portal Stencil";
                    rendererData.rendererFeatures.Add(feature);
                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                }

                rendererData.opaqueLayerMask = ~portalLayerMask;
                rendererData.transparentLayerMask = ~portalLayerMask;
                EditorUtility.SetDirty(rendererData);
            }
        }

        private static void NormalizeDemoScene()
        {
            var demoScene = SceneManager.GetSceneByPath(ScenePath);
            var openedForUpgrade = !demoScene.IsValid() || !demoScene.isLoaded;
            if (openedForUpgrade)
            {
                demoScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            foreach (var root in demoScene.GetRootGameObjects())
            {
                if (root.GetComponent<WorldBuilder>() != null)
                {
                    root.name = "Blues With You Experience";
                    EditorSceneManager.MarkSceneDirty(demoScene);
                    break;
                }
            }

            if (demoScene.isDirty)
            {
                EditorSceneManager.SaveScene(demoScene);
            }
            if (openedForUpgrade)
            {
                EditorSceneManager.CloseScene(demoScene, true);
            }
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };

            foreach (var existing in EditorBuildSettings.scenes)
            {
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(existing.path);
                if (sceneAsset != null &&
                    !string.Equals(existing.path, ScenePath, System.StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(existing.path, TitleScenePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    scenes.Add(existing);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
