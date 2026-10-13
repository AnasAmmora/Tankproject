using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Presentation only. Connection and readiness decisions belong to the coordinator.
public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }
    public event Action ReturnRequested;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private TMP_FontAsset loadingFont;
    private Canvas overlay;
    private TextMeshProUGUI status;
    private Button returnButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        Hide();
    }

    public void Show(string message)
    {
        overlay.gameObject.SetActive(true);
        status.text = message;
        status.rectTransform.anchoredPosition = new Vector2(0f, 64f);
        returnButton.gameObject.SetActive(false);
    }

    public void ShowError(string message, bool canReturn)
    {
        Show(message);
        status.rectTransform.anchoredPosition = new Vector2(0f, 144f);
        returnButton.gameObject.SetActive(true);
        returnButton.interactable = canReturn;
    }

    public void Hide() => overlay.gameObject.SetActive(false);

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("LoadingCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32760;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Background", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.035f, 0.05f, 0.075f, 1f);
        GameObject artwork = new GameObject("LoadingArtwork", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        artwork.transform.SetParent(panel.transform, false);
        Image background = artwork.GetComponent<Image>();
        background.sprite = backgroundSprite;
        background.preserveAspect = true;
        background.raycastTarget = false;
        AspectRatioFitter fit = artwork.GetComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = backgroundSprite != null ? backgroundSprite.rect.width / backgroundSprite.rect.height : 16f / 9f;
        // Fill the viewport by uniform scaling and cropping, never stretching the art.
        status = MakeText("Status", panel.transform, new Vector2(1400, 170), Vector2.zero, 40);
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 64f);

        GameObject button = new GameObject("ReturnToMenu", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(panel.transform, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(350, 80);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 24);
        button.GetComponent<Image>().color = new Color(0.14f, 0.3f, 0.5f);
        returnButton = button.GetComponent<Button>();
        MakeText("Label", button.transform, rect.sizeDelta, Vector2.zero, 28).text = "Return to menu";
        returnButton.onClick.AddListener(OnReturnClicked);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 size, Vector2 position, int fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = loadingFont != null ? loadingFont : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnReturnClicked() => ReturnRequested?.Invoke();
    private void LateUpdate()
    {
        if (overlay == null || !overlay.gameObject.activeSelf) return;
        float width = ((RectTransform)overlay.transform).rect.width;
        status.rectTransform.sizeDelta = new Vector2(Mathf.Min(1400f, Mathf.Max(1f, width - 96f)), 170f);
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (returnButton != null) returnButton.onClick.RemoveListener(OnReturnClicked);
    }
}
