# Network presentation verification

Baseline inspected: commit `52690ad`, Unity 6000.4.7f1, NGO 2.13.3,
resolved Transport 2.7.3. No Unity instance or batchmode was launched.

## Changes and boundaries

- Replica turret yaw uses `SmoothDampAngle` in LateUpdate. Local aim and the owner-written
  NetworkVariable remain separate. The authored Player prefab sets smooth time to 0.05 s.
- Explicit life resets clear turret history. The first received yaw after a reset snaps;
  a root displacement greater than 2 m in one presentation frame also clears history.
  Existing respawn relocation is explicitly covered; a new teleport mechanism added in
  future should call the presentation reset methods, including for rotation-only jumps.
- A shot key is `(shooter NetworkObjectId, shot sequence)`. MatchManager sends a reliable
  terminal result containing target ID, point and normal, or no-impact for rejection/expiry.
  Local muzzle/flight starts immediately. Local contact only releases the flight: there is
  no speculative explosion, damage, kill, camera hit shake, or impulse.
- Terminal records live for the match, independently of pooled Bullet instances. This
  handles contact followed by pool reuse, result-before-launch, duplicate results, and
  match teardown. Expect a few thousand small terminal records in a full four-player match.
- Damage/kill feedback still comes from accepted server damage. The victim owner alone
  applies AddForce. The visual trajectory remains confirmation-driven, bounded, wall-checked,
  and subtracts received root movement. It is not lag compensation or full prediction.
- The 0.5 m visual limit and 0.15 s reconciliation interval are unchanged. Outward travel
  eases into the limit; hidden displacement cannot accumulate behind it. A blocked offset
  removes velocity into the obstruction. Received steering adjusts the baseline without
  rotating the world-space impulse. Acceleration/drive/physics authority are unchanged.
- Statistics serialize on data revision changes, not unchanged LateUpdate frames.
  Nearest-hit selection is one pass instead of sorting entire query arrays. SweepTestAll
  and RaycastAll still allocate; replacing collider geometry or truncating hits without
  profiling would risk correctness. Existing effect prefabs still instantiate normally.

## Settings deliberately not tuned without an A/B run

Player: owner-authoritative NetworkTransform; UseRigidBodyForMotion=true;
AutoUpdateKinematicState=true; Rigidbody Interpolate; NT Interpolate=true;
Position Lerp / Rotation LegacyLerp; extra position/rotation smoothing enabled, maxima
0.1 s; SlerpPosition=true; UseUnreliableDeltas=false; tick=30; physics=50 Hz.

Neither the cause of body jitter nor the best replacement interpolation settings can be
established from the available measurements. No body interpolation/tick/threshold setting
was changed. Bullet interpolation remains disabled: enabling it would add a physics-step
display lag to an already forward-swept projectile; it needs a separate visual comparison.
Independent projectile trajectories can still disagree before confirmation, and a locally
hidden flight can precede the authoritative explosion. No round trip was added before the
victim's real impulse. Result and impulse are reliable events on different NetworkObjects;
their exact render-frame arrival order must be checked in a real session.

## Automated checks performed

- Runtime C# compiled against installed Unity/NGO/Cinemachine references, including the new
  source. This is **not** Unity import, NGO IL post-processing, or a player build.
- Combat.Tests: 55 assertions. Real match rules/health/feedback/presentation code with
  simulated engine/RPC boundary. Checks late/duplicate/out-of-order results, rejection,
  cleanup, JSON invalidation, bounded offset, wall constraint, successive hits, steering,
  30/60/120 FPS limiter consistency, and subtraction of an already displayed trajectory.
- Projectile.Tests: 12 assertions on the production Bullet + ShotPresentation code with
  simulated physics, pooling callbacks and result delivery. Checks nearest eligible sweep
  hit, cosmetic vs authoritative contact, late result after actual OnDisable/relaunch,
  trigger/sweep/expiry deduplication, lethal hit, and terminal miss.
- Existing UI lifecycle: 88 assertions; existing match rules: 77 assertions passed.

Run the two combat projects with .NET 10, not Unity:

```powershell
dotnet run --project Tests/Combat/Combat.Tests.csproj
dotnet run --project Tests/Combat/Projectile.Tests.csproj
```

These do not prove remote smoothness, camera update order in Unity, collisions against real
level geometry, packet timing, or NGO RPC code generation.

## Required Unity / built-player comparison

Use the same build conditions and scene for baseline and changed version. Do not open a
second Editor on this project. First allow the current Editor to import and compile; check
Console for NGO post-processing errors. Inspect Player's two new presentation settings.
Keep all existing NetworkTransform/NetworkRigidbody values unchanged for the first run.

1. Host + client, then host + two clients with distinct authentication identities.
   Test Host -> Client, Client -> Host, Client -> Client. Record victim and shooter views.
2. Drive straight, turn while driving, stop, reverse. Rotate turret through 359/0 in both
   directions while stopped and driving. Compare a fixed camera against the following camera.
3. One nonlethal shot, repeated shots, moving targets, wall-adjacent targets. Check one
   explosion per server result, one HP decrement, one owner impulse; compare wall blocks.
4. Test lethal shots, respawn at a different heading, shot lifetime expiry, repeated pool
   reuse, a shooter disconnecting during flight, Results, return, and a second match.
5. Inject per-direction latency/jitter/loss: 0/0/0; 25 ms/±5 ms/0%; 50 ms/±15 ms/1%;
   100 ms/±30 ms/3%. Do not inject the same delay twice. Record actual RTT and use asymmetric
   links for the client-to-client case. Repeat each case under the same rendering frame cap.
6. Optionally enable the existing TankShooter `logHitTiming` only for a short Development
   Build capture. Correlate `shot=shooter:sequence` and target across server receipt, contact,
   terminal result, confirmation receipt, owner impulse and visual reconciliation.
   `localMark`/`elapsedHere` are same-peer timings only. Buffered ServerTime is not a direct
   one-way latency measurement. Any root-position change is not proof of hit-state receipt.
7. Capture frame-time p50/p95/p99, GC allocation spikes, physics time, actual state-update
   cadence, RTT/jitter/loss, visual contact/result/push gaps, peak offset and reconciliation.
   Record video for turret wrap, wall contacts and owner/observer comparisons.

Only then A/B one body setting at a time (for example rotation interpolation mode separately
from extra position smoothing), restore the baseline before each comparison, and retain a
change only if it improves measured jitter without unacceptable latency/corrections. Do not
enable unreliable deltas with rigidbody-driven motion or raise tick rate as an automatic fix.
