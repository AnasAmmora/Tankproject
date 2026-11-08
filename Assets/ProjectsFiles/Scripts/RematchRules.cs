using System.Collections.Generic;
using System.Linq;

// One frozen results session. No transport/UI work; all callers run on the server.
public sealed class RematchRules
{
    public enum State { Results, Starting, Cancelled }
    private readonly Dictionary<ulong, int> participants;
    public uint Round { get; }
    public State Phase { get; private set; }
    public int ReadyMask { get; private set; }
    public RematchRules(uint round, IEnumerable<MatchRules.PlayerRecord> players)
    { Round = round; participants = players.ToDictionary(p => p.clientId, p => p.ordinal); }
    public bool Present(IEnumerable<ulong> connected)
    { var present = new HashSet<ulong>(connected); return participants.Count >= 2 && participants.Keys.All(present.Contains); }
    public bool SetReady(uint round, ulong sender, bool ready, IEnumerable<ulong> connected)
    {
        if (round != Round || Phase != State.Results) return false;
        if (!Present(connected)) { Cancel(); return false; }
        if (!participants.TryGetValue(sender, out int ordinal)) return false;
        if (ready) ReadyMask |= 1 << ordinal; else ReadyMask &= ~(1 << ordinal);
        return true;
    }
    public bool CanStart(IEnumerable<ulong> connected) => Phase == State.Results && Present(connected) &&
        participants.Values.All(i => (ReadyMask & (1 << i)) != 0);
    public bool Start(uint round, ulong sender, IEnumerable<ulong> connected)
    {
        if (round != Round || sender != 0 || !CanStart(connected)) return false;
        Phase = State.Starting; return true;
    }
    public bool Back(uint round, ulong sender)
    {
        if (round != Round || Phase == State.Starting || !participants.ContainsKey(sender)) return false;
        Cancel(); return true;
    }
    public void Disconnect(ulong sender) { if (participants.ContainsKey(sender)) Cancel(); }
    public void Cancel() { Phase = State.Cancelled; ReadyMask = 0; }
    public void Restore(IEnumerable<ulong> connected)
    { ReadyMask = 0; Phase = Present(connected) ? State.Results : State.Cancelled; }
}
