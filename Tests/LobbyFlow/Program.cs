using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

static class Program
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Assert(bool result, string message) { checks++; if (!result) throw new Exception(message); }
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Fields).SetValue(obj, value);
    static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Fields).GetValue(obj);
    static object Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Fields).Invoke(obj, args);
    static T Component<T>() where T : Component => new GameObject("test", typeof(T)).GetComponent<T>();
    static LobbyServiceException Timeout() => new(LobbyExceptionReason.NetworkError, "(0) Request timeout");
    static Task<T> Failed<T>() => Task.FromException<T>(Timeout());
    static LobbyManager Menu()
    {
        var menu = new LobbyManager();
        Set(menu, "roomBrowserPanel", new GameObject()); Set(menu, "waitingRoomPanel", new GameObject());
        Set(menu, "operationStatus", Component<TMP_Text>()); Set(menu, "playersListText", Component<TMP_Text>());
        Set(menu, "maxPlayersDropdown", Component<TMP_Dropdown>()); Set(menu, "roomListContainer", new Transform());
        Set(menu, "roomItemPrefab", new GameObject("room", typeof(RoomItemUI)));
        foreach (string field in new[] { "createRoomButton", "refreshButton", "readyButton", "startGameButton", "leaveRoomButton" }) Set(menu, field, Component<Button>());
        UnityEngine.Object.Found[typeof(LobbyManager)] = menu;
        Call(menu, "Start");
        return menu;
    }
    static Lobby Room(bool ready = true)
    {
        var room = new Lobby { Data = new() { ["RelayCode"] = new(DataObject.VisibilityOptions.Member, "0") } };
        foreach (string id in new[] { "host", "client" })
            room.Players.Add(new Player { Id = id, Data = new()
            {
                ["IsReady"] = new(PlayerDataObject.VisibilityOptions.Member, ready ? "True" : "False"),
                ["PlayerName"] = new(PlayerDataObject.VisibilityOptions.Member, id),
                ["ConnectionTokenHash"] = new(PlayerDataObject.VisibilityOptions.Member, MatchParticipantRoster.TokenHash(id + "-ticket"))
            } });
        return room;
    }
    static void AssignRoom(LobbyManager menu, Lobby room)
    { Set(menu, "currentLobby", room); Set(menu, "localJoinToken", Unity.Services.Authentication.AuthenticationService.Instance.PlayerId + "-ticket"); Call(menu, "ShowWaitingRoom"); }
    static Lobby AgreedRoom()
    {
        var room = Room(); room.IsLocked = true;
        room.Data["RoomState"] = new(DataObject.VisibilityOptions.Member, "Starting");
        room.Data["TransitionId"] = new(DataObject.VisibilityOptions.Member, "start-ticket");
        room.Players[1].Data["StartAck"] = new(PlayerDataObject.VisibilityOptions.Member, "start-ticket");
        return room;
    }
    static async Task FailedStart(MatchConnectionCoordinator coordinator, Lobby room)
    {
        bool failed = false;
        try { await coordinator.StartHostMatchAsync(room, "host-ticket", "GameScene", "MainMenuScene"); }
        catch (InvalidOperationException) { failed = true; }
        Assert(failed, "Pre-commit failure is returned to waiting-room owner");
    }
    static (MatchConnectionCoordinator coordinator, NetworkManager network) Connection()
    {
        if (MatchConnectionCoordinator.Instance != null) Call(MatchConnectionCoordinator.Instance, "OnDestroy");
        LoadingScreenController.Instance = new();
        var network = new NetworkManager(); network.Components[typeof(UnityTransport)] = new UnityTransport();
        var coordinator = new MatchConnectionCoordinator(); coordinator.Components[typeof(NetworkManager)] = network;
        Call(coordinator, "Awake");
        return (coordinator, network);
    }
    static async Task Idle(LobbyManager menu, string field = "actionPending")
    {
        for (int i = 0; i < 100 && Get<bool>(menu, field); i++) await Task.Delay(10);
        Assert(!Get<bool>(menu, field), field + " released");
    }
    static async Task Drain(MatchConnectionCoordinator coordinator)
    {
        foreach (IEnumerator routine in coordinator.Routines.ToArray())
        {
            int frames = 0;
            while (routine.MoveNext())
            {
                if (++frames > 1000) throw new Exception("Recovery did not finish");
                Time.realtimeSinceStartupAsDouble += .02;
                await Task.Yield();
            }
        }
        coordinator.Routines.Clear();
    }

    static async Task Main()
    {
        // Failed Create restores controls. A second click during the first request
        // never reaches the service; a lost success is recovered without Create #2.
        LobbyService.Instance = new();
        var service = LobbyService.Instance;
        var menu = Menu();
        int creates = 0;
        var createResponse = new TaskCompletionSource<Lobby>();
        service.Create = _ => { creates++; return createResponse.Task; };
        Call(menu, "CreateRoom"); Call(menu, "CreateRoom");
        Assert(creates == 1 && !Get<Button>(menu, "createRoomButton").interactable, "Create serialized");
        createResponse.SetException(new LobbyServiceException(LobbyExceptionReason.InternalServerError, "HTTP 500"));
        await Idle(menu);
        Assert(Get<Button>(menu, "createRoomButton").interactable, "Create failure restores button");
        var saved = Room(false);
        service.Joined = () => Task.FromResult(new List<string> { saved.Id }); service.Get = _ => Task.FromResult(saved);
        service.PlayerUpdate = options => { saved.Players[0].Data = options.Data; return Task.FromResult(saved); };
        Call(menu, "CreateRoom"); await Idle(menu);
        Assert(creates == 1 && Get<Lobby>(menu, "currentLobby") == saved, "Committed create recovered; no duplicate room");
        Assert(Get<Button>(menu, "readyButton").interactable, "Recovered membership can Ready");

        // Failure to verify membership cannot fall through to a blind Create.
        menu = Menu(); service.Joined = () => Failed<List<string>>();
        Call(menu, "CreateRoom"); await Idle(menu);
        Assert(creates == 1 && Get<Button>(menu, "createRoomButton").interactable, "Membership check failure remains retryable");
        service.Joined = () => Task.FromResult(new List<string>());
        service.Create = _ => { creates++; return Task.FromResult(Room()); };
        Call(menu, "CreateRoom"); await Idle(menu);
        Assert(creates == 2 && Get<Lobby>(menu, "currentLobby") != null, "New Create succeeds after service recovery");

        // Refresh timeout then success, and a query that finishes after Join.
        menu = Menu(); service.Query = () => Failed<QueryResponse>();
        menu.RefreshLobbyList(); await Idle(menu, "refreshPending");
        Assert(Get<Button>(menu, "refreshButton").interactable, "Refresh timeout unlocks retry");
        var query = new TaskCompletionSource<QueryResponse>(); int queries = 0;
        service.Query = () => { queries++; return query.Task; };
        menu.RefreshLobbyList(); menu.RefreshLobbyList();
        Assert(queries == 1, "No overlapping refreshes");
        service.Join = _ => Task.FromResult(saved);
        menu.JoinLobby(saved); await Idle(menu);
        query.SetResult(new QueryResponse()); await Idle(menu, "refreshPending");
        Assert(Get<Lobby>(menu, "currentLobby") == saved && !Get<Button>(menu, "createRoomButton").interactable,
            "Late browser result cannot overwrite joined session or enable Create");

        // Ready is committed only on success; double-clicks cannot toggle twice.
        menu = Menu(); saved = Room(false); AssignRoom(menu, saved);
        var readyResponse = new TaskCompletionSource<Lobby>(); int readyCalls = 0;
        service.PlayerUpdate = _ => { readyCalls++; return readyResponse.Task; };
        Call(menu, "ToggleReady"); Call(menu, "ToggleReady");
        Assert(readyCalls == 1 && !Get<bool>(menu, "isReady"), "Ready changes only after confirmation");
        readyResponse.SetException(Timeout()); await Idle(menu);
        Assert(!Get<bool>(menu, "isReady") && Get<Button>(menu, "readyButton").interactable, "Failed Ready is retryable");
        service.PlayerUpdate = options => { saved.Players[0].Data = options.Data; return Task.FromResult(saved); };
        Call(menu, "ToggleReady"); await Idle(menu);
        Assert(Get<bool>(menu, "isReady"), "Ready succeeds on explicit retry");

        // A poll started before a session change cannot reset it on a late 404.
        var poll = new TaskCompletionSource<Lobby>(); service.Get = _ => poll.Task;
        Call(menu, "HandleLobbyPolling");
        Call(menu, "ResetLobbyUI", "test leave"); AssignRoom(menu, Room());
        Lobby replacement = Get<Lobby>(menu, "currentLobby");
        poll.SetException(new LobbyServiceException(LobbyExceptionReason.LobbyNotFound, "gone"));
        await Idle(menu, "pollPending");
        Assert(Get<Lobby>(menu, "currentLobby") == replacement, "Stale poll 404 cannot reset new session");
        int deletes = 0; service.Delete = () => { deletes++; return Task.CompletedTask; };
        Call(menu, "OnDestroy"); Assert(deletes == 0, "Destroying UI never deletes Lobby");

        // Run the actual host start path: lock, host, scene request, publish.
        // Publication committed but response failed -> read-back confirms it.
        service = LobbyService.Instance = new(); saved = AgreedRoom();
        var (coordinator, network) = Connection(); int writes = 0;
        service.Update = options =>
        {
            writes++;
            if (options.IsLocked.HasValue) saved.IsLocked = options.IsLocked.Value;
            foreach (var item in options.Data) saved.Data[item.Key] = item.Value;
            if (writes == 2)
            {
                Assert(network.IsListening, "Host listening before publication");
                return Failed<Lobby>();
            }
            return Task.FromResult(saved);
        };
        service.Get = _ => Task.FromResult(saved);
        await coordinator.StartHostMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(writes == 2 && coordinator.TransitionActive && !Get<bool>(coordinator, "returning"), "Publish read-back prevents false abort");
        Assert(!coordinator.MatchStarted && LoadingScreenController.Instance.Visible, "Loading stays until readiness");
        service.Heartbeat = () => Task.FromException(Timeout());
        Call(coordinator, "SendHeartbeat");
        Assert(!Get<bool>(coordinator, "returning") && network.ShutdownCalls == 0, "Heartbeat failure keeps network alive");

        // Publication truly failed: exactly one repeated update, then normal recovery.
        service = LobbyService.Instance = new(); saved = AgreedRoom(); (coordinator, network) = Connection(); writes = 0;
        service.Update = options =>
        {
            writes++;
            if (writes > 1) return Failed<Lobby>();
            saved.IsLocked = true; foreach (var item in options.Data) saved.Data[item.Key] = item.Value;
            return Task.FromResult(saved);
        };
        service.Get = _ => Task.FromResult(saved);
        await coordinator.StartHostMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(writes == 3 && Get<bool>(coordinator, "returning"), "One retry only, then recovery");
        coordinator.Fail("duplicate failure"); Assert(coordinator.Routines.Count == 1, "Cleanup is idempotent");
        await Drain(coordinator);
        Assert(network.ShutdownCalls == 1 && coordinator.CanBegin && !LoadingScreenController.Instance.Visible, "Cleanup stops network and releases overlay");

        // StartHost false is logged and cleaned, without publishing connection data.
        service = LobbyService.Instance = new(); saved = AgreedRoom(); (coordinator, network) = Connection(); network.HostResult = false; writes = 0;
        service.Update = options => { writes++; return Task.FromResult(saved); };
        await FailedStart(coordinator, saved);
        Assert(writes == 1 && !coordinator.TransitionActive, "StartHost false prevents publication");
        await Drain(coordinator);
        Assert(coordinator.CanBegin && !LoadingScreenController.Instance.Visible, "StartHost failure allows another attempt");


        // Scene rejection and StartClient false must both use the same recoverable exit.
        service = LobbyService.Instance = new(); saved = AgreedRoom(); (coordinator, network) = Connection(); writes = 0;
        network.SceneManager.Result = SceneEventProgressStatus.SceneFailedVerification;
        service.Update = options => { writes++; return Task.FromResult(saved); };
        service.Get = _ => Task.FromResult(saved);
        await FailedStart(coordinator, saved);
        Assert(writes == 1 && !coordinator.TransitionActive, "Rejected scene request prevents publication");
        await Drain(coordinator);
        Assert(coordinator.CanBegin && !network.IsListening, "Rejected scene request cleans host");
        (coordinator, network) = Connection(); network.ClientResult = false;
        saved.Data["RelayCode"] = new(DataObject.VisibilityOptions.Member, "test-code");
        saved.Data["TransitionId"] = new(DataObject.VisibilityOptions.Member, "transition");
        saved.Data["ExpectedCount"] = new(DataObject.VisibilityOptions.Member, "2");
        await coordinator.JoinMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(Get<bool>(coordinator, "returning"), "StartClient false enters recovery");
        await Drain(coordinator);
        Assert(coordinator.CanBegin && !LoadingScreenController.Instance.Visible, "StartClient failure releases menu");

        // A failure received after all peers joined must not cancel an active match.
        service = LobbyService.Instance = new(); saved = AgreedRoom(); (coordinator, network) = Connection(); writes = 0;
        service.Update = options =>
        {
            writes++;
            if (writes == 1) return Task.FromResult(saved);
            coordinator.Roster.TryBind(1, "client", "client-ticket", out _);
            network.ConnectedClients[1] = new();
            coordinator.NotifyMatchStarted();
            return Failed<Lobby>();
        };
        service.Get = _ => Task.FromResult(saved);
        await coordinator.StartHostMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(writes == 2 && coordinator.MatchStarted && !Get<bool>(coordinator, "returning"), "Late publish timeout cannot cancel connected match");
        Assert(!LoadingScreenController.Instance.Visible && network.ShutdownCalls == 0, "Connected match keeps gameplay presentation");

        // Both a destroyed UI and a newer transition invalidate old query completion.
        service = LobbyService.Instance = new(); menu = Menu(); query = new();
        service.Query = () => query.Task; menu.RefreshLobbyList(); Call(menu, "OnDestroy");
        string beforeDestroy = Get<TMP_Text>(menu, "operationStatus").text;
        query.SetException(Timeout()); await Task.Delay(20);
        Assert(Get<TMP_Text>(menu, "operationStatus").text == beforeDestroy, "Old query cannot write to destroyed UI");

        Assert(!Debug.Messages.Any(m => m.Contains("host-ticket") || m.Contains("test-code")), "No ticket or Relay code in diagnostic logs");
        await StartLeaveRaces();
        await AuditRegressions();
        Console.WriteLine($"PASS: {checks} lobby flow assertions (simulated service/engine boundary).");
    }

    static async Task AuditRegressions()
    {
        var auth = Unity.Services.Authentication.AuthenticationService.Instance;
        auth.PlayerId = "host";
        var (coordinator, network) = Connection();
        var service = LobbyService.Instance = new();
        var room = AgreedRoom();
        var finalRead = new TaskCompletionSource<Lobby>();
        service.Update = _ => Task.FromResult(room);
        service.Get = _ => finalRead.Task;
        Task oldStart = coordinator.StartHostMatchAsync(room, "host-ticket", "GameScene", "MainMenuScene");
        Assert(network.IsListening && !oldStart.IsCompleted, "Old Start is suspended at final roster read");
        Call(coordinator, "OnTransportFailure");
        await Drain(coordinator);
        Assert(coordinator.CanBegin, "Recovery completes while old service response is pending");
        var newerRoom = AgreedRoom(); newerRoom.Id = "new-room";
        service.Update = _ => Task.FromResult(newerRoom); service.Get = _ => Task.FromResult(newerRoom);
        await coordinator.StartHostMatchAsync(newerRoom, "host-ticket", "GameScene", "MainMenuScene");
        int shutdowns = network.ShutdownCalls, generation = Get<int>(coordinator, "generation");
        var roster = coordinator.Roster;
        finalRead.SetResult(room);
        bool cancelled = false;
        try { await oldStart; } catch (OperationCanceledException) { cancelled = true; }
        Assert(cancelled && network.ShutdownCalls == shutdowns && network.IsListening, "Late canceled Start cannot shut down a newer host");
        Assert(coordinator.Roster == roster && Get<int>(coordinator, "generation") == generation &&
            LoadingScreenController.Instance.Visible, "Late canceled Start preserves newer roster, generation and loading UI");

        // A host reconciliation suspended in one room must stop before another removal.
        (coordinator, network) = Connection(); service = LobbyService.Instance = new();
        var menu = Menu(); room = Room();
        room.Players.Add(new Player { Id = "third", Data = new() });
        foreach (var p in room.Players.Skip(1)) p.Data["LeaveRequested"] = new(PlayerDataObject.VisibilityOptions.Member, "True");
        AssignRoom(menu, room);
        var removal = new TaskCompletionSource(); service.Remove = () => removal.Task;
        Task reconcile = (Task)Call(menu, "ReconcileRoomActions");
        Call(menu, "ResetLobbyUI", "switch room"); var replacement = Room(); replacement.Id = "replacement"; AssignRoom(menu, replacement);
        removal.SetResult(); await reconcile;
        Assert(service.RemovalCalls.Count == 1 && service.RemovalCalls[0].lobby == room.Id,
            "Old removal loop never sends a request to the replacement room");
        var restore = new TaskCompletionSource<Lobby>(); service.Update = _ => restore.Task;
        Task rollback = (Task)Call(menu, "RestoreWaitingRoom");
        Call(menu, "ResetLobbyUI", "switch again"); var newest = Room(); newest.Id = "newest"; AssignRoom(menu, newest);
        restore.SetResult(replacement); await rollback;
        Assert(Get<Lobby>(menu, "currentLobby") == newest, "Late rollback response cannot replace a newer room");

        // A failed cleanup keeps ownership; Create must reconcile it first.
        (coordinator, network) = Connection(); service = LobbyService.Instance = new(); room = AgreedRoom();
        Set(coordinator, "lobby", room); Set(coordinator, "ownsLobby", true);
        service.Delete = () => Task.FromException(Timeout());
        await (Task)Call(coordinator, "CleanupLobbyAsync");
        Assert(coordinator.HasPendingLobbyCleanup && !coordinator.CanBegin, "Failed cleanup retains room identity and blocks connection reuse");
        menu = Menu(); int creates = 0; service.Create = _ => { creates++; return Task.FromResult(Room()); };
        Call(menu, "CreateRoom"); await Idle(menu);
        Assert(creates == 0 && coordinator.HasPendingLobbyCleanup, "Create does not bypass failed prior lobby cleanup");
        var cleanup = new TaskCompletionSource(); int deletes = 0;
        service.Delete = () => { deletes++; return cleanup.Task; };
        Task cleanupA = coordinator.CompletePendingCleanupAsync(), cleanupB = coordinator.CompletePendingCleanupAsync();
        Assert(deletes == 1, "Concurrent cleanup callers share the actual service request");
        cleanup.SetResult(); await Task.WhenAll(cleanupA, cleanupB);
        Assert(!coordinator.HasPendingLobbyCleanup && coordinator.CanBegin, "Confirmed cleanup releases ownership for another session");
        Set(coordinator, "lobby", room);
        var uncertain = new TaskCompletionSource(); deletes = 0;
        service.Delete = () => { deletes++; return uncertain.Task; };
        bool timedOut = false;
        try { await coordinator.CompletePendingCleanupAsync(); } catch (TimeoutException) { timedOut = true; }
        Task retry = coordinator.CompletePendingCleanupAsync();
        Assert(timedOut && deletes == 1 && coordinator.HasPendingLobbyCleanup && !coordinator.CanBegin,
            "Cleanup timeout retains the real in-flight request and blocks duplicate deletion or connection");
        uncertain.SetResult(); await retry;
        Assert(coordinator.CanBegin, "A late cleanup success is reconciled before connection reuse");
        Set(coordinator, "lobby", room); Set(coordinator, "localPlayerId", "host");
        var migrated = AgreedRoom(); migrated.HostId = "client";
        service.Delete = () => Task.FromException(new LobbyServiceException(LobbyExceptionReason.Forbidden, "host changed"));
        service.Joined = () => Task.FromResult(new List<string> { room.Id }); service.Get = _ => Task.FromResult(migrated);
        await coordinator.CompletePendingCleanupAsync();
        Assert(coordinator.CanBegin && service.RemovalCalls.Count == 1,
            "Cleanup respects host migration and removes only local membership after deletion permission is revoked");

        // Starting without a local secret must not ACK or connect; timeout withdraws membership.
        auth.PlayerId = "client"; (coordinator, network) = Connection(); service = LobbyService.Instance = new();
        room = AgreedRoom(); room.Players[1].Data.Remove("StartAck"); menu = Menu(); AssignRoom(menu, room);
        Set(menu, "localJoinToken", null); int acks = 0;
        service.PlayerUpdate = _ => { acks++; return Task.FromResult(room); };
        await (Task)Call(menu, "ReconcileRoomActions");
        Assert(acks == 0 && !network.IsListening, "Missing ticket cannot acknowledge Starting or connect");
        Call(menu, "LeaveLobby"); Assert(service.RemovalCalls.Count == 0, "Manual Leave remains blocked during Starting");
        service.Get = _ => Task.FromResult(room); service.Remove = () => Task.CompletedTask;
        Time.realtimeSinceStartupAsDouble = Get<double>(menu, "roomWaitDeadline") + 1;
        Call(menu, "Update");
        Assert(service.RemovalCalls.Count == 1 && Get<Lobby>(menu, "currentLobby") == null,
            "Starting timeout ends membership instead of leaving a permanent locked panel");

        // Host disappeared while accepting a manual Leave.
        service = LobbyService.Instance = new(); room = Room(); menu = Menu(); AssignRoom(menu, room);
        service.PlayerUpdate = options => { foreach (var p in options.Data) room.Players[1].Data[p.Key] = p.Value; return Task.FromResult(room); };
        service.Get = _ => Task.FromResult(room);
        Call(menu, "LeaveLobby");
        Assert(Get<bool>(menu, "actionPending") && Get<bool>(menu, "leaveRequested"), "Leave waits for authoritative acceptance initially");
        Time.realtimeSinceStartupAsDouble = Get<double>(menu, "roomWaitDeadline") + 1;
        Call(menu, "Update");
        Assert(service.RemovalCalls.Count == 1 && Get<Lobby>(menu, "currentLobby") == null,
            "Timed-out Leave can finish even when the host never removes the player");
        Assert(Get<Button>(menu, "createRoomButton").interactable && Get<Button>(menu, "refreshButton").interactable,
            "Membership recovery unlocks the browser after its final callback");

        service = LobbyService.Instance = new(); room = Room(); menu = Menu(); AssignRoom(menu, room);
        service.Get = _ => Task.FromResult(room);
        service.PlayerUpdate = options => { foreach (var p in options.Data) room.Players[1].Data[p.Key] = p.Value; return Task.FromResult(room); };
        service.Remove = () => Task.FromException(Timeout());
        Call(menu, "LeaveLobby"); Time.realtimeSinceStartupAsDouble = Get<double>(menu, "roomWaitDeadline") + 1;
        Call(menu, "Update");
        Assert(Get<Lobby>(menu, "currentLobby") == room && !Get<bool>(menu, "departurePending") &&
            !Get<Button>(menu, "readyButton").interactable, "Failed withdrawal stays scoped, blocks actions and releases its retry guard");
        service.Remove = () => Task.CompletedTask;
        Time.realtimeSinceStartupAsDouble = Get<double>(menu, "roomWaitDeadline") + 1; Call(menu, "Update");
        Assert(Get<Lobby>(menu, "currentLobby") == null && service.RemovalCalls.Count == 2,
            "Withdrawal retries after service recovery without trapping the panel");

        // Restored locked membership and old Leave intent are retired, never revived.
        foreach (bool locked in new[] { true, false })
        {
            service = LobbyService.Instance = new(); room = Room(); room.IsLocked = locked;
            if (!locked) room.Players[1].Data["LeaveRequested"] = new(PlayerDataObject.VisibilityOptions.Member, "True");
            service.Joined = () => Task.FromResult(new List<string> { room.Id }); service.Get = _ => Task.FromResult(room);
            menu = Menu(); int updates = 0; service.PlayerUpdate = _ => { updates++; return Task.FromResult(room); };
            menu.JoinLobby(room); await Idle(menu);
            Assert(service.RemovalCalls.Count == 1 && updates == 0 && Get<Lobby>(menu, "currentLobby") == null,
                locked ? "Recovered locked room is left without an invalid ACK" : "Recovered old Leave is completed, not silently revived");
        }
        service = LobbyService.Instance = new(); room = Room(); menu = Menu();
        service.Joined = () => Task.FromResult(new List<string> { room.Id }); service.Get = _ => Task.FromResult(room);
        service.PlayerUpdate = options => { foreach (var p in options.Data) room.Players[1].Data[p.Key] = p.Value; return Task.FromResult(room); };
        menu.JoinLobby(room); await Idle(menu);
        Assert(room.Players[1].Data["LeaveRequested"].Value == "False" && room.Players[1].Data["StartAck"].Value == "",
            "Fresh recovery registration clears previous session metadata");
        Assert(Get<string>(menu, "localJoinToken") != null, "Recovery publishes a secret only after registration succeeds");
        auth.PlayerId = "host"; service = LobbyService.Instance = new(); room = AgreedRoom(); menu = Menu();
        service.Joined = () => Task.FromResult(new List<string> { room.Id }); service.Get = _ => Task.FromResult(room);
        Call(menu, "CreateRoom"); await Idle(menu);
        Assert(service.DeleteCalls.Count == 1 && Get<Lobby>(menu, "currentLobby") == null,
            "Restarted host retires its stale locked room without reviving a dead transition");

        // A reset owns cancellation, including its agreement object and old notice.
        menu = Menu(); AssignRoom(menu, Room());
        Set(menu, "startAgreement", new TaskCompletionSource<Lobby>());
        Call(menu, "ResetLobbyUI", "new session");
        Assert(Get<TaskCompletionSource<Lobby>>(menu, "startAgreement") == null, "Reset cannot leave a completed agreement in a later room");
        auth.PlayerId = "client"; service = LobbyService.Instance = new(); room = Room(); AssignRoom(menu, room);
        var leaveWrite = new TaskCompletionSource<Lobby>(); service.PlayerUpdate = _ => leaveWrite.Task;
        Call(menu, "LeaveLobby"); Call(menu, "ResetLobbyUI", "replacement notice");
        string notice = Get<TMP_Text>(menu, "operationStatus").text;
        leaveWrite.SetResult(room); await Task.Yield();
        Assert(Get<TMP_Text>(menu, "operationStatus").text == notice, "Late Leave response cannot overwrite a newer panel notice");

        // Authentication success followed by name failure must retry the name.
        auth.PlayerId = "host"; auth.IsSignedIn = false; int signIns = 0, nameUpdates = 0;
        auth.SignIn = () => { signIns++; auth.IsSignedIn = true; return Task.CompletedTask; };
        auth.UpdateName = name => { nameUpdates++; return nameUpdates == 1 ? Task.FromException(Timeout()) : Task.CompletedTask; };
        var identity = new PlayerIdentityManager();
        bool first = await (Task<bool>)Call(identity, "InitializeAndSignInAsync", "Tester");
        bool second = await (Task<bool>)Call(identity, "InitializeAndSignInAsync", "Tester");
        Assert(!first && second && signIns == 1 && nameUpdates == 2, "Name retry completes partial authentication without signing in twice");
        auth.SignIn = null; auth.UpdateName = _ => Task.CompletedTask;
    }

    static async Task StartLeaveRaces()
    {
        var auth = Unity.Services.Authentication.AuthenticationService.Instance;
        auth.PlayerId = "host";
        var (coordinator, network) = Connection();
        var service = LobbyService.Instance = new();
        var room = Room(); var host = Menu(); AssignRoom(host, room);
        int writes = 0, removes = 0;
        service.Get = _ => Task.FromResult(room);
        service.Update = options => { writes++; if(options.IsLocked.HasValue)room.IsLocked=options.IsLocked.Value;
            foreach(var pair in options.Data) room.Data[pair.Key]=pair.Value; return Task.FromResult(room); };
        service.Remove = () => { removes++; room.Players.RemoveAll(p=>p.Id=="client"); return Task.CompletedTask; };
        Call(host,"StartGame"); for(int i=0;i<20;i++)Call(host,"StartGame");
        Assert(writes==1 && room.IsLocked && room.Data["RoomState"].Value=="Starting", "Repeated Start publishes one Starting ticket");
        Assert(!Get<Button>(host,"leaveRoomButton").interactable && !Get<Button>(host,"startGameButton").interactable,"Host locks immediately");
        var client=Menu(); auth.PlayerId="client"; AssignRoom(client,room);
        for(int i=0;i<20;i++)Call(client,"LeaveLobby");
        Assert(removes==0,"Repeated manual Leave after Starting cannot remove membership");
        service.PlayerUpdate=options=>{foreach(var p in options.Data)room.Players[1].Data[p.Key]=p.Value;return Task.FromResult(room);};
        await (Task)Call(client,"ReconcileRoomActions");
        Assert(room.Players[1].Data["StartAck"].Value==room.Data["TransitionId"].Value,"Client acknowledges exact Starting ticket");
        auth.PlayerId="host"; await (Task)Call(host,"ReconcileRoomActions"); await Idle(host);
        Assert(network.SceneManager.LoadCalls==1,"Acknowledged roster commits exactly one scene request");
        coordinator.Roster.TryBind(1,"client","client-ticket",out _);
        Call(coordinator,"OnDisconnected",1ul);
        Assert(Get<bool>(coordinator,"returning"),"Real disconnect during committed starting still invokes recovery");
        await Drain(coordinator);

        // A manual Leave write started first, but is delivered after Starting.
        (coordinator,network)=Connection(); service=LobbyService.Instance=new();room=Room();
        auth.PlayerId="client";client=Menu();AssignRoom(client,room);
        var intent=new TaskCompletionSource<Lobby>(); int intentCalls=0;
        service.PlayerUpdate=_=>{intentCalls++;return intent.Task;};
        Call(client,"LeaveLobby");Call(client,"LeaveLobby");
        Assert(intentCalls==1&&Get<bool>(client,"leaveRequested"),"In-flight Leave is recorded synchronously and deduplicated");
        auth.PlayerId="host";host=Menu();AssignRoom(host,room);writes=removes=0;
        service.Get=_=>Task.FromResult(room);
        service.Update=options=>{writes++;if(options.IsLocked.HasValue)room.IsLocked=options.IsLocked.Value;foreach(var p in options.Data)room.Data[p.Key]=p.Value;return Task.FromResult(room);};
        Call(host,"StartGame");
        room.Players[1].Data["LeaveRequested"]=new(PlayerDataObject.VisibilityOptions.Member,"True");intent.SetResult(room);
        await (Task)Call(host,"ReconcileRoomActions");await Idle(host);
        Assert(!room.IsLocked&&room.Data["RoomState"].Value=="Waiting"&&network.SceneManager.LoadCalls==0,"Leave-before-Start cancels start before committing");
        service.Remove=()=>{removes++;room.Players.RemoveAll(p=>p.Id=="client");return Task.CompletedTask;};
        await (Task)Call(host,"ReconcileRoomActions");
        Assert(removes==1,"Host authoritatively accepts the prior manual Leave");
        auth.PlayerId="client";await (Task)Call(client,"ReconcileRoomActions");
        Assert(Get<Lobby>(client,"currentLobby")==null,"Normal client Leave completes on confirmed removal");

        // A pre-commit transport failure restores all remaining participants.
        auth.PlayerId="host";(coordinator,network)=Connection();network.HostResult=false;
        service=LobbyService.Instance=new();room=Room();host=Menu();AssignRoom(host,room);
        service.Get=_=>Task.FromResult(room);
        service.Update=options=>{if(options.IsLocked.HasValue)room.IsLocked=options.IsLocked.Value;foreach(var p in options.Data)room.Data[p.Key]=p.Value;return Task.FromResult(room);};
        Call(host,"StartGame");room.Players[1].Data["StartAck"]=new(PlayerDataObject.VisibilityOptions.Member,room.Data["TransitionId"].Value);
        await (Task)Call(host,"ReconcileRoomActions");await Idle(host);
        Assert(!room.IsLocked&&room.Data["RoomState"].Value=="Waiting"&&Get<Button>(host,"leaveRoomButton").interactable,"Failed Start restores host waiting controls");
        auth.PlayerId="client";client=Menu();AssignRoom(client,room);Set(client,"starting",true);
        await (Task)Call(client,"ReconcileRoomActions");
        Assert(!Get<bool>(client,"starting")&&Get<Button>(client,"leaveRoomButton").interactable,"Remaining client unlocks on rollback publication");
        removes=0;service.Remove=()=>{removes++;return Task.CompletedTask;};Set(client,"starting",true);
        Call(client,"OnApplicationQuit");Assert(removes==1,"Application closure bypasses manual Leave guard for genuine cleanup");
        auth.PlayerId="host";

        // Real membership loss is rechecked after Relay setup, before scene load.
        (coordinator,network)=Connection();service=LobbyService.Instance=new();
        var frozen=AgreedRoom();var departed=AgreedRoom();departed.Players.RemoveAt(1);
        service.Update=_=>Task.FromResult(frozen);service.Get=_=>Task.FromResult(departed);
        await FailedStart(coordinator,frozen);
        Assert(network.SceneManager.LoadCalls==0&&!network.IsListening,"Pre-commit disconnect rejects the final roster and shuts down the uncommitted host");
    }
}
