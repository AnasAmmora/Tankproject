using System;
using System.Linq;
using System.Collections.Generic;
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
    [SerializeField] private Transform blueRows;
    [SerializeField] private Transform redRows;
    [SerializeField] private ResultRow rowPrefab;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Button back;
    [SerializeField] private EventSystem sceneEventSystem;
    [SerializeField] private string menuSceneName = "MainMenuScene";
    private MatchResults displayed;
    private bool configured;
    private readonly Dictionary<ulong, ResultRow> blueEntries = new Dictionary<ulong, ResultRow>();
    private readonly Dictionary<ulong, ResultRow> redEntries = new Dictionary<ulong, ResultRow>();
    private ulong? localClientId;
    private ulong? displayedLocalClientId;

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
        valid &= Required(blueRows, nameof(blueRows));
        valid &= Required(redRows, nameof(redRows));
        valid &= Required(rowPrefab, nameof(rowPrefab));
        if (rowPrefab != null) valid &= rowPrefab.ValidateReferences();
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
        CaptureLocalIdentity();
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
        CaptureLocalIdentity();
        resultsPanel.SetActive(true);
        timer.gameObject.SetActive(false);
        lifeStatus.gameObject.SetActive(false);
        // The owner's component also checks the retained result at spawn and in
        // LateUpdate, covering UI setup/activation that happens after this call.
        var player = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (player != null && player.TryGetComponent<PlayerCanvasController>(out var ui)) ui.RefreshVisibility();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (displayed == result && displayedLocalClientId == localClientId) return;
        displayed = result;
        displayedLocalClientId = localClientId;
        resultTitle.text = result.winner < 0 ? "Draw" : (result.winner == 0 ? "Blue team wins" : "Red team wins");
        UpdateRows(result, 0, blueRows, blueEntries);
        UpdateRows(result, 1, redRows, redEntries);
    }

    private void CaptureLocalIdentity()
    {
        NetworkManager network = NetworkManager.Singleton;
        // NGO resets its local ID on shutdown. Retain the identity from this match
        // so the highlight remains correct after the host leaves the results.
        if (!localClientId.HasValue && network != null && network.IsConnectedClient)
            localClientId = network.LocalClientId;
    }

    private void UpdateRows(MatchResults result, int team, Transform container, Dictionary<ulong, ResultRow> entries)
    {
        var players = (result.players ?? Array.Empty<MatchRules.PlayerRecord>())
            .Where(p => p != null && p.team == team).GroupBy(p => p.clientId).Select(group => group.Last())
            .OrderBy(p => p.ordinal).ThenBy(p => p.clientId).ToArray();
        var present = new HashSet<ulong>(players.Select(p => p.clientId));
        foreach (ulong id in entries.Keys.Where(id => !present.Contains(id)).ToArray())
        {
            entries[id].gameObject.SetActive(false);
            Destroy(entries[id].gameObject);
            entries.Remove(id);
        }
        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];
            if (!entries.TryGetValue(player.clientId, out ResultRow row))
            {
                row = Instantiate(rowPrefab, container);
                entries.Add(player.clientId, row);
            }
            row.transform.SetSiblingIndex(i);
            row.Bind(player, i % 2 != 0, localClientId == player.clientId);
        }
    }

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
        // Instantiated rows belong to this canvas and are destroyed with its root.
        blueEntries.Clear();
        redEntries.Clear();
        if (Instance == this) Instance = null;
    }
}
