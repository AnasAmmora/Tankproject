using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankShooter : NetworkBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;

    [Header("Firing")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireCooldown = 0.4f;
    [Tooltip("Firing recoil on this tank's root. Queued immediately after a successful local owner launch.")]
    [SerializeField] private TankRecoil recoil;

    [Header("Aiming")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private float aimRayDistance = 200f;
    [SerializeField] private LayerMask aimLayerMask = ~0;

    [Header("Muzzle Effect")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private float muzzleFlashDestroyTime = 1.5f;

    [Header("Diagnostics (Editor / Development Build)")]
    [SerializeField] private bool logHitTiming;

    private Collider[] ownerColliders;
    private TankCameraShake ownerShake;
    private float nextFireTime;
    private uint nextShotId;
    private uint lastServerShot;
    private double nextServerFireAt;
    private uint lastRecoilShot;
    private bool fireSubscribed;
    public event Action WeaponStateChanged;
    public float CooldownDuration => fireCooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    // Expose the existing deadline so presentation can distinguish completion
    // from a reset/cancellation without maintaining a second reload timer.
    public float CooldownEndsAt => nextFireTime;
    public bool CanFire => IsSpawned && IsOwner && MatchManager.CanControl(GetComponent<PlayerHealth>()) && Time.time >= nextFireTime;

    public event Action ShotFired;
    public event Action EmptyFireAttempted;

    private void Awake()
    {
        ownerColliders = GetComponentsInChildren<Collider>();
        ownerShake = GetComponent<TankCameraShake>();
        if (recoil == null || recoil.gameObject != gameObject)
            Debug.LogError("TankShooter: recoil must reference TankRecoil on this tank's root.", this);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        nextFireTime = Time.time;

        if (fireAction != null && !fireSubscribed)
        {
            fireAction.action.Enable();
            fireAction.action.performed += OnFirePerformed;
            fireSubscribed = true;
        }

        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) aimCamera = FindAnyObjectByType<Camera>();
    }

    private void ClearFireSubscription()
    {
        if (fireAction != null && fireSubscribed)
        {
            fireAction.action.performed -= OnFirePerformed;
            fireAction.action.Disable();
            fireSubscribed = false;
        }
    }

    private void OnDisable() => ClearFireSubscription();
    public override void OnNetworkDespawn() => ClearFireSubscription();

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        TryFire();
    }

    private void TryFire()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (!MatchManager.CanControl(GetComponent<PlayerHealth>()))
            return;

        if (Time.time < nextFireTime)
        {
            EmptyFireAttempted?.Invoke();
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        WeaponStateChanged?.Invoke();

        Vector3 direction = GetAimDirection();
        uint shotId = ++nextShotId;
        // NGO stamps NetworkTransform states in this ServerTime tick domain.
        double firedAt = NetworkManager.ServerTime.Time;

        if (!SpawnBulletLocally(muzzlePoint.position, direction, shotId)) return;
        GetComponent<TankNetworkTransform>()?.WatchShot(shotId, firedAt, logHitTiming, true);
        Vector3 recoilImpulse = Vector3.zero;
        // Predict recoil alongside the owner's existing projectile/audio/flash.
        // Capture this shot's direction now; neither an echo nor a later turret
        // rotation may apply it again. Rejection cancels the projectile through
        // the existing result path; owner movement has no rollback/resimulation.
        if (shotId > lastRecoilShot)
        {
            lastRecoilShot = shotId;
            if (recoil != null && recoil.gameObject == gameObject) recoilImpulse = recoil.ApplyRecoil(direction, shotId);
        }
        ownerShake?.ShakeOnFire();

        FireServerRpc(muzzlePoint.position, direction, shotId, logHitTiming, firedAt, recoilImpulse);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Vector3 aimDir, uint shotId, bool diagnostics, double firedAt, Vector3 recoilImpulse)
    {
        if (shotId <= lastServerShot) return; // Never reject a duplicate of an already active shot.
        lastServerShot = shotId;
        if (!MatchManager.CanControl(GetComponent<PlayerHealth>()) ||
            NetworkManager.ServerTime.Time + 1d / Mathf.Max(1, NetworkManager.NetworkConfig.TickRate) < nextServerFireAt)
        {
            MatchManager match = MatchManager.Instance;
            if (match != null && match.TryResolveProjectile(NetworkObjectId, shotId))
                match.PublishProjectileResult(NetworkObjectId, shotId, ulong.MaxValue, false, spawnPos, Vector3.zero, diagnostics);
            return;
        }
        nextServerFireAt = NetworkManager.ServerTime.Time + fireCooldown;
        TraceShot(diagnostics, NetworkObjectId, shotId, ulong.MaxValue, "server-shot-received");
        if (!IsOwner)
        {
            if (!SpawnBulletLocally(spawnPos, aimDir, shotId, diagnostics)) return;
            PresentRemoteFiring(shotId, firedAt, recoilImpulse, diagnostics);
        }

        FireClientRpc(spawnPos, aimDir, shotId, diagnostics, firedAt, recoilImpulse);
    }

    [ClientRpc]
    private void FireClientRpc(Vector3 spawnPos, Vector3 aimDir, uint shotId, bool diagnostics, double firedAt, Vector3 recoilImpulse)
    {
        if (MatchResults.LocalFinal != null || (MatchManager.Instance != null && !MatchManager.Instance.Running)) return;
        // The owner already launched and queued recoil locally. Confirmation is
        // presentation for observers only, even if it arrives after a life reset.
        if (IsOwner) return;

        if (IsServer) return;

        if (SpawnBulletLocally(spawnPos, aimDir, shotId, diagnostics))
            PresentRemoteFiring(shotId, firedAt, recoilImpulse, diagnostics);
    }

    private void PresentRemoteFiring(uint shotId, double firedAt, Vector3 impulse, bool diagnostics)
    {
        if (!IsClient || IsOwner) return;
        GetComponent<TankNetworkTransform>()?.WatchShot(shotId, firedAt, diagnostics, false);
        GetComponent<PlayerMovement>()?.PresentFiringRecoil(impulse, firedAt);
    }

    private bool SpawnBulletLocally(Vector3 pos, Vector3 dir, uint shotId, bool? diagnostics = null)
    {
        MatchManager match = MatchManager.Instance;
        Bullet definition = bulletPrefab != null ? bulletPrefab.GetComponent<Bullet>() : null;
        if (match == null || definition == null)
        {
            Debug.LogError("TankShooter requires MatchManager and bulletPrefab with Bullet.", this);
            return false;
        }
        if (!match.Projectiles.Register(NetworkObjectId, shotId, definition.ImpactEffectPrefab,
            definition.EffectLifetime, IsOwner ? ownerShake : null)) return false;
        Quaternion bulletRotation = Quaternion.LookRotation(dir);
        GameObject bulletInstance = PoolManager.Get(bulletPrefab, pos, bulletRotation);

        if (bulletInstance == null || !bulletInstance.TryGetComponent<Bullet>(out Bullet bullet))
        {
            Debug.LogError("TankShooter: the projectile pool did not return an object with Bullet.", this);
            return false;
        }

        int myTeamIndex = -1;
        if (TryGetComponent<PlayerState>(out PlayerState pState))
            myTeamIndex = pState.teamIndex.Value;

        bool trace = diagnostics ?? logHitTiming;
        bullet.Launch(dir, ownerColliders, myTeamIndex, NetworkObjectId, shotId, trace);
        TraceShot(trace, NetworkObjectId, shotId, ulong.MaxValue, "projectile-launched");

        if (IsClient) ShotFired?.Invoke();

        if (muzzleFlashPrefab != null)
        {
            GameObject effect = Instantiate(muzzleFlashPrefab, pos, muzzlePoint.rotation);
            Destroy(effect, muzzleFlashDestroyTime);
        }
        return true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void TraceShot(bool enabled, ulong shooterId, uint shotId, ulong targetId,
        string stage, double confirmedAt = -1d, double elapsed = 0d)
    {
        if (!enabled || NetworkManager.Singleton == null) return;
        NetworkManager manager = NetworkManager.Singleton;
        // ServerTime is a buffered network timeline, not a wall-clock latency probe.
        // Compare confirmStamp across peers; elapsed is measured only on this peer.
        Debug.Log($"[HitTiming] node={manager.LocalClientId} shot={shooterId}:{shotId} " +
            $"target={targetId} stage={stage} timeline={manager.ServerTime.Time:F4} " +
            $"confirmStamp={confirmedAt:F4} localMark={Time.realtimeSinceStartupAsDouble:F4} " +
            $"elapsedHere={elapsed:F4}");
    }

    private Vector3 GetAimDirection()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) aimCamera = FindAnyObjectByType<Camera>();
        if (aimCamera == null) return muzzlePoint.forward;

        Ray screenCenterRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        RaycastHit[] hits = Physics.RaycastAll(screenCenterRay, aimRayDistance, aimLayerMask, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        Vector3 nearestPoint = default;
        foreach (var hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(transform) && hit.distance < nearest)
            {
                nearest = hit.distance;
                nearestPoint = hit.point;
            }
        }
        if (nearest < float.PositiveInfinity) return (nearestPoint - muzzlePoint.position).normalized;

        targetPoint = screenCenterRay.origin + screenCenterRay.direction * aimRayDistance;
        return (targetPoint - muzzlePoint.position).normalized;
    }
}
