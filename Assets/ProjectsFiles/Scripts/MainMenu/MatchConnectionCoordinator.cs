using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public class MatchConnectionCoordinator : MonoBehaviour
{
    [Serializable] private class ConnectionTicket
    {
        public string LobbyId;
        public string TransitionId;
        public string PlayerId;
        public string Token;
    }

    public static MatchConnectionCoordinator Instance { get; private set; }
    public static bool GameplayAllowed => MatchResults.LocalFinal == null && (Instance == null || !Instance.TransitionActive || Instance.MatchStarted);
    [SerializeField, Min(15f)] private float transitionTimeout = 90f;
    public bool TransitionActive { get; private set; }
    public bool MatchStarted { get; private set; }
    public bool LocalSceneSynchronized { get; private set; }
    public int ExpectedCount { get; private set; }
    public MatchParticipantRoster Roster { get; private set; }
    public double PreparationDeadline => deadline;
    private NetworkManager network;
    private NetworkSceneManager sceneEvents;
    private LoadingScreenController screen;
    private Lobby lobby;
    private bool ownsLobby;
    private bool returning;
    private bool recoveryComplete;
    private bool heartbeatPending;
    private double deadline;
    private float heartbeatTimer;
    private int generation;
    private string transitionId;
    private string gameScene;
    private string menuScene = "MainMenuScene";
    private string localPlayerId;
    private string stage = "Idle";
    private bool quitting;
    private bool heartbeatWarningShown;
    public bool CanBegin => !TransitionActive && !returning && !network.IsListening && !network.ShutdownInProgress;

    public static void Trace(string operation, string detail, string role = null, bool warning = false)
    {
        var owner = Instance;
        role = role ?? (owner != null && owner.TransitionActive ? (owner.ownsLobby ? "Host" : "Client") : "Menu");
        string message = $"[MatchFlow] role={role} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
            $"stage={owner?.stage ?? "Idle"} operation={operation} {detail}";
        if (warning) Debug.LogWarning(message); else Debug.Log(message);
    }

    public static string ErrorDescription(Exception error)
    {
        string detail = error is LobbyServiceException service
            ? $"{service.GetType().Name} reason={service.Reason} code={service.ErrorCode}: {service.Message}"
            : $"{error.GetType().Name}: {error.Message}";
        // Preserve the underlying HTTP status without logging response bodies/payloads.
        string inner = error.InnerException?.Message;
        if (inner != null)
        {
            var status = System.Text.RegularExpressions.Regex.Match(inner, @"\bHTTP/\S+\s+(\d{3})\b");
            if (status.Success) detail += " HTTP=" + status.Groups[1].Value;
        }
        return detail;
    }

    private void SetStage(string value) { stage = value; Trace(value, "begin"); }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        network = GetComponent<NetworkManager>();
        DontDestroyOnLoad(gameObject);
    }

    private void Begin(Lobby source, string token, string id, string destination, string menu)
    {
        if (!CanBegin)
            throw new InvalidOperationException("Another connection transition is already active.");
        lobby = source;
        MatchResults.Clear();
        localPlayerId = AuthenticationService.Instance.PlayerId;
        ownsLobby = source.HostId == localPlayerId;
        transitionId = id;
        gameScene = destination;
        menuScene = menu;
        TransitionActive = true;
        MatchStarted = LocalSceneSynchronized = recoveryComplete = false;
        heartbeatPending = false;
        heartbeatWarningShown = false;
        heartbeatTimer = 0f;
        ExpectedCount = source.Players.Count;
        deadline = Time.realtimeSinceStartupAsDouble + transitionTimeout;
        generation++;
        screen = LoadingScreenController.Instance;
        if (screen == null || !screen.IsConfigured)
            throw new InvalidOperationException("LoadingCanvas is missing or its LoadingScreenController Inspector references are incomplete. See the preceding UI errors.");
        screen.ReturnRequested -= DismissFailure;
        screen.ReturnRequested += DismissFailure;
        screen.Show("Connecting");
        SetStage("Connecting");
        Trace("Identity", "fingerprint=" + MatchParticipantRoster.TokenHash(localPlayerId).Substring(0, 12));
        network.OnClientDisconnectCallback += OnDisconnected;
        network.OnTransportFailure += OnTransportFailure;
        network.NetworkConfig.ConnectionApproval = true;
        network.NetworkConfig.AutoSpawnPlayerPrefabClientSide = false;
        network.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new ConnectionTicket
        { LobbyId = source.Id, TransitionId = id, PlayerId = localPlayerId, Token = token }));
    }

    public async Task StartHostMatchAsync(Lobby source, string token, string destination, string menu)
    {
        if (!CanBegin) throw new InvalidOperationException("The previous connection is still closing. Please try again shortly.");
        int operation = -1;
        try
        {
            string id = Guid.NewGuid().ToString("N");
            Begin(source, token, id, destination, menu);
            operation = generation;
            // Lock first. The response is the final membership snapshot, including any
            // participant who joined just before the lock took effect.
            SetStage("LockLobby");
            Lobby lockedLobby = await UpdateTransitionLobbyAsync(new UpdateLobbyOptions
            {
                IsLocked = true,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { "TransitionId", new DataObject(DataObject.VisibilityOptions.Member, id) }
                }
            }, value => value.IsLocked && DataEquals(value, "TransitionId", id), operation);
            EnsureCurrent(operation);
            lobby = lockedLobby;
            Dictionary<string, string> hashes = new Dictionary<string, string>();
            foreach (Player member in lobby.Players)
            {
                if (member.Data == null || !member.Data.TryGetValue("IsReady", out PlayerDataObject ready) ||
                    ready.Value != "True" || !member.Data.TryGetValue("ConnectionTokenHash", out PlayerDataObject hash))
                    throw new InvalidOperationException("Every lobby member must be ready and registered before starting.");
                hashes.Add(member.Id, hash.Value);
            }
            Roster = new MatchParticipantRoster(hashes, localPlayerId);
            ExpectedCount = Roster.ExpectedCount;
            if (!Roster.TryBind(NetworkManager.ServerClientId, localPlayerId, token, out string reason))
                throw new InvalidOperationException(reason);
            network.ConnectionApprovalCallback += ApproveConnection;

            SetStage("AllocateRelay");
            var allocation = await Bounded(RelayService.Instance.CreateAllocationAsync(lobby.MaxPlayers - 1));
            EnsureCurrent(operation);
            string code = await Bounded(RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId));
            EnsureCurrent(operation);
            network.GetComponent<UnityTransport>().SetHostRelayData(allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);
            SetStage("StartHost");
            if (!network.StartHost()) throw new InvalidOperationException("StartHost returned false. See the preceding Netcode/Transport log.");
            EnsureCurrent(operation);
            Trace("StartHost", "succeeded");
            AttachSceneEvents();
            screen.Show("Loading");
            SetStage("LoadGameScene");
            SceneEventProgressStatus result = network.SceneManager.LoadScene(destination, LoadSceneMode.Single);
            Trace("LoadGameScene", "result=" + result);
            if (result != SceneEventProgressStatus.Started)
                throw new InvalidOperationException("Network scene load was rejected: " + result);

            // Clients learn the code only after transport, host startup and the scene
            // load request have succeeded. This task is owned by this persistent object.
            SetStage("PublishConnection");
            Lobby publishedLobby = await UpdateTransitionLobbyAsync(new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, code) },
                    { "ExpectedCount", new DataObject(DataObject.VisibilityOptions.Member, ExpectedCount.ToString()) }
                }
            }, value => DataEquals(value, "RelayCode", code) &&
                DataEquals(value, "ExpectedCount", ExpectedCount.ToString()), operation, true);
            EnsureCurrent(operation);
            lobby = publishedLobby;
            if (!MatchStarted) SetStage("WaitingForPlayers");
        }
        catch (Exception e) { if (operation < 0 || operation == generation) Fail("Start failed at " + stage + ": " + ErrorDescription(e)); }
    }

    public async Task JoinMatchAsync(Lobby source, string token, string destination, string menu)
    {
        if (!CanBegin) throw new InvalidOperationException("The previous connection is still closing. Please try again shortly.");
        int operation = -1;
        try
        {
            string id = source.Data != null && source.Data.TryGetValue("TransitionId", out DataObject value) ? value.Value : "";
            Begin(source, token, id, destination, menu);
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("The room has no transition identifier.");
            operation = generation;
            ExpectedCount = int.Parse(source.Data["ExpectedCount"].Value);
            if (ExpectedCount < 2 || ExpectedCount > 4) throw new InvalidOperationException("Invalid participant count.");
            SetStage("JoinRelay");
            var allocation = await Bounded(RelayService.Instance.JoinAllocationAsync(source.Data["RelayCode"].Value));
            EnsureCurrent(operation);
            network.GetComponent<UnityTransport>().SetClientRelayData(allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes, allocation.Key,
                allocation.ConnectionData, allocation.HostConnectionData);
            SetStage("StartClient");
            if (!network.StartClient()) throw new InvalidOperationException("StartClient returned false. See the preceding Netcode/Transport log.");
            EnsureCurrent(operation);
            Trace("StartClient", "started; waiting for approval and synchronization");
            AttachSceneEvents();
        }
        catch (Exception e) { if (operation < 0 || operation == generation) Fail("Join failed at " + stage + ": " + ErrorDescription(e)); }
    }

    private static bool DataEquals(Lobby value, string key, string expected) =>
        value.Data != null && value.Data.TryGetValue(key, out DataObject item) && item.Value == expected;

    // A timed-out write may have committed. Read it back before repeating this
    // idempotent update. Never retry CreateLobby or allocate a second Relay here.
    private async Task<Lobby> UpdateTransitionLobbyAsync(UpdateLobbyOptions options,
        Func<Lobby, bool> applied, int operation, bool publishing = false)
    {
        string lobbyId = lobby.Id;
        for (int attempt = 0; ; attempt++)
        {
            EnsureCurrent(operation);
            try
            {
                // Let the SDK's request timeout finish the write before cleanup/retry.
                Lobby updated = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
                EnsureCurrent(operation);
                return updated;
            }
            catch (LobbyServiceException e) when (IsTransient(e))
            {
                EnsureCurrent(operation);
                Trace(stage, "write failed: " + ErrorDescription(e), warning: true);
                // All expected peers could only join using this published ticket.
                if (publishing && Roster != null && BoundParticipantCount() == ExpectedCount)
                { Trace(stage, "publication confirmed by connected roster"); return lobby; }
                try
                {
                    Lobby observed = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                    EnsureCurrent(operation);
                    if (applied(observed)) { Trace(stage, "write confirmed by read-back"); return observed; }
                }
                catch (LobbyServiceException readError)
                { Trace(stage, "read-back failed: " + ErrorDescription(readError), warning: true); }
                EnsureCurrent(operation);
                if (publishing && Roster != null && BoundParticipantCount() == ExpectedCount) return lobby;
                if (attempt >= 1) throw;
                Trace(stage, "one bounded retry of the same update");
                await Task.Delay(1000);
            }
        }
    }

    private int BoundParticipantCount()
    {
        int count = 0;
        foreach (var assignment in Roster.Assignments)
            if (network.ConnectedClients.ContainsKey(assignment.ClientId)) count++;
        return count;
    }

    public static bool IsTransient(LobbyServiceException error) =>
        error.Reason == LobbyExceptionReason.UnknownErrorCode || error.Reason == LobbyExceptionReason.Unknown ||
        error.Reason == LobbyExceptionReason.NetworkError || error.Reason == LobbyExceptionReason.RequestTimeOut ||
        error.Reason == LobbyExceptionReason.RateLimited ||
        ((int)error.Reason >= 16500 && (int)error.Reason < 16600);

    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.Pending = false;
        response.Approved = false;
        response.CreatePlayerObject = false;
        response.Reason = "This match is closed to new participants.";
        try
        {
            if (!TransitionActive || MatchStarted || returning || Roster == null || request.Payload == null || request.Payload.Length > 2048)
                return;
            ConnectionTicket ticket = JsonUtility.FromJson<ConnectionTicket>(Encoding.UTF8.GetString(request.Payload));
            if (ticket == null || ticket.LobbyId != lobby.Id || ticket.TransitionId != transitionId) return;
            response.Approved = Roster.TryBind(request.ClientNetworkId, ticket.PlayerId, ticket.Token, out string reason);
            response.Reason = response.Approved ? "" : reason;
        }
        catch (Exception) { response.Reason = "Invalid connection ticket."; }
        finally { Trace("Approval", $"client={request.ClientNetworkId} approved={response.Approved} reason={response.Reason}"); }
    }

    private void AttachSceneEvents()
    {
        if (sceneEvents != null || network.SceneManager == null) return;
        sceneEvents = network.SceneManager;
        sceneEvents.OnSceneEvent += OnSceneEvent;
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (!TransitionActive || returning) return;
        Trace("SceneEvent", $"type={sceneEvent.SceneEventType} client={sceneEvent.ClientId} sceneName={sceneEvent.SceneName}");
        if (sceneEvent.SceneEventType == SceneEventType.Synchronize ||
            (sceneEvent.SceneEventType == SceneEventType.Load && sceneEvent.SceneName == gameScene))
            screen.Show("Loading");
        bool gameLoaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(gameScene).isLoaded;
        bool completed = (sceneEvent.SceneEventType == SceneEventType.LoadComplete && sceneEvent.SceneName == gameScene) ||
            (sceneEvent.SceneEventType == SceneEventType.SynchronizeComplete && gameLoaded);
        if (completed)
        {
            if (sceneEvent.ClientId == network.LocalClientId)
            {
                LocalSceneSynchronized = true;
                screen.Show("Waiting for players");
            }
            if (network.IsServer && Roster != null && Roster.TryGet(sceneEvent.ClientId, out var assignment))
                assignment.SceneSynchronized = true;
        }
        // This event covers connected clients only, never the entire frozen lobby.
        if (sceneEvent.SceneEventType == SceneEventType.LoadEventCompleted && sceneEvent.SceneName == gameScene &&
            sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0)
            Fail("A participant timed out while loading the game scene.");
    }

    private void Update()
    {
        if (!TransitionActive || returning) return;
        if (network.IsListening) AttachSceneEvents();
        if (!MatchStarted && Time.realtimeSinceStartupAsDouble > deadline)
        {
            Fail("Match preparation timed out. Waiting for: " + (Roster != null ? Roster.WaitingFor : "the host or other players"));
            return;
        }
        if (ownsLobby && lobby != null)
        {
            heartbeatTimer -= Time.unscaledDeltaTime;
            if (heartbeatTimer <= 0f && !heartbeatPending) SendHeartbeat();
        }
    }

    private async void SendHeartbeat()
    {
        int operation = generation;
        heartbeatPending = true;
        heartbeatTimer = 15f;
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
            if (operation == generation) heartbeatWarningShown = false;
        }
        catch (Exception e)
        {
            // Lobby availability is separate from the active Relay connection.
            if (operation == generation && !returning && !heartbeatWarningShown)
            {
                heartbeatWarningShown = true;
                Trace("Heartbeat", ErrorDescription(e) + "; keeping the current connection", warning: true);
            }
        }
        finally { if (operation == generation) heartbeatPending = false; }
    }

    public void NotifyMatchStarted()
    {
        if (!TransitionActive || returning || MatchStarted) return;
        MatchStarted = true;
        SetStage("MatchBegins");
        screen.Hide();
    }

    public string ParticipantName(string playerId)
    {
        if (lobby?.Players != null)
            foreach (Player member in lobby.Players)
                if (member.Id == playerId && member.Data != null && member.Data.TryGetValue("PlayerName", out PlayerDataObject name) &&
                    !string.IsNullOrWhiteSpace(name.Value))
                    return name.Value.Length > 80 ? name.Value.Substring(0, 80) : name.Value;
        return "Player";
    }

    private void OnDisconnected(ulong clientId)
    {
        if (!TransitionActive || returning || quitting) return;
        Trace("Disconnected", "client=" + clientId + " reason=" + network.DisconnectReason, warning: true);
        if (MatchResults.LocalFinal != null) { MatchHud.Instance?.ShowFinal(); return; }
        if (clientId == network.LocalClientId || (!network.IsServer && !network.IsConnectedClient))
            Fail(string.IsNullOrEmpty(network.DisconnectReason) ? "Disconnected from the match host." : network.DisconnectReason);
        else if (network.IsServer && !MatchStarted && Roster != null && Roster.TryGet(clientId, out _))
            Fail("A participant disconnected during match preparation. Please create or join a room again.");
    }

    private void OnTransportFailure()
    {
        if (MatchResults.LocalFinal != null) { MatchHud.Instance?.ShowFinal(); return; }
        Fail("The network transport failed. Returning to the menu.");
    }

    public void ReturnFromResults()
    {
        if (returning || MatchResults.LocalFinal == null) return;
        returning = true;
        MatchStarted = false;
        generation++;
        screen?.Show("Returning to menu");
        StartCoroutine(RecoverToMenu("", true));
    }

    public void Fail(string reason)
    {
        if (returning || quitting) return;
        Trace("Failure", reason, warning: true);
        if (network.IsServer && MatchManager.Instance != null && MatchManager.Instance.IsSpawned)
            MatchManager.Instance.BroadcastAbort(reason);
        else ReturnAfterFailure(reason);
    }

    public void ReturnAfterFailure(string reason)
    {
        if (returning || quitting) return;
        returning = true;
        MatchStarted = false;
        generation++;
        screen = screen != null ? screen : LoadingScreenController.Instance;
        screen?.ShowError(reason + "\nReturning to menu...", false);
        Trace("ReturnToMenu", "reason=" + reason, warning: true);
        stage = "Recovering";
        StartCoroutine(RecoverToMenu(reason));
    }

    private IEnumerator RecoverToMenu(string reason, bool fromResults = false)
    {
        // If the host clicks Back immediately, allow connected clients to store
        // the final snapshot before closing Relay. A broken peer cannot block forever.
        double receiptsDeadline = Time.realtimeSinceStartupAsDouble + 10f;
        while (fromResults && network.IsServer && MatchManager.Instance != null &&
            !MatchManager.Instance.FinalSnapshotsDelivered && Time.realtimeSinceStartupAsDouble < receiptsDeadline)
            yield return null;
        // Allow the abort RPC to leave during the normal network update before teardown.
        yield return null;
        yield return null;
        DetachNetworkEvents();
        PoolManager.Clear();
        Trace("Shutdown", "reason=" + (fromResults ? "results exit" : reason));
        network.Shutdown();
        double shutdownDeadline = Time.realtimeSinceStartupAsDouble + 10f;
        while (network.ShutdownInProgress && Time.realtimeSinceStartupAsDouble < shutdownDeadline) yield return null;
        if (!network.IsListening && !network.ShutdownInProgress)
        {
            UnityTransport transport = network.GetComponent<UnityTransport>();
            transport.SetRelayServerData(default);
            transport.SetConnectionData("127.0.0.1", 7777);
        }
        Task cleanup = CleanupLobbyAsync();
        double cleanupDeadline = Time.realtimeSinceStartupAsDouble + 12f;
        while (!cleanup.IsCompleted && Time.realtimeSinceStartupAsDouble < cleanupDeadline) yield return null;
        {
            AsyncOperation load = null;
            PlayerIdentityManager.OpenBrowserOnNextMenu = true;
            try { load = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(menuScene, LoadSceneMode.Single); }
            catch (Exception e) { reason += "\nCould not reload the menu: " + e.Message; }
            double menuDeadline = Time.realtimeSinceStartupAsDouble + 30f;
            while (load != null && !load.isDone && Time.realtimeSinceStartupAsDouble < menuDeadline) yield return null;
        }
        network.NetworkConfig.ConnectionData = Array.Empty<byte>();
        LocalSceneSynchronized = false;
        Roster = null;
        ExpectedCount = 0;
        transitionId = null;
        recoveryComplete = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == menuScene &&
            !network.IsListening && !network.ShutdownInProgress;
        TransitionActive = !recoveryComplete;
        returning = !recoveryComplete;
        if (recoveryComplete)
        {
            stage = "Idle";
            // Do not leave a persistent raycast-blocking overlay above a usable menu.
            screen?.Hide();
            FindAnyObjectByType<LobbyManager>()?.ShowNotice(fromResults ? "" : reason + " You can try again.");
            Trace("RecoveryComplete", "menu unlocked");
        }
        else screen?.ShowError(reason + "\nRecovery failed; restart the game.", false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DismissFailure()
    {
        if (recoveryComplete) screen.Hide();
    }

    private async Task CleanupLobbyAsync()
    {
        Lobby oldLobby = lobby;
        lobby = null;
        if (oldLobby == null) return;
        try
        {
            Trace("CleanupLobby", ownsLobby ? "delete owned lobby" : "remove local membership");
            if (ownsLobby) await Bounded(LobbyService.Instance.DeleteLobbyAsync(oldLobby.Id), 10);
            else await Bounded(LobbyService.Instance.RemovePlayerAsync(oldLobby.Id, localPlayerId), 10);
        }
        catch (Exception e) { Trace("CleanupLobby", ErrorDescription(e), warning: true); }
    }

    private void EnsureCurrent(int operation)
    {
        if (generation != operation || returning || !TransitionActive) throw new OperationCanceledException("Transition was cancelled.");
    }

    private static async Task<T> Bounded<T>(Task<T> task, int seconds = 25)
    {
        if (await Task.WhenAny(task, Task.Delay(seconds * 1000)) != task) throw new TimeoutException("Online service request timed out.");
        return await task;
    }
    private static async Task Bounded(Task task, int seconds = 25)
    {
        if (await Task.WhenAny(task, Task.Delay(seconds * 1000)) != task) throw new TimeoutException("Online service request timed out.");
        await task;
    }

    private void DetachNetworkEvents()
    {
        network.ConnectionApprovalCallback -= ApproveConnection;
        network.OnClientDisconnectCallback -= OnDisconnected;
        network.OnTransportFailure -= OnTransportFailure;
        if (sceneEvents != null) sceneEvents.OnSceneEvent -= OnSceneEvent;
        sceneEvents = null;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        DetachNetworkEvents();
        if (screen != null) screen.ReturnRequested -= DismissFailure;
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        quitting = true;
        if (Instance == this) { DetachNetworkEvents(); _ = CleanupLobbyAsync(); }
    }
}
