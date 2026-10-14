using TMPro;
using UnityEngine;

// All cell layout, typography and backgrounds belong to ResultRow.prefab.
public class ResultRow : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text kills;
    [SerializeField] private TMP_Text deaths;
    [SerializeField] private GameObject evenBackground;
    [SerializeField] private GameObject oddBackground;
    [SerializeField] private GameObject localMarker;
    private bool configured;

    private void Awake() => configured = ValidateReferences();

    public bool ValidateReferences()
    {
        bool valid = true;
        valid &= Required(playerName, nameof(playerName));
        valid &= Required(kills, nameof(kills));
        valid &= Required(deaths, nameof(deaths));
        valid &= Required(evenBackground, nameof(evenBackground));
        valid &= Required(oddBackground, nameof(oddBackground));
        valid &= Required(localMarker, nameof(localMarker));
        return valid;
    }

    private bool Required(Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"ResultRow on '{name}': missing Inspector reference '{field}'.", this);
        return false;
    }

    public void Bind(MatchRules.PlayerRecord player, bool alternate, bool isLocal)
    {
        if (!configured) return;
        playerName.text = player.name + (player.disconnected ? " (Disconnected)" : "");
        kills.text = player.kills.ToString();
        deaths.text = player.deaths.ToString();
        evenBackground.SetActive(!alternate);
        oddBackground.SetActive(alternate);
        localMarker.SetActive(isLocal);
    }
}
