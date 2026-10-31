using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>GameScene-only, local shader presentation. Never writes transforms or network state.</summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class CurvedWorldView : MonoBehaviour
{
    public bool IsActive = true;
    [Tooltip("Downward bend in inverse world units. Strong values deliberately distort distant aiming. Displacement is smoothly capped at 64 units.")]
    [Range(0f, 0.1f)] public float BendStrength = 0.04f;
    [Tooltip("Unaffected vertex radius. Small values give a pronounced curve but visuals beyond this radius diverge from physics. Scene View preview centers on its orbit pivot.")]
    [Min(0f)] public float BendStartDistance = 5f;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private MeshFilter waterSurface;
    [SerializeField] private Mesh originalWaterMesh;
    [SerializeField] private Mesh curvedWaterMesh;
    [SerializeField] private Renderer[] sceneRenderers;
    [SerializeField] private Terrain sceneTerrain;
    [SerializeField] private Vector3 originalTerrainPatchBounds = Vector3.one;
    [Serializable] public struct MaterialReplacement { public Material original; public Material curved; }
    [SerializeField] private MaterialReplacement[] playerMaterials;

    private sealed class RendererState
    {
        public Renderer Renderer;
        public Bounds Bounds;
        public Bounds WorldBounds;
        public Material[] OriginalMaterials, CurvedMaterials;
        public bool BoundsInitialized, BoundsExpanded, WasStaticBatch;
    }
    private readonly List<RendererState> renderers = new List<RendererState>();
    private readonly HashSet<Renderer> registered = new HashSet<Renderer>();
    private readonly Dictionary<Terrain, Vector3> terrainBounds = new Dictionary<Terrain, Vector3>();
    private static readonly int OriginId = Shader.PropertyToID("_TankCurveOrigin");
    private static readonly int PreviousOriginId = Shader.PropertyToID("_TankCurvePreviousOrigin");
    private static readonly int ParametersId = Shader.PropertyToID("_TankCurveParameters");
    private static readonly int PreviousParametersId = Shader.PropertyToID("_TankCurvePreviousParameters");
    private Vector4 previousOrigin, previousParameters;
    private bool configured, appliedActive, hasPrevious, boundsReady;
    private int previousFrame = -1;
    private Vector3 boundsOrigin;

    private void OnEnable()
    {
        configured = Required(gameplayCamera, nameof(gameplayCamera)) & Required(waterSurface, nameof(waterSurface)) &
            Required(originalWaterMesh, nameof(originalWaterMesh)) & Required(curvedWaterMesh, nameof(curvedWaterMesh));
        if (!configured) { enabled = false; return; }
        if (sceneRenderers == null || playerMaterials == null)
        { Debug.LogError("CurvedWorldView: missing Inspector references 'sceneRenderers' or 'playerMaterials'.", this); enabled = false; return; }
        boundsOrigin = gameplayCamera.transform.position;
        foreach (var renderer in sceneRenderers)
        {
            if (renderer != null) { renderer.ResetBounds(); renderer.ResetLocalBounds(); }
            Register(renderer, false);
        }
        // Explicit baseline survives Edit Mode preview -> Play Mode scene snapshots.
        // Reading the preview's current value would incorrectly cache the padded value.
        if (sceneTerrain != null) terrainBounds.Add(sceneTerrain, originalTerrainPatchBounds);
        if (Application.isPlaying) PlayerHealth.Spawned += OnPlayerSpawned;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        if (Application.isPlaying) foreach (var player in FindObjectsByType<PlayerHealth>()) OnPlayerSpawned(player);
        ApplyActiveState();
    }

    private bool Required(UnityEngine.Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"CurvedWorldView: missing Inspector reference '{field}'.", this);
        return false;
    }

    private void OnValidate()
    {
        BendStrength = float.IsNaN(BendStrength) || float.IsInfinity(BendStrength) ? 0f : Mathf.Clamp(BendStrength, 0f, 0.1f);
        BendStartDistance = float.IsNaN(BendStartDistance) || float.IsInfinity(BendStartDistance) ? 5f : Mathf.Clamp(BendStartDistance, 0f, 10000f);
        if (Application.isPlaying && configured && isActiveAndEnabled) ApplyActiveState();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += RepaintPreview;
#endif
    }

