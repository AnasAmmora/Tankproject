using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Select an authored control when its panel opens; no polling or UI construction.
public sealed class MenuPanelFocus : MonoBehaviour
{
    [SerializeField] private Selectable initialSelection;
    private void OnEnable() => StartCoroutine(Focus());
    private IEnumerator Focus()
    {
        yield return null; // Let the existing menu flow enable its controls first.
        if (initialSelection == null)
        {
            Debug.LogError($"MenuPanelFocus on '{name}': missing Inspector reference 'initialSelection'.", this);
            yield break;
        }
        if (EventSystem.current != null && initialSelection.IsActive() && initialSelection.IsInteractable())
            initialSelection.Select();
    }
    private void OnDisable() => StopAllCoroutines();
}
