using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeammateRow : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private Image healthFill;
    private MatchRules.PlayerRecord record;
    private PlayerHealth health;
    public ulong ClientId => record.clientId;
    public bool ValidateReferences()
    {
        if (playerName != null && healthText != null && lifeText != null && healthFill != null) return true;
        Debug.LogError($"TeammateRow on '{name}': missing Inspector reference '{(playerName == null ? nameof(playerName) : healthText == null ? nameof(healthText) : lifeText == null ? nameof(lifeText) : nameof(healthFill))}'.", this);
        return false;
    }
    public void Bind(MatchRules.PlayerRecord value, PlayerHealth source)
    {
        record = value;
        playerName.text = value.name;
        if (health != source)
        {
            Unbind(); health = source;
            if (health != null) health.currentHealth.OnValueChanged += OnHealth;
        }
        RefreshHealth();
    }
    public void Tick(MatchRules.Stage phase, int bases, double now)
    {
        bool available = health != null && health.IsSpawned && !record.disconnected;
        string value = HudPresentation.Life(phase, true, available ? health.isDead.Value : !record.alive,
            available ? health.respawnAt.Value : record.respawnAt, record.team, bases, now, record.disconnected);
        if (lifeText.text != value) lifeText.text = value;
        if (!available) { healthText.text = "—"; healthFill.fillAmount = 0; }
    }
    private void OnHealth(int previous, int current) => RefreshHealth();
    private void RefreshHealth()
    {
        if (health == null || record.disconnected) { healthText.text = "—"; healthFill.fillAmount = 0; return; }
        healthText.text = health.currentHealth.Value + " / " + health.maxHealth;
        healthFill.fillAmount = Mathf.Clamp01((float)health.currentHealth.Value / Mathf.Max(1, health.maxHealth));
    }
    private void Unbind() { if (health != null) health.currentHealth.OnValueChanged -= OnHealth; health = null; }
    private void OnDestroy() => Unbind();
}
