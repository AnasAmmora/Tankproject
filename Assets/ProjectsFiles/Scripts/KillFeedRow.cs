using TMPro;
using UnityEngine;

public class KillFeedRow : MonoBehaviour
{
    [SerializeField] private TMP_Text killer;
    [SerializeField] private TMP_Text action;
    [SerializeField] private TMP_Text victim;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private Color blue = new Color(0.55f, 0.74f, 0.95f);
    [SerializeField] private Color red = new Color(0.95f, 0.6f, 0.62f);
    private float expires;
    public bool ValidateReferences()
    {
        if (killer != null && action != null && victim != null && fade != null) return true;
        Debug.LogError($"KillFeedRow on '{name}': missing Inspector reference '{(killer == null ? nameof(killer) : action == null ? nameof(action) : victim == null ? nameof(victim) : nameof(fade))}'.", this);
        return false;
    }
    public void Bind(CombatFeedback feedback, float lifetime)
    {
        killer.gameObject.SetActive(feedback.HasAttacker);
        killer.text = feedback.AttackerName;
        killer.color = feedback.AttackerTeam == 0 ? blue : red;
        victim.text = feedback.VictimName;
        victim.color = feedback.VictimTeam == 0 ? blue : red;
        action.text = feedback.HasAttacker ? "eliminated" : "Eliminated:";
        expires = Time.unscaledTime + lifetime;
        fade.alpha = 1f;
    }
    public bool Tick()
    {
        fade.alpha = Mathf.Clamp01((expires - Time.unscaledTime) / 0.35f);
        return Time.unscaledTime < expires;
    }
}
