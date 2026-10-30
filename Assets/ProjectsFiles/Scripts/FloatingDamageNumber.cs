using UnityEngine;
using TMPro;

/// <summary>Local, pooled presentation of one confirmed damage event; no networking or damage logic.</summary>
public sealed class FloatingDamageNumber : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Vector3 impactOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField, Min(0f)] private float horizontalSpread = 0.15f;
    [SerializeField, Min(0.05f)] private float lifetime = 0.9f;
    [SerializeField, Min(0f)] private float riseDistance = 1.2f;
    [Tooltip("Text height in screen pixels before the pop multiplier.")]
    [SerializeField, Min(1f)] private float scale = 30f;
    [SerializeField] private Color color = new Color(1f, 0.85f, 0.25f, 1f);
    [SerializeField, Min(1f)] private float popScale = 1.3f;
    [SerializeField, Range(0.01f, 0.5f)] private float popDuration = 0.2f;
    [SerializeField, Range(0f, 0.99f)] private float fadeStart = 0.35f;
    private Vector3 origin;
    private float elapsed;
    private float textHeight;
    private bool playing;

    public bool ValidateReferences()
    {
        if (label != null) return true;
        Debug.LogError($"FloatingDamageNumber on '{name}': missing Inspector reference 'label'.", this);
        return false;
    }

    public void Show(int damage, Vector3 impact, Camera view)
    {
        playing = false;
        if (!ValidateReferences() || damage <= 0) return;
        elapsed = 0f;
        origin = impact + impactOffset;
        if (view != null) origin += view.transform.right * Random.Range(-horizontalSpread, horizontalSpread);
        transform.SetPositionAndRotation(origin, view != null ? view.transform.rotation : Quaternion.identity);
        transform.localScale = Vector3.one;
        label.text = damage.ToString();
        label.color = color;
        label.ForceMeshUpdate();
        textHeight = Mathf.Max(0.01f, label.textBounds.size.y);
        playing = true;
        Tick(0f, view);
    }

    // The owning HUD ticks after camera updates and returns completed instances to the shared pool.
    public bool Tick(float deltaTime, Camera view)
    {
        if (!playing) return false;
        elapsed += Mathf.Max(0f, deltaTime);
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, lifetime));
        if (t >= 1f) { playing = false; return false; }
        transform.position = origin + Vector3.up * (riseDistance * t);
        float popTime = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, popDuration));
        float pop = popTime < 0.4f ? Mathf.Lerp(0.65f, popScale, popTime / 0.4f)
            : Mathf.Lerp(popScale, 1f, (popTime - 0.4f) / 0.6f);
        Color tint = color;
        tint.a *= 1f - Mathf.InverseLerp(Mathf.Clamp(fadeStart, 0f, 0.99f), 1f, t);
        label.color = tint;
        if (view == null) { label.alpha = 0f; return true; }
        transform.rotation = view.transform.rotation;
        float depth = Vector3.Dot(transform.position - view.transform.position, view.transform.forward);
        if (depth <= view.nearClipPlane) { label.alpha = 0f; return true; }
        float visibleHeight = view.orthographic ? 2f * view.orthographicSize
            : 2f * depth * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float worldScale = Mathf.Max(1f, scale) * visibleHeight / Mathf.Max(1, view.pixelHeight) / textHeight;
        transform.localScale = Vector3.one * (worldScale * pop);
        return true;
    }

    private void OnDisable()
    {
        playing = false;
        elapsed = textHeight = 0f;
        origin = Vector3.zero;
        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        if (label == null) return;
        label.text = string.Empty;
        label.color = color;
    }
}
