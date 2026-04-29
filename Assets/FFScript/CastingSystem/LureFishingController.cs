using UnityEngine;

public class LureFishingController : MonoBehaviour
{
    private Animator animator;
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
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (animator == null)
            return;

        // 处理空格键按下的状态切换
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetBool(ISFISHING, true);
            StartCoroutine(ResetSwingParameter(ISFISHING));

        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            animator.SetBool(RETRIEVE, true);
            StartCoroutine(ResetRetrieveParameter(RETRIEVE));
            
        }


        if (Input.GetKeyDown(KeyCode.W) && animator.GetBool("FishOn"))
        {
            
            animator.SetTrigger(SETTHEHOOK);


        }
        if (Input.GetKey(KeyCode.A))
        {

            animator.SetBool(LIFT_ROD, true);
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