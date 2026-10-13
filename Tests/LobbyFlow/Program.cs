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
    { Set(menu, "currentLobby", room); Set(menu, "localJoinToken", "host-ticket"); Call(menu, "ShowWaitingRoom"); }
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
        service = LobbyService.Instance = new(); saved = Room();
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
        service = LobbyService.Instance = new(); saved = Room(); (coordinator, network) = Connection(); writes = 0;
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
        service = LobbyService.Instance = new(); saved = Room(); (coordinator, network) = Connection(); network.HostResult = false; writes = 0;
        service.Update = options => { writes++; return Task.FromResult(saved); };
        await coordinator.StartHostMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(writes == 1 && Get<bool>(coordinator, "returning"), "StartHost false prevents publication");
        await Drain(coordinator);
        Assert(coordinator.CanBegin && !LoadingScreenController.Instance.Visible, "StartHost failure allows another attempt");
        Assert(Debug.Messages.Any(m => m.Contains("StartHost returned false")), "Original network start error logged");

        // Scene rejection and StartClient false must both use the same recoverable exit.
        service = LobbyService.Instance = new(); saved = Room(); (coordinator, network) = Connection(); writes = 0;
        network.SceneManager.Result = SceneEventProgressStatus.SceneFailedVerification;
        service.Update = options => { writes++; return Task.FromResult(saved); };
        await coordinator.StartHostMatchAsync(saved, "host-ticket", "GameScene", "MainMenuScene");
        Assert(writes == 1 && Get<bool>(coordinator, "returning"), "Rejected scene request prevents publication");
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
        service = LobbyService.Instance = new(); saved = Room(); (coordinator, network) = Connection(); writes = 0;
        service.Update = options =>
        {
            writes++;
            if (writes == 1) return Task.FromResult(saved);
            coordinator.Roster.TryBind(1, "client", "client-ticket", out _);
            network.ConnectedClients[1] = new();
            coordinator.NotifyMatchStarted();
            return Failed<Lobby>();
        };
        service.Get = _ => throw new Exception("Read-back unnecessary when every authenticated peer joined");
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
        Console.WriteLine($"PASS: {checks} lobby flow assertions (simulated service/engine boundary).");
    }
}
