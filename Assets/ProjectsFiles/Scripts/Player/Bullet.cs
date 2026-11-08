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
    private double launchedAt;
    private bool confirmedImpactPending;
    private float confirmedFlightAge;
    private Vector3 confirmedPoint, confirmedNormal;
    private HostShotHistory hostHistory;
    private bool compensated;
    private uint hostGeneration;
    private double acceptedFire, targetView, simulatedAge;
    private Vector3 initialVelocity;
    private float trajectoryStep;
    private bool reportedHistoryGap;
    private bool reportedHistoryNear;
    private readonly RaycastHit[] timingHits = new RaycastHit[32];
    private readonly Collider[] timingOverlaps = new Collider[32];

    public void ConfigureHostCompensation(double fire, double view, HostShotHistory history)
    {
        hostHistory = history; compensated = true;
        // Life boundaries are permanent. A missing pose is not a life boundary:
        // fail closed for that segment, then try later segments in the same life.
        hostGeneration = view >= history.GenerationStartedAt ? history.Generation : uint.MaxValue;
        reportedHistoryGap = false;
        reportedHistoryNear = false;
        if (diagnostics)
            TankShooter.TraceShot(true, shooterId, shotId, history.NetworkObjectId,
                $"host-history-launch-eligible{hostGeneration == history.Generation}-sample{history.Sample(view, out _, out _)}-fire{fire:F4}-view{view:F4}");
        acceptedFire = fire; targetView = view; simulatedAge = 0;
        initialVelocity = rb.linearVelocity; trajectoryStep = Time.fixedDeltaTime;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true; rb.useGravity = false;
    }

    private Vector3 FlightPosition(double age)
    {
        int n = (int)System.Math.Floor(age / trajectoryStep);
        float remainder = (float)(age - n * (double)trajectoryStep);
        return sourcePosition + initialVelocity * (float)age + Physics.gravity *
            (trajectoryStep * trajectoryStep * n * (n + 1) * .5f + trajectoryStep * (n + 1) * remainder);
    }

    private void StepHostFlight()
    {
        double desired = System.Math.Min(maxLifetime, System.Math.Max(0, NetworkManager.Singleton.ServerTime.Time - acceptedFire));
        if (desired - simulatedAge > .5d) { Expire(); return; }
        var box = (BoxCollider)col;
        Vector3 extents = Vector3.Scale(box.size * .5f, transform.lossyScale);
        Quaternion rotation = rb.rotation;
        Vector3 offset = rotation * Vector3.Scale(box.center, transform.lossyScale);
        for (int step = 0; step < 80 && launched && simulatedAge < desired - .000001d; step++)
        {
            double endAge = System.Math.Min(desired, simulatedAge + .005d);
            Vector3 start = FlightPosition(simulatedAge) + offset, end = FlightPosition(endAge) + offset;
            Vector3 delta = end - start; float distance = delta.magnitude;
            if (diagnostics && hostHistory != null && !reportedHistoryNear &&
                hostHistory.Sample(targetView + simulatedAge, out Vector3 sampledHost, out _) &&
                (sampledHost - start).sqrMagnitude < 4f)
            {
                reportedHistoryNear = true;
                TankShooter.TraceShot(true, shooterId, shotId, hostHistory.NetworkObjectId,
                    $"host-history-near-query{targetView + simulatedAge:F4}-host{sampledHost:F4}-from{start:F4}-to{end:F4}-eligible{hostGeneration == hostHistory.Generation}");
            }
            Collider other = null; Vector3 point = default, normal = default; float fraction = float.PositiveInfinity;
            int count = Physics.OverlapBoxNonAlloc(start, extents, timingOverlaps, rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count == timingOverlaps.Length) { Expire(); return; }
            for (int i = 0; i < count; i++)
                if (CanHit(timingOverlaps[i]) && !IsHostCollider(timingOverlaps[i]))
                { other = timingOverlaps[i]; point = start; normal = -delta.normalized; fraction = 0; break; }
            if (distance > .000001f)
            {
                count = Physics.BoxCastNonAlloc(start, extents, delta / distance, timingHits, rotation, distance, ~0, QueryTriggerInteraction.Ignore);
                if (count == timingHits.Length) { Expire(); return; }
                for (int i = 0; i < count; i++)
                    if (timingHits[i].distance / distance < fraction && CanHit(timingHits[i].collider) &&
                        !IsHostCollider(timingHits[i].collider))
                    { other = timingHits[i].collider; point = timingHits[i].point; normal = timingHits[i].normal; fraction = timingHits[i].distance / distance; }
            }
            if (hostHistory != null && hostHistory.Cast(hostGeneration, start, end, extents, rotation,
                targetView + simulatedAge, targetView + endAge, gameObject.layer, out Collider candidate,
                out Vector3 contact, out Vector3 contactNormal, out float t) && t < fraction - .00001f && CanHit(candidate))
            { other = candidate; point = contact; normal = contactNormal; fraction = t; }
            if (diagnostics && hostHistory != null && !reportedHistoryGap &&
                !hostHistory.Sample(targetView + simulatedAge, out _, out _))
            {
                reportedHistoryGap = true;
                TankShooter.TraceShot(true, shooterId, shotId, hostHistory.NetworkObjectId,
                    $"host-history-gap-generation{hostGeneration == hostHistory.Generation}-query{targetView + simulatedAge:F4}-age{simulatedAge:F4}");
            }
            if (other != null)
            {
                simulatedAge += (endAge - simulatedAge) * fraction;
                Vector3 velocity = initialVelocity + Physics.gravity * trajectoryStep * (float)(System.Math.Floor(simulatedAge / trajectoryStep) + 1);
                HandleImpact(other, point, normal, velocity); return;
            }
            simulatedAge = endAge; rb.position = FlightPosition(simulatedAge);
        }
        if (launched && simulatedAge >= maxLifetime - .000001d) Expire();
    }
    private static bool IsHostCollider(Collider other)
    {
        var player = other.GetComponentInParent<PlayerHealth>();
        return player != null && player.OwnerClientId == Unity.Netcode.NetworkManager.ServerClientId;
    }

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
        confirmedImpactPending = false;
        hostHistory = null;
        compensated = false;
        rb.isKinematic = false; rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        launchedAt = Time.timeAsDouble;
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
        if (MatchResults.LocalFinal != null || (MatchManager.Instance != null && !MatchManager.Instance.PresentingGameplay))
        { DestroyBullet(); return; }
        if (compensated) { StepHostFlight(); return; }
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
        if (!launched || impactHandled || compensated || !CanHit(other)) return;
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
                true, point, normal, diagnostics, compensated ? 0f : (float)(Time.timeAsDouble - launchedAt));
            return;
        }
        // Present at this peer's visible contact, without waiting for the launch
        // request -> server flight -> result trip. Confirmation owns all gameplay.
        presentation?.PredictImpact(shooterId, shotId, point, normal);
        DestroyBullet();
    }

    public bool ConfirmImpactIfShot(ulong source, uint sequence, Vector3 point, Vector3 normal, float flightAge)
    {
        if (!launched || shooterId != source || shotId != sequence) return false;
        confirmedPoint = point;
        confirmedNormal = normal;
        confirmedFlightAge = Mathf.Clamp(flightAge, 0f, maxLifetime);
        confirmedImpactPending = true;
        // Same-peer flight duration, not buffered NGO ServerTime or RTT. Never
        // fast-forward the authoritative projectile, which would alter damage.
        if (impactHandled || Time.timeAsDouble - launchedAt >= confirmedFlightAge) CompleteConfirmedImpact();
        return true;
    }

    private void LateUpdate()
    {
        if (launched && confirmedImpactPending && Time.timeAsDouble - launchedAt >= confirmedFlightAge)
            CompleteConfirmedImpact();
    }

    private void CompleteConfirmedImpact()
    {
        // Fallback for a moving/despawned target no longer intersecting the local
        // flight. Snap its final contact to the confirmed point and consume once.
        impactHandled = true;
        col.enabled = false;
        transform.position = confirmedPoint;
        TankShooter.TraceShot(diagnostics, shooterId, shotId, ulong.MaxValue, "projectile-confirmed-contact");
        presentation?.PredictImpact(shooterId, shotId, confirmedPoint, confirmedNormal);
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
        launchedAt = 0d;
        hostHistory = null;
        compensated = false;
        confirmedImpactPending = false;
        confirmedPoint = confirmedNormal = Vector3.zero;
        confirmedFlightAge = 0f;
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
        if (confirmedImpactPending) { CompleteConfirmedImpact(); return; }
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
