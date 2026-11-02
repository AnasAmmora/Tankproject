using UnityEngine;

// The lobby already adds/removes authored room prefabs. React to that change only.
public sealed class MenuListEmptyState : MonoBehaviour
{
    [SerializeField] private GameObject emptyMessage;
    private void OnEnable() => Refresh();
    private void OnTransformChildrenChanged() => Refresh();
    private void Refresh()
    {
        if (emptyMessage == null)
        {
            Debug.LogError($"MenuListEmptyState on '{name}': missing Inspector reference 'emptyMessage'.", this);
            return;
        }
        emptyMessage.SetActive(transform.childCount == 0);
    }
}
