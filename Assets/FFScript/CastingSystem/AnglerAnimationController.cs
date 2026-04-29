using UnityEngine;
using System.Collections;

public class AnglerAnimationController : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private NewGameInputManager inputManager;

    private readonly string PRESSING_SPACE = "PressingSpace";
    private readonly string SWING_LEFT = "SwingLeft";
    private readonly string SWING_RIGHT = "SwingRight";
    private readonly string RETRIEVE = "Retrieve";
    private readonly string LIFT_ROD = "LiftRod";
    private readonly string SETTHEHOOK = "SetTheHook";

    void Start()
    {
        animator = GetComponent<Animator>();
        if (inputManager == null)
            inputManager = FindObjectOfType<NewGameInputManager>();

        if (inputManager != null)
        {
            inputManager.ActionPerformed += OnActionPerformed;
            Debug.Log("AnglerAnimationController: Subscribed in Start.");
        }
        else
        {
            Debug.LogError("AnglerAnimationController: No NewGameInputManager found.");
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.ActionPerformed -= OnActionPerformed;
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (animator == null || inputManager == null)
            return;

        bool isPressingSpace = inputManager.IsHeld(GameInputAction.PressSpace);
        animator.SetBool(PRESSING_SPACE, isPressingSpace);

        bool isLiftingRod = inputManager.IsHeld(GameInputAction.LiftRod);
        animator.SetBool(LIFT_ROD, isLiftingRod);
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (animator == null || inputManager == null)
            return;

        switch (action)
        {
            case GameInputAction.SwingLeft:
                Debug.Log("Swinging left");
                animator.SetBool(SWING_LEFT, true);
                StartCoroutine(ResetSwingParameter(SWING_LEFT));
                break;

            case GameInputAction.SwingRight:
                Debug.Log("Swinging right");
                animator.SetBool(SWING_RIGHT, true);
                StartCoroutine(ResetSwingParameter(SWING_RIGHT));
                break;

            case GameInputAction.Retrieve:
                if (!inputManager.IsHeld(GameInputAction.PressSpace))
                {
                    Debug.Log("Retrieving");
                    animator.SetTrigger(RETRIEVE);
                }
                else
                {
                    Debug.Log("Retrieve input while pressing space: not retrieving");
                }
                break;

            case GameInputAction.SetHook:
                if (animator.GetBool("FishOn"))
                {
                    Debug.Log("Set the hook");
                    animator.SetTrigger(SETTHEHOOK);
                }
                break;
        }
    }

    private System.Collections.IEnumerator ResetSwingParameter(string parameter)
    {
        yield return new WaitForSeconds(0.5f);

        if (animator != null)
        {
            animator.SetBool(parameter, false);
        }
    }

    public void OnAnimationEnd(string parameter)
    {
        if (animator != null)
        {
            animator.SetBool(parameter, false);
            Debug.Log($"Animation ended: {parameter} reset to false");
        }
    }
}