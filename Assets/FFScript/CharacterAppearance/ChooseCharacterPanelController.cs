using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChooseCharacterPanelController : MonoBehaviour
{
    private const string AppearanceKey = "MainCharacter.Appearance";

    [SerializeField] private string fishingSceneName = "FishingScene";

    private static readonly Color BackgroundColor = new Color32(20, 35, 52, 255);
    private static readonly Color PanelColor = new Color32(18, 31, 44, 230);
    private static readonly Color PrimaryTextColor = new Color32(243, 244, 246, 255);
    private static readonly CharacterOption[] CharacterOptions =
    {
        new CharacterOption("classic", "CharacterPortraits/classic_portrait"),
        new CharacterOption("forest-guide", "CharacterPortraits/forest_guide_portrait"),
        new CharacterOption("retro-bucket", "CharacterPortraits/retro_bucket_portrait"),
        new CharacterOption("female-dark", "CharacterPortraits/female_dark_portrait"),
    };

    private readonly List<CharacterButtonView> buttonViews = new List<CharacterButtonView>();

    private Font uiFont;
    private Sprite uiSprite;
    private RectTransform panelRect;
    private RectTransform contentRect;
    private VerticalLayoutGroup contentLayout;
    private GridLayoutGroup optionsGrid;
    private LayoutElement optionsGridLayoutElement;
    private string selectedAppearanceId = CharacterOptions[0].Id;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        uiFont = CreateUiFont();
        uiSprite = CreateUiSprite();
        EnsureEventSystem();
        BuildUi();
        LoadSavedState();
        ApplySelectionVisuals();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            RefreshResponsiveLayout();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveSelection(1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmSelection();
        }
    }

    public void ConfirmSelection()
    {
        PlayerPrefs.SetString(AppearanceKey, selectedAppearanceId);
        PlayerPrefs.Save();
        SceneManager.LoadScene(fishingSceneName);
    }

    private void BuildUi()
    {
        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        panelRect = CreatePanel(canvas.transform);

        ScrollRect scrollRect = panelRect.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 36f;
        panelRect.gameObject.AddComponent<RectMask2D>();

        contentRect = CreateStretchRectTransform(
            "Content",
            panelRect,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(56f, 0f),
            new Vector2(-56f, 0f),
            new Vector2(0.5f, 0.5f));

        contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 28f;

        ContentSizeFitter contentFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = panelRect;
        scrollRect.content = contentRect;

        Text titleText = CreateText(
            "Title",
            contentRect,
            "\u9009\u62e9\u4f60\u7684\u9493\u624b",
            48,
            FontStyle.Bold,
            PrimaryTextColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        ConfigureResponsiveText(titleText, 28, 48);
        SetPreferredHeight(titleText.rectTransform, 92f);

        RectTransform optionsGridRect = CreateRectTransform(
            "CharacterGrid",
            contentRect,
            Vector2.zero,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero);
        optionsGrid = optionsGridRect.gameObject.AddComponent<GridLayoutGroup>();
        optionsGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        optionsGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
        optionsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        optionsGrid.constraintCount = CharacterOptions.Length;
        optionsGrid.spacing = new Vector2(24f, 24f);
        optionsGrid.childAlignment = TextAnchor.UpperCenter;
        optionsGridLayoutElement = optionsGridRect.gameObject.AddComponent<LayoutElement>();

        for (int i = 0; i < CharacterOptions.Length; i++)
        {
            CreateCharacterCard(optionsGridRect, CharacterOptions[i]);
        }

        Canvas.ForceUpdateCanvases();
        RefreshResponsiveLayout();
    }

    private void LoadSavedState()
    {
        string savedAppearanceId = PlayerPrefs.GetString(AppearanceKey, CharacterOptions[0].Id);
        if (!HasOption(savedAppearanceId))
        {
            savedAppearanceId = CharacterOptions[0].Id;
        }

        selectedAppearanceId = savedAppearanceId;
    }

    private void CreateCharacterCard(RectTransform parent, CharacterOption option)
    {
        Sprite defaultPortrait = LoadPortraitSprite(option.PortraitResourcePath);
        Sprite selectedPortrait = LoadPortraitSprite(option.SelectedPortraitResourcePath);

        RectTransform cardRect = CreateRectTransform(
            option.Id + "Card",
            parent,
            new Vector2(240f, 240f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero);

        Image clickTarget = cardRect.gameObject.AddComponent<Image>();
        clickTarget.color = new Color(1f, 1f, 1f, 0f);

        Button button = cardRect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = clickTarget;

        string optionId = option.Id;
        button.onClick.AddListener(() => OnCharacterChosen(optionId));

        RectTransform portraitRect = CreateStretchRectTransform(
            "Portrait",
            cardRect,
            Vector2.zero,
            Vector2.one,
            new Vector2(8f, 8f),
            new Vector2(-8f, -8f),
            new Vector2(0.5f, 0.5f));
        Image portraitImage = portraitRect.gameObject.AddComponent<Image>();
        portraitImage.sprite = defaultPortrait;
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        portraitImage.color = portraitImage.sprite != null ? Color.white : new Color32(190, 190, 190, 255);

        RectTransform borderRect = CreateStretchRectTransform(
            "SelectionBorder",
            cardRect,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Vector2(0.5f, 0.5f));
        borderRect.gameObject.SetActive(false);

        RectTransform topLine = CreateBorderLine("Top", borderRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f), new Vector2(0f, 4f));
        RectTransform bottomLine = CreateBorderLine("Bottom", borderRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 4f));
        RectTransform leftLine = CreateBorderLine("Left", borderRect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(2f, 0f), new Vector2(4f, 0f));
        RectTransform rightLine = CreateBorderLine("Right", borderRect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-2f, 0f), new Vector2(4f, 0f));

        buttonViews.Add(new CharacterButtonView(
            option,
            portraitImage,
            defaultPortrait,
            selectedPortrait,
            borderRect.gameObject,
            topLine,
            bottomLine,
            leftLine,
            rightLine));
    }

    private void OnCharacterChosen(string appearanceId)
    {
        SetSelectedCharacter(appearanceId);
        ConfirmSelection();
    }

    private void MoveSelection(int direction)
    {
        if (CharacterOptions.Length == 0 || direction == 0)
        {
            return;
        }

        int currentIndex = GetSelectedIndex();
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int nextIndex = (currentIndex + direction + CharacterOptions.Length) % CharacterOptions.Length;
        SetSelectedCharacter(CharacterOptions[nextIndex].Id);
    }

    private void SetSelectedCharacter(string appearanceId)
    {
        if (!HasOption(appearanceId))
        {
            return;
        }

        selectedAppearanceId = appearanceId;
        ApplySelectionVisuals();
    }

    private int GetSelectedIndex()
    {
        for (int i = 0; i < CharacterOptions.Length; i++)
        {
            if (string.Equals(CharacterOptions[i].Id, selectedAppearanceId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplySelectionVisuals()
    {
        for (int i = 0; i < buttonViews.Count; i++)
        {
            CharacterButtonView view = buttonViews[i];
            bool isSelected = string.Equals(view.Option.Id, selectedAppearanceId, StringComparison.OrdinalIgnoreCase);
            Sprite targetPortrait = isSelected && view.SelectedPortrait != null
                ? view.SelectedPortrait
                : view.DefaultPortrait;
            view.PortraitImage.sprite = targetPortrait;
            view.PortraitImage.color = targetPortrait != null ? Color.white : new Color32(190, 190, 190, 255);
            view.SelectionBorder.SetActive(isSelected);
        }
    }

    private static Sprite LoadPortraitSprite(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        return Resources.Load<Sprite>(resourcePath);
    }

    private static RectTransform CreateBorderLine(
        string objectName,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform lineRect = CreateRectTransform(objectName, parent, sizeDelta, anchorMin, anchorMax, anchoredPosition);
        Image lineImage = lineRect.gameObject.AddComponent<Image>();
        lineImage.color = Color.white;
        lineImage.raycastTarget = false;
        return lineRect;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("CharacterSelectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private void CreateBackground(Transform parent)
    {
        RectTransform backgroundRect = CreateRectTransform("Background", parent as RectTransform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundRect.gameObject.AddComponent<Image>();
        ApplyUiSprite(background);
        background.color = BackgroundColor;

        CreateDecoration(backgroundRect, new Vector2(220f, -140f), new Vector2(340f, 340f), new Color(1f, 1f, 1f, 0.04f));
        CreateDecoration(backgroundRect, new Vector2(-260f, 140f), new Vector2(240f, 240f), new Color(1f, 1f, 1f, 0.03f));
    }

    private static void CreateDecoration(RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        RectTransform decorationRect = CreateRectTransform("Decoration", parent, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        Image image = decorationRect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
    }

    private RectTransform CreatePanel(Transform parent)
    {
        RectTransform rectTransform = CreateRectTransform("Panel", parent as RectTransform, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        rectTransform.offsetMin = new Vector2(36f, 36f);
        rectTransform.offsetMax = new Vector2(-36f, -36f);

        Image panelImage = rectTransform.gameObject.AddComponent<Image>();
        ApplyUiSprite(panelImage);
        panelImage.color = PanelColor;

        Shadow shadow = rectTransform.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -10f);

        return rectTransform;
    }

    private Text CreateText(
        string objectName,
        RectTransform parent,
        string content,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform rect = CreateRectTransform(objectName, parent, size, anchorMin, anchorMax, anchoredPosition);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }

    private static void ConfigureResponsiveText(Text text, int minSize, int maxSize)
    {
        if (text == null)
        {
            return;
        }

        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
    }

    private static RectTransform CreateRectTransform(
        string objectName,
        RectTransform parent,
        Vector2 size,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        return rectTransform;
    }

    private static RectTransform CreateStretchRectTransform(
        string objectName,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Vector2 pivot)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private static void SetPreferredHeight(RectTransform rectTransform, float preferredHeight)
    {
        if (rectTransform == null)
        {
            return;
        }

        LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = preferredHeight;
    }

    private void RefreshResponsiveLayout()
    {
        if (panelRect == null || contentRect == null || contentLayout == null || optionsGrid == null || optionsGridLayoutElement == null)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float edgePadding = Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) * 0.035f, 16f, 40f);
        panelRect.offsetMin = new Vector2(edgePadding, edgePadding);
        panelRect.offsetMax = new Vector2(-edgePadding, -edgePadding);

        Canvas.ForceUpdateCanvases();

        float panelWidth = panelRect.rect.width;
        float contentPadding = Mathf.Clamp(panelWidth * 0.04f, 20f, 64f);
        float sectionSpacing = panelWidth < 1100f ? 20f : 28f;

        contentRect.offsetMin = new Vector2(contentPadding, 0f);
        contentRect.offsetMax = new Vector2(-contentPadding, 0f);
        contentLayout.spacing = sectionSpacing;

        float contentWidth = Mathf.Max(280f, panelWidth - (contentPadding * 2f));
        float gridSpacing = panelWidth < 1100f ? 18f : 24f;
        optionsGrid.spacing = new Vector2(gridSpacing, gridSpacing);

        int columns = Mathf.Clamp(
            Mathf.FloorToInt((contentWidth + gridSpacing) / (180f + gridSpacing)),
            1,
            CharacterOptions.Length);

        float cellSize = Mathf.Floor((contentWidth - (gridSpacing * (columns - 1))) / columns);
        cellSize = Mathf.Clamp(cellSize, 140f, 260f);

        optionsGrid.constraintCount = columns;
        optionsGrid.cellSize = new Vector2(cellSize, cellSize);

        int rows = Mathf.CeilToInt((float)CharacterOptions.Length / columns);
        optionsGridLayoutElement.preferredHeight = (rows * cellSize) + (Mathf.Max(0, rows - 1) * gridSpacing);

        float borderThickness = Mathf.Clamp(cellSize * 0.02f, 2f, 5f);
        for (int i = 0; i < buttonViews.Count; i++)
        {
            UpdateBorderLine(buttonViews[i].TopBorder, true, borderThickness);
            UpdateBorderLine(buttonViews[i].BottomBorder, true, borderThickness);
            UpdateBorderLine(buttonViews[i].LeftBorder, false, borderThickness);
            UpdateBorderLine(buttonViews[i].RightBorder, false, borderThickness);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        float panelHeight = panelRect.rect.height;
        float contentHeight = Mathf.Max(contentRect.rect.height, LayoutUtility.GetPreferredHeight(contentRect));
        float centeredY = 0f;
        float topAlignedY = (panelHeight - contentHeight) * 0.5f;
        contentRect.anchoredPosition = new Vector2(0f, contentHeight <= panelHeight ? centeredY : topAlignedY);
    }

    private static void UpdateBorderLine(RectTransform lineRect, bool horizontal, float thickness)
    {
        if (lineRect == null)
        {
            return;
        }

        if (horizontal)
        {
            float direction = lineRect.anchorMin.y > 0.5f ? -1f : 1f;
            lineRect.sizeDelta = new Vector2(0f, thickness);
            lineRect.anchoredPosition = new Vector2(0f, direction * thickness * 0.5f);
        }
        else
        {
            float direction = lineRect.anchorMin.x > 0.5f ? -1f : 1f;
            lineRect.sizeDelta = new Vector2(thickness, 0f);
            lineRect.anchoredPosition = new Vector2(direction * thickness * 0.5f, 0f);
        }
    }

    private static Font CreateUiFont()
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Arial Unicode MS", "Arial" },
                32);
        }
        catch
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    private static Sprite CreateUiSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "CharacterSelectionRuntimeTexture";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        sprite.name = "CharacterSelectionRuntimeSprite";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private void ApplyUiSprite(Image image)
    {
        if (image == null)
        {
            return;
        }

        if (uiSprite != null)
        {
            image.sprite = uiSprite;
            image.type = Image.Type.Simple;
        }
    }

    private static bool HasOption(string appearanceId)
    {
        for (int i = 0; i < CharacterOptions.Length; i++)
        {
            if (string.Equals(CharacterOptions[i].Id, appearanceId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class CharacterOption
    {
        public CharacterOption(string id, string portraitResourcePath)
        {
            Id = id;
            PortraitResourcePath = portraitResourcePath;
        }

        public string Id { get; }
        public string PortraitResourcePath { get; }
        public string SelectedPortraitResourcePath => PortraitResourcePath + "_FishingRot";
    }

    private sealed class CharacterButtonView
    {
        public CharacterButtonView(
            CharacterOption option,
            Image portraitImage,
            Sprite defaultPortrait,
            Sprite selectedPortrait,
            GameObject selectionBorder,
            RectTransform topBorder,
            RectTransform bottomBorder,
            RectTransform leftBorder,
            RectTransform rightBorder)
        {
            Option = option;
            PortraitImage = portraitImage;
            DefaultPortrait = defaultPortrait;
            SelectedPortrait = selectedPortrait;
            SelectionBorder = selectionBorder;
            TopBorder = topBorder;
            BottomBorder = bottomBorder;
            LeftBorder = leftBorder;
            RightBorder = rightBorder;
        }

        public CharacterOption Option { get; }
        public Image PortraitImage { get; }
        public Sprite DefaultPortrait { get; }
        public Sprite SelectedPortrait { get; }
        public GameObject SelectionBorder { get; }
        public RectTransform TopBorder { get; }
        public RectTransform BottomBorder { get; }
        public RectTransform LeftBorder { get; }
        public RectTransform RightBorder { get; }
    }
}
