#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TownCharacterPrefabBuilder
{
    private const string SourceScenePath = "Assets/Scenes/InUse/FishingScene.unity";
    private const string TownScenePath = "Assets/Scenes/InUse/Town.unity";
    private const string GeneratedFolderPath = "Assets/Generated";
    private const string GeneratedTownFolderPath = "Assets/Generated/Town";
    private const string PrefabPath = "Assets/Generated/Town/TownCharacter.prefab";
    private const string SourceCharacterName = "autoriggedmainch";
    private const string TownBootstrapName = "TownBootstrap";
    private static readonly string[] FishingOnlyObjectNames =
    {
        "FlyLineSolver",
        "FlyLine",
        "SlackLineSolver",
        "SlackLine",
        "flyhook",
        "New FabrikSolver2D",
        "New FabrikSolver2D_Target",
        "bandIK"
    };

    static TownCharacterPrefabBuilder()
    {
        EditorApplication.delayCall += EnsureTownCharacterSetup;
    }

    private static void EnsureTownCharacterSetup()
    {
        EditorApplication.delayCall -= EnsureTownCharacterSetup;

        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
        {
            return;
        }

        EnsureFolder("Assets", "Generated");
        EnsureFolder(GeneratedFolderPath, "Town");

        GameObject prefab = BuildTownCharacterPrefab();

        if (prefab == null)
        {
            return;
        }

        AssignPrefabToTownBootstrap(prefab);
    }

    private static GameObject BuildTownCharacterPrefab()
    {
        Scene sourceScene = OpenSceneForEditing(SourceScenePath, out bool sourceSceneAlreadyLoaded);
        try
        {
            GameObject sourceCharacter = FindGameObject(sourceScene, SourceCharacterName);
            if (sourceCharacter == null)
            {
                Debug.LogError($"TownCharacterPrefabBuilder could not find '{SourceCharacterName}' in {SourceScenePath}.");
                return null;
            }

            GameObject clone = Object.Instantiate(sourceCharacter);
            clone.name = "TownCharacter";
            StripGameplayScripts(clone);
            RemoveFishingOnlyObjects(clone.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, PrefabPath);
            Object.DestroyImmediate(clone);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }
        finally
        {
            CloseSceneIfNeeded(sourceScene, sourceSceneAlreadyLoaded);
        }
    }

    private static void AssignPrefabToTownBootstrap(GameObject prefab)
    {
        Scene townScene = OpenSceneForEditing(TownScenePath, out bool townSceneAlreadyLoaded);
        try
        {
            bool sceneChanged = false;
            TownSceneBootstrap bootstrap = GetOrCreateTownBootstrap(townScene, ref sceneChanged);

            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            SerializedProperty characterPrefabProperty = serializedBootstrap.FindProperty("characterPrefab");
            if (characterPrefabProperty == null)
            {
                Debug.LogError("TownCharacterPrefabBuilder could not find the serialized 'characterPrefab' field.");
                return;
            }

            if (characterPrefabProperty.objectReferenceValue != prefab)
            {
                characterPrefabProperty.objectReferenceValue = prefab;
                serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
                sceneChanged = true;
            }

            if (sceneChanged)
            {
                EditorSceneManager.MarkSceneDirty(townScene);
                EditorSceneManager.SaveScene(townScene);
            }
        }
        finally
        {
            CloseSceneIfNeeded(townScene, townSceneAlreadyLoaded);
        }
    }

    private static TownSceneBootstrap GetOrCreateTownBootstrap(Scene townScene, ref bool sceneChanged)
    {
        GameObject bootstrapObject = FindBootstrapObject(townScene);
        if (bootstrapObject == null)
        {
            bootstrapObject = new GameObject(TownBootstrapName);
            SceneManager.MoveGameObjectToScene(bootstrapObject, townScene);
            sceneChanged = true;
        }

        if (bootstrapObject.name != TownBootstrapName)
        {
            bootstrapObject.name = TownBootstrapName;
            sceneChanged = true;
        }

        TownSceneBootstrap bootstrap = bootstrapObject.GetComponent<TownSceneBootstrap>();
        if (bootstrap == null)
        {
            bootstrap = bootstrapObject.AddComponent<TownSceneBootstrap>();
            sceneChanged = true;
        }

        return bootstrap;
    }

    private static GameObject FindBootstrapObject(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            TownSceneBootstrap bootstrap = roots[i].GetComponentInChildren<TownSceneBootstrap>(true);
            if (bootstrap != null)
            {
                return bootstrap.gameObject;
            }
        }

        return FindGameObject(scene, TownBootstrapName);
    }

    private static Scene OpenSceneForEditing(string scenePath, out bool alreadyLoaded)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        if (scene.IsValid() && scene.isLoaded)
        {
            alreadyLoaded = true;
            return scene;
        }

        alreadyLoaded = false;
        return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
    }

    private static void CloseSceneIfNeeded(Scene scene, bool alreadyLoaded)
    {
        if (alreadyLoaded || !scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (SceneManager.sceneCount <= 1)
        {
            return;
        }

        EditorSceneManager.CloseScene(scene, true);
    }

    private static void StripGameplayScripts(GameObject root)
    {
        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour is CharacterAppearanceController)
            {
                continue;
            }

            if (behaviour.GetType().Assembly == typeof(CharacterAppearanceController).Assembly)
            {
                Object.DestroyImmediate(behaviour, true);
            }
        }
    }

    private static void RemoveFishingOnlyObjects(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            RemoveFishingOnlyObjects(child);

            if (ShouldRemoveObject(child.name))
            {
                Object.DestroyImmediate(child.gameObject, true);
            }
        }
    }

    private static bool ShouldRemoveObject(string objectName)
    {
        for (int i = 0; i < FishingOnlyObjectNames.Length; i++)
        {
            if (FishingOnlyObjectNames[i] == objectName)
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject FindGameObject(Scene scene, string targetName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject found = FindGameObjectRecursive(roots[i].transform, targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject FindGameObjectRecursive(Transform current, string targetName)
    {
        if (current.name == targetName)
        {
            return current.gameObject;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject found = FindGameObjectRecursive(current.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string combinedPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(combinedPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
#endif
