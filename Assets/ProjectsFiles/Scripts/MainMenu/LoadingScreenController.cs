using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Presentation only. All layout and artwork are authored in LoadingCanvas.prefab.
public class LoadingScreenController : MonoBehaviour
{
    public static LoadingScreenController Instance { get; private set; }
    public event Action ReturnRequested;
    [SerializeField] private Canvas overlay;
    [SerializeField] private GraphicRaycaster inputBlocker;
    [SerializeField] private TMP_Text status;
    [SerializeField] private Button returnButton;
    public bool IsConfigured { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (overlay != null) overlay.enabled = false;
            if (inputBlocker != null) inputBlocker.enabled = false;
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsConfigured = ValidateReferences();
        if (!IsConfigured) { enabled = false; return; }
        DontDestroyOnLoad(gameObject);
        returnButton.onClick.AddListener(OnReturnClicked);
        Hide();
    }

    private bool ValidateReferences()
    {
        bool valid = true;
        if (overlay == null) { Missing(nameof(overlay)); valid = false; }
        if (inputBlocker == null) { Missing(nameof(inputBlocker)); valid = false; }
        if (status == null) { Missing(nameof(status)); valid = false; }
        if (returnButton == null) { Missing(nameof(returnButton)); valid = false; }
        if (transform.parent != null || (overlay != null && overlay.gameObject != gameObject))
        {
            Debug.LogError("LoadingScreenController must be attached to the LoadingCanvas prefab root, with overlay pointing to its root Canvas.", this);
            valid = false;
        }
        return valid;
    }

    private void Missing(string field) => Debug.LogError($"LoadingScreenController on '{name}': missing Inspector reference '{field}'.", this);

    public void Show(string message)
    {
        if (!IsConfigured) return;
        // Keep the root active so its persistent controller remains available.
        overlay.enabled = true;
        inputBlocker.enabled = true;
        status.text = message;
        returnButton.gameObject.SetActive(false);
    }

    public void ShowError(string message, bool canReturn)
    {
        if (!IsConfigured) return;
        Show(message);
        returnButton.gameObject.SetActive(true);
        returnButton.interactable = canReturn;
    }

    public void Hide()
    {
        if (!IsConfigured) return;
        overlay.enabled = false;
        inputBlocker.enabled = false;
        returnButton.gameObject.SetActive(false);
    }

    private void OnReturnClicked() => ReturnRequested?.Invoke();
    private void OnDestroy()
    {
        if (returnButton != null) returnButton.onClick.RemoveListener(OnReturnClicked);
        if (Instance == this) Instance = null;
        ReturnRequested = null;
    }
}