#if UNITY_EDITOR
    private static void RepaintPreview()
    {
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    private void OnPlayerSpawned(PlayerHealth player)
    {
        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true)) Register(renderer, true);
    }

    private void Register(Renderer renderer, bool replaceMaterials)
    {
        if (renderer == null || !registered.Add(renderer)) return;
        var state = new RendererState { Renderer = renderer, Bounds = renderer.localBounds, WorldBounds = renderer.bounds };
        if (replaceMaterials)
        {
            state.OriginalMaterials = renderer.sharedMaterials;
            state.CurvedMaterials = (Material[])state.OriginalMaterials.Clone();
            for (int i = 0; i < state.CurvedMaterials.Length; i++)
                foreach (var replacement in playerMaterials)
                    if (state.CurvedMaterials[i] == replacement.original) { state.CurvedMaterials[i] = replacement.curved; break; }
            renderer.sharedMaterials = IsActive ? state.CurvedMaterials : state.OriginalMaterials;
        }
        renderers.Add(state);
        if (boundsReady) UpdateBounds(state);
    }

    private void UpdateBounds(RendererState state)
    {
        if (state.Renderer == null) return;
        Transform pose = state.Renderer.transform;
        Vector3 center = pose.TransformPoint(state.Bounds.center);
        Vector3 scale = pose.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        float radius = state.Bounds.extents.magnitude * maxScale;
        Vector3 delta = center - boundsOrigin;
        // An enclosing sphere is conservative even for rotated/skinned renderers.
        // Keep the original culling bounds when every vertex is in the flat area.
        bool expand = IsActive && BendStrength > 0f &&
            new Vector2(delta.x, delta.z).magnitude + radius > Mathf.Max(0f, BendStartDistance);
        bool batch = state.Renderer.isPartOfStaticBatch;
        if (state.BoundsInitialized && state.BoundsExpanded == expand && state.WasStaticBatch == batch) return;
        SetBounds(state, expand);
        state.BoundsExpanded = expand;
        state.WasStaticBatch = batch;
        state.BoundsInitialized = true;
    }

    private static void SetBounds(RendererState state, bool active)
    {
        if (state.Renderer == null) return;
        // Static batching uses combined world-space geometry; a local override can be
        // interpreted at the batch origin and incorrectly cull translated props.
        if (state.Renderer.isPartOfStaticBatch)
        {
            state.Renderer.ResetLocalBounds();
            if (!active) { state.Renderer.ResetBounds(); return; }
            Bounds world = state.WorldBounds;
            world.Encapsulate(new Bounds(world.center + Vector3.down * 64f, world.size));
            state.Renderer.bounds = world;
            return;
        }
        state.Renderer.ResetBounds();
        if (!active) { state.Renderer.localBounds = state.Bounds; return; }
        // Conservative for all allowed strengths/origins; changes culling only, never the mesh/collider.
        Bounds bounds = state.Bounds;
        Vector3 down = state.Renderer.transform.InverseTransformVector(Vector3.down * 64f);
        bounds.Encapsulate(new Bounds(state.Bounds.center + down, state.Bounds.size));
        state.Renderer.localBounds = bounds;
    }

    private void ApplyActiveState()
    {
        waterSurface.sharedMesh = IsActive ? curvedWaterMesh : originalWaterMesh;
        foreach (var pair in terrainBounds)
            if (pair.Key != null) pair.Key.patchBoundsMultiplier = IsActive && BendStrength > 0f
                ? new Vector3(pair.Value.x, Mathf.Max(pair.Value.y, 1024f), pair.Value.z) : pair.Value;
        foreach (var state in renderers)
        {
            if (state.Renderer == null) continue;
            if (state.OriginalMaterials != null)
                state.Renderer.sharedMaterials = IsActive ? state.CurvedMaterials : state.OriginalMaterials;
            if (boundsReady) UpdateBounds(state);
        }
        appliedActive = IsActive;
        hasPrevious = false;
        if (!IsActive) ClearGlobals();
    }

    private void BeforeCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!configured) return;
        Vector3 position = camera.transform.position;
        bool accepted = camera == gameplayCamera;
#if UNITY_EDITOR
        if (!Application.isPlaying && camera.cameraType == CameraType.SceneView &&
            UnityEngine.SceneManagement.SceneManager.GetActiveScene() == gameObject.scene)
        {
            foreach (UnityEditor.SceneView preview in UnityEditor.SceneView.sceneViews)
                if (preview.camera == camera) { position = preview.pivot; accepted = true; break; }
        }
#endif
        if (!accepted) return;
        boundsOrigin = position;
        // Unity finishes static batching after OnEnable. Wait until the first real
        // camera render before overriding culling bounds, then update only on toggles/spawns.
        if (!boundsReady) { boundsReady = true; ApplyActiveState(); }
        else if (appliedActive != IsActive) ApplyActiveState();
        foreach (var state in renderers) UpdateBounds(state);
        var origin = new Vector4(position.x, position.y, position.z, 0f);
        var parameters = new Vector4(IsActive ? Mathf.Clamp(BendStrength, 0f, 0.1f) : 0f,
            Mathf.Max(0f, BendStartDistance), 0f, 0f);
        if (!hasPrevious) { previousOrigin = origin; previousParameters = parameters; hasPrevious = true; }
        Shader.SetGlobalVector(OriginId, origin);
        Shader.SetGlobalVector(ParametersId, parameters);
        Shader.SetGlobalVector(PreviousOriginId, previousOrigin);
        Shader.SetGlobalVector(PreviousParametersId, previousParameters);
        if (previousFrame != Time.frameCount)
        { previousOrigin = origin; previousParameters = parameters; previousFrame = Time.frameCount; }
    }

    private static void ClearGlobals()
    {
        Shader.SetGlobalVector(ParametersId, Vector4.zero);
        Shader.SetGlobalVector(PreviousParametersId, Vector4.zero);
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        PlayerHealth.Spawned -= OnPlayerSpawned;
        ClearGlobals();
        if (waterSurface != null && originalWaterMesh != null) waterSurface.sharedMesh = originalWaterMesh;
        foreach (var pair in terrainBounds) if (pair.Key != null) pair.Key.patchBoundsMultiplier = pair.Value;
        terrainBounds.Clear();
        foreach (var state in renderers)
        {
            if (state.Renderer == null) continue;
            state.Renderer.ResetBounds();
            if (state.Renderer.isPartOfStaticBatch) state.Renderer.ResetLocalBounds();
            else state.Renderer.localBounds = state.Bounds;
            if (state.OriginalMaterials != null) state.Renderer.sharedMaterials = state.OriginalMaterials;
        }
        renderers.Clear(); registered.Clear(); hasPrevious = configured = boundsReady = false;
    }
}
