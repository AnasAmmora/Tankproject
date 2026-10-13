using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(NetworkObject))]
public class TeamBase : NetworkBehaviour
{
    [Range(0, 1)] public int teamIndex;
    [Min(1)] public int maxHealth = 1000;
    [SerializeField] private GameObject destructionEffectPrefab;
    [SerializeField, Min(0.1f)] private float destructionEffectLifetime = 4f;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(1000);
    public NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false);
    public Action<int, int> OnHealthChanged;
    public Action OnBaseDestroyed;
    private bool effectsPlayed;
    public override void OnNetworkSpawn()
    {
        if (IsServer) { currentHealth.Value = maxHealth; isDestroyed.Value = false; }
        currentHealth.OnValueChanged += HealthChanged;
        isDestroyed.OnValueChanged += DestroyedChanged;
        if (isDestroyed.Value) TriggerDestructionEffects();
    }
    private void HealthChanged(int previous, int current) => OnHealthChanged?.Invoke(current, maxHealth);
    private void DestroyedChanged(bool previous, bool current) { if (current) TriggerDestructionEffects(); }
    public bool TakeDamage(int damage, int attackerTeam)
    {
        if (!IsServer || isDestroyed.Value || damage <= 0 || MatchManager.Instance == null ||
            !MatchManager.Instance.CanDamage(attackerTeam, teamIndex)) return false;
        currentHealth.Value = Mathf.Max(0, currentHealth.Value - damage);
        if (currentHealth.Value == 0) DestroyOnServer();
        return true;
    }
    public void DestroyOnServer()
    {
        if (!IsServer || isDestroyed.Value) return;
        // MatchManager retains this flag independently of this object's lifetime.
        MatchManager.Instance?.RecordBaseDestroyed(teamIndex);
        currentHealth.Value = 0;
        isDestroyed.Value = true;
    }
    private void TriggerDestructionEffects()
    {
        if (effectsPlayed) return;
        effectsPlayed = true;
        if (destructionEffectPrefab != null)
            Destroy(Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity), destructionEffectLifetime);
        OnBaseDestroyed?.Invoke();
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        // Keep the NetworkObject active so late replication and cleanup still run.
    }
    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HealthChanged;
        isDestroyed.OnValueChanged -= DestroyedChanged;
    }
}
