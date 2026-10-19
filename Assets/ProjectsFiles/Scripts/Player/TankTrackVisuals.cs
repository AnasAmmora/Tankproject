using UnityEngine;

[DefaultExecutionOrder(200)]
public sealed class TankTrackVisuals : MonoBehaviour
{
    [Header("Authored model references")]
    [SerializeField] private Transform[] wheels = new Transform[6];
    [SerializeField] private Renderer trackRenderer;
    [SerializeField, Min(0)] private int materialSlot;
    [SerializeField] private string textureProperty = "_BaseMap";

    [Header("Visual speeds at full motion")]
    [SerializeField, Min(0f)] private float wheelDegreesPerSecond = 360f;
    [SerializeField, Min(0f)] private float trackCyclesPerSecond = 0.75f;
    [SerializeField, Min(0f)] private float driveContribution = 1f;
    [SerializeField, Min(0f)] private float turnContribution = 1f;
    [SerializeField, Range(0f, 0.2f)] private float motionDeadZone = 0.02f;

    private Quaternion[] restRotations;
    private Material originalMaterial;
    private Material trackMaterial;
    private int textureId;
    private int boundSlot;
    private Vector2 initialOffset;
    private float motion;
    private float wheelAngle;
    private float trackPhase;

    private void Awake()
    {
        if (wheels == null || wheels.Length != 6)
        { ConfigurationError("wheels must contain exactly six Transforms."); return; }
        for (int i = 0; i < wheels.Length; i++)
        {
            Transform wheel = wheels[i];
            if (wheel == null || wheel == transform || !wheel.IsChildOf(transform) ||
                wheel.GetComponentInChildren<Collider>(true) != null || wheel.GetComponentInChildren<Rigidbody>(true) != null)
            { ConfigurationError($"wheels[{i}] must reference a visual child without physics components."); return; }
            for (int j = 0; j < i; j++)
                if (wheels[j] == wheel)
                { ConfigurationError($"wheels[{i}] duplicates wheels[{j}]."); return; }
        }
        if (trackRenderer == null || !trackRenderer.transform.IsChildOf(transform))
        { ConfigurationError("trackRenderer must reference the tank's track Renderer."); return; }
        Material[] materials = trackRenderer.sharedMaterials;
        if (materialSlot < 0 || materialSlot >= materials.Length || materials[materialSlot] == null)
        { ConfigurationError("materialSlot must select an existing material on trackRenderer."); return; }
        if (string.IsNullOrWhiteSpace(textureProperty) || !materials[materialSlot].HasProperty(textureProperty))
        { ConfigurationError($"textureProperty '{textureProperty}' is missing from the selected track material."); return; }

        restRotations = new Quaternion[wheels.Length];
        for (int i = 0; i < wheels.Length; i++) restRotations[i] = wheels[i].localRotation;
        boundSlot = materialSlot;
        textureId = Shader.PropertyToID(textureProperty);
        originalMaterial = materials[boundSlot];
        initialOffset = originalMaterial.GetTextureOffset(textureId);
        trackMaterial = new Material(originalMaterial) { name = originalMaterial.name + " (Track instance)" };
        materials[boundSlot] = trackMaterial;
        trackRenderer.sharedMaterials = materials;
    }

    /// <param name="forward">Signed drive amount, normalized to -1..1.</param>
    /// <param name="turn">Signed turn amount, normalized to -1..1.</param>
    public void SetMotion(float forward, float turn)
    {
        forward = Mathf.Clamp(forward, -1f, 1f);
        turn = Mathf.Clamp(turn, -1f, 1f);
        if (Mathf.Abs(forward) <= motionDeadZone) forward = 0f;
        if (Mathf.Abs(turn) <= motionDeadZone) turn = 0f;
        float amount = Mathf.Max(Mathf.Abs(forward) * driveContribution, Mathf.Abs(turn) * turnContribution);
        motion = (forward < 0f ? -1f : 1f) * amount;
    }

    private void LateUpdate()
    {
        if (trackMaterial == null || motion == 0f) return;
        float travel = motion * Time.deltaTime;
        wheelAngle = Mathf.Repeat(wheelAngle + travel * wheelDegreesPerSecond, 360f);
        Quaternion spin = Quaternion.AngleAxis(wheelAngle, Vector3.right);
        for (int i = 0; i < wheels.Length; i++)
            wheels[i].localRotation = restRotations[i] * spin;

        trackPhase = (trackPhase + travel * trackCyclesPerSecond) % 1f;
        trackMaterial.SetTextureOffset(textureId, new Vector2(initialOffset.x, initialOffset.y + trackPhase));
    }

    private void OnEnable() => SetMotion(0f, 0f);
    private void OnDisable() => SetMotion(0f, 0f); 

    private void ConfigurationError(string message)
    {
        Debug.LogError("TankTrackVisuals: " + message, this);
        enabled = false;
    }

    private void OnDestroy()
    {
        if (trackMaterial == null) return;
        if (trackRenderer != null)
        {
            Material[] materials = trackRenderer.sharedMaterials;
            if (boundSlot < materials.Length && materials[boundSlot] == trackMaterial)
            {
                materials[boundSlot] = originalMaterial;
                trackRenderer.sharedMaterials = materials;
            }
        }
        Destroy(trackMaterial);
        trackMaterial = null;
    }
}
