# Track presentation verification

Production files: `TankTrackVisuals.cs`, the presentation hookup in `PlayerMovement.cs`,
and the existing `Player.prefab`. No physics, damage, network authority, model, material
asset, scene, UI or package setting is changed.

Authored references:

- `Player` root: one `TankTrackVisuals`, referenced by `PlayerMovement.trackVisuals`.
- `Player/TankModel/Wheel1` through `Wheel6`: the six Transform references, in order.
- `Player/TankModel/Track`: MeshRenderer, slot 0, `TREADMILE`, URP Lit `_BaseMap`.
- Original UV tiling `(1,1)`, offset `(0,0.33)`, texture wraps in Y.

Inspector controls:

- `wheelDegreesPerSecond = 360`, `trackCyclesPerSecond = 0.75`: independent speeds.
- `driveContribution = 1`, `turnContribution = 1`: independent intensity factors.
  The larger weighted magnitude wins; forward/reverse drive determines direction.
  Both signs of turn-only motion animate forward. `motionDeadZone = 0.02` ignores noise.
- `PlayerMovement.trackTeleportAngle = 45` degrees per frame; position discontinuities
  reuse the existing `presentationTeleportDistance = 2` metres. Known spawn/life/return
  paths explicitly invalidate the motion sample. These thresholds are not driving limits.

Only the owning player's existing input is used for its immediate presentation. Observers
derive normalized travel and yaw rate from the synchronized root, not the hit-jolt model.
Thus sliding/knockback or driving against a wall may differ from the owner's input-driven
animation. No wheel phase, input or texture offset is newly sent over the network.

One material is cloned per track Renderer slot in Awake, retained while disabled, restored
and destroyed on component destruction. Other material slots, shared assets, tiling, X
offset, tint and Renderer property blocks are untouched. No per-frame material allocation.

## Automated verification

```powershell
dotnet run --project Tests/Tracks/Tracks.Tests.csproj
python Tests/Tracks/verify_prefab.py
```

119 assertions execute the actual TankTrackVisuals, PlayerMovement and hit-presentation
source with a simulated Unity/NGO boundary. They cover all drive/turn direction combinations,
stopping, configurable magnitude, 30/60/120 FPS, authored rotations, material independence,
bounded offsets, cleanup, replica travel/turn/yaw wrap without reading remote input or
applying forces, teleport rejection, death, return, Results and despawn. They are not Unity
rendering, PhysX, Shader execution or real network tests.

The prefab was reopened from disk and checked for all local references, six correct visual
wheel children, one component, the renderer slot and original material settings. Runtime
C# was compiled against the installed Unity 6000.4.7f1 / NGO references without launching
Unity. This does not run Unity import or NGO IL post-processing.

## Still requires play testing

Use the current Editor or a built player; do not open another Editor on this project.
Import/compile, then test a host and client: forward/back, turn left/right while stopped,
drive+turn in both directions, release controls, death/respawn, Results and a second match.
Observe both tanks with one stationary: only the moving tank's track UV should scroll.
Repeat with network delay; observers should follow displayed root motion without a new RPC.
Check wheel geometry and texture direction visually and tune the two speeds to taste.

FBX inspection found wheel vertices centred near zero in local Y/Z; their nonzero axial X
centre does not obstruct rotation about local X. Pivots, meshes and authored rotations were
preserved. The final appearance has not been visually verified inside Unity.
