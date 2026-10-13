using System;

[Serializable]
public sealed class MatchResults
{
    public int winner;
    public MatchRules.PlayerRecord[] players;
    // A detached JSON snapshot survives NetworkObject despawn and host shutdown.
    public static MatchResults LocalFinal { get; private set; }
    public static void Store(string json) => LocalFinal = UnityEngine.JsonUtility.FromJson<MatchResults>(json);
    public static void Clear() => LocalFinal = null;
}
