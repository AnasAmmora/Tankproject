using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : NetworkBehaviour
{
    public static event System.Action<PlayerHealth> Spawned;
    [Min(1)] public int maxHealth = 100;
    private PlayerState playerState;
    private Collider[] colliders;
    private Renderer[] renderers;
    private bool[] colliderDefaults;
    private bool[] rendererDefaults;
    public bool isPreparedForLife => playerState != null && playerState.isPrepared.Value;
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100);
    public NetworkVariable<bool> isDead = new NetworkVariable<bool>(false);
    public NetworkVariable<double> respawnAt = new NetworkVariable<double>(-1);
    private uint returnLife;
    private bool returnAcknowledged;
    public bool HasReturnAcknowledgement(uint life) => returnAcknowledged && returnLife == life;

    private void Awake()
    {
        playerState = GetComponent<PlayerState>();
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        colliderDefaults = System.Array.ConvertAll(colliders, c => c.enabled);
        rendererDefaults = System.Array.ConvertAll(renderers, r => r.enabled);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer) { currentHealth.Value = maxHealth; isDead.Value = false; respawnAt.Value = -1; }
        isDead.OnValueChanged += OnDeadChanged;
        ApplyLifePresentation(isDead.Value);
        Spawned?.Invoke(this);
    }
    public bool TakeDamage(int damage, int attackerTeam, ulong attackerObject = ulong.MaxValue,
        Vector3? sourcePosition = null, Vector3? impactPosition = null)
    {
        if (!IsServer || isDead.Value || currentHealth.Value <= 0 || damage <= 0 || MatchManager.Instance == null ||
            !MatchManager.Instance.CanDamage(attackerTeam, playerState.teamIndex.Value)) return false;
        int appliedDamage = Mathf.Min(currentHealth.Value, damage);
        currentHealth.Value -= appliedDamage;
        if (currentHealth.Value == 0)
        {
            isDead.Value = true;
            MatchManager.Instance.RecordDeath(this, attackerObject, sourcePosition, appliedDamage, impactPosition);
        }
        else MatchManager.Instance.NotifyPlayerDamage(this, attackerObject, sourcePosition, appliedDamage, impactPosition);
        return true;
    }
    private void OnDeadChanged(bool previous, bool current) => ApplyLifePresentation(current);
    private void ApplyLifePresentation(bool dead)
    {
        for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = !dead && colliderDefaults[i];
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].enabled = !dead && rendererDefaults[i];
        GetComponent<PlayerMovement>()?.ClearLifeMotion();
        GetComponent<PlayerLook>()?.ResetPresentation();
    }
    public void BeginReturn(Vector3 position, Quaternion rotation, uint life)
    {
        if (!IsServer || !isDead.Value) return;
        returnLife = life;
        returnAcknowledged = false;
        RelocateRpc(position, rotation, life);
    }
    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void RelocateRpc(Vector3 position, Quaternion rotation, uint life)
    {
        if (!IsOwner || !isDead.Value) return;
        // Only the existing movement authority writes the root. Dead input remains
        // locked until the server validates the acknowledgement and revives us.
        GetComponent<PlayerMovement>().RelocateForReturn(position, rotation);
        GetComponent<PlayerLook>()?.ResetAfterReturn();
        ReturnAppliedRpc(life);
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReturnAppliedRpc(uint life, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId || life != returnLife || !isDead.Value) return;
        // The RPC can arrive before the owner's NetworkTransform state. The
        // manager waits for that state before making the tank visible/alive.
        returnAcknowledged = true;
    }
    public void RestoreOnServer()
    {
        if (!IsServer) return;
        returnAcknowledged = false;
        currentHealth.Value = maxHealth;
        respawnAt.Value = -1;
        isDead.Value = false;
    }
    public override void OnNetworkDespawn()
    {
        isDead.OnValueChanged -= OnDeadChanged;
    }
}
