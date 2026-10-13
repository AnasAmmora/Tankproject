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

    private void Awake()
    {
        ownerColliders = GetComponentsInChildren<Collider>();
        ownerShake = GetComponent<TankCameraShake>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (aimCamera == null) aimCamera = Camera.main;
        if (aimCamera == null) aimCamera = FindAnyObjectByType<Camera>();
    }

    private void OnEnable()
    {
        if (fireAction != null)
        {
            fireAction.action.Enable();
            fireAction.action.performed += OnFirePerformed;
        }
    }

    private void OnDisable()
    {
        if (fireAction != null)
        {
            fireAction.action.performed -= OnFirePerformed;
            fireAction.action.Disable();
        }
    }

    private void OnFirePerformed(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        Vector3 direction = GetAimDirection();
        uint shotId = ++nextShotId;

        SpawnBulletLocally(muzzlePoint.position, direction, shotId);
        ownerShake?.ShakeOnFire();

        FireServerRpc(muzzlePoint.position, direction, shotId, logHitTiming);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Vector3 aimDir, uint shotId, bool diagnostics)
    {
        TraceShot(diagnostics, NetworkObjectId, shotId, ulong.MaxValue, "server-shot-received");
        if (!IsOwner)
        {
            SpawnBulletLocally(spawnPos, aimDir, shotId, diagnostics);
        }

        FireClientRpc(spawnPos, aimDir, shotId, diagnostics);
    }

    [ClientRpc]
    private void FireClientRpc(Vector3 spawnPos, Vector3 aimDir, uint shotId, bool diagnostics)
    {
        if (IsOwner) return;

        if (IsServer) return;

        SpawnBulletLocally(spawnPos, aimDir, shotId, diagnostics);
    }

    private void SpawnBulletLocally(Vector3 pos, Vector3 dir, uint shotId, bool? diagnostics = null)
    {
        Quaternion bulletRotation = Quaternion.LookRotation(dir);
        GameObject bulletInstance = Instantiate(bulletPrefab, pos, bulletRotation);

        if (bulletInstance.TryGetComponent<Bullet>(out Bullet bullet))
        {
            int myTeamIndex = -1;
            if (TryGetComponent<PlayerState>(out PlayerState pState))
            {
                myTeamIndex = pState.teamIndex.Value;
            }

            bool trace = diagnostics ?? logHitTiming;
            bullet.Launch(dir, ownerColliders, IsOwner ? ownerShake : null, myTeamIndex,
                NetworkObjectId, shotId, trace);
            TraceShot(trace, NetworkObjectId, shotId, ulong.MaxValue, "projectile-launched");
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject effect = Instantiate(muzzleFlashPrefab, pos, muzzlePoint.rotation);
            Destroy(effect, muzzleFlashDestroyTime);
        }
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
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(transform))
            {
                targetPoint = hit.point;
                return (targetPoint - muzzlePoint.position).normalized;
            }
        }

        targetPoint = screenCenterRay.origin + screenCenterRay.direction * aimRayDistance;
        return (targetPoint - muzzlePoint.position).normalized;
    }
}
