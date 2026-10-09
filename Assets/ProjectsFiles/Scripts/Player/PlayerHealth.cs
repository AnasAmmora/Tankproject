using Unity.Netcode;
using UnityEngine;
using System;

[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : NetworkBehaviour
{
    public int maxHealth = 100;
    private PlayerState playerState;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDead.Value = false;
        }
    }

    public void TakeDamage(int damage, int attackerTeam)
    {
        if (!IsServer || isDead.Value) return;

        if (attackerTeam == playerState.teamIndex.Value) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }

    private void Die()
    {
        isDead.Value = true;
        Debug.Log($"<color=red>Player {playerState.playerName.Value} has died!</color>");
    }
}