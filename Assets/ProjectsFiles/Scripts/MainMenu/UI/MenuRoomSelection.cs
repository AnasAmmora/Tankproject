using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Keep keyboard/gamepad selection visible in the authored room ScrollRect.
public sealed class MenuRoomSelection : MonoBehaviour, ISelectHandler
{
    [SerializeField] private RectTransform roomRow;
    public void OnSelect(BaseEventData eventData)
    {
        if (roomRow == null)
        {
            Debug.LogError($"MenuRoomSelection on '{name}': missing Inspector reference 'roomRow'.", this);
            return;
        }
        var scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || scroll.viewport == null) return;
        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, roomRow);
        var viewport = scroll.viewport.rect;
        float correction = bounds.min.y < viewport.yMin ? viewport.yMin - bounds.min.y
            : bounds.max.y > viewport.yMax ? viewport.yMax - bounds.max.y : 0f;
        if (Mathf.Approximately(correction, 0f)) return;
        scroll.StopMovement();
        Vector2 position = scroll.content.anchoredPosition;
        position.y = Mathf.Clamp(position.y + correction, 0f, Mathf.Max(0f, scroll.content.rect.height - viewport.height));
        scroll.content.anchoredPosition = position;
    }
}
