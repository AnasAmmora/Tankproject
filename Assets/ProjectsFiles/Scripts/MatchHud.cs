using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Presentation survives host disconnection after results. All decisions stay on server.
public class MatchHud : MonoBehaviour
{
    public static MatchHud Instance { get; private set; }
    [SerializeField] private TMP_FontAsset font;
    private TextMeshProUGUI timer;
    private TextMeshProUGUI lifeStatus;
    private TextMeshProUGUI resultTitle;
    private TextMeshProUGUI firstTeam;
    private TextMeshProUGUI secondTeam;
    private GameObject resultsPanel;
    private Button back;
    private MatchResults displayed;
    private RectTransform canvasRect;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Build();
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureEventSystem();
    }
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject events = new GameObject("MatchEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        // Scene-owned: it is removed when returning to the menu's own EventSystem.
    }
    private void Update()
    {
        if (MatchResults.LocalFinal != null) { ShowFinal(); return; }
        MatchManager match = MatchManager.Instance;
        NetworkManager network = NetworkManager.Singleton;
        if (match == null || !match.IsSpawned || network == null || !match.Running) return;
        double seconds = Math.Max(0, Math.Ceiling(match.phaseEndsAt.Value - network.ServerTime.Time));
        timer.text = (match.phase.Value == (int)MatchRules.Stage.Overtime ? "Overtime  " : "") +
            ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
        var player = network.LocalClient?.PlayerObject;
        lifeStatus.text = "";
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.isDead.Value)
        {
            int team = player.GetComponent<PlayerState>().teamIndex.Value;
            bool permitted = team >= 0 && team < 2 && (match.destroyedBases.Value & (1 << team)) == 0 &&
                match.phase.Value == (int)MatchRules.Stage.Regulation && health.respawnAt.Value >= 0;
            lifeStatus.text = permitted ? "Respawn in " + Math.Max(0, Math.Ceiling(health.respawnAt.Value - network.ServerTime.Time)) + "s" :
                "Eliminated — Waiting for match results";
        }
    }
    public void ShowFinal()
    {
        MatchResults result = MatchResults.LocalFinal;
        if (result == null) return;
        resultsPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (displayed == result) return;
        displayed = result;
        resultTitle.text = result.winner < 0 ? "Draw" : (result.winner == 0 ? "Blue team wins" : "Red team wins");
        firstTeam.text = TeamSummary(result, 0);
        secondTeam.text = TeamSummary(result, 1);
    }
    private static string TeamSummary(MatchResults result, int team)
    {
        return (team == 0 ? "Blue team" : "Red team") + "\n\n" + string.Join("\n\n", result.players
            .Where(p => p.team == team).Select(p => p.name + (p.disconnected ? " (Disconnected)" : "") +
                "\nKills: " + p.kills + "    Deaths: " + p.deaths));
    }
    private void Build()
    {
        GameObject canvasObject = new GameObject("MatchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvasRect = canvasObject.GetComponent<RectTransform>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        timer = Text("Timer", canvasObject.transform, new Vector2(800, 100), new Vector2(0, -24), 40);
        timer.rectTransform.anchorMin = timer.rectTransform.anchorMax = timer.rectTransform.pivot = new Vector2(0.5f, 1);
        lifeStatus = Text("LifeStatus", canvasObject.transform, new Vector2(1400, 100), new Vector2(0, 50), 32);
        lifeStatus.rectTransform.anchorMin = lifeStatus.rectTransform.anchorMax = lifeStatus.rectTransform.pivot = new Vector2(0.5f, 0);
        resultsPanel = new GameObject("Results", typeof(RectTransform), typeof(Image));
        resultsPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = resultsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        resultsPanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.97f);
        resultTitle = Text("Result", resultsPanel.transform, new Vector2(1200, 120), new Vector2(0, 330), 50);
        resultTitle.rectTransform.anchorMin = resultTitle.rectTransform.anchorMax = resultTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
        resultTitle.rectTransform.anchoredPosition = new Vector2(0, -64);
        firstTeam = Text("BlueResults", resultsPanel.transform, new Vector2(600, 440), new Vector2(-330, 0), 32);
        secondTeam = Text("RedResults", resultsPanel.transform, new Vector2(600, 440), new Vector2(330, 0), 32);
        firstTeam.alignment = secondTeam.alignment = TextAlignmentOptions.TopLeft;
        GameObject button = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
        button.transform.SetParent(resultsPanel.transform, false);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(320, 80); rect.anchoredPosition = new Vector2(0, 64);
        button.GetComponent<Image>().color = new Color(0.12f, 0.28f, 0.45f);
        Text("Label", button.transform, rect.sizeDelta, Vector2.zero, 32).text = "Back";
        back = button.GetComponent<Button>(); back.onClick.AddListener(OnBack);
        resultsPanel.SetActive(false);
    }
    private TextMeshProUGUI Text(string name, Transform parent, Vector2 size, Vector2 position, int fontSize)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font != null ? font : TMP_Settings.defaultFontAsset;
        text.fontSize = fontSize; text.color = Color.white;
        text.richText = false; text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true; text.fontSizeMin = 20; text.fontSizeMax = fontSize;
        text.rectTransform.sizeDelta = size; text.rectTransform.anchoredPosition = position;
        text.text = "";
        return text;
    }
    private void OnBack()
    {
        if (MatchConnectionCoordinator.Instance == null) return;
        back.interactable = false;
        MatchConnectionCoordinator.Instance.ReturnFromResults();
    }
    private void LateUpdate()
    {
        if (canvasRect == null) return;
        float width = Mathf.Max(1f, canvasRect.rect.width - 96f);
        float columnsWidth = Mathf.Min(1300f, width);
        timer.rectTransform.sizeDelta = new Vector2(Mathf.Min(800f, width), 100f);
        lifeStatus.rectTransform.sizeDelta = new Vector2(width, 100f);
        resultTitle.rectTransform.sizeDelta = new Vector2(Mathf.Min(1200f, width), 120f);
        Vector2 columnSize = new Vector2(Mathf.Max(1f, columnsWidth * 0.5f - 24f), Mathf.Clamp(canvasRect.rect.height - 400f, 160f, 440f));
        firstTeam.rectTransform.sizeDelta = secondTeam.rectTransform.sizeDelta = columnSize;
        firstTeam.rectTransform.anchoredPosition = new Vector2(-columnsWidth * 0.25f, 0);
        secondTeam.rectTransform.anchoredPosition = new Vector2(columnsWidth * 0.25f, 0);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene") Destroy(gameObject);
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (back != null) back.onClick.RemoveListener(OnBack);
        if (Instance == this) Instance = null;
    }
}
