using Unity.Cinemachine;
using UnityEngine;

// PlayerLook presents its world-space camera target at order 100.
[DefaultExecutionOrder(200)]
public sealed class TankCameraUpdate : MonoBehaviour
{
    [SerializeField] private CinemachineBrain brain;
    [SerializeField] private CinemachineCamera rig;

    private void Awake()
    {
        if (brain == null) Debug.LogError("TankCameraUpdate: brain reference is required.", this);
        if (rig == null) Debug.LogError("TankCameraUpdate: rig reference is required.", this);
    }

    private void LateUpdate()
    {
        if (brain == null || rig == null || rig.Follow == null) return;
        var owner = rig.Follow.GetComponentInParent<PlayerLook>();
        if (owner == null || !owner.IsSpawned || !owner.IsOwner) return;
        // Keep the physics queries and camera on this frame's presented transforms.
        Physics.SyncTransforms();
        brain.ManualUpdate();
    }
}
