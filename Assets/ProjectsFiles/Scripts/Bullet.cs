using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 40f;

    [SerializeField] private float maxLifetime = 5f;

    [Header("Impact")]
    [SerializeField] private GameObject impactEffectPrefab;

    public GameObject ImpactEffectPrefab => impactEffectPrefab;

    private Rigidbody rb;
    private Collider col;
    private Collider[] ignoredOwnerColliders;
    private TankCameraShake ownerShake;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; 
    }

    public void Launch(Vector3 direction, Collider[] ownerColliders, TankCameraShake shake)
    {
        ownerShake = shake;
        ignoredOwnerColliders = ownerColliders;

        if (ignoredOwnerColliders != null)
        {
            foreach (Collider ownerCol in ignoredOwnerColliders)
            {
                if (ownerCol != null) Physics.IgnoreCollision(col, ownerCol, true);
            }
        }

        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Vector3.zero;

        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        SpawnImpactEffect(contact.point, contact.normal);
        ownerShake?.ShakeOnHit();
        ReturnToPool();
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab == null) return;
        PoolManager.Get(impactEffectPrefab, position, Quaternion.LookRotation(normal));
    }

    private void ReturnToPool()
    {
        CancelInvoke(nameof(ReturnToPool));

        if (ignoredOwnerColliders != null)
        {
            foreach (Collider ownerCol in ignoredOwnerColliders)
            {
                if (ownerCol != null) Physics.IgnoreCollision(col, ownerCol, false);
            }
        }

        ignoredOwnerColliders = null;
        ownerShake = null;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        PoolManager.Release(gameObject);
    }
}