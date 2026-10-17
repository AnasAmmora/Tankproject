using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Owner-only presentation. Gameplay/physics decisions remain in their existing systems.
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [SerializeField] private TankShooter shooter;
    [SerializeField] private PlayerState state;
    [SerializeField] private GameObject combat;
    [SerializeField] private GameObject abilitySlot;
    [SerializeField] private Image reload;
    [SerializeField] private GameObject weaponReady;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private CanvasGroup lowHealth;
    [SerializeField] private CanvasGroup confirmation;
    [SerializeField] private GameObject hitMarker;
    [SerializeField] private GameObject killMarker;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private DamageIndicatorView[] damageIndicators;
    [SerializeField, Range(0.05f, 0.5f)] private float lowHealthThreshold = 0.25f;
    [SerializeField, Min(0.1f)] private float healthLerpSpeed = 2f;
    private bool bound;
    private MatchManager match;
    private GameplayHud hud;
    private Camera view;
    private float healthTarget;
    private float confirmedUntil;
    private float confirmedDuration;
    private int nextIndicator;
    private bool observedReload;
    private float observedReloadEndsAt;
    private bool readyScaleCached;
    private Vector3 readyRestScale;
    private float readyStartedAt = -1f;
    private const float ReadyDuration = 1f;
    public PlayerHealth Health => health;
    public PlayerState State => state;
    public ulong ClientId => health.OwnerClientId;

    private void OnEnable()
    {
        if (health != null && health.IsSpawned && health.IsOwner) OnPlayerSpawned();
        else ResetWeaponPresentation();
    }

    public void OnPlayerSpawned()
    {
        if (bound) return;
        if (!ValidateReferences()) { enabled = false; return; }
        if (!health.IsOwner) return;
        bound = true;
        if (!readyScaleCached)
        {
            readyRestScale = weaponReady.transform.localScale;
            readyScaleCached = true;
        }
        ClearEffects();
        ReadHealth(true);
        ReadWeapon();
        RefreshCombatVisibility();
        health.currentHealth.OnValueChanged += OnHealth;
        health.isDead.OnValueChanged += OnDead;
        health.respawnAt.OnValueChanged += OnRespawnTime;
        state.teamIndex.OnValueChanged += OnTeam;
        state.isPrepared.OnValueChanged += OnPrepared;
        shooter.WeaponStateChanged += ReadWeapon;
        BindPresentation();
    }

    private bool ValidateReferences()
    {
        bool ok = true;
        ok &= Required(health, nameof(health)); ok &= Required(shooter, nameof(shooter)); ok &= Required(state, nameof(state));
        ok &= Required(combat, nameof(combat)); ok &= Required(reload, nameof(reload)); ok &= Required(weaponReady, nameof(weaponReady));
        ok &= Required(abilitySlot, nameof(abilitySlot));
        ok &= Required(healthFill, nameof(healthFill)); ok &= Required(healthText, nameof(healthText)); ok &= Required(lowHealth, nameof(lowHealth));
        ok &= Required(confirmation, nameof(confirmation)); ok &= Required(hitMarker, nameof(hitMarker));
        ok &= Required(killMarker, nameof(killMarker)); ok &= Required(killText, nameof(killText));
        if (damageIndicators == null || damageIndicators.Length == 0) { Debug.LogError("PlayerUI: missing Inspector references 'damageIndicators'.", this); ok = false; }
        else foreach (var indicator in damageIndicators) ok &= Required(indicator, "damageIndicators entry") && indicator.ValidateReferences();
        return ok;
    }
    private bool Required(Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"PlayerUI on '{name}': missing Inspector reference '{field}'.", this); return false;
    }

    private void BindPresentation()
    {
        if (match != MatchManager.Instance)
        {
            if (match != null) match.CombatConfirmed -= OnCombat;
            match = MatchManager.Instance;
            if (match != null) match.CombatConfirmed += OnCombat;
        }
        if (hud != GameplayHud.Instance)
        {
            if (hud != null) hud.ReleasePlayer(this);
            hud = GameplayHud.Instance;
            if (hud != null) hud.BindPlayer(this);
        }
    }
    private void Update()
    {
        // Network spawn order and a later re-enable must not strand presentation
        // after OnDisable unsubscribed it. Read current replicated values on bind.
        if (!bound && health != null && health.IsSpawned && health.IsOwner)
            OnPlayerSpawned();
        if (!bound) return;
        if (health == null || !health.IsSpawned || !health.IsOwner) { OnPlayerDespawned(); return; }
        BindPresentation(); // Reference comparison also covers late scene/NGO initialization.
        bool finished = MatchResults.LocalFinal != null || MatchHud.ResultsVisible;
        RefreshCombatVisibility();
        if (finished) { ClearEffects(); return; }
        healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, healthTarget, healthLerpSpeed * Time.unscaledDeltaTime);
        lowHealth.alpha = !health.isDead.Value && healthTarget <= lowHealthThreshold
            ? 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
        ReadWeapon();
        TickWeaponReady();
        confirmation.alpha = Mathf.Clamp01((confirmedUntil - Time.unscaledTime) / Mathf.Max(0.01f, confirmedDuration));
        if (view == null) view = Camera.main;
        foreach (var indicator in damageIndicators) indicator.Tick(view != null ? view.transform : null, health.transform.position);
    }

    private void ReadHealth(bool immediate)
    {
        healthTarget = Mathf.Clamp01((float)health.currentHealth.Value / Mathf.Max(1, health.maxHealth));
        healthText.text = health.currentHealth.Value + " / " + health.maxHealth;
        if (immediate) healthFill.fillAmount = healthTarget;
    }
    private void OnHealth(int previous, int current) => ReadHealth(!health.isDead.Value && previous == 0 && current > 0);
    private void OnDead(bool previous, bool current)
    {
        if (!current) ClearEffects();
        else
        {
            ResetWeaponPresentation();
            ClearDamage();
            if (!killMarker.activeSelf || Time.unscaledTime >= confirmedUntil)
            { confirmedUntil = 0; confirmation.alpha = 0; }
        }
        ReadHealth(!current); RefreshCombatVisibility(); hud?.RefreshLocal();
    }
    private void RefreshCombatVisibility()
    {
        bool finished = MatchResults.LocalFinal != null || MatchHud.ResultsVisible;
        combat.SetActive(!finished && !health.isDead.Value && MatchManager.CanControl(health));
        abilitySlot.SetActive(!finished && !health.isDead.Value);
    }
    private void OnRespawnTime(double previous, double current) => hud?.RefreshLocal();
    private void OnTeam(int previous, int current) => hud?.RefreshLocal();
    private void OnPrepared(bool previous, bool current) => hud?.RefreshLocal();
    private void ReadWeapon()
    {
        float remaining = shooter.CooldownRemaining;
        reload.fillAmount = HudPresentation.Reload(remaining, shooter.CooldownDuration);
        bool reloading = remaining > 0f;
        if (reload.gameObject.activeSelf != reloading) reload.gameObject.SetActive(reloading);
        if (reloading)
        {
            CancelWeaponReady();
            observedReload = true;
            observedReloadEndsAt = shooter.CooldownEndsAt;
        }
        else if (observedReload)
        {
            observedReload = false;
            // A cleared/replaced deadline is cancellation, even if input is ready.
            bool completed = shooter.CooldownEndsAt == observedReloadEndsAt && Time.time >= observedReloadEndsAt;
            if (completed && shooter.CanFire && !health.isDead.Value && combat.activeInHierarchy)
            {
                CancelWeaponReady();
                weaponReady.transform.localScale = readyRestScale * 0f;
                readyStartedAt = Time.unscaledTime;
                weaponReady.SetActive(true);
            }
            else CancelWeaponReady();
        }
    }
    private void TickWeaponReady()
    {
        if (readyStartedAt < 0f) return;
        float elapsed = Time.unscaledTime - readyStartedAt;
        if (elapsed >= ReadyDuration || !combat.activeInHierarchy) { CancelWeaponReady(); return; }
        // Same unscaled Update-driven approach as the other HUD effects:
        // scale from zero to the authored size for 0.5 seconds, then back to zero.
        float pulse = Mathf.Clamp01(elapsed < 0.5f ? elapsed * 2f : (ReadyDuration - elapsed) * 2f);
        pulse = pulse * pulse * (3f - 2f * pulse);
        weaponReady.transform.localScale = readyRestScale * pulse;
    }
    private void CancelWeaponReady()
    {
        readyStartedAt = -1f;
        if (weaponReady == null) return;
        weaponReady.SetActive(false);
        if (readyScaleCached) weaponReady.transform.localScale = readyRestScale;
    }
    private void ResetWeaponPresentation()
    {
        observedReload = false;
        if (reload != null) reload.gameObject.SetActive(false);
        CancelWeaponReady();
    }
    private void OnCombat(CombatFeedback feedback)
    {
        if (!bound || MatchResults.LocalFinal != null || MatchHud.ResultsVisible) return;
        if (!feedback.IsBase && feedback.VictimId == ClientId && feedback.HasSource && !health.isDead.Value)
            damageIndicators[nextIndicator++ % damageIndicators.Length].Show(feedback.Source);
        if (!feedback.HasAttacker || feedback.AttackerId != ClientId || (health.isDead.Value && !feedback.IsKill)) return;
        // A normal hit arriving just after a lethal one must not replace its stronger cue.
        if (!feedback.IsKill && killMarker.activeSelf && Time.unscaledTime < confirmedUntil) return;
        confirmedDuration = feedback.IsKill ? 1.2f : 0.25f;
        confirmedUntil = Time.unscaledTime + confirmedDuration;
        hitMarker.SetActive(!feedback.IsKill);
        killMarker.SetActive(feedback.IsKill);
        killText.text = feedback.IsKill ? "Eliminated " + feedback.VictimName : "";
    }
    private void ClearEffects()
    {
        ResetWeaponPresentation();
        confirmedUntil = 0;
        if (confirmation != null) confirmation.alpha = 0;
        ClearDamage();
    }
    private void ClearDamage()
    {
        if (lowHealth != null) lowHealth.alpha = 0;
        if (damageIndicators != null) foreach (var indicator in damageIndicators) if (indicator != null) indicator.Clear();
    }
    public void OnPlayerDespawned()
    {
        ResetWeaponPresentation();
        if (!bound) return;
        bound = false;
        health.currentHealth.OnValueChanged -= OnHealth; health.isDead.OnValueChanged -= OnDead;
        health.respawnAt.OnValueChanged -= OnRespawnTime; state.teamIndex.OnValueChanged -= OnTeam;
        state.isPrepared.OnValueChanged -= OnPrepared; shooter.WeaponStateChanged -= ReadWeapon;
        if (match != null) match.CombatConfirmed -= OnCombat;
        if (hud != null) hud.ReleasePlayer(this);
        match = null; hud = null;
        ClearEffects();
    }
    private void OnDisable() => OnPlayerDespawned();
}
