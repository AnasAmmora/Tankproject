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
    [Tooltip("Impulse in Newton-seconds, applied only by the hit tank's owner.")]
    [SerializeField] private float impactForce = 50f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyTime = 2f;
    public GameObject ImpactEffectPrefab => impactEffectPrefab;

    private Rigidbody rb;
    private Collider col;
    private TankCameraShake ownerShake;
    private Collider[] ignoredColliders;
    private int attackerTeam = -1;
    private bool launched;
    private bool impactHandled;
    private ulong shooterId;
    private uint shotId;
    private bool diagnostics;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        // Cosmetic projectiles must never transfer collision-solver forces.
        col.isTrigger = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
    }

    public void Launch(Vector3 direction, Collider[] ownerColliders, TankCameraShake shake, int teamIndex,
        ulong sourceId, uint sourceShotId, bool trace)
    {
        CancelInvoke(nameof(DestroyBullet));
        ownerShake = shake;
        attackerTeam = teamIndex;
        shooterId = sourceId;
        shotId = sourceShotId;
        diagnostics = trace;
        ignoredColliders = ownerColliders;
        impactHandled = false;
        launched = true;
        col.enabled = true;
        rb.linearVelocity = direction.normalized * speed + Vector3.up * launchUpwardForce;
        rb.angularVelocity = Vector3.zero;
        Invoke(nameof(DestroyBullet), maxLifetime);
    }

    private bool CanHit(Collider other)
    {
        if (other == col || other.isTrigger) return false;
        if (ignoredColliders != null)
        {
            foreach (Collider ownerCollider in ignoredColliders)
                if (other == ownerCollider) return false;
        }
        return !Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer);
    }

    private void FixedUpdate()
    {
        if (!launched || impactHandled) return;
        Vector3 velocity = rb.linearVelocity;
        // Sweep the actual collider so fast trigger projectiles do not tunnel.
        Vector3 displacement = velocity * Time.fixedDeltaTime
            + Physics.gravity * (0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime);
        if (displacement.sqrMagnitude < 0.000001f) return;
        RaycastHit[] hits = rb.SweepTestAll(displacement.normalized, displacement.magnitude,
            QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (!CanHit(hit.collider)) continue;
            HandleImpact(hit.collider, hit.point, hit.normal, velocity);
            break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || impactHandled || !CanHit(other)) return;
        Vector3 point = other.ClosestPoint(rb.position);
        Vector3 normal = rb.position - point;
        if (normal.sqrMagnitude < 0.000001f) normal = -rb.linearVelocity;
        HandleImpact(other, point, normal.normalized, rb.linearVelocity);
    }

    private void HandleImpact(Collider other, Vector3 point, Vector3 normal, Vector3 velocity)
    {
        if (impactHandled) return;
        impactHandled = true;
        col.enabled = false;
        ownerShake?.ShakeOnHit();
        PlayerMovement tank = other.GetComponentInParent<PlayerMovement>();
        TankShooter.TraceShot(diagnostics, shooterId, shotId,
            tank != null ? tank.NetworkObjectId : ulong.MaxValue, "projectile-contact");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(damageAmount, attackerTeam);
            TeamBase teamBase = other.GetComponentInParent<TeamBase>();
            if (teamBase != null) teamBase.TakeDamage(damageAmount, attackerTeam);
            if (tank != null) tank.ApplyHitImpulse(velocity.normalized * impactForce,
                shooterId, shotId, diagnostics);
        }

        if (impactEffectPrefab != null)
        {
            Quaternion rotation = normal.sqrMagnitude > 0f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject effect = Instantiate(impactEffectPrefab, point, rotation);
            Destroy(effect, effectDestroyTime);
        }
        DestroyBullet();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DestroyBullet));
        launched = false;
        ignoredColliders = null;
        ownerShake = null;
    }

    private void DestroyBullet()
    {
        CancelInvoke(nameof(DestroyBullet));
        launched = false;
        impactHandled = true;
        col.enabled = false;
        // Release also destroys instances that were not obtained from a pool.
        PoolManager.Release(gameObject);
    }
}
