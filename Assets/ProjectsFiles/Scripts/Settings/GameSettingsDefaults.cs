using UnityEngine;

[CreateAssetMenu(menuName = "Tanks Away/Settings Defaults")]
public sealed class GameSettingsDefaults : ScriptableObject
{
    [Range(0, 1)] public float masterVolume = 1f;
    [Tooltip("-1 uses the configured platform quality level.")]
    public int qualityLevel = -1;
}
