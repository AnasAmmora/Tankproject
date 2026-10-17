using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

// The single MatchCanvas owns match-wide presentation. No life/score decisions here.
public class GameplayHud : MonoBehaviour
{
    public static GameplayHud Instance { get; private set; }
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private TMP_Text timer;
    [SerializeField] private TMP_Text phaseLabel;
    [SerializeField] private TMP_Text lifeStatus;
    [SerializeField] private CanvasGroup timerAccent;
    [SerializeField] private Color normalTime = Color.white;
    [SerializeField] private Color lowTime = new Color(1f, 0.58f, 0.45f);
    [SerializeField] private Color overtimeTime = new Color(1f, 0.8f, 0.45f);
    [SerializeField, Min(1f)] private float lowTimeSeconds = 30f;
    [SerializeField] private CanvasGroup baseNotice;
    [SerializeField] private CanvasGroup overtimeNotice;
    [SerializeField, Min(1f)] private float noticeDuration = 5f;
    [SerializeField] private GameObject teammatePanel;
    [SerializeField] private Transform teammateRows;
    [SerializeField] private TeammateRow teammatePrefab;
    [SerializeField] private Transform feedRows;
    [SerializeField] private KillFeedRow feedPrefab;
    [SerializeField, Min(1)] private int maxFeedRows = 4;
    [SerializeField, Min(0.5f)] private float feedLifetime = 5f;
    private MatchManager match;
    private PlayerUI local;
    private MatchRules.PlayerRecord[] roster = Array.Empty<MatchRules.PlayerRecord>();
    private readonly Dictionary<ulong, TeammateRow> teammates = new Dictionary<ulong, TeammateRow>();
    private readonly List<KillFeedRow> feed = new List<KillFeedRow>();
    private float baseUntil;
    private float overtimeUntil;
    private int notifiedBases;
    private bool notifiedOvertime;
    private bool hidden;
    private bool configured;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        configured = ValidateReferences();
        if (!configured) { enabled = false; return; }
        baseNotice.alpha = overtimeNotice.alpha = 0;
        teammatePanel.SetActive(false);
        lifeStatus.text = "Waiting for match";
        timer.text = "00:00";
        PlayerHealth.Spawned += OnPlayerSpawned;
    }
    private bool ValidateReferences()
    {
        bool ok = true;
        ok &= Required(gameplayRoot, nameof(gameplayRoot)); ok &= Required(timer, nameof(timer));
        ok &= Required(phaseLabel, nameof(phaseLabel)); ok &= Required(lifeStatus, nameof(lifeStatus));
        ok &= Required(timerAccent, nameof(timerAccent)); ok &= Required(baseNotice, nameof(baseNotice));
        ok &= Required(overtimeNotice, nameof(overtimeNotice)); ok &= Required(teammatePanel, nameof(teammatePanel));
        ok &= Required(teammateRows, nameof(teammateRows)); ok &= Required(teammatePrefab, nameof(teammatePrefab));
        ok &= Required(feedRows, nameof(feedRows)); ok &= Required(feedPrefab, nameof(feedPrefab));
        if (teammatePrefab != null) ok &= teammatePrefab.ValidateReferences();
        if (feedPrefab != null) ok &= feedPrefab.ValidateReferences();
        return ok;
    }
    private bool Required(UnityEngine.Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"GameplayHud on '{name}': missing Inspector reference '{field}'.", this); return false;
    }
    public void BindPlayer(PlayerUI player)
    {
        local = player;
        if (configured) RefreshLocal();
    }
    public void ReleasePlayer(PlayerUI player)
    {
        if (local != player) return;
        local = null;
        ClearRows();
        if (configured)
        {
            teammatePanel.SetActive(false);
            baseUntil = overtimeUntil = 0;
            baseNotice.alpha = overtimeNotice.alpha = 0;
        }
    }
    private void OnPlayerSpawned(PlayerHealth player) { if (configured && !hidden) RefreshLocal(); }
    private void BindMatch()
    {
        if (match == MatchManager.Instance) return;
        UnbindMatch();
        match = MatchManager.Instance;
        if (match == null) return;
        match.phase.OnValueChanged += OnPhase;
        match.destroyedBases.OnValueChanged += OnBases;
        match.statistics.OnValueChanged += OnRoster;
        match.CombatConfirmed += OnCombat;
        ReadRoster(match.statistics.Value);
        ObserveNotices();
    }
    private void OnPhase(int previous, int current) { ObserveNotices(); RefreshLocal(); }
    private void OnBases(int previous, int current) { ObserveNotices(); RefreshLocal(); }
    private void OnRoster(FixedString4096Bytes previous, FixedString4096Bytes current) => ReadRoster(current);
    private void ReadRoster(FixedString4096Bytes json)
    {
        roster = json.Length == 0 ? Array.Empty<MatchRules.PlayerRecord>() :
            JsonUtility.FromJson<MatchResults>(json.ToString()).players ?? Array.Empty<MatchRules.PlayerRecord>();
        RefreshLocal();
    }
    public void RefreshLocal()
    {
        if (!configured || hidden || local == null || match == null) return;
        ObserveNotices();
        int team = local.State.teamIndex.Value;
        var wanted = roster.Where(p => p.team == team && p.clientId != local.ClientId).ToArray();
        var ids = new HashSet<ulong>(wanted.Select(p => p.clientId));
        foreach (ulong id in teammates.Keys.Where(id => !ids.Contains(id)).ToArray())
        { Remove(teammates[id].gameObject); teammates.Remove(id); }
        foreach (var record in wanted)
        {
            if (!teammates.TryGetValue(record.clientId, out var row))
            { row = Instantiate(teammatePrefab, teammateRows); teammates.Add(record.clientId, row); }
            row.Bind(record, FindHealth(record.clientId));
        }
        teammatePanel.SetActive(wanted.Length > 0);
    }
    private PlayerHealth FindHealth(ulong clientId)
    {
        var network = NetworkManager.Singleton;
        var obj = network != null && network.SpawnManager != null ? network.SpawnManager.GetPlayerNetworkObject(clientId) : null;
        return obj != null ? obj.GetComponent<PlayerHealth>() : null;
    }
    private void ObserveNotices()
    {
        if (match == null || hidden) return;
        if (match.phase.Value == (int)MatchRules.Stage.Overtime && !notifiedOvertime)
        { notifiedOvertime = true; overtimeUntil = Time.unscaledTime + noticeDuration; }
        if (local == null) return;
        int team = local.State.teamIndex.Value;
        if (team < 0 || team > 1) return;
        int bit = 1 << team;
        if ((match.destroyedBases.Value & bit) != 0 && (notifiedBases & bit) == 0)
        { notifiedBases |= bit; baseUntil = Time.unscaledTime + noticeDuration; }
    }
    private void OnCombat(CombatFeedback feedback)
    {
        if (hidden || !feedback.IsKill || feedback.IsBase) return;
        var row = Instantiate(feedPrefab, feedRows);
        row.Bind(feedback, feedLifetime);
        feed.Add(row);
        while (feed.Count > Mathf.Max(1, maxFeedRows)) { Remove(feed[0].gameObject); feed.RemoveAt(0); }
    }
    private void Update()
    {
        if (!configured || hidden) return;
        if (MatchResults.LocalFinal != null || MatchHud.ResultsVisible) { HideForResults(); return; }
        BindMatch();
        if (match != null)
        {
            var phase = (MatchRules.Stage)match.phase.Value;
            var network = NetworkManager.Singleton;
            double now = network != null ? network.ServerTime.Time : 0;
            double remaining = Math.Max(0, match.phaseEndsAt.Value - now);
            string clock = match.Running ? HudPresentation.Clock(remaining) : "00:00";
            if (timer.text != clock) timer.text = clock;
            phaseLabel.text = phase == MatchRules.Stage.Overtime ? "OVERTIME" : phase == MatchRules.Stage.Preparing ? "WAITING" : "MATCH TIME";
            bool urgent = match.Running && remaining <= lowTimeSeconds;
            timer.color = urgent ? lowTime : phase == MatchRules.Stage.Overtime ? overtimeTime : normalTime;
            timerAccent.alpha = urgent ? 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
            if (local != null)
            {
                string life = HudPresentation.Life(phase, local.Health.isPreparedForLife && MatchConnectionCoordinator.GameplayAllowed,
                    local.Health.isDead.Value, local.Health.respawnAt.Value, local.State.teamIndex.Value, match.destroyedBases.Value, now);
                if (lifeStatus.text != life) lifeStatus.text = life;
                lifeStatus.color = local.Health.isDead.Value ? lowTime : normalTime;
            }
            foreach (var row in teammates.Values)
            {
                // Spawned may have fired before NGO registered the PlayerObject.
                // Reconcile the source reference without creating another row.
                var record = roster.FirstOrDefault(p => p.clientId == row.ClientId);
                if (record != null) row.Bind(record, FindHealth(row.ClientId));
                row.Tick(phase, match.destroyedBases.Value, now);
            }
        }
        baseNotice.alpha = Mathf.Clamp01((baseUntil - Time.unscaledTime) / 0.6f);
        overtimeNotice.alpha = Mathf.Clamp01((overtimeUntil - Time.unscaledTime) / 0.6f);
        for (int i = feed.Count - 1; i >= 0; i--)
            if (!feed[i].Tick()) { Remove(feed[i].gameObject); feed.RemoveAt(i); }
    }
    public void HideForResults()
    {
        if (hidden || !configured) return;
        hidden = true;
        gameplayRoot.SetActive(false);
        baseNotice.alpha = overtimeNotice.alpha = 0;
        ClearRows();
        UnbindMatch();
    }
    private static void Remove(GameObject row) { row.SetActive(false); Destroy(row); }
    private void ClearRows()
    {
        foreach (var row in teammates.Values) if (row != null) Remove(row.gameObject);
        foreach (var row in feed) if (row != null) Remove(row.gameObject);
        teammates.Clear(); feed.Clear();
    }
    private void UnbindMatch()
    {
        if (match != null)
        {
            match.phase.OnValueChanged -= OnPhase; match.destroyedBases.OnValueChanged -= OnBases;
            match.statistics.OnValueChanged -= OnRoster; match.CombatConfirmed -= OnCombat;
        }
        match = null;
    }
    private void OnDestroy()
    {
        PlayerHealth.Spawned -= OnPlayerSpawned;
        UnbindMatch(); ClearRows(); if (Instance == this) Instance = null;
    }
}
