using Unity.Netcode;
using UnityEngine;

// Lives on the player root. Only the authored UI child is hidden; network and
// gameplay components remain active. Retained results cover late player setup.
public class PlayerCanvasController : NetworkBehaviour
{
    [SerializeField] private Canvas playerCanvas;
    private bool configured;

    private void Awake()
    {
        configured = playerCanvas != null && playerCanvas.gameObject != gameObject &&
            playerCanvas.transform.IsChildOf(transform) &&
            playerCanvas.GetComponentsInChildren<NetworkObject>(true).Length == 0 &&
            playerCanvas.GetComponentsInChildren<NetworkBehaviour>(true).Length == 0;
        if (!configured)
        {
            Debug.LogError($"PlayerCanvasController on '{name}': 'playerCanvas' must reference a UI-only child Canvas, without NetworkObject/NetworkBehaviour components.", this);
            return;
        }
        playerCanvas.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn() => RefreshVisibility();

    public void RefreshVisibility()
    {
        if (!configured) return;
        bool visible = IsSpawned && IsOwner && MatchResults.LocalFinal == null && !MatchHud.ResultsVisible;
        if (playerCanvas.gameObject.activeSelf != visible) playerCanvas.gameObject.SetActive(visible);
    }

    private void LateUpdate() => RefreshVisibility();

    public override void OnNetworkDespawn()
    {
        if (configured) playerCanvas.gameObject.SetActive(false);
    }
}
