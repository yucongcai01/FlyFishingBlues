using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhenBtnPressed : MonoBehaviour
{
    public GameObject gameObjectA;
    public GameObject gameObjectD;
    public GameObject gameObjectS;
    public GameObject gameObjectSpace;

    [SerializeField] private float virtualPressSeconds = 0.2f;

    private NewGameInputManager inputManager;
    private bool virtualAActive;
    private bool virtualDActive;
    private bool virtualSActive;
    private Coroutine virtualACoroutine;
    private Coroutine virtualDCoroutine;
    private Coroutine virtualSCoroutine;

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

    void Update()
    {
        if (gameObjectA != null)
        {
            gameObjectA.SetActive(Input.GetKey(KeyCode.A) || virtualAActive || IsHeld(GameInputAction.LiftRod));
        }

        if (gameObjectD != null)
        {
            gameObjectD.SetActive(Input.GetKey(KeyCode.D) || virtualDActive);
        }

        if (gameObjectS != null)
        {
            gameObjectS.SetActive(Input.GetKey(KeyCode.S) || virtualSActive);
        }

        if (gameObjectSpace != null)
        {
            gameObjectSpace.SetActive(Input.GetKey(KeyCode.Space) || IsHeld(GameInputAction.PressSpace));
        }
    }

    private void OnActionPerformed(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.SwingLeft:
                PulseVirtualA();
                break;
            case GameInputAction.SwingRight:
                PulseVirtualD();
                break;
            case GameInputAction.Retrieve:
                PulseVirtualS();
                break;
        }
    }

    private bool IsHeld(GameInputAction action)
    {
        return inputManager != null && inputManager.IsHeld(action);
    }

    private void PulseVirtualA()
    {
        if (virtualACoroutine != null)
        {
            StopCoroutine(virtualACoroutine);
        }

        virtualAActive = true;
        virtualACoroutine = StartCoroutine(ReleaseVirtualPress(() => virtualAActive = false));
    }

    private void PulseVirtualD()
    {
        if (virtualDCoroutine != null)
        {
            StopCoroutine(virtualDCoroutine);
        }

        virtualDActive = true;
        virtualDCoroutine = StartCoroutine(ReleaseVirtualPress(() => virtualDActive = false));
    }

    private void PulseVirtualS()
    {
        if (virtualSCoroutine != null)
        {
            StopCoroutine(virtualSCoroutine);
        }

        virtualSActive = true;
        virtualSCoroutine = StartCoroutine(ReleaseVirtualPress(() => virtualSActive = false));
    }

    private IEnumerator ReleaseVirtualPress(Action release)
    {
        yield return new WaitForSecondsRealtime(virtualPressSeconds);
        release();
    }
}
