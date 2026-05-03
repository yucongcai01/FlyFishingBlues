using UnityEngine;

public class FishingRodController : MonoBehaviour
{
    public Transform bone1;
    public Transform fishingRod;
    public float rotationSpeed = 10f;
    [SerializeField] private NewGameInputManager inputManager;
    
    private Animator animator;
    private bool pressingSpace = false;
    private bool isAnimationFinished = false;
    private readonly Vector3 targetRotation = new Vector3(45.059967f, 0.7008133f, 0.5643768f);

    void Start()
    {
        animator = GetComponent<Animator>();
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<NewGameInputManager>();
        }

        if (inputManager != null)
        {
            inputManager.ActionStarted += OnActionStarted;
        }
        else
        {
            Debug.LogError("FishingRodController: No NewGameInputManager found.");
        }

        // 如果没有手动设置bone1，尝试自动查找
        if (bone1 == null)
        {
            bone1 = transform.Find("bone_1");
        }
        
        // 如果没有手动设置fishingRod，尝试在场景中查找
        if (fishingRod == null)
        {
            fishingRod = transform;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.ActionStarted -= OnActionStarted;
        }
    }

    void Update()
    {
        // 检查动画是否结束
        if (animator != null && pressingSpace && !isAnimationFinished)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 1.0f)
            {
                OnFishingAnimationComplete();
            }
        }
        
    }

    private void OnActionStarted(GameInputAction action)
    {
        if (action != GameInputAction.PressSpace)
        {
            return;
        }

        pressingSpace = !pressingSpace;
        if (animator != null)
        {
            animator.SetBool("PressingSpace", pressingSpace);
            if (pressingSpace)
            {
                isAnimationFinished = false;
                animator.Play("Fishing");
            }
        }
    }

    // 在动画事件中调用此方法
    public void OnFishingAnimationComplete()
    {
        isAnimationFinished = true;
        // 动画结束后，平滑转向目标角度
        if (isAnimationFinished && fishingRod != null)
        {
            fishingRod.rotation = Quaternion.Lerp(fishingRod.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * rotationSpeed);
        }
    }

    // 重置状态的方法（如果需要重新开始）
    public void ResetState()
    {
        isAnimationFinished = false;
    }
}
