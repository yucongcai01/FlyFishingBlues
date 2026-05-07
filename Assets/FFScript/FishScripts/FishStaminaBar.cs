using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishStaminaBar : MonoBehaviour
{
    public Slider fishStaminaBar;

    private int maxStamina = 100;
    public float currentStamina;

    // 正常耐力回复速度
    public float normalRegenSpeed = 2f; // 每秒回复的耐力值

    // 耐力为0时的延迟和特定回复速度
    public float zeroStaminaDelay = 2f; // 延迟时间
    public float staminaChargingSpeed = 5f; // 特定的每秒回复速度

    private bool isRegeneratingAfterZero = false; // 标记是否在特定回复状态
    private float zeroStaminaTimer = 0f; // 耐力耗尽后的计时器

    public static FishStaminaBar instance;

    // 新增的变量
    public int rechargeTimes = 2; // 耐力条最多可以被重新恢复的次数
    private int currentRechargeTimes = 0; // 当前已经恢复的次数

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;
        fishStaminaBar.maxValue = maxStamina;
        fishStaminaBar.value = currentStamina;
    }

    void Update()
    {
        // 当恢复次数未达到上限时，允许耐力回复
        if (currentRechargeTimes < rechargeTimes)
        {
            if (currentStamina < maxStamina)
            {
                if (currentStamina > 0 && !isRegeneratingAfterZero)
                {
                    // 正常回复
                    RegenerateStamina(normalRegenSpeed);
                }
                else if (currentStamina <= 0)
                {
                    // 耐力耗尽后的延迟计时
                    zeroStaminaTimer += Time.deltaTime;
                    if (zeroStaminaTimer >= zeroStaminaDelay)
                    {
                        isRegeneratingAfterZero = true;
                        RegenerateStamina(staminaChargingSpeed);

                        if (currentStamina >= maxStamina)
                        {
                            isRegeneratingAfterZero = false;
                            zeroStaminaTimer = 0f;
                            currentRechargeTimes++;
                        }
                    }
                }
                else if (currentStamina > 0 && isRegeneratingAfterZero)
                {
                    // 在特定回复状态下继续回复
                    RegenerateStamina(staminaChargingSpeed);

                    if (currentStamina >= maxStamina)
                    {
                        isRegeneratingAfterZero = false;
                        zeroStaminaTimer = 0f;
                        currentRechargeTimes++;
                    }
                }
            }
        }
        else
        {
            // 恢复次数已达上限，停止一切耐力回复
            isRegeneratingAfterZero = false;
            zeroStaminaTimer = 0f;
        }
    }

    private void RegenerateStamina(float regenSpeed)
    {
        currentStamina += regenSpeed * Time.deltaTime;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        fishStaminaBar.value = currentStamina;
    }

    public void UseStamina(int amount)
    {
        if (isRegeneratingAfterZero)
        {
            // 在特定回复期间，耐力无法被减少
            return;
        }

        if (currentStamina - amount >= 0)
        {
            currentStamina -= amount;
            fishStaminaBar.value = currentStamina;
        }
        else
        {
            currentStamina = 0;
            fishStaminaBar.value = currentStamina;

            // 检查是否已达到最大充能次数
            if (currentRechargeTimes >= rechargeTimes)
            {
                // 禁用耐力条的 UI 组件
                DisableStaminaBarUI();
            }
        }
    }


    public void ApplyWearableForceSmooth(float currentForceSmooth)
    {
        float clampedForce = Mathf.Clamp(currentForceSmooth, 0f, maxStamina);
        currentStamina = Mathf.Clamp(maxStamina - clampedForce, 0f, maxStamina);

        if (fishStaminaBar != null)
        {
            fishStaminaBar.value = currentStamina;
        }

        if (currentStamina <= 0f && currentRechargeTimes >= rechargeTimes)
        {
            DisableStaminaBarUI();
        }
    }
    /// <summary>
    /// 禁用耐力条的 UI 组件，使其在游戏中不再显示。
    /// </summary>
    private void DisableStaminaBarUI()
    {
        if (fishStaminaBar != null)
        {
            // 禁用 Slider 组件的 GameObject
            fishStaminaBar.gameObject.SetActive(false);

            // 可选：添加日志以调试
            Debug.Log("耐力条 UI 已被禁用，因为达到最大充能次数并且耐力条被清零。");
        }
        else
        {
            Debug.LogWarning("fishStaminaBar 尚未被赋值。");
        }
    }
}
