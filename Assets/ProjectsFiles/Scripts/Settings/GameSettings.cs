using UnityEngine;

// One application-wide settings service; no scene managers or audio-source rewrites.
public static class GameSettings
{
    public const string VolumeKey = "TanksAway.MasterVolume";
    public const string QualityKey = "TanksAway.GraphicsQuality";
    public static float MasterVolume { get; private set; }
    public static int GraphicsQuality => QualitySettings.GetQualityLevel();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var defaults = Resources.Load<GameSettingsDefaults>("TanksAwaySettingsDefaults");
        if (defaults == null) { Debug.LogError("GameSettings: missing Resources/TanksAwaySettingsDefaults asset."); return; }
        float volume = PlayerPrefs.GetFloat(VolumeKey, defaults.masterVolume);
        if (float.IsNaN(volume) || float.IsInfinity(volume)) volume = defaults.masterVolume;
        MasterVolume = Mathf.Clamp01(volume);
        AudioListener.volume = MasterVolume;
        int fallback = defaults.qualityLevel >= 0 && defaults.qualityLevel < QualitySettings.names.Length
            ? defaults.qualityLevel : QualitySettings.GetQualityLevel();
        int saved = PlayerPrefs.GetInt(QualityKey, fallback);
        QualitySettings.SetQualityLevel(saved >= 0 && saved < QualitySettings.names.Length ? saved : fallback, true);
        Application.quitting -= Flush;
        Application.quitting += Flush;
    }
    public static void SetMasterVolume(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, MasterVolume)) return;
        MasterVolume = value;
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumeKey, value);
    }
    public static void SetGraphicsQuality(int level)
    {
        if (level < 0 || level >= QualitySettings.names.Length || level == GraphicsQuality) return;
        QualitySettings.SetQualityLevel(level, true);
        PlayerPrefs.SetInt(QualityKey, level);
        Flush();
    }
    public static void Flush() => PlayerPrefs.Save();
}
