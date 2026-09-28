using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankShooter : NetworkBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference fireAction;

    [Header("Firing")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("Prefab الرصاصة")]
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

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        ownerColliders = GetComponentsInChildren<Collider>();
        ownerShake = GetComponent<TankCameraShake>();

        if (aimCamera == null) aimCamera = Camera.main;

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

        FireServerRpc(muzzlePoint.position, direction);

        ownerShake?.ShakeOnFire();
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 spawnPos, Vector3 aimDir)
    {
        Quaternion bulletRotation = Quaternion.LookRotation(aimDir);
        GameObject bulletInstance = Instantiate(bulletPrefab, spawnPos, bulletRotation);

        bulletInstance.GetComponent<NetworkObject>().Spawn();

        if (bulletInstance.TryGetComponent<Bullet>(out Bullet bullet))
        {
            bullet.Launch(aimDir, ownerColliders, ownerShake);
        }

        PlayEffectsClientRpc(spawnPos);
    }

    [ClientRpc]
    private void PlayEffectsClientRpc(Vector3 spawnPos)
    {
        if (muzzleFlashPrefab != null)
        {
            GameObject effect = Instantiate(muzzleFlashPrefab, spawnPos, muzzlePoint.rotation);
            Destroy(effect, muzzleFlashDestroyTime);
        }
    }

    private Vector3 GetAimDirection()
    {
        if (aimCamera == null) return muzzlePoint.forward;

        Ray screenCenterRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 targetPoint;

        if (Physics.Raycast(screenCenterRay, out RaycastHit hit, aimRayDistance, aimLayerMask, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = screenCenterRay.origin + screenCenterRay.direction * aimRayDistance;
        }

        return (targetPoint - muzzlePoint.position).normalized;
    }
}