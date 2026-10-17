using UnityEngine;

// Presentation event, emitted only after the server accepted damage/death.
public struct CombatFeedback
{
    public ulong Sequence;
    public ulong VictimId;
    public ulong AttackerId;
    public bool HasAttacker;
    public int VictimTeam;
    public int AttackerTeam;
    public string VictimName;
    public string AttackerName;
    public bool IsKill;
    public bool IsBase;
    public bool HasSource;
    public Vector3 Source;
}
