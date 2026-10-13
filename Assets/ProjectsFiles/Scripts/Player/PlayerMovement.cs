using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 120f;
    [Min(0.01f)] public float driveAcceleration = 12f;

    [Header("Confirmed hit presentation (non-owners only)")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private bool anticipateConfirmedHits = true;
    [SerializeField, Min(0f)] private float maxVisualOffset = 0.5f;
    [SerializeField, Min(0.01f)] private float visualReconcileTime = 0.15f;
    private Rigidbody rb;
    private TankInputHandler inputHandler;
    private Vector3 pendingImpulse;
    private RigidbodyConstraints drivingConstraints;
    private bool preparationLocked;
    private readonly ConfirmedHitVisualPrediction visualPrediction = new ConfirmedHitVisualPrediction();
    private readonly HashSet<(ulong, uint)> receivedHits = new HashSet<(ulong, uint)>();
    private readonly Queue<(ulong, uint)> hitHistory = new Queue<(ulong, uint)>();
    private readonly List<HitTrace> pendingHitTraces = new List<HitTrace>();
    private Vector3 modelLocalPosition;
    private Vector3 previousRootPosition;
    private Vector3 observedRootVelocity;
    private NetworkTransform networkTransform;
    private HitTrace lastHitTrace;
    private Vector3 authorityPositionAtHit;
    private Vector3 displayedRootAtHit;
    private bool awaitingAuthorityState;
    private bool awaitingDisplayedMovement;
    private bool visualStartLogged;

    private struct HitTrace
    {
        public ulong ShooterId;
        public uint ShotId;
        public bool Enabled;
        public double ConfirmedAt;
        public double ReceivedAt;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputHandler = GetComponent<TankInputHandler>();
        drivingConstraints = rb.constraints;
        networkTransform = GetComponent<NetworkTransform>();
        // Refuse presentation targets containing colliders or network/physics bodies.
        if (visualModel != null && (visualModel == transform ||
            !visualModel.IsChildOf(transform) || visualModel.GetComponentInChildren<Collider>(true) != null ||
            visualModel.GetComponentInChildren<Rigidbody>(true) != null ||
            visualModel.GetComponentInChildren<NetworkObject>(true) != null))
        {
            Debug.LogError("Hit presentation requires a model hierarchy without physics/network components.", this);
            visualModel = null;
        }
        if (visualModel != null) modelLocalPosition = visualModel.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        pendingImpulse = Vector3.zero;
        receivedHits.Clear();
        hitHistory.Clear();
        pendingHitTraces.Clear();
        ResetPresentation();
        if (IsOwner) SetPreparationLock(!MatchManager.CanControl(GetComponent<PlayerHealth>()));
    }

    // The server confirms a hit; only the movement authority simulates its impulse.
    public void ApplyHitImpulse(Vector3 impulse, ulong shooterId, uint shotId, bool diagnostics)
    {
        if (!IsServer || !IsSpawned) return;
        double confirmedAt = NetworkManager.ServerTime.Time;
        TankShooter.TraceShot(diagnostics, shooterId, shotId, NetworkObjectId,
            "server-hit-confirmed", confirmedAt);
        float ownerRtt = OwnerClientId == NetworkManager.ServerClientId ? 0f :
            NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) * 0.001f;
        float tickBudget = 2f / Mathf.Max(1f, NetworkManager.NetworkConfig.TickRate);
        float interpolationBudget = networkTransform != null && networkTransform.Interpolate ?
            networkTransform.PositionMaxInterpolationTime : 0f;
        // The event skips the owner->server return trip needed by movement updates.
        float visualLead = Mathf.Clamp(ownerRtt + tickBudget + interpolationBudget, 0.05f, 0.6f);
        ConfirmHitRpc(impulse, shooterId, shotId, confirmedAt, visualLead, diagnostics);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ConfirmHitRpc(Vector3 impulse, ulong shooterId, uint shotId,
        double confirmedAt, float visualLead, bool diagnostics)
    {
        if (!IsSpawned || !MatchManager.CanControl(GetComponent<PlayerHealth>()) ||
            !receivedHits.Add((shooterId, shotId))) return;
        hitHistory.Enqueue((shooterId, shotId));
        if (hitHistory.Count > 128) receivedHits.Remove(hitHistory.Dequeue());
        HitTrace trace = new HitTrace { ShooterId = shooterId, ShotId = shotId,
            Enabled = diagnostics, ConfirmedAt = confirmedAt, ReceivedAt = Time.realtimeSinceStartupAsDouble };
        TankShooter.TraceShot(diagnostics, shooterId, shotId, NetworkObjectId,
            "hit-confirmation-received", confirmedAt);
        if (IsOwner)
        {
            pendingImpulse += impulse;
            if (diagnostics) pendingHitTraces.Add(trace);
            return;
        }

        lastHitTrace = trace;
        authorityPositionAtHit = networkTransform != null ? networkTransform.GetSpaceRelativePosition(true) : transform.position;
        displayedRootAtHit = transform.position;
        awaitingAuthorityState = awaitingDisplayedMovement = diagnostics;
        if (anticipateConfirmedHits && visualModel != null)
        {
            visualPrediction.Begin(transform.position, observedRootVelocity, impulse / rb.mass, visualLead);
            visualStartLogged = false;
            TraceHit(trace, "confirmed-visual-scheduled");
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || rb.isKinematic) return;
        SetPreparationLock(!MatchManager.CanControl(GetComponent<PlayerHealth>()));
        if (preparationLocked) return;

        // Accelerate toward drive speed without overwriting the hit velocity.
        // The motor brakes knockback gradually; gravity remains independent.
        Vector3 velocity = rb.linearVelocity;
        Vector3 planarVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 targetVelocity = forward * (inputHandler.MoveInput.y * moveSpeed);
        Vector3 nextVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity,
            driveAcceleration * Time.fixedDeltaTime);
        rb.AddForce(nextVelocity - planarVelocity, ForceMode.VelocityChange);

        if (pendingImpulse != Vector3.zero)
        {
            rb.AddForce(pendingImpulse, ForceMode.Impulse);
            pendingImpulse = Vector3.zero;
            foreach (HitTrace trace in pendingHitTraces) TraceHit(trace, "owner-impulse-applied");
            pendingHitTraces.Clear();
        }

        float turnAmount = inputHandler.MoveInput.x * rotationSpeed * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
    }

    private void SetPreparationLock(bool locked)
    {
        if (preparationLocked == locked) return;
        preparationLocked = locked;
        rb.constraints = locked ? RigidbodyConstraints.FreezeAll : drivingConstraints;
        if (locked)
        {
            rb.linearVelocity = rb.angularVelocity = Vector3.zero;
            pendingImpulse = Vector3.zero;
        }
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;
        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            Vector3 sample = (transform.position - previousRootPosition) / dt;
            observedRootVelocity = Vector3.Lerp(observedRootVelocity, sample, 1f - Mathf.Exp(-dt / 0.1f));
        }
        previousRootPosition = transform.position;
        if (IsOwner) { ResetPresentation(); return; }

        if (awaitingAuthorityState && networkTransform != null &&
            (networkTransform.GetSpaceRelativePosition(true) - authorityPositionAtHit).sqrMagnitude > 0.000025f)
        {
            // This is a state-change sample, not proof that the packet contains this hit.
            TraceHit(lastHitTrace, "authority-position-changed");
            awaitingAuthorityState = false;
        }
        if (awaitingDisplayedMovement && (transform.position - displayedRootAtHit).sqrMagnitude > 0.000025f)
        {
            TraceHit(lastHitTrace, "replica-root-moved");
            awaitingDisplayedMovement = false;
        }
        if (visualModel == null || !visualPrediction.IsActive) return;
        Vector3 offset = visualPrediction.Tick(transform.position, driveAcceleration, dt,
            maxVisualOffset, visualReconcileTime);
        Vector3 permittedOffset = LimitVisualOffset(offset);
        if ((permittedOffset - offset).sqrMagnitude > 0.000001f)
        {
            // Presentation also respects solid cover; this does not apply a force.
            visualPrediction.Constrain(transform.position, permittedOffset);
            offset = permittedOffset;
        }
        visualModel.localPosition = modelLocalPosition + visualModel.parent.InverseTransformVector(offset);
        if (!visualStartLogged && offset.sqrMagnitude > 0.000001f)
        {
            TraceHit(lastHitTrace, "confirmed-visual-start");
            visualStartLogged = true;
        }
        if (!visualPrediction.IsActive) TraceHit(lastHitTrace, "visual-reconciled");
    }

    private Vector3 LimitVisualOffset(Vector3 offset)
    {
        float distance = offset.magnitude;
        if (distance <= 0.0001f) return offset;
        float permitted = distance;
        foreach (RaycastHit hit in rb.SweepTestAll(offset / distance, distance, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer)) continue;
            permitted = Mathf.Min(permitted, Mathf.Max(0f, hit.distance - 0.002f));
        }
        return offset * (permitted / distance);
    }

    private void TraceHit(HitTrace trace, string stage)
    {
        TankShooter.TraceShot(trace.Enabled, trace.ShooterId, trace.ShotId, NetworkObjectId,
            stage, trace.ConfirmedAt, Time.realtimeSinceStartupAsDouble - trace.ReceivedAt);
    }

    private void ResetPresentation()
    {
        visualPrediction.Reset();
        if (visualModel != null) visualModel.localPosition = modelLocalPosition;
        previousRootPosition = transform.position;
        observedRootVelocity = Vector3.zero;
    }

    public void ClearLifeMotion()
    {
        pendingImpulse = Vector3.zero;
        pendingHitTraces.Clear();
        awaitingAuthorityState = awaitingDisplayedMovement = false;
        ResetPresentation();
        if (IsOwner && !rb.isKinematic)
            rb.linearVelocity = rb.angularVelocity = Vector3.zero;
    }

    public void RelocateForReturn(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner || !IsSpawned) return;
        ClearLifeMotion();
        rb.position = position;
        rb.rotation = rotation;
        networkTransform.Teleport(position, rotation, transform.localScale);
    }

    public override void OnNetworkDespawn()
    {
        SetPreparationLock(false);
        ResetPresentation();
        pendingImpulse = Vector3.zero;
        pendingHitTraces.Clear();
        awaitingAuthorityState = awaitingDisplayedMovement = false;
    }
}
