using UnityEngine;
using UnityEngine.SceneManagement;

public static class LandSceneFishScaleRandomizer
{
    private const string TargetSceneName = "LandScene";
    private const string TargetObjectName = "TroutWithJawfbx";
    private const float MinScale = 0.11f;
    private const float MaxScale = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHandler()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name != TargetSceneName)
        {
            return;
        }

        ApplyRandomScale(scene);
    }

    private static void ApplyRandomScale(Scene scene)
    {
        GameObject[] rootObjects = scene.GetRootGameObjects();
        bool foundTarget = false;

        for (int rootIndex = 0; rootIndex < rootObjects.Length; rootIndex++)
        {
            Transform[] transforms = rootObjects[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform currentTransform = transforms[transformIndex];
                if (currentTransform.name != TargetObjectName)
                {
                    continue;
                }

                float randomScale = Random.Range(MinScale, MaxScale);
                currentTransform.localScale = Vector3.one * randomScale;
                foundTarget = true;
            }
        }

        if (!foundTarget)
        {
            Debug.LogWarning(
                $"LandSceneFishScaleRandomizer could not find '{TargetObjectName}' in scene '{TargetSceneName}'.");
        }
    }
}
