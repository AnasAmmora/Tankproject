using UnityEngine;
using UnityEngine.InputSystem;

public class TankShooter : MonoBehaviour
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

    [Header("Pooling")]
    [SerializeField] private int bulletPoolPrewarm = 15;
    [SerializeField] private int muzzleEffectPoolPrewarm = 5;

    private Collider[] ownerColliders;
    private TankCameraShake ownerShake;
    private float nextFireTime;

    private void Awake()
    {
        ownerColliders = GetComponentsInChildren<Collider>();
        ownerShake = GetComponent<TankCameraShake>();

        if (aimCamera == null) aimCamera = Camera.main; 

        if (bulletPrefab != null)
        {
            PoolManager.Prewarm(bulletPrefab, bulletPoolPrewarm);

            Bullet bulletTemplate = bulletPrefab.GetComponent<Bullet>();
            if (bulletTemplate != null && bulletTemplate.ImpactEffectPrefab != null)
            {
                PoolManager.Prewarm(bulletTemplate.ImpactEffectPrefab, bulletPoolPrewarm);
            }
        }

        if (muzzleFlashPrefab != null) PoolManager.Prewarm(muzzleFlashPrefab, muzzleEffectPoolPrewarm);
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
        TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (bulletPrefab == null || muzzlePoint == null) return;

        nextFireTime = Time.time + fireCooldown;

        Vector3 direction = GetAimDirection();
        Quaternion bulletRotation = Quaternion.LookRotation(direction);

        GameObject bulletInstance = PoolManager.Get(bulletPrefab, muzzlePoint.position, bulletRotation);
        bulletInstance.GetComponent<Bullet>().Launch(direction, ownerColliders, ownerShake);

        if (muzzleFlashPrefab != null)
        {
            PoolManager.Get(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        ownerShake?.ShakeOnFire();
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