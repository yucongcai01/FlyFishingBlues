using System.Collections.Generic;
using UnityEngine;

public class CharacterVariantSelector : MonoBehaviour
{
    [SerializeField] private CharacterAppearanceController appearanceController;
    [SerializeField] private List<string> selectionOrder = new List<string>();
    [SerializeField] private string playerPrefsKey = "MainCharacter.Appearance";
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private bool applyFirstVariantIfSelectionMissing;

    private readonly KeyCode[] shortcutKeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
    };

    private void Reset()
    {
        appearanceController = GetComponent<CharacterAppearanceController>();
        if (selectionOrder.Count == 0)
        {
            selectionOrder.Add("classic");
            selectionOrder.Add("forest-guide");
            selectionOrder.Add("retro-bucket");
            selectionOrder.Add("female-dark");
        }
    }

    private void Awake()
    {
        if (appearanceController == null)
        {
            appearanceController = GetComponent<CharacterAppearanceController>();
        }

        if (applyFirstVariantIfSelectionMissing &&
            selectionOrder.Count > 0 &&
            !PlayerPrefs.HasKey(playerPrefsKey))
        {
            SelectByIndex(0);
        }
    }

    private void Start()
    {
        ValidateSelectionOrder();
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts)
        {
            return;
        }

        int count = Mathf.Min(selectionOrder.Count, shortcutKeys.Length);
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown(shortcutKeys[i]))
            {
                SelectByIndex(i);
            }
        }
    }

    public void SelectByIndex(int index)
    {
        if (index < 0 || index >= selectionOrder.Count)
        {
            Debug.LogWarning($"Character selection index {index} is out of range.", this);
            return;
        }

        SelectById(selectionOrder[index]);
    }

    public void SelectById(string appearanceId)
    {
        if (string.IsNullOrWhiteSpace(appearanceId))
        {
            Debug.LogWarning("Character selection id is empty.", this);
            return;
        }

        if (appearanceController != null && !appearanceController.HasAppearance(appearanceId))
        {
            Debug.LogWarning($"Character appearance '{appearanceId}' is not configured on {appearanceController.name}.", this);
            return;
        }

        PlayerPrefs.SetString(playerPrefsKey, appearanceId);
        PlayerPrefs.Save();

        if (appearanceController != null)
        {
            appearanceController.ApplyAppearance(appearanceId);
        }
    }

    public void SelectClassic()
    {
        SelectByIndex(0);
    }

    public void SelectForestGuide()
    {
        SelectByIndex(1);
    }

    public void SelectRetroBucket()
    {
        SelectByIndex(2);
    }

    public void SelectFemaleDark()
    {
        SelectByIndex(3);
    }

    private void ValidateSelectionOrder()
    {
        if (appearanceController == null)
        {
            Debug.LogWarning("CharacterVariantSelector has no CharacterAppearanceController reference.", this);
            return;
        }

        for (int i = 0; i < selectionOrder.Count; i++)
        {
            string appearanceId = selectionOrder[i];
            if (!appearanceController.HasAppearance(appearanceId))
            {
                Debug.LogWarning($"Selection entry '{appearanceId}' is missing from CharacterAppearanceController.", this);
            }
        }
    }
}
