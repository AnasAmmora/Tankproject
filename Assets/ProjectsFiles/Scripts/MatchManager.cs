using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints = new Transform[4];
    [Header("Match Rules (seconds)")]
    [SerializeField, Min(1)] private float regulationDuration = 300f;
    [SerializeField, Min(1)] private float overtimeDuration = 120f;
    [SerializeField, Min(0)] private float respawnDelay = 3f;
    [SerializeField] private TeamBase[] teamBases = new TeamBase[2];
    public readonly NetworkVariable<int> phase = new NetworkVariable<int>((int)MatchRules.Stage.Preparing);
    public readonly NetworkVariable<double> phaseEndsAt = new NetworkVariable<double>(0);
    public readonly NetworkVariable<int> destroyedBases = new NetworkVariable<int>(0);
    public readonly NetworkVariable<int> winningTeam = new NetworkVariable<int>(-2);
    public readonly NetworkVariable<FixedString4096Bytes> statistics = new NetworkVariable<FixedString4096Bytes>();
    private readonly MatchRules rules = new MatchRules();
    private uint publishedStatisticsRevision;
    private bool statisticsPublished;
    public ShotPresentation Projectiles { get; } = new ShotPresentation();
    private readonly HashSet<(ulong, uint)> resolvedProjectiles = new HashSet<(ulong, uint)>();
    private readonly HashSet<ulong> finalReceipts = new HashSet<ulong>();
    public bool FinalSnapshotsDelivered => IsServer && NetworkManager.ConnectedClientsIds.All(id => finalReceipts.Contains(id));
    public bool Running => IsSpawned && (phase.Value == (int)MatchRules.Stage.Regulation || phase.Value == (int)MatchRules.Stage.Overtime);
    public static bool CanControl(PlayerHealth health) => MatchConnectionCoordinator.GameplayAllowed &&
        (Instance == null || Instance.Running) && (health == null || (!health.isDead.Value && health.isPreparedForLife &&
        (!health.IsOwner || health.GetComponent<PlayerLook>() == null || health.GetComponent<PlayerLook>().LocalCameraReady)));
    private readonly NetworkVariable<bool> matchStarted = new NetworkVariable<bool>(false);
    private readonly Dictionary<ulong, Coroutine> preparation = new Dictionary<ulong, Coroutine>();
    private readonly HashSet<ulong> scheduled = new HashSet<ulong>();
    private bool aborted;
    private MatchConnectionCoordinator coordinator;
    public event System.Action<CombatFeedback> CombatConfirmed;
    private ulong feedbackSequence;
    private ulong lastFeedbackSequence;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Projectiles.Clear();
        resolvedProjectiles.Clear();
        statisticsPublished = false;
        feedbackSequence = lastFeedbackSequence = 0;
        coordinator = MatchConnectionCoordinator.Instance;
        MatchConnectionCoordinator.Trace("MatchManager spawned", "rulesPhase=" + rules.Phase);
        if (IsServer && !ValidateBases()) { coordinator?.Fail("GameScene requires exactly one networked base for each team (0 and 1)."); return; }
        matchStarted.OnValueChanged += OnStartedChanged;
        if (coordinator == null || !coordinator.TransitionActive) return;
        if (IsServer) NetworkManager.OnClientDisconnectCallback += OnDisconnected;
        if (IsClient) StartCoroutine(WaitForLocalReadiness());
        if (matchStarted.Value) coordinator.NotifyMatchStarted();
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || coordinator == null || !coordinator.TransitionActive ||
            coordinator.Roster == null || matchStarted.Value || aborted) return;
        // Scheduling by clientId prevents duplicated coroutines across load/connect events.
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (!coordinator.Roster.TryGet(clientId, out _) || !scheduled.Add(clientId)) continue;
            preparation[clientId] = StartCoroutine(PreparePlayer(clientId));
        }
    }

    private IEnumerator PreparePlayer(ulong clientId)
    {
        while (!aborted && coordinator.TransitionActive && NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            if (Time.realtimeSinceStartupAsDouble > coordinator.PreparationDeadline)
            { coordinator.Fail("Timed out preparing player " + clientId); yield break; }
            if (coordinator.Roster.TryGet(clientId, out var assignment) && assignment.SceneSynchronized)
            {
                if (spawnPoints.Length != 4 || spawnPoints[assignment.SpawnIndex] == null)
                { coordinator.Fail("A required spawn point is missing."); yield break; }
                if (NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId) != null)
                { coordinator.Fail("An unexpected automatic PlayerObject already exists."); yield break; }
                GameObject prefab = NetworkManager.NetworkConfig.PlayerPrefab;
                if (prefab == null || prefab.GetComponent<NetworkObject>() == null || prefab.GetComponent<PlayerState>() == null)
                { coordinator.Fail("The player prefab requires NetworkObject and PlayerState."); yield break; }
                Transform spawn = spawnPoints[assignment.SpawnIndex];
                try
                {
                    GameObject player = Instantiate(prefab, spawn.position, spawn.rotation);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player, gameObject.scene);
                    NetworkObject playerObject = player.GetComponent<NetworkObject>();
                    playerObject.SpawnAsPlayerObject(clientId, true);
                    PlayerState state = player.GetComponent<PlayerState>();
                    state.teamIndex.Value = assignment.Team;
                    state.spawnOrdinal.Value = assignment.Ordinal;
                    state.isPrepared.Value = true;
                    rules.Register(clientId, playerObject.NetworkObjectId, coordinator.ParticipantName(assignment.PlayerId), assignment.Team, assignment.Ordinal);
                    assignment.Prepared = true;
                    MatchConnectionCoordinator.Trace("Player prepared", $"client={clientId} team={assignment.Team} spawn={assignment.SpawnIndex}");
                }
                catch (System.Exception e) { coordinator.Fail("Could not prepare player " + clientId + ": " + e.Message); }
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator WaitForLocalReadiness()
    {
        bool sent = false;
        while (IsSpawned && coordinator.TransitionActive && !sent && !matchStarted.Value)
        {
            if (Time.realtimeSinceStartupAsDouble > coordinator.PreparationDeadline)
            { coordinator.Fail("Timed out waiting for scene synchronization, player setup or the local camera."); yield break; }
            NetworkObject player = NetworkManager.LocalClient?.PlayerObject;
            if (coordinator.LocalSceneSynchronized && player != null && player.IsSpawned &&
                player.TryGetComponent<PlayerState>(out var state) && state.isPrepared.Value &&
                state.spawnOrdinal.Value >= 0 && state.spawnOrdinal.Value < 4 &&
                state.teamIndex.Value == state.spawnOrdinal.Value % 2 &&
                player.TryGetComponent<PlayerLook>(out var look) && look.LocalCameraReady && AllPlayersVisibleAndPrepared())
            {
                int spawnIndex = new[] { 0, 2, 1, 3 }[state.spawnOrdinal.Value];
                Transform spawn = spawnPoints[spawnIndex];
                if (spawn != null && Vector3.Distance(player.transform.position, spawn.position) < 0.05f &&
                    Quaternion.Angle(player.transform.rotation, spawn.rotation) < 1f)
                {
                    // Give camera/replica setup one complete presentation frame under the overlay.
                    yield return null;
                    if (!IsSpawned || !coordinator.TransitionActive) yield break;
                    ReadyRpc(player.NetworkObjectId);
                    MatchConnectionCoordinator.Trace("Local ready", "scene, player and camera prepared");
                    sent = true;
                }
            }
            yield return null;
        }
    }

    private bool AllPlayersVisibleAndPrepared()
    {
        int count = 0;
        foreach (NetworkObject obj in NetworkManager.SpawnManager.SpawnedObjectsList)
        {
            if (!obj.IsPlayerObject || !obj.TryGetComponent<PlayerState>(out var state)) continue;
            if (!state.isPrepared.Value || state.teamIndex.Value < 0) return false;
            count++;
        }
        return count == coordinator.ExpectedCount;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReadyRpc(ulong playerObjectId, RpcParams rpcParams = default)
    {
        if (aborted || matchStarted.Value || coordinator == null || coordinator.Roster == null) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!coordinator.Roster.TryGet(sender, out var assignment)) return;
        NetworkObject player = NetworkManager.SpawnManager.GetPlayerNetworkObject(sender);
        if (player == null || player.NetworkObjectId != playerObjectId || !player.TryGetComponent<PlayerState>(out var state) ||
            !state.isPrepared.Value || state.spawnOrdinal.Value != assignment.Ordinal || state.teamIndex.Value != assignment.Team)
            return;
        Transform spawn = spawnPoints[assignment.SpawnIndex];
        if (Vector3.Distance(player.transform.position, spawn.position) > 0.05f) return;
        if (!coordinator.Roster.MarkReady(sender)) return;
        MatchConnectionCoordinator.Trace("Ready accepted", "sender=" + sender);
        if (coordinator.Roster.AllReady)
        {
            rules.Start(NetworkManager.ServerTime.Time, regulationDuration);
            PublishState();
            matchStarted.Value = true;
        }
    }

    private void OnStartedChanged(bool previous, bool current)
    {
        if (current) coordinator?.NotifyMatchStarted();
    }

    private void OnDisconnected(ulong clientId)
    {
        if (preparation.TryGetValue(clientId, out Coroutine routine) && routine != null) StopCoroutine(routine);
        preparation.Remove(clientId);
        if (rules.Running) rules.Disconnect(clientId);
        // The coordinator aborts preparation; disconnected slots are not reassigned mid-transition.
    }

    public void BroadcastAbort(string reason)
    {
        if (!IsServer || aborted) return;
        aborted = true;
        AbortMatchRpc(reason);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void AbortMatchRpc(string reason) => coordinator?.ReturnAfterFailure(reason);

    public override void OnNetworkDespawn()
    {
        Projectiles.Clear();
        resolvedProjectiles.Clear();
        matchStarted.OnValueChanged -= OnStartedChanged;
        if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnDisconnected;
        StopAllCoroutines();
        preparation.Clear();
        scheduled.Clear();
        if (Instance == this) Instance = null;
    }

    private bool ValidateBases()
    {
        if (teamBases == null || teamBases.Length != 2 || teamBases.Any(b => b == null)) return false;
        return teamBases[0].teamIndex == 0 && teamBases[1].teamIndex == 1 && teamBases.All(b => b.GetComponent<NetworkObject>() != null);
    }

    public bool CanDamage(int attackerTeam, int victimTeam) => IsServer && rules.Running && MatchRules.IsEnemy(attackerTeam, victimTeam);

    // Claim before applying damage or an impulse, not just before displaying FX.
    public bool TryResolveProjectile(ulong shooter, uint shot) => IsServer && IsSpawned && resolvedProjectiles.Add((shooter, shot));

    public void PublishProjectileResult(ulong shooter, uint shot, ulong target, bool impact,
        Vector3 point, Vector3 normal, bool diagnostics)
    {
        if (!IsServer || !IsSpawned) return;
        ProjectileResultRpc(shooter, shot, target, impact, point, normal, NetworkManager.ServerTime.Time, diagnostics);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void ProjectileResultRpc(ulong shooter, uint shot, ulong target, bool impact,
        Vector3 point, Vector3 normal, double confirmedAt, bool diagnostics)
    {
        if (MatchResults.LocalFinal != null) return;
        TankShooter.TraceShot(diagnostics, shooter, shot, target,
            impact ? "projectile-result-impact" : "projectile-result-no-impact", confirmedAt);
        Projectiles.Resolve(shooter, shot, impact, point, normal);
    }

    public bool TryGetShotTeam(ulong shooterObjectId, out int team)
    {
        var player = rules.Players.Values.FirstOrDefault(p => p.objectId == shooterObjectId);
        team = player != null ? player.team : -1;
        return IsServer && rules.Running && player != null;
    }

    public void RecordDeath(PlayerHealth health, ulong attackerObjectId, Vector3? sourcePosition = null)
    {
        if (!IsServer || !rules.RecordDeath(health.OwnerClientId, attackerObjectId, NetworkManager.ServerTime.Time, respawnDelay)) return;
        var player = rules.Players[health.OwnerClientId];
        health.respawnAt.Value = player.respawnAt;
        EmitCombat(player, attackerObjectId, true, false, sourcePosition);
    }

    public void NotifyPlayerDamage(PlayerHealth health, ulong attackerObjectId, Vector3? sourcePosition)
    {
        if (!IsServer || !rules.Running || !rules.Players.TryGetValue(health.OwnerClientId, out var victim)) return;
        EmitCombat(victim, attackerObjectId, false, false, sourcePosition);
    }

    public void NotifyBaseDamage(int team, ulong attackerObjectId)
    {
        if (!IsServer || !rules.Running) return;
        EmitCombat(new MatchRules.PlayerRecord { clientId = ulong.MaxValue, team = team, name = "Base" },
            attackerObjectId, false, true, null);
    }

    private void EmitCombat(MatchRules.PlayerRecord victim, ulong attackerObjectId, bool killed, bool baseHit, Vector3? source)
    {
        var attacker = rules.Players.Values.FirstOrDefault(p => p.objectId == attackerObjectId && MatchRules.IsEnemy(p.team, victim.team));
        CombatFeedbackRpc(++feedbackSequence, victim.clientId, attacker != null ? attacker.clientId : ulong.MaxValue,
            attacker != null, victim.team, attacker != null ? attacker.team : -1, victim.name,
            attacker != null ? attacker.name : "", killed, baseHit, source.HasValue, source.GetValueOrDefault());
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void CombatFeedbackRpc(ulong sequence, ulong victim, ulong attacker, bool hasAttacker, int victimTeam,
        int attackerTeam, string victimName, string attackerName, bool killed, bool baseHit, bool hasSource, Vector3 source)
    {
        if (sequence <= lastFeedbackSequence) return;
        lastFeedbackSequence = sequence;
        CombatConfirmed?.Invoke(new CombatFeedback { Sequence = sequence, VictimId = victim, AttackerId = attacker,
            HasAttacker = hasAttacker, VictimTeam = victimTeam, AttackerTeam = attackerTeam, VictimName = victimName,
            AttackerName = attackerName, IsKill = killed, IsBase = baseHit, HasSource = hasSource, Source = source });
    }

    public void RecordBaseDestroyed(int team)
    {
        if (IsServer) rules.DestroyBase(team);
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsServer || !rules.Running || aborted) return;
        double now = NetworkManager.ServerTime.Time;
        rules.Resolve(now, overtimeDuration);
        // Overtime applies both persistent flags before effects or return processing.
        foreach (TeamBase teamBase in teamBases)
            if (teamBase != null && teamBase.IsSpawned && !rules.BaseAlive(teamBase.teamIndex)) teamBase.DestroyOnServer();
        foreach (var record in rules.Players.Values)
        {
            NetworkObject obj = NetworkManager.SpawnManager.GetPlayerNetworkObject(record.clientId);
            if (obj == null || !obj.TryGetComponent<PlayerHealth>(out var health))
            {
                if (record.respawnAt >= 0 && now > record.respawnAt + 15d) DropUnresponsivePlayer(record);
                continue;
            }
            health.respawnAt.Value = record.respawnAt;
            if (record.alive || record.disconnected || !rules.CanReturn(record) || record.respawnAt < 0) continue;
            Transform spawn = spawnPoints[new[] { 0, 2, 1, 3 }[record.ordinal]];
            if (record.relocating && health.HasReturnAcknowledgement(record.life) &&
                Vector3.Distance(obj.transform.position, spawn.position) < 0.05f &&
                Quaternion.Angle(obj.transform.rotation, spawn.rotation) < 1f)
            {
                ConfirmReturn(health, record.life);
                continue;
            }
            if (!record.relocating && now >= record.respawnAt)
            {
                rules.BeginReturn(record.clientId);
                health.BeginReturn(spawn.position, spawn.rotation, record.life);
            }
            else if (record.relocating && now > record.respawnAt + 15d)
            {
                // An owner that never acknowledges its authoritative teleport cannot
                // reserve an immortal pending return. Treat it as a connection failure.
                DropUnresponsivePlayer(record);
            }
        }
        PublishState();
        if (rules.Phase == MatchRules.Stage.Finished)
        {
            PoolManager.ReleaseActiveBullets();
            FinishRpc(statistics.Value.ToString());
        }
    }

    public void ConfirmReturn(PlayerHealth health, uint life)
    {
        if (!IsServer || !rules.ConfirmReturn(health.OwnerClientId, life, NetworkManager.ServerTime.Time)) return;
        health.RestoreOnServer();
    }

    private void DropUnresponsivePlayer(MatchRules.PlayerRecord record)
    {
        rules.Disconnect(record.clientId);
        if (record.clientId != NetworkManager.ServerClientId) NetworkManager.DisconnectClient(record.clientId, "Respawn synchronization timed out.");
        else coordinator?.Fail("Local respawn synchronization timed out.");
    }

    private void PublishState()
    {
        phaseEndsAt.Value = rules.EndsAt;
        destroyedBases.Value = rules.DestroyedBases;
        winningTeam.Value = rules.Winner;
        if (!statisticsPublished || publishedStatisticsRevision != rules.StatisticsRevision)
        {
            string json = JsonUtility.ToJson(new MatchResults { winner = rules.Winner, players = rules.Players.Values.OrderBy(p => p.ordinal).ToArray() });
            statistics.Value = new FixedString4096Bytes(json);
            publishedStatisticsRevision = rules.StatisticsRevision;
            statisticsPublished = true;
        }
        phase.Value = (int)rules.Phase;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void FinishRpc(string json)
    {
        MatchResults.Store(json);
        Projectiles.Clear();
        PoolManager.ReleaseActiveBullets();
        MatchHud.Instance?.ShowFinal();
        FinalReceivedRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void FinalReceivedRpc(RpcParams rpcParams = default)
    {
        if (rules.Phase == MatchRules.Stage.Finished && rules.Players.ContainsKey(rpcParams.Receive.SenderClientId))
            finalReceipts.Add(rpcParams.Receive.SenderClientId);
    }
}
