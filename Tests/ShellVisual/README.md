# Cartoon shell visual

Created through Unity MCP from the user's written description. No reference image was accessible in this message, so exact image matching was not verified.

## Persistent assets

- `Assets/ProjectsFiles/Art/Projectiles/CartoonShell.asset`: one closed, continuous 12-sided lathed mesh, 696 render vertices (split for flat-shaded faces), 336 triangles, two submeshes. Olive body, gray tapered rounded-point tip, narrow lower metal band and wider beveled base rim. No overlapping primitives, textures or markings.
- `ShellOlive.mat` / `ShellMetal.mat` in that folder: scene-independent URP Lit materials, restrained metallic/smoothness values; no shared existing material changed.
- `ShellVisual.prefab`: reusable visual only, containing Transform, MeshFilter and MeshRenderer. No physics, scripts or NetworkObject.
- `Assets/ProjectsFiles/Prefabes/Bullet.prefab`: removed the previous root Cube MeshFilter/MeshRenderer and added a nested ShellVisual prefab at identity local pose/scale. Root scale stays (0.1,0.1,0.1). Visual dimensions are 0.041 × 0.041 × 0.1 world units. Tip points along local +Z, matching TankShooter's Quaternion.LookRotation(direction).

The generation body is retained in `create-shell.cs.txt`; it refuses to overwrite existing generated assets. Gameplay properties were compared with EditorJsonUtility before/after editing the same loaded prefab contents: Rigidbody, collider, Bullet fields and root scale unchanged. The original serialized prefab is retained in `bullet-before.prefab.txt` for comparison. No gameplay C# file was edited. Impact-effect and damage-definition references are unchanged. Existing unrelated edits, including StandardShellDamage settings, were preserved.

## Checks performed

- Inspected Bullet, TankShooter and PoolManager before authoring.
- Viewed the reusable prefab in 3D Scene View (`shell-profile.png`). Earlier end-on captures are not the intended silhouette review.
- In an isolated GameScene Play Mode Host, invoked the existing private TankShooter.TryFire entry point. The actual pool and Bullet.Launch path produced the saved shell with forward alignment dot=1 and the original gameplay size. Temporary runtime preparation/phase/position settings were not saved.
- Advanced actual Physics by two 0.02-second steps: about 2.0006 world units travelled with the authored speed/arc. Released shot 1 through Bullet.ReleaseIfShot and fired again through TryFire; the same pooled instance was reused, child local scale stayed (1,1,1), and forward dot stayed 1. See `firing-checks.txt`.
- `shot-gameplay.png`: actual gameplay-camera view at the paused launch frame. `shot-closeup.png`: magnified by moving the camera and temporarily reducing near clip/FOV, never by scaling the projectile. The tank muzzle can overlap the launch-frame base. Camera settings and Time.timeScale were restored; Play Mode was stopped without saving scene changes.

The shell is small and fast, so fine silhouette details are principally visible in close-up. There is no new billboard/trail/orientation controller. The existing projectile does not rotate continuously to follow its gravity arc; that behavior remains unchanged.

No remote-client/Relay session, full combat/damage/impact regression or standalone build was tested for this visual-only replacement. Two material slots mean up to two color submissions per visible shell versus one for the old cube; no controlled GPU/frame-time benchmark was performed.
