# Firing recoil checks

`TankRecoil` on the existing `Player.prefab` is enabled (`IsActive`) and references
the root `PlayerMovement`. The user's authored recoilForce is retained (currently
50 Newton-seconds); tests use a controlled 10 Newton-second configuration.
`TankShooter.recoil` references that same component.

The owner queues recoil inside `TryFire`, immediately after a successful local
projectile launch and its presentation, before `FireServerRpc`. The captured launch
direction and shot sequence are used at that moment. The owner branch of
`FireClientRpc` returns without recoil; late/duplicate confirmations cannot queue
another impulse or change its direction. Replica projectile launches never recoil.

Local reload/control/cooldown/failed-launch attempts do not recoil. A locally
accepted predicted shot can still be rejected by the server. The existing projectile
result path cancels that flight and prevents authoritative damage. Like already
played local audio/flash/shake, its local firing recoil is retained. This project
has owner-authoritative movement without movement rollback/resimulation; applying
an inverse impulse or teleport on rejection would introduce a new correction model
and could conflict with driving/contact/hit impulses. No compensating force is added.

The component projects the actual launch direction onto the world horizontal plane,
normalizes it, and queues the opposite impulse. The root motor applies it in the
same accumulator as confirmed hit impulses, with `ForceMode.Impulse`. Motor
correction is skipped on that step; subsequent supported steps brake normally.
Death, return, preparation lock and despawn use the existing impulse cleanup.

## Automated checks

Run `dotnet run --project Tests/Recoil/Recoil.Tests.csproj` and
`python Tests/Recoil/verify_prefab.py` from the project root. Redirect .NET build
output outside Assets when needed.

The C# harness compiles production TankRecoil, TankShooter, PlayerMovement and
ConfirmedHitVisualPrediction against a simulated Unity/NGO boundary. It checks
forward/left/right/pitched direction, hull-independent direction, disabled/zero
configuration, owner/server/observer roles, shot identity, reload/blocked/rejected
attempts, failed projectile creation, rapid shots, shared hit/recoil accumulation,
life cleanup, and motor scheduling with a slope normal and existing velocity.

This does **not** simulate terrain contacts, actual grounding, RPC transport,
Unity lifecycle scheduling, presentation, or audio. RPC methods are called directly
in the harness, without NGO's generated RPC routing. Host/client role checks are
not a real multiplayer test.

Remote motion now uses the saved TankNetworkTransform interpolation profile and a
shared visual trajectory for accepted remote firing/hit anticipation. See
`Tests/NetworkPresentation/README.md` for the controlled NGO comparison, exact
settings, timestamp semantics, source tick correlation and remaining live checks.

The timing regression check asserts that the impulse is already queued before
server validation can run. With scripted local presentation at 10 ms, a manually
invoked next physics step at 20 ms applies it once. Explicit confirmation callbacks
delayed by 0/100/500 ms never repeat it, for host-owner and client-owner flags.
These are controlled test timestamps, not measured Unity timings or real network
latency. With the project's 20 ms fixed step, the normal running-game wait is until
the next available physics step; rendered timing also depends on frame scheduling
and Rigidbody interpolation. There is no RPC round-trip in recoil scheduling.

The saved-asset check verifies the actual component and references, one Rigidbody,
and unchanged grounding/colliders/network/UI/all other prefab documents against HEAD.

## Unity verification still required

- At rest, fire forward, right and left: tank impulse should be opposite the
  horizontal launch direction, regardless of the hull heading or camera pitch.
- Repeat while driving/reversing and on ascending/descending/cross slopes. The
  world-horizontal impulse may press into or away from the slope; vertical recoil
  is intentionally absent. Contact response and gravity remain active.
- Disable IsActive, then use recoilForce=0: firing, shot audio, UI cooldown and
  empty reload click should continue; no firing impulse should occur.
- Fire while reloading, while dead/preparing and after Results: no recoil. Force
  a server rejection when possible: the locally predicted recoil remains, the
  projectile is cancelled, and no confirmation/rejection adds another force.
- Host owner, client owner, host observing client and a second client observing:
  one impulse on the owner, synchronized movement everywhere else. Repeat with
  100 ms latency and 20 ms jitter; owner recoil should still start on the next
  available physics step, even when confirmation arrives much later.
- Repeated fire, hit received in the same physics step, death/respawn and a second
  match: impulses should add once and pending impulses should not survive resets.

No Unity Play Mode or real multiplayer test was run for this change.
