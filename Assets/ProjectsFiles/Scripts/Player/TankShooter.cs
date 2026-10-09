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

    private Collider[] ownerColliders;
    private TankCameraShake ownerShake;
    private float nextFireTime;

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

        SpawnBulletLocally(muzzlePoint.position, direction);
        ownerShake?.ShakeOnFire();

        FireServerRpc(muzzlePoint.position, direction);
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Vector3 aimDir)
    {
        if (!IsOwner)
        {
            SpawnBulletLocally(spawnPos, aimDir);
        }

        FireClientRpc(spawnPos, aimDir);
    }

    [ClientRpc]
    private void FireClientRpc(Vector3 spawnPos, Vector3 aimDir)
    {
        if (IsOwner) return;

        if (IsServer) return;

        SpawnBulletLocally(spawnPos, aimDir);
    }

    private void SpawnBulletLocally(Vector3 pos, Vector3 dir)
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

            bullet.Launch(dir, ownerColliders, IsOwner ? ownerShake : null, myTeamIndex);
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject effect = Instantiate(muzzleFlashPrefab, pos, muzzlePoint.rotation);
            Destroy(effect, muzzleFlashDestroyTime);
        }
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