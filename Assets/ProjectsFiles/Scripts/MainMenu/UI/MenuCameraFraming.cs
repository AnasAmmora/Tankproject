using UnityEngine;

// Only the decorative main-menu camera. Gameplay cameras keep their existing FOV.
[RequireComponent(typeof(Camera))]
public sealed class MenuCameraFraming : MonoBehaviour
{
    [SerializeField, Range(20f, 90f)] private float verticalFieldOfView = 54f;
    [SerializeField, Min(1f)] private float referenceAspect = 16f / 9f;
    private Camera view;
    private float previousAspect;
    private void OnEnable() { view = GetComponent<Camera>(); previousAspect = -1; Refresh(); }
    private void LateUpdate() => Refresh();
    private void Refresh()
    {
        if (view == null || Mathf.Approximately(previousAspect, view.aspect)) return;
        previousAspect = view.aspect;
        float expansion = Mathf.Max(1f, referenceAspect / Mathf.Max(.1f, view.aspect));
        view.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(verticalFieldOfView * Mathf.Deg2Rad * .5f) * expansion) * Mathf.Rad2Deg;
    }
}
