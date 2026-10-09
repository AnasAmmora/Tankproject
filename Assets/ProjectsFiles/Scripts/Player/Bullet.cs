using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour 
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private int damageAmount = 25;

    [Header("Arc / Trajectory")]
    [SerializeField] private float launchUpwardForce = 6f;

    [Header("Impact Settings")]
    [SerializeField] private float impactForce = 5000f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyTime = 2f;

    public GameObject ImpactEffectPrefab => impactEffectPrefab;

    private Rigidbody rb;
    private Collider col;
    private TankCameraShake ownerShake;
    private int attackerTeam = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
    }

    public void Launch(Vector3 direction, Collider[] ownerColliders, TankCameraShake shake, int teamIndex)
    {
        ownerShake = shake;
        attackerTeam = teamIndex;

        if (ownerColliders != null)
        {
            foreach (Collider ownerCol in ownerColliders)
            {
                if (ownerCol != null) Physics.IgnoreCollision(col, ownerCol, true);
            }
        }

        Vector3 velocity = direction.normalized * speed + Vector3.up * launchUpwardForce;
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        Invoke(nameof(DestroyBullet), maxLifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);

        if (ownerShake != null) ownerShake.ShakeOnHit();


        if (NetworkManager.Singleton.IsServer)
        {
            PlayerHealth hitHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (hitHealth != null) hitHealth.TakeDamage(damageAmount, attackerTeam);

            TeamBase hitBase = collision.gameObject.GetComponentInParent<TeamBase>();
            if (hitBase != null) hitBase.TakeDamage(damageAmount, attackerTeam);

            PlayerMovement hitTank = collision.gameObject.GetComponentInParent<PlayerMovement>();
            if (hitTank != null)
            {
                Vector3 pushForce = rb.linearVelocity.normalized * impactForce;
                hitTank.ApplyImpactClientRpc(pushForce);
            }
        }

        if (impactEffectPrefab != null)
        {
            GameObject effect = Instantiate(impactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(effect, effectDestroyTime);
        }

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        CancelInvoke(nameof(DestroyBullet));
        Destroy(gameObject); 
    }
}