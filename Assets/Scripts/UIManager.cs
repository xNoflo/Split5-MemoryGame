using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private const string ThemeFontResourcePath = "Fonts & Materials/Bangers SDF";
    private const string FallbackFontResourcePath = "Fonts & Materials/Anton SDF";
    private const string BackgroundImageFileName = "waterfall-game-background.jpg";

    [SerializeField] private Transform boardRoot;
    [SerializeField] private TMP_Text timerLabel;
    [SerializeField] private TMP_Text movesLabel;
    [SerializeField] private TMP_Text roundLabel;
    [SerializeField] private Button menuButton;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text endGameBodyLabel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button restartGameButton;
    [SerializeField] private Button endGameMainMenuButton;

    private MemoryGameManager _gameManager;
    private TMP_FontAsset _themeFontAsset;
    private Sprite _backgroundSprite;
    private Texture2D _backgroundTexture;

    public Transform BoardRoot => boardRoot;
    public TMP_FontAsset ActiveFont => _themeFontAsset;

    public void Initialize(MemoryGameManager gameManager)
    {
        _gameManager = gameManager;
        ApplyTheme();
        // edited by Noah: this manager now drives scene-authored UI references only, instead of generating canvas objects in code.
        WireButtons();
        UpdateHud(30, 0, 0, 8, 0, 16, 1, 2);
    }

    public void UpdateHud(int secondsRemaining, int moves, int roundPairsMatched, int pairsPerRound, int totalPairsMatched, int totalPairs, int round, int totalRounds)
    {
        if (timerLabel != null)
        {
            timerLabel.text = $"Timer\n{secondsRemaining}s";
        }

        if (movesLabel != null)
        {
            movesLabel.text = $"Moves\n{moves}";
        }

        if (roundLabel != null)
        {
            roundLabel.text = $"Round {round}/{totalRounds}\nBoard {roundPairsMatched}/{pairsPerRound}\nTotal {totalPairsMatched}/{totalPairs}";
        }
    }

    public void ShowMainMenu()
    {
        SetHudTextVisible(false);
        SetActiveIfPresent(hudPanel, true);
        SetActiveIfPresent(menuButton != null ? menuButton.gameObject : null, false);
        SetActiveIfPresent(mainMenuPanel, true);
        SetActiveIfPresent(endGamePanel, false);
    }

    public void ShowEndScreen(int totalPairsMatched, int totalPairs, int roundsCompleted, int totalRounds, bool clearedAllRounds)
    {
        if (endGameBodyLabel != null)
        {
            string headline = clearedAllRounds ? "Run Complete" : "Time's Up";
            endGameBodyLabel.text = $"{headline}\nPairs matched: {totalPairsMatched}/{totalPairs}\nRounds reached: {roundsCompleted}/{totalRounds}";
        }

        SetHudTextVisible(false);
        SetActiveIfPresent(hudPanel, true);
        SetActiveIfPresent(menuButton != null ? menuButton.gameObject : null, false);
        SetActiveIfPresent(mainMenuPanel, false);
        SetActiveIfPresent(endGamePanel, true);
    }

    public void HideOverlay()
    {
        SetHudTextVisible(true);
        SetActiveIfPresent(hudPanel, true);
        SetActiveIfPresent(menuButton != null ? menuButton.gameObject : null, true);
        SetActiveIfPresent(mainMenuPanel, false);
        SetActiveIfPresent(endGamePanel, false);
    }

    private void WireButtons()
    {
        WireButton(menuButton, _gameManager.ReturnToMainMenu);
        WireButton(startGameButton, _gameManager.StartGame);
        WireButton(restartGameButton, _gameManager.RestartGame);
        WireButton(endGameMainMenuButton, _gameManager.ReturnToMainMenu);
    }

    private void ApplyTheme()
    {
        _themeFontAsset = Resources.Load<TMP_FontAsset>(ThemeFontResourcePath)
            ?? Resources.Load<TMP_FontAsset>(FallbackFontResourcePath)
            ?? timerLabel?.font;

        ApplyCanvasBackground();
        ApplyPanelTheme();
        ApplyBoardTheme();
        ApplyTextTheme();
        ApplyButtonTheme(menuButton, new Color(0.2f, 0.36f, 0.26f, 0.96f));
        ApplyButtonTheme(startGameButton, new Color(0.27f, 0.49f, 0.26f, 0.98f));
        ApplyButtonTheme(restartGameButton, new Color(0.2f, 0.36f, 0.26f, 0.96f));
        ApplyButtonTheme(endGameMainMenuButton, new Color(0.12f, 0.24f, 0.29f, 0.96f));
    }

    private void ApplyCanvasBackground()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform existingBackground = transform.Find("ThemeBackground");
        if (existingBackground != null)
        {
            Destroy(existingBackground.gameObject);
        }

        Transform existingScrim = transform.Find("ThemeScrim");
        if (existingScrim != null)
        {
            Destroy(existingScrim.gameObject);
        }

        string backgroundPath = Path.Combine(Application.dataPath, "Images", BackgroundImageFileName);
        if (!File.Exists(backgroundPath))
        {
            Debug.LogWarning($"Theme background image was not found at {backgroundPath}", this);
            return;
        }

        byte[] fileBytes = File.ReadAllBytes(backgroundPath);
        _backgroundTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!_backgroundTexture.LoadImage(fileBytes))
        {
            Destroy(_backgroundTexture);
            _backgroundTexture = null;
            Debug.LogWarning("Theme background image could not be loaded.", this);
            return;
        }

        _backgroundTexture.name = "WaterfallBackgroundTexture";
        _backgroundSprite = Sprite.Create(
            _backgroundTexture,
            new Rect(0, 0, _backgroundTexture.width, _backgroundTexture.height),
            new Vector2(0.5f, 0.5f));
        _backgroundSprite.name = "WaterfallBackgroundSprite";

        GameObject backgroundObject = new("ThemeBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
        backgroundObject.transform.SetParent(transform, false);
        backgroundObject.transform.SetAsFirstSibling();

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        StretchRect(backgroundRect);

        Image backgroundImage = backgroundObject.GetComponent<Image>();
        backgroundImage.sprite = _backgroundSprite;
        backgroundImage.color = Color.white;
        backgroundImage.raycastTarget = false;
        backgroundImage.preserveAspect = true;

        AspectRatioFitter aspectFitter = backgroundObject.GetComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        aspectFitter.aspectRatio = (float)_backgroundTexture.width / _backgroundTexture.height;

        GameObject scrimObject = new("ThemeScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrimObject.transform.SetParent(transform, false);
        scrimObject.transform.SetSiblingIndex(1);

        RectTransform scrimRect = scrimObject.GetComponent<RectTransform>();
        StretchRect(scrimRect);

        Image scrimImage = scrimObject.GetComponent<Image>();
        scrimImage.color = new Color(0.05f, 0.12f, 0.16f, 0.46f);
        scrimImage.raycastTarget = false;
    }

    private void ApplyPanelTheme()
    {
        Transform rootPanel = FindDeepChild(transform, "Panel");
        if (rootPanel != null)
        {
            RestorePlainImage(rootPanel.gameObject, new Color(0.08f, 0.16f, 0.19f, 0.32f));
        }

        TintImage(hudPanel, new Color(0.08f, 0.18f, 0.22f, 0.72f));
        RestorePlainImage(mainMenuPanel, new Color(0.07f, 0.14f, 0.18f, 0.76f));
        RestorePlainImage(endGamePanel, new Color(0.07f, 0.14f, 0.18f, 0.82f));
    }

    private void ApplyBoardTheme()
    {
        if (boardRoot == null)
        {
            return;
        }

        Image boardImage = boardRoot.GetComponent<Image>();
        if (boardImage != null)
        {
            boardImage.color = new Color(0.06f, 0.17f, 0.22f, 0.58f);
            boardImage.raycastTarget = false;
        }

        CardLayout layout = boardRoot.GetComponent<CardLayout>();
        if (layout != null)
        {
            layout.spacing = new Vector2(14f, 14f);
            layout.preferredTopPadding = 18;
        }
    }

    private void ApplyTextTheme()
    {
        TMP_Text[] allText = GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in allText)
        {
            if (label == null)
            {
                continue;
            }

            if (_themeFontAsset != null)
            {
                label.font = _themeFontAsset;
            }

            label.color = new Color(0.95f, 0.99f, 0.96f, 1f);
        }

        StyleText(timerLabel, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        StyleText(movesLabel, 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        StyleText(roundLabel, 28f, FontStyles.Bold, TextAlignmentOptions.Center);
        StyleText(endGameBodyLabel, 44f, FontStyles.Bold, TextAlignmentOptions.Center);

        StyleNamedText("MainMenuTitle", 74f, new Color(0.93f, 0.99f, 0.94f, 1f), FontStyles.Bold);
        StyleNamedText("MainMenuBody", 34f, new Color(0.86f, 0.97f, 0.9f, 1f), FontStyles.Bold);
        StyleNamedText("StartGameButtonLabel", 34f, Color.white, FontStyles.Bold);
        StyleNamedText("RestartGameButtonLabel", 32f, Color.white, FontStyles.Bold);
        StyleNamedText("EndGameMainMenuLabel", 30f, Color.white, FontStyles.Bold);
        StyleNamedText("MainMenuButtonLabel", 28f, Color.white, FontStyles.Bold);
    }

    private void ApplyButtonTheme(Button button, Color backgroundColor)
    {
        if (button == null)
        {
            return;
        }

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = backgroundColor;
            buttonImage.raycastTarget = true;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.97f, 0.92f, 1f);
        colors.pressedColor = new Color(0.7f, 0.85f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.55f, 0.52f, 0.75f);
        button.colors = colors;

        TMP_Text buttonLabel = button.GetComponentInChildren<TMP_Text>(true);
        if (buttonLabel != null)
        {
            if (_themeFontAsset != null)
            {
                buttonLabel.font = _themeFontAsset;
            }

            buttonLabel.color = Color.white;
        }
    }

    private void StyleNamedText(string objectName, float fontSize, Color color, FontStyles fontStyle)
    {
        Transform target = transform.Find(objectName);
        if (target == null)
        {
            target = FindDeepChild(transform, objectName);
        }

        TMP_Text label = target != null ? target.GetComponent<TMP_Text>() : null;
        if (label == null)
        {
            return;
        }

        StyleText(label, fontSize, fontStyle, label.alignment);
        label.color = color;
    }

    private void StyleText(TMP_Text label, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        if (label == null)
        {
            return;
        }

        if (_themeFontAsset != null)
        {
            label.font = _themeFontAsset;
        }

        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.lineSpacing = -8f;
    }

    private static void TintImage(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
        }
    }

    private void ApplyBackgroundSprite(GameObject target, Color tint)
    {
        if (target == null || _backgroundSprite == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = _backgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = tint;
    }

    private static void RestorePlainImage(GameObject target, Color tint)
    {
        if (target == null)
        {
            return;
        }

        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = tint;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetActiveIfPresent(GameObject target, bool isActive)
    {
        if (target != null)
        {
            target.SetActive(isActive);
        }
    }

    private void SetHudTextVisible(bool isVisible)
    {
        SetTextVisible(timerLabel, isVisible);
        SetTextVisible(movesLabel, isVisible);
        SetTextVisible(roundLabel, isVisible);
    }

    private static void SetTextVisible(TMP_Text label, bool isVisible)
    {
        if (label != null)
        {
            label.enabled = isVisible;
        }
    }

    private void OnDestroy()
    {
        if (_backgroundSprite != null)
        {
            Destroy(_backgroundSprite);
            _backgroundSprite = null;
        }

        if (_backgroundTexture != null)
        {
            Destroy(_backgroundTexture);
            _backgroundTexture = null;
        }
    }
}
