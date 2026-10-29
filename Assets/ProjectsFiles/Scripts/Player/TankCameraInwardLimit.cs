using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

// Only limits inward travel on the stock rig segment. No alternate positions,
// retained world-space position, new damping, or changes to camera orientation.
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineThirdPersonFollow))]
public sealed class TankCameraInwardLimit : CinemachineExtension
{
    [SerializeField, Min(0f)] private float nearPlaneMargin = 0.01f;
    public bool HasClearanceConflict { get; private set; }

    private CinemachineThirdPersonFollow follow;
    private Transform cachedOwner;
    private MeshRenderer[] meshes;
    private MeshVolume[] volumes;
    private float savedRadius;
    private bool floorProbeAdjusted;

    public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase vcam,
        ref CameraState state, float deltaTime)
    {
        RestoreProbeRadius();
        if (vcam.Follow == null) return;
        var owner = vcam.Follow.GetComponentInParent<PlayerLook>();
        if (owner == null || !owner.IsSpawned || !owner.IsOwner) return;
        PrepareFloorProbe(vcam.Follow.position, state.Lens.NearClipPlane, state.Lens.FieldOfView);
    }

    // The tank's target is only 14.5cm above its support surface. Cinemachine's
    // 21cm hand probe starts penetrating a box deck and reports a false obstacle
    // even when travelling upwards out of it. Use the full near-plane volume
    // for this frame's casts only when that volume already clears the floor.
    internal void PrepareFloorProbe(Vector3 root, float near, float fieldOfView)
    {
        if (follow == null) follow = GetComponent<CinemachineThirdPersonFollow>();
        if (!follow.AvoidObstacles.Enabled) return;
        var camera = Camera.main;
        float aspect = camera != null ? camera.aspect : 16f / 9f;
        float h = near * Mathf.Tan(fieldOfView * Mathf.Deg2Rad * 0.5f);
        float required = Mathf.Sqrt(near * near + h * h * (1 + aspect * aspect)) + nearPlaneMargin;
        float preferred = follow.AvoidObstacles.CameraRadius;
        if (required >= preferred) return;
        RaycastHit support;
        int mask = follow.AvoidObstacles.CollisionFilter;
        if (!Physics.Raycast(root, Vector3.down, out support, preferred * 1.05f, mask,
            QueryTriggerInteraction.Ignore) || support.normal.y < 0.75f || support.distance * support.normal.y <= required * 1.05f) return;
        // Deck and terrain can overlap at an entrance. Accept multiple support
        // surfaces below the probe, but never an overlapping wall or railing.
        foreach (var col in Physics.OverlapSphere(root, preferred * 1.05f, mask, QueryTriggerInteraction.Ignore))
        {
            // TerrainCollider does not support Collider.ClosestPoint.
            if (col is TerrainCollider)
            {
                RaycastHit terrainHit;
                if (!col.Raycast(new Ray(root, Vector3.down), out terrainHit, preferred * 1.4f)
                    || terrainHit.normal.y < 0.75f || terrainHit.distance * terrainHit.normal.y <= required * 1.05f) return;
                continue;
            }
            if (!(col is BoxCollider || col is SphereCollider || col is CapsuleCollider)) return;
            Vector3 separation = root - col.ClosestPoint(root);
            if (separation.y <= required * 1.05f) return;
        }
        savedRadius = preferred;
        floorProbeAdjusted = true;
        var settings = follow.AvoidObstacles;
        settings.CameraRadius = required;
        follow.AvoidObstacles = settings;
    }

    internal void RestoreProbeRadius()
    {
        if (!floorProbeAdjusted || follow == null) return;
        var settings = follow.AvoidObstacles;
        settings.CameraRadius = savedRadius;
        follow.AvoidObstacles = settings;
        floorProbeAdjusted = false;
    }

    private void OnDisable() => RestoreProbeRadius();

    private struct MeshVolume
    {
        public Bounds Bounds;
        public Matrix4x4 LocalToWorld, WorldToLocal;
        public bool Active;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage == CinemachineCore.Stage.Body || stage == CinemachineCore.Stage.Finalize)
            RestoreProbeRadius();
        if (stage != CinemachineCore.Stage.Finalize || vcam.Follow == null) return;
        var owner = vcam.Follow.GetComponentInParent<PlayerLook>();
        if (owner == null || !owner.IsSpawned || !owner.IsOwner) return;
        if (follow == null) follow = GetComponent<CinemachineThirdPersonFollow>();

        // GetRigPositions also returns the original shoulder when its hand is
        // collision-corrected. Reconstruct this rig's normal (zero-damping) pose.
        var obstacle = follow.CurrentObstacle;
        follow.GetRigPositions(out _, out var shoulder, out _);
        follow.CurrentObstacle = obstacle;
        Vector3 normal = shoulder + vcam.Follow.up * follow.VerticalArmLength
            - vcam.Follow.forward * follow.CameraDistance + state.PositionCorrection;
        var camera = Camera.main;
        float aspect = camera != null ? camera.aspect : 16f / 9f;
        float near = state.Lens.NearClipPlane;
        float height = near * Mathf.Tan(state.Lens.FieldOfView * Mathf.Deg2Rad * 0.5f);
        // This radius encloses the eye and all near-plane corners. Cinemachine
        // retains its larger preferred obstacle padding during normal operation.
        float ownerRadius = Mathf.Sqrt(near * near + height * height * (1 + aspect * aspect)) + nearPlaneMargin;
        Vector3 corrected = state.GetFinalPosition();
        Vector3 limited = Limit(owner.transform, normal, corrected, ownerRadius,
            follow.AvoidObstacles.CollisionFilter);
        state.PositionCorrection += limited - corrected;
    }

    // Also exercised directly by isolated tests using the actual tank meshes.
    internal Vector3 Limit(Transform owner, Vector3 normal, Vector3 corrected,
        float ownerRadius, int obstacleMask)
    {
        HasClearanceConflict = false;
        float length = Vector3.Distance(normal, corrected);
        if (length < 0.00001f) return corrected;
        if (owner != cachedOwner)
        {
            cachedOwner = owner;
            var list = new List<MeshRenderer>();
            foreach (var mesh in owner.GetComponentsInChildren<MeshRenderer>(true))
                if (mesh.GetComponentInParent<Canvas>() == null) list.Add(mesh);
            meshes = list.ToArray();
            volumes = new MeshVolume[meshes.Length];
        }
        for (int i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            volumes[i].Active = mesh != null && mesh.enabled && mesh.gameObject.activeInHierarchy;
            if (!volumes[i].Active) continue;
            volumes[i].Bounds = mesh.localBounds;
            volumes[i].LocalToWorld = mesh.localToWorldMatrix;
            volumes[i].WorldToLocal = mesh.worldToLocalMatrix;
        }
        if (!OwnerClear(normal, ownerRadius))
        {
            // No increased follow distance or off-path escape if the normal
            // endpoint itself cannot accommodate this geometry.
            HasClearanceConflict = true;
            return corrected;
        }

        // Walk inward from the normal pose: stop at the FIRST tank entry, even
        // if a more compressed endpoint has emerged through the other side.
        // 5mm steps are smaller than the 10mm conservative near-plane margin.
        int steps = Mathf.Max(1, Mathf.CeilToInt(length / 0.005f));
        float outside = 0;
        for (int i = 1; i <= steps; i++)
        {
            float inside = i / (float)steps;
            if (OwnerClear(Vector3.Lerp(normal, corrected, inside), ownerRadius))
            {
                outside = inside;
                continue;
            }
            for (int j = 0; j < 10; j++)
            {
                float mid = (outside + inside) * 0.5f;
                if (OwnerClear(Vector3.Lerp(normal, corrected, mid), ownerRadius)) outside = mid;
                else inside = mid;
            }
            Vector3 candidate = Vector3.Lerp(normal, corrected, outside);
            Vector3 delta = candidate - corrected;
            float distance = delta.magnitude;
            // The full eye/near-plane volume must fit both constraints. The
            // stock solver's extra 0.2 padding is a preference, not solid space.
            float radius = Mathf.Max(0.001f, ownerRadius - 0.001f);
            if (Physics.CheckSphere(candidate, radius, obstacleMask, QueryTriggerInteraction.Ignore)
                || (distance > 0.00001f && Physics.SphereCast(corrected, radius, delta / distance,
                    out _, distance, obstacleMask, QueryTriggerInteraction.Ignore)))
            {
                // The two constraints cannot both fit on the current path.
                // Preserve the earlier obstacle result instead of entering a wall.
                HasClearanceConflict = true;
                return corrected;
            }
            return candidate;
        }
        return corrected;
    }

    private bool OwnerClear(Vector3 point, float radius)
    {
        foreach (var volume in volumes)
        {
            if (!volume.Active) continue;
            Vector3 local = volume.WorldToLocal.MultiplyPoint3x4(point);
            Vector3 closest = volume.LocalToWorld.MultiplyPoint3x4(volume.Bounds.ClosestPoint(local));
            if ((closest - point).sqrMagnitude < radius * radius) return false;
        }
        return true;
    }
}
