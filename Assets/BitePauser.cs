using UnityEngine;
using System.Collections;

public class BitePauser : MonoBehaviour
{
    // 引用FishBiteHook脚本，请在Inspector中进行赋值
    public FishBiteHook fishBiteHook;

    // 延迟触发冻结的总时间（以秒为单位）
    public float delayDuration = 1f;

    // 三个需要激活/失活的UI组件，请在Inspector中进行赋值
    public GameObject uiComponent1;
    public GameObject uiComponent2;
    public GameObject FishOn;
    

    // 新增的UI组件

    // 防止多次触发冻结
    private bool isFrozen = false;
    private bool setHookRequested = false;
    private NewGameInputManager inputManager;

    void Start()
    {
        inputManager = NewGameInputManager.EnsureInstance();
        if (inputManager != null)
        {
            inputManager.ActionPerformed += OnActionPerformed;
        }

        // 在游戏开始时确保所有UI组件处于未激活状态
        if (uiComponent1 != null)
            uiComponent1.SetActive(false);
// 确保第三个UI组件初始为未激活
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
        // 检查FishBiteHook脚本是否存在，并且isFishBite为true且当前未被冻结
        if (fishBiteHook != null && fishBiteHook.isFishBite && !isFrozen)
        {
            StartCoroutine(HandleFreeze());
        }
    }

    IEnumerator HandleFreeze()
    {
        isFrozen = true;

        // 计算在冻结前0.1秒激活UI的等待时间
        float uiActivationDelay = delayDuration - 0.3f;

        // 确保uiActivationDelay不为负数
        if (uiActivationDelay > 0)
        {
            yield return new WaitForSecondsRealtime(uiActivationDelay);
        }
        else
        {
            // 如果delayDuration小于0.1秒，立即激活UI
            uiActivationDelay = 0f;
        }

        // 激活两个UI组件，立即显示
        if (uiComponent1 != null)
            uiComponent1.SetActive(true);
             FishOn.SetActive(true);
             uiComponent2.SetActive(false);

        // 等待剩余的0.1秒再冻结游戏
        float remainingDelay = delayDuration - uiActivationDelay;
        if (remainingDelay > 0)
        {
            yield return new WaitForSecondsRealtime(remainingDelay);
        }

        // 冻结游戏
        Time.timeScale = 0f;

        // 等待玩家按下W键以恢复游戏
        while (true)
        {
            // 检测玩家是否按下W键
            if (ConsumeSetHookInput())
            {
                // 恢复游戏时间
                Time.timeScale = 1f;

                // 失活两个UI组件

                WhenBtnPressed2.isFishTired = true;

                // 激活第三个UI组件


                    // 禁用该组件，防止再次冻结
                this.enabled = false;

                // 结束协程
                yield break;
            }

            // 等待下一帧
            yield return null;
        }
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (action == GameInputAction.SetHook)
        {
            setHookRequested = true;
        }
    }

    private bool ConsumeSetHookInput()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            setHookRequested = false;
            return true;
        }

        if (setHookRequested)
        {
            setHookRequested = false;
            return true;
        }

        return false;
    }
}
