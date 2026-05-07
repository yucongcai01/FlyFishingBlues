using UnityEngine;
using System.Collections;

public class InsBeginningPauser : MonoBehaviour
{
    private bool isTimeFrozen = true;
    private NewGameInputManager inputManager;
    public float countdownDuration = 3f;

    // UI components
    public GameObject uiComponent1;
    public GameObject uiComponent2;

    void Start()
    {
        inputManager = NewGameInputManager.EnsureInstance();
        if (inputManager != null)
        {
            inputManager.ActionStarted += OnActionStarted;
            inputManager.ActionPerformed += OnActionPerformed;
        }

        // Start countdown before freezing time
        StartCoroutine(CountdownThenFreeze());
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.ActionStarted -= OnActionStarted;
            inputManager.ActionPerformed -= OnActionPerformed;
        }
    }

    IEnumerator CountdownThenFreeze()
    {
        yield return new WaitForSecondsRealtime(countdownDuration);

        // Freeze time after countdown
        Time.timeScale = 0f;
    }

    void Update()
    {
        // Check if the player is holding down the space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowSecondUiComponent();
        }

        // Check if the player is holding down the space key and presses the A key
        if (isTimeFrozen && IsPressingSpace() && Input.GetKeyDown(KeyCode.A))
        {
            ResumeFromBeginningPause();
        }
    }

    private void OnActionStarted(GameInputAction action)
    {
        if (action == GameInputAction.PressSpace)
        {
            ShowSecondUiComponent();
        }
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (isTimeFrozen && IsPressingSpace() && action == GameInputAction.SwingLeft)
        {
            ResumeFromBeginningPause();
        }
    }

    private void ShowSecondUiComponent()
    {
        // Deactivate the first UI component and activate the second UI component
        if (uiComponent1 != null && uiComponent2 != null)
        {
            uiComponent1.SetActive(false);
            uiComponent2.SetActive(true);
        }
    }

    private void ResumeFromBeginningPause()
    {
        // Unfreeze time
        Time.timeScale = 1f;
        isTimeFrozen = false;

        // Deactivate the second UI component
        if (uiComponent2 != null)
        {
            uiComponent2.SetActive(false);
        }
    }

    private bool IsPressingSpace()
    {
        return Input.GetKey(KeyCode.Space)
            || (inputManager != null && inputManager.IsHeld(GameInputAction.PressSpace));
    }
}
