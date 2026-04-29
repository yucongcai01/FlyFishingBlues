using UnityEngine;

public class AnglerAnimationController : MonoBehaviour
{
    private Animator animator;

    private readonly string PRESSING_SPACE = "PressingSpace";
    private readonly string SWING_LEFT = "SwingLeft";
    private readonly string SWING_RIGHT = "SwingRight";
    private readonly string RETRIEVE = "Retrieve";
    private readonly string LIFT_ROD = "LiftRod";
    private readonly string SETTHEHOOK = "SetTheHook";
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("AnglerAnimationController: No Animator component found on the GameObject.");
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

        bool isPressingSpace = Input.GetKey(KeyCode.Space);
        animator.SetBool(PRESSING_SPACE, isPressingSpace);

        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A pressed: swinging left");
            animator.SetBool(SWING_LEFT, true);
            StartCoroutine(ResetSwingParameter(SWING_LEFT));
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("D pressed: swinging right");
            animator.SetBool(SWING_RIGHT, true);
            StartCoroutine(ResetSwingParameter(SWING_RIGHT));
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!isPressingSpace)
            {
                Debug.Log("S pressed: retrieving");
                animator.SetTrigger(RETRIEVE);
            }
            else
            {
                Debug.Log("S pressed while pressing space: not retrieving");
            }
        }


        if (Input.GetKeyDown(KeyCode.W) && animator.GetBool("FishOn"))
        {
            animator.SetTrigger(SETTHEHOOK);
        }

        if (Input.GetKey(KeyCode.A))
        {
            animator.SetBool(LIFT_ROD, true);
        }


        if (Input.GetKeyUp(KeyCode.A))
        {
            animator.SetBool(LIFT_ROD, false);
        }


    }

    private System.Collections.IEnumerator ResetSwingParameter(string parameter)
    {
        yield return new WaitForSeconds(0.5f);
        animator.SetBool(parameter, false);
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
