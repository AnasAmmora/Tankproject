# GameScene Curved World

**Historical safe-preset report. The current setup is intentionally exaggerated (strength 0.04, start 5), with Edit Mode preview. See [the current preset and checks](Exaggerated/README.md). The combat-alignment and performance results below describe the earlier 300-unit flat-radius preset, not the current exaggerated appearance.**

Implemented and authored through Unity MCP in Unity 6000.4.7f1 / URP 17.4.0 on 31 October 2026. No compatible bending package was installed, so this uses isolated shader variants of the project's existing shaders. No asset was purchased or package installed.

## Inspector controls

Open GameScene, select the **Curved World View** root, and edit **CurvedWorldView**. Preview in Play Mode; the component does not deform the editing view outside Play Mode.

| Control | Default | Meaning |
|---|---:|---|
| IsActive | true | Enables local bending. Turning it off clears the deformation and restores the original water mesh. |
| BendStrength | 0.00006 | More downward curvature at larger values; clamped to 0–0.0002. |
| BendStartDistance | 300 | Zero vertex deformation inside this horizontal distance from the local gameplay camera. Clamped to at least 300 for the existing weapon/aiming setup. |

Increase BendStartDistance for a later onset. Increase BendStrength slightly, for example to 0.00008, for a more visible horizon. Use IsActive=false for a complete appearance restore.

The current terrain is only about 70 × 30 units. Its terrain, tanks, bridges and nearby props therefore stay flat by design; the visible default curve appears primarily on the extended distant water. Their shaders all use the same function if they enter the distant region. Bending the small playable map itself would conflict with maintaining its original physics-based aiming.

## Implementation

- `Assets/ProjectsFiles/Scripts/CurvedWorldView.cs`: local GameScene controller, with the three controls and serialized references authored through MCP. Updates four shader vectors immediately before the existing gameplay camera renders, after its Cinemachine pose is ready. The origin uses the camera's world XZ position, shared by color/depth/shadow passes. No bend parameters are networked.
- `Assets/ProjectsFiles/Rendering/CurvedWorld/Shaders/TankCurvedWorld.hlsl`: shared position deformation and analytic normal/tangent correction. It is applied after terrain instancing, skinning and the existing water-wave vertex calculation.
- `Shaders/Upstream/`: pinned URP Lit, Terrain Lit and required pass variants. Forward/deferred, shadow caster, depth and depth-normal paths use the same bend. Motion-vector paths include previous-frame origins. Meta/bake geometry stays unbent. Unity's upstream license is retained.
- `Shaders/Water.shader` and `Shaders/Rock.shader`: generated snapshots of the existing Shader Graphs, using a local variant of URP's graph varyings code. Original graph math, material properties, water animation, transparency and foam inputs remain intact.
- `Materials/`: 11 GameScene-only material copies. Original shared materials, shaders, Player.prefab, other scenes and pipeline assets remain untouched. The controller maps the two authored tank materials once when PlayerHealth.Spawned fires; it does not instantiate materials every frame.
- `CurvedWaterHorizon.asset`: a static mesh with the original 121-vertex shoreline grid retained, plus seven outer rings. The first ring has radius 260; later square rings extend to the existing 2500-unit half-width. This small geometry increase was explicitly approved. Active water has **177 vertices / 344 triangles**, versus the prior **125 / 244**. The original grid/UVs and the 121-vertex collider are preserved. Disabling restores the prior visual mesh.

With horizontal radius r, start distance s, and strength k:

```text
q = max(0, r - s)
raw = k × q³ / (q + 50)
drop = raw / (1 + raw / 64)
worldY -= drop
```

The 50-unit easing term gives a smooth start with zero slope and curvature at the boundary. The rational cap approaches 64 units smoothly and provides a conservative maximum for culling bounds. At the default settings the analytic vertex drop is 0 through 300 units, about 0.075 at 350, 1.864 at 500, 9.339 at 750, and 19.206 at 1000.

Static water triangles approximate this function between their vertices; the transition is not an exact per-pixel circular boundary. The first flat ring preserves the current map and roughly its 200-unit aiming neighborhood without runtime tessellation. Existing water UV density is continued across the outer rings rather than stretched over the new extent.

Bounds are expanded downward only when a renderer's enclosing sphere can enter the curved region. Nearby objects retain original bounds. Static batches use world-space overrides, moving renderers use local bounds, and overrides are restored on disable. Initial bounds changes wait until the camera render so Unity can finish static batching. In the checked gameplay views only the water needed padding; the other 64 scene renderers retained original bounds. The existing Terrain requires no culling expansion because it stays entirely in the flat region.

The controller never writes object transforms, Rigidbody state, colliders, TerrainData, projectile trajectories or NetworkVariables. It clears shader globals and subscriptions when disabled/unloaded. Per-frame work is uniform updates and lightweight bounds classification, without per-frame mesh deformation or material creation. No extra camera, persistent render texture, volumetric effect or reflection system was added.

Water height remains 0.2, camera far clip remains 1000, and the existing linear fog remains 120–750 with color (0.45, 0.50, 0.52). Lighting, skybox, exposure, SSAO and other pipeline settings were preserved.

## Checks actually performed

