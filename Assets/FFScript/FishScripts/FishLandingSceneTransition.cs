using UnityEngine;
using UnityEngine.SceneManagement;

public class FishLandingSceneTransition : MonoBehaviour
{
    [SerializeField] private FishLanding fishLanding;
    [SerializeField] private string nextSceneName = "Unhook Man";

    private bool hasLoadedScene;

    private void OnEnable()
    {
        if (fishLanding == null)
        {
            fishLanding = FindObjectOfType<FishLanding>(true);
        }

        if (fishLanding == null)
        {
            Debug.LogError("FishLandingSceneTransition could not find a FishLanding component in the scene.");
            return;
        }

        fishLanding.FishLanded += LoadNextScene;
    }

    private void OnDisable()
    {
        if (fishLanding != null)
        {
            fishLanding.FishLanded -= LoadNextScene;
        }
    }

    private void LoadNextScene()
    {
        if (hasLoadedScene)
        {
            return;
        }

        hasLoadedScene = true;
        SceneManager.LoadScene(nextSceneName);
    }
}
