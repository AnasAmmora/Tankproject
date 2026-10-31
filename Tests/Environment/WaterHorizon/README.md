# GameScene water horizon — 31 October 2026

Changes were made and saved through Unity MCP in Unity 6000.4.7f1. GameScene was reopened successfully afterwards with no unsaved changes.

## Inspection and implementation

- Active pipeline: URP, PC_RPAsset / PC_Renderer, with depth and opaque textures already enabled, SSAO already present, and gameplay-camera post-processing disabled. None of these assets or settings were changed.
- Original Water: built-in Plane, 121 vertices / 200 triangles, position `(44.9617, 0.2, 4.92)`, scale `(6.987936, 2, 3.003706)`, approximately 69.88 × 30.04 world units. It had a MeshRenderer and an enabled MeshCollider.
- Shared material: `Assets/ProjectsFiles/ExtarnalAsset/Simple Water Shader Package/WaterShader/Water.mat`, Shader Graphs/Water. The graph reads object-space Position for displacement and implicit UV0 for its Voronoi/noise and foam. The material's `_BasedOnWorldScale` is 0 and `_Scale` is 5. Changing transform scale or normalizing UVs across a larger plane would change the appearance.
- Camera inspection: Main Camera has FOV 60°, near 0.3, far 1000; the existing Cinemachine lens also has far 1000. Camera settings remain unchanged.
- Original fog was disabled. The existing URP lit shader path already supports built-in fog through InitializeInputDataFog/MixFog; no custom shader or rendering feature was added.

The existing Water object's MeshFilter now references `Assets/ProjectsFiles/Scenes/Environment/GameSceneWaterHorizon.asset`. This isolated mesh retains every original vertex, UV, normal, tangent and triangle index exactly, then adds four outer corner vertices and 44 triangles in four perimeter strips. It does not subdivide the original grid. Total: **125 vertices / 244 triangles**, still one water renderer and the same shared material.

Outer UVs continue the original affine mapping:

```text
u = (5 - localX) / 10
v = (5 - localZ) / 10
```

This keeps the existing anisotropic world-space pattern density and animation inputs. Water position and transform scale remain unchanged. The original MeshCollider still references the original 121-vertex built-in plane, so the visual extension does not extend gameplay collision.

Coverage is 5000 × 5000 world units, centered on the existing Water. Renderer bounds are center `(44.9617, 0.2, 4.92)`, extents `(2500, 0.2, 2500)`. The small vertical bounds padding accounts for the existing vertex displacement. The camera far clip remains **1000**.

GameScene's RenderSettings now use **Linear fog, start 120, end 750, color RGBA(0.45, 0.50, 0.52, 1)**. The entire roughly 70 × 30 playable map remains inside the clear range. The muted blue-gray affects distant geometry/water only. Skybox, lights, ambient settings, exposure, post-processing, reflection quality, terrain, islands, bridges and gameplay objects were unchanged. No shared material, shader, render-pipeline asset or other scene was edited. The final scene diff contains only the five fog settings and Water's visual mesh reference.

## Verification actually performed

1. Captured 48 settled Play Mode images with the **existing Main Camera** at 960 × 540, eight headings (0° through 315°) for each of:
   - Bridge: `(33.4, 3, 2.7)`, pitch 2°.
   - Island edge: `(76, 2.3, 15)`, pitch 2°.
   - Elevated stress view: `(45, 25, 5)`, pitch 6°.
   Each before/after pose was identical; each pose settled for 12 frames. Fog activation also received additional warmup, and capture waited while shaders were compiling. Water replaces the old exposed surface edges/empty space in all 24 modified views. Modified renderer bounds intersect all 24 view frusta. `views.txt` records the per-view observations. Two original elevated views did not intersect the small original water bounds.
2. Sampled perspective rays at 16:9, 21:9 and 32:9; eight headings, five pitches (-10°, 2°, 6°, 45°, 60°), and the three positions above. All **2376 water-plane intersections inside the camera's far clip** fell inside the new surface. Maximum sampled horizontal distance from the water center was 1622.66 units. See `coverage-check.txt`.
3. Compared nearby water with fog disabled, rendering original and expanded meshes at the same Play Mode shader time and camera pose. At 960 × 540 the mean absolute RGB-channel difference was **0.002798 / 255**, with 2835 of 518400 pixels differing slightly. The original geometry and UVs also compared exactly; the affine UV error across the entire new mesh was zero. This supports preservation of the nearby pattern, wave displacement and foam rather than rescaling them. The material/shader and every animation-speed property remain the original values. See `pattern-check.txt`.
4. Visually inspected the before/after contact sheets, full-size bridge and elevated views, and close water comparison. The existing animation was running during the Play Mode captures. No additional camera was created; the original camera pose, target texture and aspect were restored after capture.
5. Reopened the saved GameScene through Unity MCP and verified its mesh reference and fog settings. No scene/shader errors were reported. MCP logged one WebSocket transport warning after reload; subsequent tool calls succeeded. `git diff --check` passed.

Contact sheets: [before](before-contact.png), [after](after-contact.png). Full-size bridge comparison: [before](before-bridge-0.png), [after](after-bridge-0.png).

## Performance observations

Hardware reported by Unity: NVIDIA GeForce GTX 1650, Direct3D12. Compared the same outward-facing island-edge camera pose at 960 × 540, FOV 60°, far 1000, using UnityStats and FrameTimingManager during actual rendered frames. Five passes each warmed up for 40 frames, then recorded 60 samples: original, expanded without fog, expanded with fog, original again, expanded with fog again. Raw, untrimmed results are in `performance.csv`.

| State | Draw calls | SetPass calls | Submitted triangles | Submitted vertices |
|---|---:|---:|---:|---:|
| Original | 22 | 16 | 8048 | 8779 |
| Expanded, no fog | 22 | 16 | 8092 | 8783 |
| Expanded, fog | 22 | 16 | 8092 | 8783 |
| Difference | **0** | **0** | **+44** | **+4** |

| Pass | CPU frame median (ms) | GPU frame median (ms) |
|---|---:|---:|
| Original, first | 9.081 | 5.412 |
| Expanded, no fog | 16.700 | 16.443 |
| Expanded, fog, first | 16.723 | 16.276 |
| Original, repeat | 16.751 | 16.269 |
| Expanded, fog, repeat | 16.706 | 16.392 |

These Editor timings were noisy and clustered near the display interval after the first pass. The raw repeat-original pass also includes a 2532.52 ms Editor/MCP stall. The data does **not** establish a reliable incremental CPU/GPU frame-time cost or a performance guarantee. In particular, the initial faster baseline cannot be treated as an attributable water regression when the repeated baseline also measured approximately 16.75 ms. Extending visible water shades more screen pixels than empty sky, so the unchanged draw-call count does not imply zero GPU cost. A standalone build and longer controlled profiling session would be needed to quantify that cost.

The original batch-of-manual-renders statistics were stale and were discarded; the retained CSV and contact sheets come from the subsequent frame-based Play Mode capture.

## Limits

- This was a local Play Mode camera inspection, not a connected multiplayer session or a player-driven traversal of every map location.
- Eight rendered headings and additional mathematical frustum sampling were checked; arbitrary camera positions outside the playable map were not exhaustively rendered.
- No standalone build, target-device benchmark, long animation capture, or independent numerical wave-frequency measurement was performed. Wave/foam scale and speed preservation are supported by unchanged shader/material settings, exact original mesh data, running Play Mode observations, and the same-time image comparison.
- Existing water depth/transparency/shoreline behavior was preserved; extending the water does not create underwater terrain outside the existing map.
