using UnityEngine;

public class InstructionGamePauser : MonoBehaviour
{
    private bool isPaused = false;
    private bool hasLoggedInvalidSetup = false;
    private NewGameInputManager inputManager;

    public KeyCode resumeKey;
    public GameObject pauseUI;

    private void Start()
    {
        inputManager = NewGameInputManager.EnsureInstance();
        if (inputManager != null)
        {
            inputManager.ActionPerformed += OnActionPerformed;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.ActionPerformed -= OnActionPerformed;
        }
    }

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
        if (isPaused && IsPressingSpace() && Input.GetKeyDown(resumeKey))
        {
            ResumeGame();
        }
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (isPaused && IsPressingSpace() && DoesActionMatchResumeKey(action))
        {
            ResumeGame();
        }
    }

    private bool IsPressingSpace()
    {
        return Input.GetKey(KeyCode.Space)
            || (inputManager != null && inputManager.IsHeld(GameInputAction.PressSpace));
    }

    private bool DoesActionMatchResumeKey(GameInputAction action)
    {
        switch (resumeKey)
        {
            case KeyCode.A:
                return action == GameInputAction.SwingLeft;
            case KeyCode.D:
                return action == GameInputAction.SwingRight;
            case KeyCode.S:
                return action == GameInputAction.Retrieve;
            case KeyCode.W:
                return action == GameInputAction.SetHook;
            case KeyCode.Space:
                return action == GameInputAction.PressSpace;
            default:
                return false;
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
