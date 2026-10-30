using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 40f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private DamageFalloff damageFalloff;
    [Header("Arc / Trajectory")]
    [SerializeField] private float launchUpwardForce = 6f;
    [Header("Impact Settings")]
    [Tooltip("Impulse in Newton-seconds, applied only by the hit tank's owner.")]
    [SerializeField] private float impactForce = 50f;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private float effectDestroyTime = 2f;
    public GameObject ImpactEffectPrefab => impactEffectPrefab;
    public float EffectLifetime => effectDestroyTime;

    private Rigidbody rb;
    private Collider col;
    private ShotPresentation presentation;
    private Collider[] ignoredColliders;
    private int attackerTeam = -1;
    private bool launched;
    private bool impactHandled;
    private ulong shooterId;
    private uint shotId;
    private bool diagnostics;
    private Vector3 sourcePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        // Cosmetic projectiles must never transfer collision-solver forces.
        col.isTrigger = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        if (damageFalloff == null)
            Debug.LogError("Bullet: missing Inspector reference 'damageFalloff'.", this);
    }

    public void Launch(Vector3 direction, Collider[] ownerColliders, int teamIndex,
        ulong sourceId, uint sourceShotId, bool trace)
    {
        CancelInvoke(nameof(Expire));
        presentation = MatchManager.Instance?.Projectiles;
        attackerTeam = teamIndex;
        shooterId = sourceId;
        shotId = sourceShotId;
        diagnostics = trace;
        sourcePosition = transform.position;
        ignoredColliders = ownerColliders;
        impactHandled = false;
        launched = true;
        col.enabled = true;
        rb.position = transform.position;
        rb.rotation = transform.rotation;
        rb.linearVelocity = direction.normalized * speed + Vector3.up * launchUpwardForce;
        rb.angularVelocity = Vector3.zero;
        presentation?.Attach(shooterId, shotId, this);
        if (launched) Invoke(nameof(Expire), maxLifetime);
    }

    private bool CanHit(Collider other)
    {
        if (other == col || other.isTrigger) return false;
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player != null && (player.isDead.Value || player.NetworkObjectId == shooterId ||
            player.GetComponent<PlayerState>().teamIndex.Value == attackerTeam)) return false;
        TeamBase teamBase = other.GetComponentInParent<TeamBase>();
        if (teamBase != null && (teamBase.isDestroyed.Value || teamBase.teamIndex == attackerTeam)) return false;
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
        if (MatchResults.LocalFinal != null || (MatchManager.Instance != null && !MatchManager.Instance.Running))
        { DestroyBullet(); return; }
        Vector3 velocity = rb.linearVelocity;
        // Sweep the actual collider so fast trigger projectiles do not tunnel.
        Vector3 displacement = velocity * Time.fixedDeltaTime
            + Physics.gravity * (0.5f * Time.fixedDeltaTime * Time.fixedDeltaTime);
        if (displacement.sqrMagnitude < 0.000001f) return;
        RaycastHit[] hits = rb.SweepTestAll(displacement.normalized, displacement.magnitude,
            QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        RaycastHit closest = default;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance >= nearest || !CanHit(hit.collider)) continue;
            nearest = hit.distance;
            closest = hit;
        }
        if (nearest < float.PositiveInfinity)
            HandleImpact(closest.collider, closest.point, closest.normal, velocity);
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
        MatchManager match = MatchManager.Instance;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // The team is recovered from the server's retained shooter record,
            // including when that shooter has died or disconnected since firing.
            if (match == null || !match.TryGetShotTeam(shooterId, out attackerTeam)) { DestroyBullet(); return; }
            if (!CanHit(other)) return;
        }
        impactHandled = true;
        col.enabled = false;
        PlayerMovement tank = other.GetComponentInParent<PlayerMovement>();
        TankShooter.TraceShot(diagnostics, shooterId, shotId,
            tank != null ? tank.NetworkObjectId : ulong.MaxValue, "projectile-contact");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (!match.TryResolveProjectile(shooterId, shotId)) { DestroyBullet(); return; }
            int damage = damageFalloff != null ? damageFalloff.Calculate(sourcePosition, point) : 0;
            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            bool acceptedPlayerHit = health != null && health.TakeDamage(damage, attackerTeam, shooterId, sourcePosition, point);
            TeamBase teamBase = other.GetComponentInParent<TeamBase>();
            if (health == null && teamBase != null) teamBase.TakeDamage(damage, attackerTeam, shooterId, point);
            if (acceptedPlayerHit && !health.isDead.Value && tank != null) tank.ApplyHitImpulse(velocity.normalized * impactForce,
                shooterId, shotId, diagnostics);
            NetworkObject target = other.GetComponentInParent<NetworkObject>();
            // The local RPC also releases this instance. Do not read its fields afterwards.
            match.PublishProjectileResult(shooterId, shotId, target != null ? target.NetworkObjectId : ulong.MaxValue,
                true, point, normal, diagnostics);
            return;
        }
        // A cosmetic contact hides this flight only. It is not an explosion, hit
        // confirmation or force. The match-scoped record survives pool reuse.
        DestroyBullet();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Expire));
        presentation?.Detach(shooterId, shotId, this);
        presentation = null;
        launched = false;
        ignoredColliders = null;
        attackerTeam = -1;
        shooterId = ulong.MaxValue;
        shotId = 0;
        diagnostics = false;
        sourcePosition = Vector3.zero;
    }

    private void DestroyBullet()
    {
        if (!launched) return;
        CancelInvoke(nameof(Expire));
        launched = false;
        impactHandled = true;
        col.enabled = false;
        // Release also destroys instances that were not obtained from a pool.
        PoolManager.Release(gameObject);
    }

    public void ReleaseIfShot(ulong source, uint sequence)
    {
        if (launched && shooterId == source && shotId == sequence) DestroyBullet();
    }

    private void Expire()
    {
        if (!launched) return;
        MatchManager match = MatchManager.Instance;
        if (match != null && match.TryResolveProjectile(shooterId, shotId))
        {
            match.PublishProjectileResult(shooterId, shotId, ulong.MaxValue, false,
                rb.position, Vector3.zero, diagnostics);
            return;
        }
        DestroyBullet();
    }
}
