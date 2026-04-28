using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAppearanceController : MonoBehaviour
{
    [Serializable]
    public class SpriteRendererOverride
    {
        public string rendererKey;
        public SpriteRenderer renderer;
        public Sprite sprite;
        public bool overrideColor;
        public Color color = Color.white;
        public bool overrideEnabledState;
        public bool rendererEnabled = true;
    }

    [Serializable]
    public class CharacterAppearanceVariant
    {
        public string id = "default";
        public string displayName = "Default";
        public List<SpriteRendererOverride> spriteOverrides = new List<SpriteRendererOverride>();
        public List<GameObject> objectsToEnable = new List<GameObject>();
        public List<GameObject> objectsToDisable = new List<GameObject>();
    }

    private struct SpriteRendererState
    {
        public readonly Sprite Sprite;
        public readonly Color Color;
        public readonly bool Enabled;

        public SpriteRendererState(Sprite sprite, Color color, bool enabled)
        {
            Sprite = sprite;
            Color = color;
            Enabled = enabled;
        }
    }

    [SerializeField] private List<CharacterAppearanceVariant> appearances = new List<CharacterAppearanceVariant>();
    [SerializeField] private List<SpriteRenderer> managedRenderers = new List<SpriteRenderer>();
    [SerializeField] private string defaultAppearanceId = string.Empty;
    [SerializeField] private bool includeInactiveManagedRenderers;
    [SerializeField] private bool loadSavedAppearanceOnAwake = true;
    [SerializeField] private bool saveAppearanceSelection = true;
    [SerializeField] private string playerPrefsKey = "MainCharacter.Appearance";
    [SerializeField] private KeyCode debugCycleKey = KeyCode.None;

    private readonly Dictionary<string, SpriteRenderer> rendererLookup =
        new Dictionary<string, SpriteRenderer>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SpriteRenderer, SpriteRendererState> defaultRendererStates =
        new Dictionary<SpriteRenderer, SpriteRendererState>();
    private readonly Dictionary<GameObject, bool> defaultObjectStates =
        new Dictionary<GameObject, bool>();

    private string currentAppearanceId = string.Empty;

    public string CurrentAppearanceId => currentAppearanceId;
    public int AppearanceCount => appearances.Count;

    private void Reset()
    {
        AutoPopulateManagedRenderers();
    }

    private void OnValidate()
    {
        CleanupManagedRenderers();
        if (managedRenderers.Count == 0)
        {
            AutoPopulateManagedRenderers();
        }
    }

    private void Awake()
    {
        CacheManagedRenderers();
        CacheDefaultStates();

        if (loadSavedAppearanceOnAwake &&
            !string.IsNullOrWhiteSpace(playerPrefsKey) &&
            PlayerPrefs.HasKey(playerPrefsKey))
        {
            string savedAppearanceId = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(savedAppearanceId) &&
                TryApplyAppearanceInternal(savedAppearanceId, false))
            {
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(defaultAppearanceId))
        {
            TryApplyAppearanceInternal(defaultAppearanceId, false);
        }
    }

    private void Update()
    {
        if (debugCycleKey != KeyCode.None && Input.GetKeyDown(debugCycleKey))
        {
            ApplyNextAppearance();
        }
    }

    [ContextMenu("Auto Populate Managed Renderers")]
    public void AutoPopulateManagedRenderers()
    {
        managedRenderers.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
            if (childRenderer != null)
            {
                managedRenderers.Add(childRenderer);
            }
        }

        if (managedRenderers.Count == 0)
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveManagedRenderers);
            for (int i = 0; i < childRenderers.Length; i++)
            {
                managedRenderers.Add(childRenderers[i]);
            }
        }
    }

    [ContextMenu("Reset Appearance To Defaults")]
    public void ResetAppearance()
    {
        CacheManagedRenderers();
        CacheDefaultStates();
        ApplyDefaultState();
        currentAppearanceId = string.Empty;

        if (!string.IsNullOrWhiteSpace(playerPrefsKey))
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
        }
    }

    public bool ApplyAppearance(string appearanceId)
    {
        return TryApplyAppearanceInternal(appearanceId, true);
    }

    public bool ApplyAppearanceByIndex(int index)
    {
        if (index < 0 || index >= appearances.Count)
        {
            return false;
        }

        return TryApplyAppearanceInternal(appearances[index].id, true);
    }

    public bool ApplyNextAppearance()
    {
        if (appearances.Count == 0)
        {
            return false;
        }

        int currentIndex = GetCurrentAppearanceIndex();
        int nextIndex = (currentIndex + 1 + appearances.Count) % appearances.Count;
        return ApplyAppearanceByIndex(nextIndex);
    }

    public bool HasAppearance(string appearanceId)
    {
        return FindAppearance(appearanceId) != null;
    }

    public string GetAppearanceIdAt(int index)
    {
        if (index < 0 || index >= appearances.Count)
        {
            return string.Empty;
        }

        return appearances[index].id;
    }

    private bool TryApplyAppearanceInternal(string appearanceId, bool persistSelection)
    {
        if (string.IsNullOrWhiteSpace(appearanceId))
        {
            return false;
        }

        CharacterAppearanceVariant variant = FindAppearance(appearanceId);
        if (variant == null)
        {
            Debug.LogWarning($"Character appearance '{appearanceId}' was not found on {name}.", this);
            return false;
        }

        CacheManagedRenderers();
        CacheDefaultStates();
        ApplyDefaultState();

        for (int i = 0; i < variant.spriteOverrides.Count; i++)
        {
            ApplyRendererOverride(variant.spriteOverrides[i]);
        }

        SetObjectStates(variant.objectsToEnable, true);
        SetObjectStates(variant.objectsToDisable, false);

        currentAppearanceId = variant.id;

        if (persistSelection && saveAppearanceSelection && !string.IsNullOrWhiteSpace(playerPrefsKey))
        {
            PlayerPrefs.SetString(playerPrefsKey, currentAppearanceId);
            PlayerPrefs.Save();
        }

        return true;
    }

    private void ApplyRendererOverride(SpriteRendererOverride overrideEntry)
    {
        SpriteRenderer targetRenderer = ResolveRenderer(overrideEntry);
        if (targetRenderer == null)
        {
            return;
        }

        CaptureDefaultState(targetRenderer);

        if (overrideEntry.sprite != null)
        {
            targetRenderer.sprite = overrideEntry.sprite;
        }

        if (overrideEntry.overrideColor)
        {
            targetRenderer.color = overrideEntry.color;
        }

        if (overrideEntry.overrideEnabledState)
        {
            targetRenderer.enabled = overrideEntry.rendererEnabled;
        }
    }

    private void CacheManagedRenderers()
    {
        CleanupManagedRenderers();

        if (managedRenderers.Count == 0)
        {
            AutoPopulateManagedRenderers();
        }

        rendererLookup.Clear();

        for (int i = 0; i < managedRenderers.Count; i++)
        {
            SpriteRenderer renderer = managedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            AddLookupKey(BuildRendererKey(renderer), renderer);
            AddLookupKey(renderer.gameObject.name, renderer);
        }
    }

    private void CacheDefaultStates()
    {
        for (int i = 0; i < managedRenderers.Count; i++)
        {
            CaptureDefaultState(managedRenderers[i]);
        }

        defaultObjectStates.Clear();

        for (int i = 0; i < appearances.Count; i++)
        {
            CacheObjectDefaults(appearances[i].objectsToEnable);
            CacheObjectDefaults(appearances[i].objectsToDisable);
        }
    }

    private void CacheObjectDefaults(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            GameObject targetObject = objects[i];
            if (targetObject == null || defaultObjectStates.ContainsKey(targetObject))
            {
                continue;
            }

            defaultObjectStates[targetObject] = targetObject.activeSelf;
        }
    }

    private void CaptureDefaultState(SpriteRenderer renderer)
    {
        if (renderer == null || defaultRendererStates.ContainsKey(renderer))
        {
            return;
        }

        defaultRendererStates[renderer] = new SpriteRendererState(renderer.sprite, renderer.color, renderer.enabled);
    }

    private void ApplyDefaultState()
    {
        foreach (KeyValuePair<SpriteRenderer, SpriteRendererState> pair in defaultRendererStates)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.sprite = pair.Value.Sprite;
            pair.Key.color = pair.Value.Color;
            pair.Key.enabled = pair.Value.Enabled;
        }

        foreach (KeyValuePair<GameObject, bool> pair in defaultObjectStates)
        {
            if (pair.Key == null)
            {
                continue;
            }

            pair.Key.SetActive(pair.Value);
        }
    }

    private void SetObjectStates(List<GameObject> objects, bool active)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    private CharacterAppearanceVariant FindAppearance(string appearanceId)
    {
        for (int i = 0; i < appearances.Count; i++)
        {
            CharacterAppearanceVariant candidate = appearances[i];
            if (candidate != null &&
                string.Equals(candidate.id, appearanceId, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private int GetCurrentAppearanceIndex()
    {
        for (int i = 0; i < appearances.Count; i++)
        {
            if (string.Equals(appearances[i].id, currentAppearanceId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private SpriteRenderer ResolveRenderer(SpriteRendererOverride overrideEntry)
    {
        if (overrideEntry == null)
        {
            return null;
        }

        if (overrideEntry.renderer != null)
        {
            return overrideEntry.renderer;
        }

        if (string.IsNullOrWhiteSpace(overrideEntry.rendererKey))
        {
            return null;
        }

        SpriteRenderer renderer;
        if (rendererLookup.TryGetValue(overrideEntry.rendererKey, out renderer))
        {
            return renderer;
        }

        return null;
    }

    private void CleanupManagedRenderers()
    {
        for (int i = managedRenderers.Count - 1; i >= 0; i--)
        {
            if (managedRenderers[i] == null)
            {
                managedRenderers.RemoveAt(i);
            }
        }
    }

    private void AddLookupKey(string key, SpriteRenderer renderer)
    {
        if (string.IsNullOrWhiteSpace(key) || renderer == null || rendererLookup.ContainsKey(key))
        {
            return;
        }

        rendererLookup.Add(key, renderer);
    }

    private string BuildRendererKey(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return string.Empty;
        }

        List<string> segments = new List<string>();
        Transform current = renderer.transform;

        while (current != null && current != transform)
        {
            segments.Insert(0, current.name);
            current = current.parent;
        }

        return string.Join("/", segments);
    }
}
