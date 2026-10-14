using System;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Uses the scene's authored MatchCanvas. Persistence keeps final results available
// if the network host leaves first; returning to the menu removes this entire root.
public class MatchHud : MonoBehaviour
{
    public static MatchHud Instance { get; private set; }
    public static bool ResultsVisible => Instance != null && Instance.resultsPanel != null && Instance.resultsPanel.activeSelf;
    [SerializeField] private Canvas matchCanvas;
    [SerializeField] private GraphicRaycaster uiRaycaster;
    [SerializeField] private TMP_Text timer;
    [SerializeField] private TMP_Text lifeStatus;
    [SerializeField] private TMP_Text resultTitle;
    [SerializeField] private TMP_Text firstTeam;
    [SerializeField] private TMP_Text secondTeam;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Button back;
    [SerializeField] private EventSystem sceneEventSystem;
    [SerializeField] private string menuSceneName = "MainMenuScene";
    private MatchResults displayed;
    private bool configured;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (matchCanvas != null) matchCanvas.enabled = false;
            if (uiRaycaster != null) uiRaycaster.enabled = false;
            if (sceneEventSystem != null) sceneEventSystem.enabled = false;
            Destroy(gameObject);
            return;
        }
        Instance = this;
        configured = ValidateReferences();
        if (!configured) { enabled = false; return; }
        DontDestroyOnLoad(gameObject);
        resultsPanel.SetActive(false);
        timer.text = lifeStatus.text = "";
        back.interactable = true;
        back.onClick.AddListener(OnBack);
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (MatchResults.LocalFinal != null) ShowFinal();
    }

    private bool ValidateReferences()
    {
        bool valid = true;
        valid &= Required(matchCanvas, nameof(matchCanvas));
        valid &= Required(uiRaycaster, nameof(uiRaycaster));
        valid &= Required(timer, nameof(timer));
        valid &= Required(lifeStatus, nameof(lifeStatus));
        valid &= Required(resultTitle, nameof(resultTitle));
        valid &= Required(firstTeam, nameof(firstTeam));
        valid &= Required(secondTeam, nameof(secondTeam));
        valid &= Required(resultsPanel, nameof(resultsPanel));
        valid &= Required(back, nameof(back));
        valid &= Required(sceneEventSystem, nameof(sceneEventSystem));
        if (transform.parent != null || (matchCanvas != null && matchCanvas.gameObject != gameObject))
        {
            Debug.LogError("MatchHud must be attached to the MatchCanvas prefab root, with matchCanvas pointing to its root Canvas.", this);
            valid = false;
        }
        return valid;
    }

    private bool Required(UnityEngine.Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"MatchHud on '{name}': missing Inspector reference '{field}'.", this);
        return false;
    }

    private void Update()
    {
        if (!configured) return;
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
        if (!configured || result == null) return;
        resultsPanel.SetActive(true);
        timer.gameObject.SetActive(false);
        lifeStatus.gameObject.SetActive(false);
        // The owner's component also checks the retained result at spawn and in
        // LateUpdate, covering UI setup/activation that happens after this call.
        var player = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (player != null && player.TryGetComponent<PlayerCanvasController>(out var ui)) ui.RefreshVisibility();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (displayed == result) return;
        displayed = result;
        resultTitle.text = result.winner < 0 ? "Draw" : (result.winner == 0 ? "Blue team wins" : "Red team wins");
        firstTeam.text = TeamSummary(result, 0);
        secondTeam.text = TeamSummary(result, 1);
    }

    private static string TeamSummary(MatchResults result, int team) =>
        (team == 0 ? "Blue team" : "Red team") + "\n\n" + string.Join("\n\n", result.players
            .Where(p => p.team == team).Select(p => p.name + (p.disconnected ? " (Disconnected)" : "") +
                "\nKills: " + p.kills + "    Deaths: " + p.deaths));

    private void OnBack()
    {
        if (MatchConnectionCoordinator.Instance == null)
        {
            Debug.LogError("MatchHud: missing MatchConnectionCoordinator; cannot return to the menu.", this);
            return;
        }
        back.interactable = false;
        MatchConnectionCoordinator.Instance.ReturnFromResults();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != menuSceneName) return;
        matchCanvas.enabled = false;
        uiRaycaster.enabled = false;
        sceneEventSystem.enabled = false;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (back != null) back.onClick.RemoveListener(OnBack);
        if (Instance == this) Instance = null;
    }
}
