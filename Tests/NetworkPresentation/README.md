# Remote motion / recoil presentation

Unity 6000.4.7f1, NGO 2.13.3, Transport resolved to 2.7.3. Existing single Rigidbody,
owner authority, 30 Hz network tick, 50 Hz physics, reliable transform deltas retained.

## Evidence from the installed package

Read-only sources in `Library/PackageCache/com.unity.netcode.gameobjects@277a288a1cea`:

- `NetworkManager.cs` advances `NetworkTransform.CurrentTick` from **ServerTime**.
  `NetworkTransform.CheckForStateChange` stamps normal state updates with CurrentTick.
- `NetworkTimeSystem.cs` has a default client ServerBufferSec of 0.05 s. Server/host
  clocks use zero buffer; the client's clock is buffered. This is not changed.
- `NetworkTransform.UpdateInterpolation` uses ServerTime, network TickLatency, an
  additional tick on a non-server observer of a client-owned object, and fixed-step
  progression for Rigidbody motion. TickLatency is 1 on the host, but settles to at
  least 2 on a synchronized client and grows with RTT. These are different playback
  conditions for Host->Client and Client->Host.
- The previous position profile was Lerp + PositionLerpSmoothing=true, with maximum
  0.1. It queues eligible targets, then spends TimeToTarget traversing them. The second
  smoothing pass uses `1 - Pow(0.1, dt*60)`: **0.1 is not a literal 100 ms delay in this
  path**. Merely turning that pass off does not address the queue delay.
- The installed LegacyLerp path instead evaluates between sample timestamps at a
  fixed render time (ServerTime minus 2 ticks on a client / 1 on host). Enabling its
  second smoothing pass would add the old frame-dependent `dt/0.1` smoothing. The
  saved candidate therefore uses **LegacyLerp with PositionLerpSmoothing=false**.
- NetworkRigidbody.UseRigidBodyForMotion keeps interpolation in FixedUpdate and
  uses MovePosition/MoveRotation. Rigidbody Interpolate remains enabled. The render
  root can lag the physics position by a fixed step; this is distinct from state receipt.
- Shot RPCs previously presented immediately on arrival, without relating them to
  the buffered tank timeline. Local owner recoil was already immediate and stays so.

## Saved changes

`Player.prefab` keeps the same NetworkTransform component file ID, authority and
network behaviour ordering. Its script is now `TankNetworkTransform`, a subclass
that leaves all NGO transport/authority/update methods to the base implementation.
There is still one NetworkTransform and one Rigidbody. The subclass exposes the
known LegacyLerp render clock estimate and opt-in diagnostics.

- PositionInterpolationType: 1 (Lerp) -> 0 (LegacyLerp).
- PositionLerpSmoothing: true -> false.
- Interpolate=true and Rigidbody.Interpolate unchanged.
- Position/rotation thresholds, tick rate, buffer globals, UseUnreliableDeltas,
  SlerpPosition, rotation interpolation/smoothing, authority and grounding unchanged.
- User-tuned TankRecoil.recoilForce=50 retained. No new Inspector wiring required.
- PlayerMovement.anticipateFiringRecoil=true is authored separately from the
  existing anticipateConfirmedHits toggle, so either visual can be disabled for A/B.
- NGO may normalize GlobalObjectIdHash 4181086163 (prefab-stage value) to 93640355
  (asset value) on validation; the preservation check allows only that normalization.

The owner captures its actual queued recoil impulse and ServerTime fire stamp, then
includes them in the **existing** firing RPCs. No new RPC and no continuous physics
telemetry is sent. Disabled/zero recoil transmits zero. A remote accepted/registered
shot can begin anticipation only if its fire stamp plus the normal physics step is
ahead of the estimated displayed timeline. A host already displaying that client
shot's time does not add anticipation. The estimate is not packet acknowledgement.

Anticipation uses the existing `ConfirmedHitVisualPrediction` instance shared with
hit visuals. It subtracts the entire received root movement rather than layering
the same displacement twice, keeps the existing 0.5 m bound / 0.15 s reconciliation,
uses the existing compound Rigidbody sweep against obstacles, and clears on
death/return/teleport/despawn. It never AddForces a replica or moves the network root.
Local damage/kill authority and shot rejection remain unchanged. At extreme delay,
the existing 0.6 s lead safety limit still bounds this presentation estimate.

## Measurements actually run

`Interpolation.Tests.csproj` compiles the actual installed NGO generic/float
interpolator source **without editing PackageCache**. The tests use deterministic
scalar source trajectories, clocks and packet delivery, with 30 Hz state production
and 50 Hz updates. One 20 ms render step approximates Rigidbody interpolation. This
is not a Unity physics simulation, transport emulator, or live gameplay measurement.

