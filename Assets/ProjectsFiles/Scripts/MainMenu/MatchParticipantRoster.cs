using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// Frozen lobby membership and actual NGO sender identities. No Unity callbacks here.
public sealed class MatchParticipantRoster
{
    public sealed class Assignment
    {
        public string PlayerId;
        public ulong ClientId;
        public int Ordinal;
        public bool SceneSynchronized;
        public bool Prepared;
        public bool Ready;
        public int SpawnIndex => new[] { 0, 2, 1, 3 }[Ordinal];
        public int Team => Ordinal % 2;
    }

    private readonly Dictionary<string, string> expected;
    private readonly Dictionary<ulong, Assignment> clients = new Dictionary<ulong, Assignment>();
    private readonly string hostId;
    public int ExpectedCount => expected.Count;
    public IEnumerable<Assignment> Assignments => clients.Values;
    public bool AllReady => clients.Count == expected.Count && clients.Values.All(a => a.Ready);

    public MatchParticipantRoster(Dictionary<string, string> tokens, string hostPlayerId)
    {
        if (tokens.Count < 2 || tokens.Count > 4 || !tokens.ContainsKey(hostPlayerId))
            throw new ArgumentException("A match requires two to four registered lobby participants.");
        expected = new Dictionary<string, string>(tokens);
        hostId = hostPlayerId;
    }

    public static string TokenHash(string token)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(token ?? ""))).Replace("-", "");
    }

    public bool TryBind(ulong clientId, string playerId, string token, out string reason)
    {
        reason = "The connection does not belong to the frozen lobby roster.";
        if (string.IsNullOrEmpty(playerId) || !expected.TryGetValue(playerId, out string hash) ||
            hash != TokenHash(token)) return false;
        if (clients.TryGetValue(clientId, out Assignment existing))
            return existing.PlayerId == playerId;
        if (clients.Values.Any(a => a.PlayerId == playerId))
        { reason = "This lobby participant is already connected."; return false; }
        int ordinal = playerId == hostId ? 0 : Enumerable.Range(1, 3)
            .First(i => !clients.Values.Any(a => a.Ordinal == i));
        clients.Add(clientId, new Assignment { ClientId = clientId, PlayerId = playerId, Ordinal = ordinal });
        reason = "";
        return true;
    }

    public bool TryGet(ulong clientId, out Assignment assignment) => clients.TryGetValue(clientId, out assignment);
    public bool MarkReady(ulong clientId)
    {
        if (!clients.TryGetValue(clientId, out Assignment a) || !a.SceneSynchronized || !a.Prepared || a.Ready)
            return false;
        a.Ready = true;
        return true;
    }

    public string WaitingFor => string.Join(", ", expected.Keys.Where(id =>
        !clients.Values.Any(a => a.PlayerId == id && a.Ready)));
}
