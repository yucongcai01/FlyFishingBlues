using UnityEngine;

public class LureFishingController : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private NewGameInputManager inputManager;

    private readonly string RETRIEVE = "Retrieve";
    private readonly string SETTHEHOOK = "SetTheHook";
    private readonly string ISFISHING = "isFishing";
    private readonly string LIFT_ROD = "LiftRod";

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("找不到Animator组件");
        }
        else
        {
            Debug.Log("Animator组件已找到");
        }

        if (inputManager == null)
        {
            inputManager = FindObjectOfType<NewGameInputManager>();
        }

        if (inputManager != null)
        {
            inputManager.ActionStarted += OnActionStarted;
            inputManager.ActionPerformed += OnActionPerformed;
        }
        else
        {
            Debug.LogError("LureFishingController: No NewGameInputManager found.");
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.ActionStarted -= OnActionStarted;
            inputManager.ActionPerformed -= OnActionPerformed;
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (animator == null)
            return;

        if (inputManager != null)
        {
            animator.SetBool(LIFT_ROD, inputManager.IsHeld(GameInputAction.LiftRod));
        }
    }

    private void OnActionStarted(GameInputAction action)
    {
        if (animator == null || action != GameInputAction.PressSpace)
        {
            return;
        }

        animator.SetBool(ISFISHING, true);
        StartCoroutine(ResetSwingParameter(ISFISHING));
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (animator == null)
        {
            return;
        }

        switch (action)
        {
            case GameInputAction.Retrieve:
                animator.SetBool(RETRIEVE, true);
                StartCoroutine(ResetRetrieveParameter(RETRIEVE));
                break;

            case GameInputAction.SetHook:
                if (animator.GetBool("FishOn"))
                {
                    animator.SetTrigger(SETTHEHOOK);
                }
                break;
        }
    }

    private System.Collections.IEnumerator ResetSwingParameter(string parameter)
    {
        yield return new WaitForSeconds(3.24f);
        animator.SetBool(parameter, false);
    }

    private System.Collections.IEnumerator ResetRetrieveParameter(string parameter)
    {
        yield return new WaitForSeconds(0.1f);
        animator.SetBool(parameter, false);
    }

}