`interpolation-results.txt` preserves all four tested configurations, changing one
setting at a time. Nine path/link combinations assert bounded, finite, monotonic
output and that the final profile reaches the motion/stop at least as promptly as
baseline under the same schedule. Position samples stop when changes are below the
saved 0.001 threshold. Link jitter is deterministic; reliable delivery preserves order.
Client->Client delivery uses two hops. No packet loss model was run.

Selected **synthetic interpolator results**, milliseconds from source event:

| Direction / one-way link / jitter | Raw first recoil-state receipt | Visible recoil before -> after | Stop settling before -> after |
|---|---:|---:|---:|
| Host->Client / 0 / 0 | 40 | 180 -> 120 | 160 -> 140 |
| Host->Client / 50 / +/-10 | 100 | 300 -> 160 | 280 -> 200 |
| Host->Client / 100 / +/-20 | 160 | 440 -> 220 | 440 -> 240 |
| Client->Host / 50 / +/-10 | 100 | 120 -> 120 | 100 -> 80 |
| Client->Client / 50 / +/-10 | 160 | 220 -> 180 | 220 -> 120 |

Visible recoil means 2 mm displayed displacement; stop settling means less than
5 mm error. These results exclude the added visual anticipation and cannot predict
an exact in-game end-to-end value. Raw state receipt is identical before/after.

The production shooter/recoil/motor/visual trajectory/subclass also run against the
existing simulated engine/RPC boundary in Tests/Recoil: 98 assertions include local
next-physics timing, no owner echo/replica force, remote clock gating, pooled duplicate
events, combined hit+recoil, cleanup, zero impulse and invalid stamps. Real terrain
contacts and NGO RPC generation are not exercised by that harness.

## Optional live diagnosis (off by default)

Enable Player > TankNetworkTransform > logMotionTiming for movement/stop captures.
For bounded shot captures, existing TankShooter.logHitTiming activates a one-second
window around each shot. Use Editor/Development builds. State events during a shot
window are recorded per received/sent state; render samples are limited to 10 Hz.
No tokens or Relay credentials are logged.

`[MotionTiming]` stages:

1. local-shot-presented: exact shot ID and captured fireStamp in NGO ServerTime domain.
2. owner-recoil-impulse-applied: same shot, same-peer queueToPhysicsMs.
3. authority-state-sent: exact NT stateTick/stateStamp/position and afterAppliedShot.
4. state-received: exact received stateTick/stateStamp/target on the observer.
5. remote-shot-presented and render-sample: view timeline estimate, physics position,
   rendered root/model positions and latest RECEIVED tick. Latest received tick is **not**
   claimed to be the state currently displayed. During anticipation, TankModel also
   has a local offset, distinct from that rendered root.

Match source publication ticks to the observer's received ticks. A state logged
after an impulse is a source-side ordering marker, not proof that a stopped wall
contact must produce a displacement. Never infer recoil receipt from arbitrary
root movement. Local marks are comparable only on the same peer. Buffered
ServerTime is a shared state timeline, not an unbuffered wall clock or a direct
one-way latency measurement; do not subtract localStopwatch values across peers.

## Still required in Unity / real multiplayer

No additional Unity process or batchmode was launched. Runtime C# was compiled
against installed Unity/NGO assemblies. The attempt to activate the existing Editor
window timed out, so Unity import/NGO IL post-processing and Play Mode were not
verified for these changes.

Use matching host/client builds (firing RPC payload changed), first on LAN, then
50 ms per-direction delay +/-10 ms jitter, then 100 ms +/-20 ms, then 1% packet loss.
Do not inject delay twice. Keep frame caps and scene identical before/after.

- Host->Client and Client->Host, then Host with two clients / Client->Client.
- Start, stop, reverse, turn, fire while stationary/driving/reversing and on slopes.
- Simultaneous firing/hit impulses, shots into a nearby wall, repeated shots,
  death/respawn, pooling, Results/menu and a second match.
- Compare owner queueToPhysics, source exact tick publication/remote receipt,
  receiver same-clock arrival-to-render samples, shot-to-visible-model response,
  stop convergence, frame-time p50/p95/p99 and RTT/jitter/loss.
- Observe smoothness under jitter and the bound/reconciliation near obstacles.
  The profile removes deliberate extra buffering but cannot remove transmission
  time, tick quantization, Rigidbody render interpolation or reliable delivery stalls.

Do not call the observed game issue resolved solely because these checks compile
or because controlled interpolator numbers improve.
