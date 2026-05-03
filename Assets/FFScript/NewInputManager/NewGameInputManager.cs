using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine;

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

    private readonly Dictionary<GameInputAction, Coroutine> wearablePulseCoroutines = new Dictionary<GameInputAction, Coroutine>();

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
        if (tcpManager == null)
        {
            return;
        }

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

            case "press_space_down":
            case "space_down":
                SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, true);
                break;

            case "press_space_up":
            case "space_up":
                SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, false);
                break;

            case "lift_rod_down":
            case "liftrod_down":
                SetHeld(InputDeviceType.Wearable, GameInputAction.LiftRod, true);
                break;

            case "lift_rod_up":
            case "liftrod_up":
                SetHeld(InputDeviceType.Wearable, GameInputAction.LiftRod, false);
                break;

            case "press_space":
            case "space":
                PulseWearableHeldAction(GameInputAction.PressSpace);
                break;

            case "lift_rod":
            case "liftrod":
                PulseWearableHeldAction(GameInputAction.LiftRod);
                break;

            case "swing_left":
            case "swingleft":
                Perform(GameInputAction.SwingLeft);
                break;

            case "swing_right":
            case "swingright":
            case "grow_line":
            case "growline":
                Perform(GameInputAction.SwingRight);
                break;

            case "retrieve":
                Perform(GameInputAction.Retrieve);
                break;

            case "set_hook":
            case "sethook":
                Perform(GameInputAction.SetHook);
                break;

            default:
                GameInputAction action;
                if (TryParseActionName(message, out action))
                {
                    HandleWearableAction(action);
                }
                else
                {
                    Debug.LogWarning($"Unknown wearable input message: {message}");
                }
                break;
        }
    }

    private void HandleWearableAction(GameInputAction action)
    {
        if (action == GameInputAction.PressSpace || action == GameInputAction.LiftRod)
        {
            PulseWearableHeldAction(action);
            return;
        }

        Perform(action);
    }

    private void PerformTcpSpaceCombo(GameInputAction action)
    {
        PulseWearableHeldAction(GameInputAction.PressSpace);
        Perform(action);
    }

    private void PulseWearableHeldAction(GameInputAction action)
    {
        Coroutine runningPulse;
        if (wearablePulseCoroutines.TryGetValue(action, out runningPulse) && runningPulse != null)
        {
            StopCoroutine(runningPulse);
        }

        SetHeld(InputDeviceType.Wearable, action, true);
        wearablePulseCoroutines[action] = StartCoroutine(ReleaseWearableHeldActionAfterDelay(action));
    }

    private IEnumerator ReleaseWearableHeldActionAfterDelay(GameInputAction action)
    {
        yield return new WaitForSeconds(tcpComboHoldSeconds);

        SetHeld(InputDeviceType.Wearable, action, false);
        wearablePulseCoroutines.Remove(action);
    }

    private bool TryParseActionName(string message, out GameInputAction action)
    {
        string normalizedMessage = NormalizeActionName(message);
        foreach (GameInputAction candidate in Enum.GetValues(typeof(GameInputAction)))
        {
            if (NormalizeActionName(candidate.ToString()) == normalizedMessage)
            {
                action = candidate;
                return true;
            }
        }

        action = GameInputAction.PressSpace;
        return false;
    }

    private string NormalizeActionName(string value)
    {
        return value.Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
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
