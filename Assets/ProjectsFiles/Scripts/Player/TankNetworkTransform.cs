using Unity.Netcode.Components;
using UnityEngine;

// Keeps NGO's authority, state transport and Rigidbody presentation intact.
// The subclass exposes the authored presentation clock and optional diagnostics.
public sealed class TankNetworkTransform : NetworkTransform
{
    [Tooltip("Development diagnostics: state ticks, exact received targets, and rendered root samples. Disabled by default.")]
    [SerializeField] private bool logMotionTiming;
    private Rigidbody body;
    private uint watchedShot, lastAppliedShot;
    private double watchUntil, nextStateLog, nextRenderLog;
    private bool traceShot;
    private int receivedTick = -1;

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody>();
    }

    // The saved profile is LegacyLerp with no second position smoothing pass.
    // NGO uses ServerTime minus 1 tick on host/server and 2 on a client. The
    // Rigidbody renderer adds a fixed-step interpolation delay. This is an
    // estimate of the displayed timeline, NOT proof of a particular packet.
    public bool TryGetRenderTime(out double time)
    {
        time = 0d;
        if (!IsSpawned || CanCommitToTransform || !Interpolate ||
            PositionInterpolationType != InterpolationTypes.LegacyLerp || PositionLerpSmoothing) return false;
        int ticks = (IsServer ? 1 : 2) + InterpolationBufferTickOffset;
        time = NetworkManager.ServerTime.Time - ticks / (double)NetworkManager.NetworkConfig.TickRate;
        if (body != null && body.interpolation == RigidbodyInterpolation.Interpolate)
            time -= Time.fixedDeltaTime;
        return true;
    }

    public void WatchShot(uint sequence, double firedAt, bool diagnostics, bool local)
    {
        watchedShot = sequence;
        traceShot = diagnostics;
        watchUntil = Time.realtimeSinceStartupAsDouble + 1d;
        if (Tracing) Write(local ? "local-shot-presented" : "remote-shot-presented", -1,
            $"fireStamp={firedAt:F4}");
    }

    public void ImpulseApplied(uint sequence, double queuedAt)
    {
        lastAppliedShot = sequence;
        if (Tracing) Write("owner-recoil-impulse-applied", -1,
            $"appliedShot={sequence} queueToPhysicsMs={(Time.realtimeSinceStartupAsDouble - queuedAt) * 1000d:F2}");
    }

    private bool Tracing => logMotionTiming || (traceShot && Time.realtimeSinceStartupAsDouble <= watchUntil);

    protected override void OnAuthorityPushTransformState(ref NetworkTransformState state)
    {
        base.OnAuthorityPushTransformState(ref state);
        if (state.HasPositionChange && ShouldLogState())
            Write("authority-state-sent", state.GetNetworkTick(), $"afterAppliedShot={lastAppliedShot} target={state.GetPosition():F4}");
    }

    protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
    {
        base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
        if (!newState.HasPositionChange) return;
        receivedTick = newState.GetNetworkTick();
        if (ShouldLogState()) Write("state-received", receivedTick, $"target={newState.GetPosition():F4}");
    }

    private bool ShouldLogState()
    {
        if (!Tracing) return false;
        if (traceShot && Time.realtimeSinceStartupAsDouble <= watchUntil) return true;
        if (Time.realtimeSinceStartupAsDouble < nextStateLog) return false;
        nextStateLog = Time.realtimeSinceStartupAsDouble + 0.1d;
        return true;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || CanCommitToTransform || body == null || !Tracing || Time.realtimeSinceStartupAsDouble < nextRenderLog) return;
        nextRenderLog = Time.realtimeSinceStartupAsDouble + 0.1d;
        TryGetRenderTime(out double renderTime);
        var movement = GetComponent<PlayerMovement>();
        Vector3 modelPosition = movement != null ? movement.PresentedModelPosition : transform.position;
        Write("render-sample", receivedTick,
            $"estimatedRenderStamp={renderTime:F4} latestReceivedTick={receivedTick} physics={body.position:F4} renderedRoot={transform.position:F4} model={modelPosition:F4}");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void Write(string stage, int tick, string details)
    {
        Debug.Log($"[MotionTiming] node={NetworkManager.LocalClientId} owner={OwnerClientId} tank={NetworkObjectId} " +
            $"shot={NetworkObjectId}:{watchedShot} stage={stage} stateTick={tick} " +
            $"stateStamp={(tick >= 0 ? tick / (double)NetworkManager.NetworkConfig.TickRate : -1d):F4} " +
            $"serverTimeline={NetworkManager.ServerTime.Time:F4} localMark={Time.realtimeSinceStartupAsDouble:F4} {details}", this);
    }

    public void ResetLifeTracking()
    {
        watchedShot = lastAppliedShot = 0;
        traceShot = false;
        receivedTick = -1;
        watchUntil = nextStateLog = nextRenderLog = 0d;
    }

    public override void OnNetworkDespawn()
    {
        ResetLifeTracking();
        base.OnNetworkDespawn();
    }
}
