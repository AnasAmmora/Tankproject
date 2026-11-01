using UnityEngine;

/// <summary>Local presentation only. Scales the authored name visual, never the player root.</summary>
[DisallowMultipleComponent]
public sealed class PlayerNameDistanceScale : MonoBehaviour
{
    [SerializeField] private Transform nameVisual;
    [Tooltip("Multiplier of the visual's authored local scale at nearby distances.")]
    [SerializeField, Min(0.01f)] private float nearbyBaseScale = 6f;
    [SerializeField, Min(0f)] private float enlargementStartDistance = 5f;
    [SerializeField, Min(0.01f)] private float maximumScaleDistance = 16f;
    [Tooltip("Maximum multiplier relative to the nearby size.")]
    [SerializeField, Min(1f)] private float maximumScaleMultiplier = 3.2f;
    [Tooltip("Names farther than this camera-to-label distance are hidden locally.")]
    [SerializeField, Min(0f)] private float maximumVisibleDistance = 18f;
    [Tooltip("Exponential response per second. Zero applies distance changes immediately.")]
    [SerializeField, Min(0f)] private float smoothingSpeed = 10f;

    private Vector3 authoredScale;
    private Camera lastView;
    private float currentMultiplier;
    private int lastFrame = -1;
    private bool configured;

    private void Awake()
    {
        if (nameVisual == null || nameVisual == transform || !nameVisual.IsChildOf(transform))
        {
            Debug.LogError($"PlayerNameDistanceScale on '{name}': 'nameVisual' must reference the dedicated name text child.", this);
            enabled = false;
            return;
        }
        authoredScale = nameVisual.localScale;
        configured = true;
    }

    private void OnEnable() => ResetPresentation();
    private void OnDisable() => ResetPresentation();

    public void ResetPresentation()
    {
        lastView = null;
        lastFrame = -1;
        currentMultiplier = 0f;
        if (configured && nameVisual != null) nameVisual.localScale = authoredScale;
    }

    // Called with the actual local gameplay camera before it renders. No scene lookup,
    // material instances, ownership checks, or network writes belong in this component.
    public bool Present(Camera view)
    {
        if (!configured || !isActiveAndEnabled || nameVisual == null || view == null || !view.isActiveAndEnabled) return false;
        float distance = Vector3.Distance(view.transform.position, nameVisual.position);
        if (distance > maximumVisibleDistance)
        {
            ResetPresentation();
            return false;
        }
        float t = Mathf.InverseLerp(enlargementStartDistance, maximumScaleDistance, distance);
        float target = nearbyBaseScale * Mathf.Lerp(1f, maximumScaleMultiplier, t);
        if (lastView != view || lastFrame < 0 || smoothingSpeed <= 0f)
            currentMultiplier = target;
        else if (lastFrame != Time.frameCount)
            currentMultiplier = Mathf.Lerp(currentMultiplier, target, 1f - Mathf.Exp(-smoothingSpeed * Time.unscaledDeltaTime));

        // Always derive from the original scale: repeated callbacks cannot compound it.
        nameVisual.localScale = authoredScale * currentMultiplier;
        lastView = view;
        lastFrame = Time.frameCount;
        return true;
    }

    private void OnValidate()
    {
        nearbyBaseScale = FiniteAtLeast(nearbyBaseScale, 0.01f, 6f);
        enlargementStartDistance = FiniteAtLeast(enlargementStartDistance, 0f, 5f);
        maximumScaleDistance = FiniteAtLeast(maximumScaleDistance, enlargementStartDistance + 0.01f, 16f);
        maximumScaleMultiplier = FiniteAtLeast(maximumScaleMultiplier, 1f, 3.2f);
        maximumVisibleDistance = FiniteAtLeast(maximumVisibleDistance, 0f, 18f);
        smoothingSpeed = FiniteAtLeast(smoothingSpeed, 0f, 10f);
    }

    private static float FiniteAtLeast(float value, float minimum, float fallback) =>
        Mathf.Max(minimum, float.IsNaN(value) || float.IsInfinity(value) ? fallback : value);
}
