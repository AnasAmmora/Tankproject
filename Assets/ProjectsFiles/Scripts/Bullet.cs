using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : NetworkBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float maxLifetime = 5f;

    [Header("Impact Settings")]
    [SerializeField] private float impactForce = 5000f;

    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyTime = 2f;

    public GameObject ImpactEffectPrefab => impactEffectPrefab;

    private Rigidbody rb;
    private Collider col;
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

        if (ownerColliders != null)
        {
            foreach (Collider ownerCol in ownerColliders)
            {
                if (ownerCol != null) Physics.IgnoreCollision(col, ownerCol, true);
            }
        }

        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Vector3.zero;

        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), maxLifetime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        ContactPoint contact = collision.GetContact(0);
        ownerShake?.ShakeOnHit();

        PlayerMovement hitTank = collision.gameObject.GetComponentInParent<PlayerMovement>();
        if (hitTank != null)
        {
            Vector3 pushForce = rb.linearVelocity.normalized * impactForce;

            hitTank.ApplyImpactClientRpc(pushForce);
        }

        TriggerImpactClientRpc(contact.point, contact.normal);

        DestroyBullet();
    }

    [ClientRpc]
    private void TriggerImpactClientRpc(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(effect, effectDestroyTime);
        }
    }

    private void DestroyBullet()
    {
        CancelInvoke(nameof(DestroyBullet));

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}