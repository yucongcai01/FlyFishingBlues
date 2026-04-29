using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using System;
using UnityEngine.Analytics;

public enum GameInputAction
{
    PressSpace,
    SwingLeft,
    SwingRight,
    Retrieve,
    LiftRod,
    SetHook
}

public enum InputDeviceType
{
    Keyboard,
    Wearable
}

public class NewGameInputManager : MonoBehaviour
{
    public static NewGameInputManager Instance { get; private set; }

    [Header("Input Settings")]
    [SerializeField] private bool keyboardEnabled = true;
    [SerializeField] private bool wearableEnabled = true;
    [SerializeField] private TCP_Manager tcpManager;

    [Header("TCP Settings")]
    [SerializeField] private float tcpComboHoldSeconds = 0.5f;

    public event Action<GameInputAction> ActionPerformed;
    public event Action<GameInputAction> ActionStarted;
    public event Action<GameInputAction> ActionEnded;

    private readonly Dictionary<GameInputAction, bool> keyboardHeld = new Dictionary<GameInputAction, bool>();
    private readonly Dictionary<GameInputAction, bool> wearableHeld = new Dictionary<GameInputAction, bool>();

    private Coroutine tcpSpacePulseCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (tcpManager == null)
        {
            Debug.LogError("TCP_Manager reference is not set in NewGameInputManager.");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (keyboardEnabled)
        {
            ReadKeyboardInput();
        }
        else
        {
            ClearSource(InputDeviceType.Keyboard);
        }

        if (wearableEnabled)
        {
            ReadWearableInput();
        }
        else
        {
            ClearSource(InputDeviceType.Wearable);
        }
    }

    public bool IsHeld(GameInputAction action)
    {
        return GetHeld(keyboardHeld, action) || GetHeld(wearableHeld, action);
    }

    private void ReadKeyboardInput()
    {
        SetHeld(InputDeviceType.Keyboard, GameInputAction.PressSpace, Input.GetKey(KeyCode.Space));

        SetHeld(InputDeviceType.Keyboard, GameInputAction.LiftRod, Input.GetKey(KeyCode.A));

        if (Input.GetKeyDown(KeyCode.A))
        {
            Perform(GameInputAction.SwingLeft);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Perform(GameInputAction.SwingRight);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Perform(GameInputAction.Retrieve);
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Perform(GameInputAction.SetHook);
        }
    }

    private void ReadWearableInput()
    {
        string message;
        while (tcpManager.TryGetMessage(out message))
        {
            HandleWearableMessage(message.Trim().ToLowerInvariant());
            Debug.Log($"Received message from wearable: {message}");
        }
    }

    private void HandleWearableMessage(string message)
    {
        switch (message)
        {
            case "gesture_1":
                Debug.Log("Received gesture_1 from wearable: performing PressSpace + SwingLeft combo");
                PerformTcpSpaceCombo(GameInputAction.SwingLeft); // gesture_1 = pressSpace + swingLeft
                break;

            case "gesture_2":
                Debug.Log("Received gesture_2 from wearable: performing PressSpace + SwingRight combo");
                PerformTcpSpaceCombo(GameInputAction.SwingRight); // gesture_2 = pressSpace + swingRight
                break;
        }
    }

    private void PerformTcpSpaceCombo(GameInputAction action)
    {
        if (tcpSpacePulseCoroutine != null)
        {
            StopCoroutine(tcpSpacePulseCoroutine);
        }

        SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, true);
        Perform(action);

        tcpSpacePulseCoroutine = StartCoroutine(ReleaseTcpSpaceAfterDelay());
    }

    private IEnumerator ReleaseTcpSpaceAfterDelay()
    {
        yield return new WaitForSeconds(tcpComboHoldSeconds);

        SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, false);
        tcpSpacePulseCoroutine = null;
    }

    private void Perform(GameInputAction action)
    {
        Debug.Log($"Perform: {action}");
        ActionPerformed?.Invoke(action);
    }

    private void SetHeld(InputDeviceType source, GameInputAction action, bool value)
    {
        bool wasHeld = IsHeld(action);

        Dictionary<GameInputAction, bool> map = GetMap(source);
        map[action] = value;

        bool isHeld = IsHeld(action);

        if (!wasHeld && isHeld)
        {
            ActionStarted?.Invoke(action);
        }
        else if (wasHeld && !isHeld)
        {
            ActionEnded?.Invoke(action);
        }
    }

    private Dictionary<GameInputAction, bool> GetMap(InputDeviceType source)
    {
        return source == InputDeviceType.Keyboard ? keyboardHeld : wearableHeld;
    }

    private bool GetHeld(Dictionary<GameInputAction, bool> map, GameInputAction action)
    {
        bool value;
        return map.TryGetValue(action, out value) && value;
    }

    private void ClearSource(InputDeviceType source)
    {
        Dictionary<GameInputAction, bool> map = GetMap(source);

        GameInputAction[] keys = new GameInputAction[map.Keys.Count];
        map.Keys.CopyTo(keys, 0);

        foreach (GameInputAction key in keys)
        {
            SetHeld(source, key, false);
        }
    }
}
