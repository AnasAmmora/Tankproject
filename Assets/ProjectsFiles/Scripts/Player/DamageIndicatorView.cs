using UnityEngine;

public class DamageIndicatorView : MonoBehaviour
{
    [SerializeField] private RectTransform bearing;
    [SerializeField] private CanvasGroup fade;
    [SerializeField, Min(0.05f)] private float duration = 0.85f;
    private Vector3 source;
    private float expires;
    public bool ValidateReferences()
    {
        if (bearing != null && fade != null) return true;
        Debug.LogError($"DamageIndicatorView on '{name}': missing Inspector reference '{(bearing == null ? nameof(bearing) : nameof(fade))}'.", this);
        return false;
    }
    public void Show(Vector3 sourcePosition) { source = sourcePosition; expires = Time.unscaledTime + duration; }
    public void Clear() { expires = 0; if (fade != null) fade.alpha = 0; }
    public void Tick(Transform view, Vector3 playerPosition)
    {
        if (view == null || bearing == null || fade == null) return;
        fade.alpha = Mathf.Clamp01((expires - Time.unscaledTime) / duration);
        if (fade.alpha <= 0) return;
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
        Vector3 delta = Vector3.ProjectOnPlane(source - playerPosition, Vector3.up);
        if (forward.sqrMagnitude < 0.001f || delta.sqrMagnitude < 0.001f) { fade.alpha = 0; return; }
        float angle = Mathf.Atan2(Vector3.Dot(delta, Vector3.Cross(Vector3.up, forward)), Vector3.Dot(delta, forward)) * Mathf.Rad2Deg;
        bearing.localRotation = Quaternion.Euler(0, 0, -angle);
    }
}
