using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Presentation only: all graphics are authored children, never created at runtime.
[RequireComponent(typeof(Selectable))]
public sealed class UISelectionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject indicator;
    [SerializeField] private Color cream = new Color(1f, .945f, .82f);
    [SerializeField] private Color orange = new Color(1f, .52f, .1f);
    private Selectable control;
    private bool hovered, selected, pressed;
    private void Awake()
    {
        control = GetComponent<Selectable>();
        if (label == null || indicator == null)
        {
            Debug.LogError($"UISelectionFeedback on '{name}': missing Inspector reference '{(label == null ? nameof(label) : nameof(indicator))}'.", this);
            enabled = false;
        }
    }
    private void OnEnable() { hovered = selected = pressed = false; Refresh(); }
    private void OnDisable() { hovered = selected = pressed = false; Refresh(); }
    private void Refresh()
    {
        if (label == null || indicator == null) return;
        bool available = (control != null ? control : GetComponent<Selectable>()).IsInteractable();
        wasAvailable = available;
        indicator.SetActive(available && (hovered || selected));
        label.color = !available ? new Color(cream.r, cream.g, cream.b, .38f) : pressed ? orange : cream;
    }
    // Server-driven enabled/disabled controls update without changing their events.
    private void LateUpdate()
    {
        bool available = control != null && control.IsInteractable();
        if (available != wasAvailable) { wasAvailable = available; Refresh(); }
    }
    private bool wasAvailable;
    public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData e) { hovered = pressed = false; Refresh(); }
    public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData e) { selected = pressed = false; Refresh(); }
    public void OnPointerDown(PointerEventData e) { pressed = true; Refresh(); }
    public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }
}
