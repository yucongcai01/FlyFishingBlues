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
    [Serializable]
    private class WearableInputFrame
    {
        public string gesture;
        public int gesture_id = -1;
        public string gesture_name;
        public string action;
        public float current_force_smooth;
    }

    public static NewGameInputManager Instance { get; private set; }

    public static NewGameInputManager EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureTcpManager();
            return Instance;
        }

        NewGameInputManager existingManager = FindObjectOfType<NewGameInputManager>();
        if (existingManager != null)
        {
            Instance = existingManager;
            existingManager.EnsureTcpManager();
            return existingManager;
        }

        GameObject inputManagerObject = new GameObject("NewGameInputManager");
        inputManagerObject.AddComponent<TCP_Manager>();
        return inputManagerObject.AddComponent<NewGameInputManager>();
    }

    [Header("Input Settings")]
    [SerializeField] private bool keyboardEnabled = true;
    [SerializeField] private bool wearableEnabled = true;
    [SerializeField] private TCP_Manager tcpManager;

    [Header("TCP Settings")]
    [SerializeField] private float tcpComboHoldSeconds = 0.5f;

    [Header("Fish Fight Settings")]
    [SerializeField] private bool wearableForceControlsFishStamina = true;
    [SerializeField] private float fishPullPulseSeconds = 0.2f;

    public event Action<GameInputAction> ActionPerformed;
    public event Action<GameInputAction> ActionStarted;
    public event Action<GameInputAction> ActionEnded;

    private readonly Dictionary<GameInputAction, bool> keyboardHeld = new Dictionary<GameInputAction, bool>();
    private readonly Dictionary<GameInputAction, bool> wearableHeld = new Dictionary<GameInputAction, bool>();
    private readonly Dictionary<KeyCode, GameInputAction> keyDownActionMap = new Dictionary<KeyCode, GameInputAction>
    {
        { KeyCode.A, GameInputAction.SwingLeft },
        { KeyCode.D, GameInputAction.SwingRight },
        { KeyCode.S, GameInputAction.Retrieve },
        { KeyCode.W, GameInputAction.SetHook },
    };

    private readonly Dictionary<GameInputAction, Coroutine> wearablePulseCoroutines = new Dictionary<GameInputAction, Coroutine>();
    private Coroutine fishPullPulseCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.EnsureTcpManager();
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureTcpManager();
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

        foreach (KeyValuePair<KeyCode, GameInputAction> mapping in keyDownActionMap)
        {
            if (Input.GetKeyDown(mapping.Key))
            {
                PerformKeyboardKeyDown(mapping.Key);
            }
        }
    }

    private void ReadWearableInput()
    {
        if (tcpManager == null)
        {
            EnsureTcpManager();
            if (tcpManager == null)
            {
                return;
            }
        }

        string message;
        while (tcpManager.TryGetMessage(out message))
        {
            HandleWearableMessage(message.Trim());
            Debug.Log($"Received message from wearable: {message}");
        }
    }

    private void EnsureTcpManager()
    {
        if (tcpManager == null)
        {
            tcpManager = GetComponent<TCP_Manager>();
        }

        if (tcpManager == null)
        {
            tcpManager = FindObjectOfType<TCP_Manager>();
        }

        if (tcpManager == null)
        {
            tcpManager = gameObject.AddComponent<TCP_Manager>();
        }

        DontDestroyOnLoad(tcpManager.gameObject);
    }

    private void HandleWearableMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        string wearableSignal;
        if (TryExtractWearableSignal(message, out wearableSignal))
        {
            message = wearableSignal;
        }

        switch (NormalizeActionName(message))
        {
            case "gesture1":
            case "fist":
            case "gesture1fist":
                Debug.Log("Received gesture_1/fist from wearable: simulating keyboard A");
                PerformKeyboardKeyDown(KeyCode.A, true);
                break;

            case "gesture3":
            case "open":
            case "gesture3open":
                Debug.Log("Received gesture_3/open from wearable: simulating keyboard D");
                PerformKeyboardKeyDown(KeyCode.D, true);
                break;

            case "gesture0":
            case "rest":
            case "gesture0rest":
                Debug.Log("Received gesture_0/rest from wearable: no mapped action");
                break;

            case "gesture2":
            case "pinch":
            case "gesture2pinch":
                Debug.Log("Received gesture_2/pinch from wearable: simulating keyboard S and pulsing pull");
                PerformRetrieveAndPulsePull();
                break;

            case "pressspacedown":
            case "spacedown":
                SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, true);
                break;

            case "pressspaceup":
            case "spaceup":
                SetHeld(InputDeviceType.Wearable, GameInputAction.PressSpace, false);
                break;

            case "liftroddown":
                SetHeld(InputDeviceType.Wearable, GameInputAction.LiftRod, true);
                break;

            case "liftrodup":
                SetHeld(InputDeviceType.Wearable, GameInputAction.LiftRod, false);
                break;

            case "pressspace":
            case "space":
                PulseWearableHeldAction(GameInputAction.PressSpace);
                break;

            case "liftrod":
                PulseWearableHeldAction(GameInputAction.LiftRod);
                break;

            case "swingleft":
                PerformKeyboardKeyDown(KeyCode.A, true);
                break;

            case "swingright":
            case "growline":
                PerformKeyboardKeyDown(KeyCode.D, true);
                break;

            case "retrieve":
                PerformRetrieveAndPulsePull();
                break;

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

    private bool TryExtractWearableSignal(string message, out string signal)
    {
        signal = null;

        if (!message.StartsWith("{", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            WearableInputFrame frame = JsonUtility.FromJson<WearableInputFrame>(message);
            if (frame == null)
            {
                return false;
            }

            HandleWearableFrameTelemetry(frame, message.Contains("\"current_force_smooth\""));

            if (!string.IsNullOrEmpty(frame.action))
            {
                signal = frame.action;
                return true;
            }

            if (!string.IsNullOrEmpty(frame.gesture))
            {
                signal = frame.gesture;
                return true;
            }

            if (!string.IsNullOrEmpty(frame.gesture_name))
            {
                signal = frame.gesture_name;
                return true;
            }

            if (frame.gesture_id >= 0)
            {
                signal = $"gesture_{frame.gesture_id}";
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse wearable JSON message: {e.Message}. Raw message: {message}");
        }

        return false;
    }

    private void HandleWearableFrameTelemetry(WearableInputFrame frame, bool hasCurrentForceSmooth)
    {
        if (wearableForceControlsFishStamina && hasCurrentForceSmooth && IsGestureOneFistFrame(frame))
        {
            ApplyFishStaminaFromWearableForce(frame.current_force_smooth);
        }
    }

    private bool IsGestureOneFistFrame(WearableInputFrame frame)
    {
        return frame != null
            && (frame.gesture_id == 1
                || NormalizeActionName(frame.gesture) == "gesture1"
                || NormalizeActionName(frame.gesture_name) == "fist"
                || NormalizeActionName(frame.action) == "swingleft");
    }

    private void ApplyFishStaminaFromWearableForce(float currentForceSmooth)
    {
        if (!IsFishFightActive())
        {
            return;
        }

        FishStaminaBar staminaBar = FishStaminaBar.instance;
        if (staminaBar == null)
        {
            staminaBar = FindObjectOfType<FishStaminaBar>();
        }

        if (staminaBar == null)
        {
            return;
        }

        staminaBar.ApplyWearableForceSmooth(currentForceSmooth);
        Debug.Log($"Applied wearable force to fish stamina: current_force_smooth={currentForceSmooth}");
    }

    private bool IsFishFightActive()
    {
        FishBiteHook biteHook = FindObjectOfType<FishBiteHook>();
        if (biteHook != null && biteHook.isFishBite)
        {
            return true;
        }

        FishLanding fishLanding = FindObjectOfType<FishLanding>();
        return fishLanding != null && fishLanding.enabled;
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

    private void PerformRetrieveAndPulsePull()
    {
        PerformKeyboardKeyDown(KeyCode.S, true);
        PulseFishPull();
    }

    private void PulseFishPull()
    {
        if (!IsFishFightActive())
        {
            return;
        }

        FishDragLine fishDragLine = FindObjectOfType<FishDragLine>();
        if (fishDragLine == null)
        {
            return;
        }

        if (fishPullPulseCoroutine != null)
        {
            StopCoroutine(fishPullPulseCoroutine);
        }

        fishDragLine.StartPulling();
        fishPullPulseCoroutine = StartCoroutine(StopFishPullAfterDelay(fishDragLine));
    }

    private IEnumerator StopFishPullAfterDelay(FishDragLine fishDragLine)
    {
        yield return new WaitForSecondsRealtime(fishPullPulseSeconds);

        if (fishDragLine != null)
        {
            fishDragLine.StopPulling();
        }

        fishPullPulseCoroutine = null;
    }

    private void PerformKeyboardKeyDown(KeyCode keyCode, bool simulateHeldState = false)
    {
        GameInputAction action;
        if (keyDownActionMap.TryGetValue(keyCode, out action))
        {
            if (simulateHeldState && keyCode == KeyCode.A)
            {
                PulseWearableHeldAction(GameInputAction.LiftRod);
            }

            Debug.Log($"Simulated keyboard key down: {keyCode} -> {action}");
            Perform(action);
        }
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
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

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
