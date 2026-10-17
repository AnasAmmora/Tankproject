using System;

// Formatting replicated state only; these helpers never change match/life rules.
public static class HudPresentation
{
    public static string Clock(double seconds)
    {
        int whole = (int)Math.Max(0, Math.Ceiling(seconds));
        return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
    }

    public static string Life(MatchRules.Stage phase, bool prepared, bool dead, double respawnAt,
        int team, int destroyedBases, double serverNow, bool disconnected = false)
    {
        if (disconnected) return "Disconnected";
        if (!prepared || phase == MatchRules.Stage.Preparing) return "Waiting for match";
        if (!dead) return "Alive";
        bool baseAlive = team >= 0 && team < 2 && (destroyedBases & (1 << team)) == 0;
        return phase == MatchRules.Stage.Regulation && baseAlive && respawnAt >= 0
            ? "Respawn in " + Math.Max(0, Math.Ceiling(respawnAt - serverNow)) + "s"
            : "Eliminated — No respawns";
    }

    public static float Reload(float remaining, float duration) => duration <= 0 ? 1f :
        Math.Max(0f, Math.Min(1f, 1f - remaining / duration));
}
