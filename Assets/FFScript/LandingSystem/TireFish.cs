using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TireFish : MonoBehaviour
{
    private NewGameInputManager inputManager;

    void Start()
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

    // Update is called once per frame
    void Update()
    {
        if (inputManager == null && Input.GetKeyDown(KeyCode.W))
            FishStaminaBar.instance.UseStamina(15);
    }

    private void OnActionPerformed(GameInputAction action)
    {
        if (action == GameInputAction.SetHook)
        {
            FishStaminaBar.instance.UseStamina(15);
        }
    }
}
