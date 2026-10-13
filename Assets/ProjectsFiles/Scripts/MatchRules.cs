using System;
using System.Collections.Generic;
using System.Linq;

// Server-owned rules, separated from Unity presentation and transport for verification.
public sealed class MatchRules
{
    public enum Stage { Preparing, Regulation, Overtime, Finished }
    [Serializable] public sealed class PlayerRecord
    {
        public ulong clientId;
        public ulong objectId;
        public string name;
        public int team;
        public int ordinal;
        public int kills;
        public int deaths;
        public bool disconnected;
        public bool alive = true;
        public double respawnAt = -1;
        public bool relocating;
        public uint life;
    }
    public readonly Dictionary<ulong, PlayerRecord> Players = new Dictionary<ulong, PlayerRecord>();
    public Stage Phase { get; private set; }
    public double EndsAt { get; private set; }
    public int DestroyedBases { get; private set; }
    public int Winner { get; private set; } = -2; // -1 draw, 0/1 team, -2 undecided.
    public bool Running => Phase == Stage.Regulation || Phase == Stage.Overtime;
    public static bool IsEnemy(int attackerTeam, int victimTeam) => attackerTeam >= 0 && attackerTeam < 2 &&
        victimTeam >= 0 && victimTeam < 2 && attackerTeam != victimTeam;
    public bool BaseAlive(int team) => team >= 0 && team < 2 && (DestroyedBases & (1 << team)) == 0;
    public bool CanReturn(PlayerRecord p) => Phase == Stage.Regulation && !p.disconnected && BaseAlive(p.team);

    public void Register(ulong clientId, ulong objectId, string name, int team, int ordinal)
    {
        if (Phase != Stage.Preparing || Players.ContainsKey(clientId) || team < 0 || team > 1 || ordinal < 0 || ordinal > 3)
            throw new InvalidOperationException("Invalid, duplicate or late player registration.");
        Players.Add(clientId, new PlayerRecord { clientId = clientId, objectId = objectId, name = name, team = team, ordinal = ordinal });
    }
    public void Start(double now, double duration)
    {
        if (Phase != Stage.Preparing) return;
        Phase = Stage.Regulation;
        EndsAt = now + duration;
    }
    public bool RecordDeath(ulong victim, ulong attackerObject, double now, double delay)
    {
        if (!Running || !Players.TryGetValue(victim, out var p) || !p.alive || p.disconnected) return false;
        p.alive = false;
        p.deaths++;
        p.life++;
        p.respawnAt = CanReturn(p) ? now + delay : -1;
        p.relocating = false;
        var attacker = Players.Values.FirstOrDefault(a => a.objectId == attackerObject);
        if (attacker != null && IsEnemy(attacker.team, p.team)) attacker.kills++;
        return true;
    }
    public bool DestroyBase(int team)
    {
        if (!Running || !BaseAlive(team)) return false;
        DestroyedBases |= 1 << team;
        CancelInvalidReturns();
        return true;
    }
    public void Disconnect(ulong clientId)
    {
        if (Phase == Stage.Finished) return;
        if (!Players.TryGetValue(clientId, out var p)) return;
        p.disconnected = true;
        p.alive = false;
        p.respawnAt = -1;
        p.relocating = false;
    }
    public bool ConfirmReturn(ulong clientId, uint life, double now)
    {
        if (now >= EndsAt || !Players.TryGetValue(clientId, out var p) || now < p.respawnAt ||
            p.life != life || !p.relocating || !CanReturn(p)) return false;
        p.alive = true;
        p.respawnAt = -1;
        p.relocating = false;
        return true;
    }
    private void CancelInvalidReturns()
    {
        foreach (var p in Players.Values)
            if (!CanReturn(p)) { p.respawnAt = -1; p.relocating = false; }
    }
    public bool TeamCanContinue(int team) => Players.Values.Any(p => p.team == team && !p.disconnected &&
        (p.alive || (CanReturn(p) && p.respawnAt >= 0)));

    // Called once after all damage/death/disconnect events in the Unity frame.
    public void Resolve(double now, double overtimeDuration)
    {
        if (!Running) return;
        if (Phase == Stage.Regulation && now >= EndsAt)
        {
            DestroyedBases = 3; // Atomic: destroy both before considering either team's viability.
            Phase = Stage.Overtime;
            EndsAt = now + overtimeDuration;
            CancelInvalidReturns();
        }
        bool first = TeamCanContinue(0), second = TeamCanContinue(1);
        if (!first || !second) Finish(first ? 0 : second ? 1 : -1);
        else if (Phase == Stage.Overtime && now >= EndsAt) Finish(-1);
    }
    private void Finish(int winner)
    {
        if (Phase == Stage.Finished) return;
        Winner = winner;
        Phase = Stage.Finished;
        CancelInvalidReturns();
    }
}
