using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhenBtnPressed2 : MonoBehaviour
{
    [Header("A key states")]
    public GameObject A1;
    public GameObject A2;

    [Header("W key states")]
    public GameObject W1;
    public GameObject W2;

    [Header("Fish state")]
    [Tooltip("True when the fish is tired.")]
    public static bool isFishTired = false;

    [SerializeField] private float virtualPressSeconds = 0.2f;

    private BlinkingImageButton blinkingA;
    private BlinkingImageButton blinkingW;
    private NewGameInputManager inputManager;
    private bool virtualAActive;
    private bool virtualWActive;
    private Coroutine virtualACoroutine;
    private Coroutine virtualWCoroutine;

    void Start()
    {
        if (A1 != null) A1.SetActive(true);
        if (A2 != null) A2.SetActive(false);
        if (W1 != null) W1.SetActive(true);
        if (W2 != null) W2.SetActive(false);

        if (A1 != null)
            blinkingA = A1.GetComponent<BlinkingImageButton>();
        if (W1 != null)
            blinkingW = W1.GetComponent<BlinkingImageButton>();

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
        bool isAPressed = Input.GetKey(KeyCode.A) || virtualAActive || IsHeld(GameInputAction.LiftRod);
        bool isWPressed = Input.GetKey(KeyCode.W) || virtualWActive;

        if (isAPressed)
        {
            if (A1 != null) A1.SetActive(false);
            if (A2 != null) A2.SetActive(true);
        }
        else
        {
            if (A1 != null) A1.SetActive(true);
            if (A2 != null) A2.SetActive(false);

            if (blinkingA != null)
            {
                blinkingA.enabled = isFishTired;
            }
        }

        if (isWPressed)
        {
            if (W1 != null) W1.SetActive(false);
            if (W2 != null) W2.SetActive(true);
        }
        else
        {
            if (W1 != null) W1.SetActive(true);
            if (W2 != null) W2.SetActive(false);

            if (blinkingW != null)
            {
                blinkingW.enabled = !isFishTired;
            }
        }
    }

    private void OnActionPerformed(GameInputAction action)
    {
        switch (action)
        {
            case GameInputAction.SwingLeft:
                PulseVirtualA();
                break;
            case GameInputAction.SetHook:
                PulseVirtualW();
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

    private void PulseVirtualW()
    {
        if (virtualWCoroutine != null)
        {
            StopCoroutine(virtualWCoroutine);
        }

        virtualWActive = true;
        virtualWCoroutine = StartCoroutine(ReleaseVirtualPress(() => virtualWActive = false));
    }

    private IEnumerator ReleaseVirtualPress(Action release)
    {
        yield return new WaitForSecondsRealtime(virtualPressSeconds);
        release();
    }
}