- **64 settled Play Mode captures:** eight headings at bridge `(33.4,3,2.7)`, shoreline `(76,2.3,15)`, slope `(45,3,-4)` and elevated `(45,25,5)` positions, before/after, using the existing Main Camera at 960 × 540 / FOV 60. The first three use pitch 2°, the elevated view 6°. Contact sheets were visually inspected. Static-batch culling was corrected after an early check found disappearing props, then all directions were repeated.
- **Per-view statistics:** all 32 matched final views had **zero extra draw calls and zero extra SetPass calls**, with exactly +100 triangles / +52 vertices. `views.txt` contains the observations. A conservative-bounds prototype had caused additional draws and was replaced before retaining these final results.
- **Real local NGO host / PhysX comparison:** a Player prefab was spawned and its tank material mapping verified. Production pooled Bullet flights hit the actual enemy-base collider at 10, 20, 40 and 60 units, once with the effect off and once on. All eight flights applied positive authoritative damage, produced exactly eight confirmations, and paired impact positions differed by **0**. The existing TankShooter.GetAimDirection output was identical for each pair. No synthetic contact/impact callback was invoked. See `physics-check.txt`.
- The firing fixture used identical ballistic launch directions in each mode, a stationary tank with movement/input disabled only in the fixture, and the real Bullet/health/confirmation path. It did not exercise player input or the network firing RPC. Reported damage pairs were 25, 23, 12 and 5 under the existing falloff settings.
- **Appearance restore:** original materials/mesh and disabled curved variants were rendered at the same shader time. Both the bridge and close water images had **zero differing pixels** when disabled. With the effect active, the close water mean RGB-channel error was 0.003237 out of 255; the full bridge image includes the intended changed horizon. See `close-check.txt`. Nearby colors, wave pattern and shore foam remained visually consistent.
- Saved and reopened GameScene through MCP, then confirmed the saved setup after a transport reconnect. All 65 renderer references and two tank mappings were present, with one gameplay camera, no target render texture, and no missing required references. The scene was clean. `saved-setup-check.txt` contains a readback.
- Existing transform, camera, collider, Rigidbody and light YAML blocks compared unchanged against the prior scene. Shader compiler messages showed no errors after rendered passes. `git diff --check` passed after removing whitespace emitted in new Unity override lines.

Contact sheets: [before](before-contact.png), [after](after-contact.png). Full-size views: [bridge before](before-bridge-0.png), [bridge after](after-bridge-0.png), [elevated before](before-elevated-0.png), [elevated after](after-elevated-0.png).

## Performance observations

NVIDIA GeForce GTX 1650 / Direct3D12, existing gameplay camera at the same outward-facing shoreline pose, 960 × 540. Four alternating passes: original materials with the controller disabled, active, original again, active again. Each warmed for 35 frames and retained 60 FrameTimingManager samples, for **240 samples**. Capture render textures were temporary verification tools and are absent from the saved game.

| State | Draw calls | SetPass | Submitted triangles | Submitted vertices |
|---|---:|---:|---:|---:|
| Original baseline | 22 | 16 | 8108 | 8815 |
| Active | 22 | 16 | 8208 | 8867 |
| Added | **0** | **0** | **100** | **52** |

| Pass | CPU median ms | GPU median ms |
|---|---:|---:|
| Original 1 | 16.471 | 16.204 |
| Active 1 | 16.673 | 16.427 |
| Original 2 | 16.404 | 16.145 |
| Active 2 | 16.671 | 16.321 |

The observed median differences were approximately +0.20–0.27 ms CPU and +0.18–0.22 ms GPU in this Editor session. Frame timings clustered near the display interval and varied more than these differences; these are observations, not an isolated standalone cost or performance guarantee. Raw samples, including zero GPU readings where timing data was unavailable, are preserved in `performance.csv`. A standalone build/target-device profile is still needed.

## Aiming and compatibility limits

- Physics stays unbent. Targets outside the flat region can diverge from their physics raycast/impact positions. The default protects this map, the 200-unit aim ray and the default projectile's roughly 250-unit horizontal flight. Keep the flat radius beyond reachable targets if weapons/map size change; this feature does not rewrite aiming to compensate for distant curved targets.
- No independent remote client, latency/reconnection scenario, player-controlled traversal, camera collision-avoidance playthrough or standalone build was tested. Local-host shooter/impact checks passed; remote-client origin isolation follows the local camera callback and absence of network parameter synchronization.
- Default opaque geometry and shadow distance are inside the flat region. Existing near shadows/depth/SSAO were visually checked; curved opaque shadows beyond the start radius, TAA/ASW/XR and motion-vector appearance were not separately tested.
- Supports the inspected URP Lit, Terrain Lit, Water and Rock shaders. New shaders/materials require matching variants; new authored scene renderers need to be added to sceneRenderers. Additional terrain chunks, terrain trees/details, longer-range combat, split-screen cameras, and large maps need further integration/culling/aiming checks.
- The shader variants are pinned snapshots of URP 17.4.0 and the current graphs. Review/regenerate them after package or graph changes. Pass `export-shaders.cs.txt` to Unity MCP execute_code, then run `node Tests/Environment/CurvedWorld/generate-shaders.js` from the project root. The generator checks the installed URP version and explicit patch anchors before changing shader files. Shared source material edits do not automatically propagate to the isolated GameScene copies.
