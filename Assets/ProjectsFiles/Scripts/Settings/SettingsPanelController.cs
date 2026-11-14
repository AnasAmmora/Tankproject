using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Binds authored controls only. The active Canvas owns this while the panel is hidden.
public sealed class SettingsPanelController : MonoBehaviour
{
    [SerializeField] private GameObject firstMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private TMP_Text volumePercent;
    [SerializeField] private TMP_Dropdown graphicsQuality;
    private bool bound;
    private void Awake()
    {
        bool valid = Required(firstMenuPanel, nameof(firstMenuPanel)) & Required(settingsPanel, nameof(settingsPanel)) &
            Required(settingsButton, nameof(settingsButton)) & Required(backButton, nameof(backButton)) &
            Required(masterVolume, nameof(masterVolume)) & Required(volumePercent, nameof(volumePercent)) &
            Required(graphicsQuality, nameof(graphicsQuality));
        if (!valid) { enabled = false; return; }
        settingsPanel.SetActive(false);
        settingsButton.onClick.AddListener(Open);
        backButton.onClick.AddListener(Back);
        masterVolume.onValueChanged.AddListener(OnVolume);
        graphicsQuality.onValueChanged.AddListener(OnQuality);
        bound = true;
    }
    private bool Required(Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"SettingsPanelController on '{name}': missing Inspector reference '{field}'.", this);
        return false;
    }
    public void Open()
    {
        if (!bound || !firstMenuPanel.activeSelf) return;
        masterVolume.SetValueWithoutNotify(GameSettings.MasterVolume);
        volumePercent.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100) + "%";
        graphicsQuality.ClearOptions();
        graphicsQuality.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        graphicsQuality.SetValueWithoutNotify(GameSettings.GraphicsQuality);
        graphicsQuality.RefreshShownValue();
        firstMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        masterVolume.Select();
    }
    public void Back()
    {
        if (!bound || !settingsPanel.activeSelf) return;
        GameSettings.Flush();
        settingsPanel.SetActive(false);
        firstMenuPanel.SetActive(true);
        settingsButton.Select();
        StartCoroutine(RestoreFocus());
    }
    private System.Collections.IEnumerator RestoreFocus()
    {
        yield return null; // After the existing first-panel MenuPanelFocus coroutine.
        if (firstMenuPanel.activeInHierarchy && !settingsPanel.activeSelf) settingsButton.Select();
    }
    private void OnVolume(float value)
    {
        GameSettings.SetMasterVolume(value);
        volumePercent.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100) + "%";
    }
    private void OnQuality(int value) => GameSettings.SetGraphicsQuality(value);
    private void OnDisable() { if (bound) GameSettings.Flush(); }
    private void OnDestroy()
    {
        if (!bound) return;
        settingsButton.onClick.RemoveListener(Open);
        backButton.onClick.RemoveListener(Back);
        masterVolume.onValueChanged.RemoveListener(OnVolume);
        graphicsQuality.onValueChanged.RemoveListener(OnQuality);
    }
}
