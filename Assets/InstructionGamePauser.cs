using UnityEngine;

public class InstructionGamePauser : MonoBehaviour
{
    private bool isPaused = false;
    private bool hasLoggedInvalidSetup = false;

    public KeyCode resumeKey;
    public GameObject pauseUI;

    private void OnTriggerEnter(Collider other)
    {
        // Unity can still dispatch trigger callbacks to disabled behaviours.
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (!HasValidSetup() || isPaused)
        {
            return;
        }

        pauseUI.SetActive(true);
        PauseGame();
    }

    private void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
        Debug.Log("Game Paused");
    }

    private void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Debug.Log("Game Resumed");

        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPaused && Input.GetKey(KeyCode.Space) && Input.GetKeyDown(resumeKey))
        {
            ResumeGame();
        }
    }

    private bool HasValidSetup()
    {
        if (pauseUI != null && resumeKey != KeyCode.None)
        {
            return true;
        }

        if (!hasLoggedInvalidSetup)
        {
            string missingConfig = pauseUI == null ? "pauseUI" : "resumeKey";
            Debug.LogWarning(
                $"InstructionGamePauser on '{gameObject.name}' is missing {missingConfig}, so this trigger will be ignored.",
                this);
            hasLoggedInvalidSetup = true;
        }

        return false;
    }
}
