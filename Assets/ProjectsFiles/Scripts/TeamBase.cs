using Unity.Netcode;
using UnityEngine;
using System;

public class TeamBase : NetworkBehaviour
{
    [Header("Base Settings")]
    public int teamIndex;
    public int maxHealth = 1000;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public Action<int, int> OnHealthChanged;
    public Action OnBaseDestroyed;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDestroyed.Value = false;
        }

        currentHealth.OnValueChanged += (oldValue, newValue) =>
        {
            OnHealthChanged?.Invoke(newValue, maxHealth);
        };

        isDestroyed.OnValueChanged += (oldValue, newValue) =>
        {
            if (newValue && !oldValue)
            {
                TriggerDestructionEffects();
            }
        };
    }

    public void TakeDamage(int damage, int attackerTeam)
    {
        if (!IsServer || isDestroyed.Value) return;

        if (attackerTeam == teamIndex) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            isDestroyed.Value = true;

            Debug.Log($"<color=red>Base for Team {teamIndex} has been DESTROYED!</color>");
        }
    }

    private void TriggerDestructionEffects()
    {
        OnBaseDestroyed?.Invoke();

        gameObject.SetActive(false);
    }
}