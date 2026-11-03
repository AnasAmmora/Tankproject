using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Core;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;

public class LobbyManager : MonoBehaviour
{
    [Header("Room Browser UI")]
    [SerializeField] private GameObject roomBrowserPanel;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;

    [Header("Waiting Room UI")]
    [SerializeField] private GameObject waitingRoomPanel;
    [SerializeField] private TMP_Text playersListText;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TMP_Text operationStatus;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string menuSceneName = "MainMenuScene";

    private Lobby currentLobby;
    private float heartbeatTimer;
    private float pollTimer;
    private bool isReady;
    private bool hasJoinedRelay;
    private string localJoinToken;
    private bool pollPending;
    private bool heartbeatPending;
    private bool actionPending;
    private bool starting;
    private bool leaveRequested;
    private string acknowledgedStart;
    private TaskCompletionSource<Lobby> startAgreement;
    private double startDeadline;
    private string[] startRoster;
    private string startHost;
    private bool recoveringStart;
    private double roomWaitDeadline;
    private string waitingTransition;
    private bool departurePending;
    private Task departureRequest;
    private bool RoomLocked => starting || leaveRequested || departureRequest != null || hasJoinedRelay || currentLobby?.IsLocked == true ||
        MatchConnectionCoordinator.Instance?.TransitionActive == true;
    private bool refreshPending;
    private bool destroyed;
    private int revision;
    private bool pollWarningShown;
    private bool heartbeatWarningShown;
    private bool missingStatusReported;
    private bool HasLocalTicket => currentLobby?.Players != null && !string.IsNullOrEmpty(localJoinToken) &&
        currentLobby.Players.Any(p => p.Id == AuthenticationService.Instance.PlayerId && p.Data != null &&
        p.Data.TryGetValue("ConnectionTokenHash", out PlayerDataObject hash) && hash.Value == MatchParticipantRoster.TokenHash(localJoinToken));
    private bool SignedIn => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
    private string Role => currentLobby == null ? "Menu" : currentLobby.HostId == AuthenticationService.Instance.PlayerId ? "Host" : "Client";

    private void Start()
    {
        bool valid = true;
        valid &= Required(roomBrowserPanel, nameof(roomBrowserPanel));
        valid &= Required(maxPlayersDropdown, nameof(maxPlayersDropdown));
        valid &= Required(createRoomButton, nameof(createRoomButton));
        valid &= Required(refreshButton, nameof(refreshButton));
        valid &= Required(roomListContainer, nameof(roomListContainer));
        valid &= Required(roomItemPrefab, nameof(roomItemPrefab));
        valid &= Required(waitingRoomPanel, nameof(waitingRoomPanel));
        valid &= Required(playersListText, nameof(playersListText));
        valid &= Required(readyButton, nameof(readyButton));
        valid &= Required(startGameButton, nameof(startGameButton));
        valid &= Required(leaveRoomButton, nameof(leaveRoomButton));
        valid &= Required(operationStatus, nameof(operationStatus));
        if (!valid) { enabled = false; return; }
        createRoomButton.onClick.AddListener(CreateRoom);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        readyButton.onClick.AddListener(ToggleReady);
        startGameButton.onClick.AddListener(StartGame);
        leaveRoomButton.onClick.AddListener(LeaveLobby);
        waitingRoomPanel.SetActive(currentLobby != null);
        UpdateControls();
    }

    private bool Required(UnityEngine.Object value, string field)
    {
        if (value != null) return true;
        Debug.LogError($"LobbyManager on '{name}': missing Inspector reference '{field}'.", this);
        return false;
    }

    private void Update()
    {
        if (startAgreement != null && Time.realtimeSinceStartupAsDouble >= startDeadline)
            startAgreement.TrySetException(new TimeoutException("Participants did not confirm the starting roster."));
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
        if (!hasJoinedRelay && startAgreement == null && roomWaitDeadline > 0 &&
            Time.realtimeSinceStartupAsDouble >= roomWaitDeadline && (leaveRequested || starting))
            RecoverStalledRoom();
    }

    private bool Current(int version) => this != null && !destroyed && revision == version && !hasJoinedRelay;
    private bool CurrentRoom(int version, string id) => Current(version) && currentLobby?.Id == id;
    private double RoomWaitLimit => MatchConnectionCoordinator.Instance?.StartPreparationTimeout ?? 90f;

    public void ConnectionRecoveryFinished(string message)
    {
        ShowNotice(message);
        UpdateControls();
    }

    private bool BeginAction(string operation, bool connecting = false)
    {
        if (destroyed || actionPending || departurePending || hasJoinedRelay || (!connecting && RoomLocked)) return false;
        if (!SignedIn) { ShowNotice("Sign in to Unity services before trying again."); return false; }
        actionPending = true;
        revision++; // Invalidate a poll/query that began before this mutation.
        MatchConnectionCoordinator.Trace(operation, "begin", Role);
        ShowNotice(operation + "...");
        UpdateControls();
        return true;
    }

    private void EndAction(int version)
    {
        if (this == null || destroyed || revision != version) return;
        actionPending = false;
        UpdateControls();
    }

    private void ReportFailure(string operation, Exception error)
    {
        MatchConnectionCoordinator.Trace(operation, MatchConnectionCoordinator.ErrorDescription(error), Role, true);
        ShowNotice(operation + " failed. " + (error is LobbyServiceException
            ? "The lobby service could not complete the request. Please try again."
            : error.Message + " Please try again."));
    }

    // The menu scene supplies this text; missing UI is reported, never generated.
    public void ShowNotice(string message)
    {
        if (this == null || destroyed) return;
        if (operationStatus == null)
        {
            if (!missingStatusReported)
            {
                missingStatusReported = true;
                Debug.LogError($"LobbyManager on '{name}': missing Inspector reference 'operationStatus'.", this);
            }
            return;
        }
        operationStatus.text = message;
    }

    private async void HandleLobbyHeartbeat()
    {
        if (!SignedIn || hasJoinedRelay || heartbeatPending || currentLobby == null ||
            currentLobby.HostId != AuthenticationService.Instance.PlayerId) return;
        heartbeatTimer -= Time.unscaledDeltaTime;
        if (heartbeatTimer > 0f) return;
        heartbeatTimer = 15f;
        heartbeatPending = true;
        int version = revision;
        string id = currentLobby.Id;
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(id);
            if (Current(version)) heartbeatWarningShown = false;
        }
        catch (Exception e)
        {
            if (Current(version) && !heartbeatWarningShown)
            {
                heartbeatWarningShown = true;
                ReportFailure("Heartbeat", e);
            }
        }
        finally { if (this != null) heartbeatPending = false; }
    }

    private async void HandleLobbyPolling()
    {
        if (!SignedIn || currentLobby == null || hasJoinedRelay ||
            departurePending || (departureRequest != null && !departureRequest.IsCompleted) ||
            (actionPending && !starting && !leaveRequested) || pollPending) return;
        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f) return;
        pollTimer = 1.5f;
        pollPending = true;
        int version = revision;
        string id = currentLobby.Id;
        try
        {
            Lobby updated = await LobbyService.Instance.GetLobbyAsync(id);
            if (!Current(version) || currentLobby?.Id != id) return;
            if (updated.LastUpdated < currentLobby.LastUpdated) return;
            currentLobby = updated;
            await ReconcileRoomActions();
            if (!Current(version) || currentLobby == null) return;
            if (pollWarningShown) ShowNotice("");
            pollWarningShown = false;
            UpdateWaitingRoomUI();
            CheckIfGameStarted();
        }
        catch (Exception e)
        {
            if (!Current(version) || currentLobby?.Id != id) return;
            if (e is LobbyServiceException service && (service.Reason == LobbyExceptionReason.LobbyNotFound ||
                service.Reason == LobbyExceptionReason.PlayerNotFound))
                ResetLobbyUI("The lobby no longer exists.");
            else if (!pollWarningShown)
            {
                pollWarningShown = true;
                ReportFailure("Update room", e);
            }
            pollTimer = 5f;
        }
        finally { if (this != null) pollPending = false; }
    }

    private Player GetPlayer()
    {
        localJoinToken = Guid.NewGuid().ToString("N");
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerIdentityManager.LocalPlayerName) },
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "False") },
                { "LeaveRequested", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "False") },
                { "StartAck", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "") },
                { "ConnectionTokenHash", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, MatchParticipantRoster.TokenHash(localJoinToken)) }
            }
        };
    }

    // Check membership before another Create/Join. A lost response does not prove
    // that the service failed to create the room. A failed check stops creation.
    private async Task<bool> RecoverMembershipAsync(int version)
    {
        var coordinator = MatchConnectionCoordinator.Instance;
        if (coordinator != null && coordinator.HasPendingLobbyCleanup)
        {
            await coordinator.CompletePendingCleanupAsync();
            if (!Current(version)) return true;
        }
        List<string> joined = await LobbyService.Instance.GetJoinedLobbiesAsync();
        if (!Current(version)) return true;
        foreach (string id in joined)
        {
            Lobby existing;
            try { existing = await LobbyService.Instance.GetLobbyAsync(id); }
            catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound) { continue; }
            if (!Current(version)) return true;
            if (existing.Data == null || !existing.Data.ContainsKey("RelayCode")) continue;
            Player member = existing.Players?.Find(p => p.Id == AuthenticationService.Instance.PlayerId);
            if (member == null) continue;
            currentLobby = existing;
            ShowWaitingRoom();
            if (existing.IsLocked || Leaving(member))
            {
                // The old process's secret ticket is unavailable. Never acknowledge
                // its transition or revive a membership already being removed.
                leaveRequested = true;
                roomWaitDeadline = Time.realtimeSinceStartupAsDouble + RoomWaitLimit;
                await RecoverStalledRoomAsync(version, existing);
                return true;
            }
            string previousToken = localJoinToken;
            Player registration = GetPlayer();
            string registrationToken = localJoinToken;
            localJoinToken = previousToken;
            Lobby updated = await LobbyService.Instance.UpdatePlayerAsync(id, member.Id,
                new UpdatePlayerOptions { Data = registration.Data });
            if (!CurrentRoom(version, id)) return true;
            localJoinToken = registrationToken;
            currentLobby = updated;
            UpdateWaitingRoomUI();
            ShowNotice("Recovered your existing room. Mark Ready when you are ready.");
            MatchConnectionCoordinator.Trace("RecoverMembership", "existing membership restored", Role);
            return true;
        }
        return false;
    }

    private async void CreateRoom()
    {
        if (currentLobby != null || !BeginAction("Create room")) return;
        int version = revision;
        try
        {
            if (await RecoverMembershipAsync(version)) return;
            if (!Current(version)) return;
            var options = new CreateLobbyOptions
            {
                IsPrivate = false, Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    { "HostName", new DataObject(DataObject.VisibilityOptions.Public, PlayerIdentityManager.LocalPlayerName) },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { "RoomState", new DataObject(DataObject.VisibilityOptions.Member, "Waiting") }
                }
            };
            Lobby created = await LobbyService.Instance.CreateLobbyAsync(PlayerIdentityManager.LocalPlayerName + "'s Room",
                maxPlayersDropdown.value == 0 ? 2 : 4, options);
            if (!Current(version)) return;
            currentLobby = created;
            ShowWaitingRoom();
            ShowNotice("");
            MatchConnectionCoordinator.Trace("Create room", "succeeded", Role);
        }
        catch (Exception e) { if (Current(version)) ReportFailure("Create room", e); }
        finally { EndAction(version); }
    }

    public async void JoinLobby(Lobby target)
    {
        if (target == null || currentLobby != null || !BeginAction("Join room")) return;
        int version = revision;
        try
        {
            if (await RecoverMembershipAsync(version)) return;
            if (!Current(version)) return;
            Lobby joined = await LobbyService.Instance.JoinLobbyByIdAsync(target.Id, new JoinLobbyByIdOptions { Player = GetPlayer() });
            if (!Current(version)) return;
            currentLobby = joined;
            ShowWaitingRoom();
            ShowNotice("");
            MatchConnectionCoordinator.Trace("Join room", "succeeded", Role);
        }
        catch (Exception e) { if (Current(version)) ReportFailure("Join room", e); }
        finally { EndAction(version); }
    }

    public async void RefreshLobbyList()
    {
        if (this == null || destroyed || hasJoinedRelay || actionPending || refreshPending || currentLobby != null) return;
        if (!SignedIn) { ShowNotice("Sign in to refresh rooms."); return; }
        refreshPending = true;
        int version = revision;
        UpdateControls();
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter> { new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT) },
                Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) }
            });
            if (!Current(version) || currentLobby != null) return;
            foreach (Transform child in roomListContainer) Destroy(child.gameObject);
            foreach (Lobby room in response.Results)
            {
                if (room.IsLocked) continue;
                Instantiate(roomItemPrefab, roomListContainer).GetComponent<RoomItemUI>().Initialize(room, this);
            }
            MatchConnectionCoordinator.Trace("Refresh rooms", "succeeded", Role);
        }
        catch (Exception e) { if (Current(version)) ReportFailure("Refresh rooms", e); }
        finally
        {
            if (this != null && !destroyed) { refreshPending = false; UpdateControls(); }
        }
    }

    private async void ToggleReady()
    {
        if (currentLobby == null || currentLobby.IsLocked || !BeginAction("Update Ready")) return;
        int version = revision;
        bool requestedReady = !isReady;
        try
        {
            if (string.IsNullOrEmpty(localJoinToken)) GetPlayer();
            Lobby updated = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId,
                new UpdatePlayerOptions { Data = new Dictionary<string, PlayerDataObject>
                {
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, requestedReady ? "True" : "False") },
                    { "ConnectionTokenHash", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, MatchParticipantRoster.TokenHash(localJoinToken)) }
                } });
            if (!Current(version)) return;
            currentLobby = updated;
            UpdateWaitingRoomUI();
            ShowNotice("");
        }
        catch (Exception e) { if (Current(version)) ReportFailure("Update Ready", e); }
        finally { EndAction(version); }
    }

    private bool AllReady => currentLobby?.Players != null && currentLobby.Players.Count > 1 &&
        currentLobby.Players.All(p => p.Data != null && p.Data.TryGetValue("IsReady", out PlayerDataObject ready) && ready.Value == "True");

    private void UpdateControls()
    {
        if (this == null || destroyed) return;
        bool available = !actionPending && !departurePending && !RoomLocked;
        if (createRoomButton != null) createRoomButton.interactable = available && currentLobby == null;
        if (refreshButton != null) refreshButton.interactable = available && !refreshPending && currentLobby == null;
        if (maxPlayersDropdown != null) maxPlayersDropdown.interactable = available && currentLobby == null;
        if (readyButton != null) readyButton.interactable = available && currentLobby != null && !currentLobby.IsLocked;
        if (leaveRoomButton != null) leaveRoomButton.interactable = available && currentLobby != null;
        if (startGameButton != null)
        {
            bool host = SignedIn && currentLobby?.HostId == AuthenticationService.Instance.PlayerId;
            startGameButton.gameObject.SetActive(host);
            startGameButton.interactable = available && host && !currentLobby.IsLocked && HasLocalTicket && AllReady &&
                MatchConnectionCoordinator.Instance?.CanBegin == true;
        }
        if (roomListContainer != null)
            foreach (Button button in roomListContainer.GetComponentsInChildren<Button>(true))
                button.interactable = available && currentLobby == null;
    }

    private void UpdateWaitingRoomUI()
    {
        if (currentLobby == null) return;
        string text = "";
        foreach (Player player in currentLobby.Players)
        {
            bool ready = player.Data != null && player.Data.TryGetValue("IsReady", out PlayerDataObject value) && value.Value == "True";
            string name = player.Data != null && player.Data.TryGetValue("PlayerName", out PlayerDataObject label) ? label.Value : "Unknown";
            text += name + (ready ? " <color=green>[Ready]</color>\n" : " <color=red>[Not Ready]</color>\n");
            if (player.Id == AuthenticationService.Instance.PlayerId) isReady = ready;
        }
        playersListText.text = text;
        UpdateControls();
    }

    private void ShowWaitingRoom()
    {
        roomBrowserPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);
        pollTimer = heartbeatTimer = 0f;
        UpdateWaitingRoomUI();
    }

    private async void LeaveLobby()
    {
        if (currentLobby == null || !BeginAction("Leave room")) return;
        int version = revision;
        Lobby leaving = currentLobby;
        try
        {
            if (leaving.HostId == AuthenticationService.Instance.PlayerId)
                await LobbyService.Instance.DeleteLobbyAsync(leaving.Id);
            else
            {
                // Only the host removes members for manual Leave. An intent already
                // in flight must settle before this client can acknowledge Starting.
                leaveRequested = true;
                roomWaitDeadline = Time.realtimeSinceStartupAsDouble + RoomWaitLimit;
                await LobbyService.Instance.UpdatePlayerAsync(leaving.Id, AuthenticationService.Instance.PlayerId,
                    new UpdatePlayerOptions { Data = new Dictionary<string, PlayerDataObject>
                    { { "LeaveRequested", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "True") } } });
                if (CurrentRoom(version, leaving.Id)) ShowNotice("Waiting for the host to accept Leave...");
                return; // Polling observes the authoritative removal.
            }
            if (Current(version)) ResetLobbyUI("Left room.");
        }
        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound || e.Reason == LobbyExceptionReason.PlayerNotFound)
        { if (Current(version)) ResetLobbyUI("Room membership has ended."); }
        catch (Exception e) { if (Current(version)) ReportFailure("Leave room", e); }
        finally { if (!leaveRequested) EndAction(version); }
    }

    private async void RecoverStalledRoom()
    {
        if (currentLobby == null || departurePending) return;
        await RecoverStalledRoomAsync(revision, currentLobby);
    }

    private async Task RecoverStalledRoomAsync(int version, Lobby room)
    {
        if (!CurrentRoom(version, room.Id) || departurePending) return;
        departurePending = true;
        leaveRequested = true; // Withdraw participation; never send a later StartAck.
        actionPending = true;
        UpdateControls();
        ShowNotice("Recovering room membership. Please wait...");
        try
        {
            // This is failed-session recovery, not a manual Leave during Start.
            // A timed-out SDK request remains owned until it actually settles.
            if (departureRequest == null)
                departureRequest = room.HostId == AuthenticationService.Instance.PlayerId
                    ? LobbyService.Instance.DeleteLobbyAsync(room.Id)
                    : LobbyService.Instance.RemovePlayerAsync(room.Id, AuthenticationService.Instance.PlayerId);
            Task request = departureRequest;
            if (await Task.WhenAny(request, Task.Delay(10000)) != request)
                throw new TimeoutException("Membership recovery is still pending. It will be checked again.");
            await request;
            if (CurrentRoom(version, room.Id)) ResetLobbyUI("Previous room membership ended. You can create or join a room.");
        }
        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound ||
            e.Reason == LobbyExceptionReason.PlayerNotFound)
        { if (CurrentRoom(version, room.Id)) ResetLobbyUI("Room membership has ended."); }
        catch (Exception e)
        {
            if (CurrentRoom(version, room.Id))
            {
                if (departureRequest?.IsCompleted == true) departureRequest = null;
                roomWaitDeadline = Time.realtimeSinceStartupAsDouble + 5;
                ReportFailure("Recover room membership (will retry)", e);
            }
        }
        finally
        {
            if (this != null && !destroyed) { departurePending = false; UpdateControls(); }
        }
    }

    private void ResetLobbyUI(string reason)
    {
        MatchConnectionCoordinator.Trace("ResetLobbyUI", "reason=" + reason, Role);
        revision++;
        actionPending = false;
        starting = leaveRequested = false;
        recoveringStart = false;
        roomWaitDeadline = 0;
        waitingTransition = null;
        departureRequest = null;
        var agreement = startAgreement;
        startAgreement = null;
        startRoster = null;
        startHost = null;
        agreement?.TrySetException(new InvalidOperationException("Room membership ended during Start."));
        acknowledgedStart = null;
        currentLobby = null;
        localJoinToken = null;
        isReady = hasJoinedRelay = false;
        waitingRoomPanel.SetActive(false);
        roomBrowserPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowNotice(reason);
        UpdateControls();
        RefreshLobbyList();
    }

    private static string RoomData(Lobby room, string key) => room?.Data != null &&
        room.Data.TryGetValue(key, out var value) ? value.Value : "";

    private static bool Leaving(Player player) => player.Data != null &&
        player.Data.TryGetValue("LeaveRequested", out var value) && value.Value == "True";

    private async Task ReconcileRoomActions()
    {
        int version = revision;
        Lobby room = currentLobby;
        if (room == null || departurePending) return;
        string id = room.Id;
        string local = AuthenticationService.Instance.PlayerId;
        if (currentLobby.Players == null || !currentLobby.Players.Any(p => p.Id == local))
        { ResetLobbyUI("Room membership has ended."); return; }
        bool host = currentLobby.HostId == local;
        if (host && recoveringStart) { await RestoreWaitingRoom(); return; }
        bool announced = RoomData(currentLobby, "RoomState") == "Starting";
        if (announced && host && RoomData(currentLobby, "StartingHost") != local)
        { await RestoreWaitingRoom(); return; } // Host migration cancels the old host's proposal.
        bool wasStarting = starting;
        starting = announced || (host && startAgreement != null);
        string transition = RoomData(room, "TransitionId");
        if (announced && waitingTransition != transition)
        {
            waitingTransition = transition;
            if (!leaveRequested) roomWaitDeadline = Time.realtimeSinceStartupAsDouble + RoomWaitLimit;
        }
        if (!announced && !leaveRequested) { roomWaitDeadline = 0; waitingTransition = null; }
        UpdateControls();
        if (startAgreement != null)
        {
            bool unchanged = currentLobby.HostId == startHost && currentLobby.Players.Select(p => p.Id)
                .OrderBy(id => id).SequenceEqual(startRoster);
            if (!unchanged || !AllReady || currentLobby.Players.Any(Leaving))
            { startAgreement.TrySetException(new InvalidOperationException("The roster changed or a player requested Leave. Start canceled.")); return; }
        }
        if (!announced)
        {
            acknowledgedStart = null;
            if (wasStarting && !starting) ShowNotice("Start canceled. The waiting room is available again.");
            if (leaveRequested && !Leaving(room.Players.First(p => p.Id == local)))
                await LobbyService.Instance.UpdatePlayerAsync(id, local, new UpdatePlayerOptions
                { Data = new Dictionary<string, PlayerDataObject> {
                    { "LeaveRequested", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "True") } } });
            if (!CurrentRoom(version, id)) return;
            if (host)
                foreach (var player in room.Players.Where(Leaving).ToArray())
                {
                    if (!CurrentRoom(version, id) || starting || RoomData(currentLobby, "RoomState") == "Starting") return;
                    try { await LobbyService.Instance.RemovePlayerAsync(id, player.Id); }
                    catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.PlayerNotFound)
                    { /* A concurrent real disconnect already removed this member. */ }
                    if (!CurrentRoom(version, id)) return;
                }
            return;
        }
        ShowNotice("Starting match. Please wait...");
        string ticket = RoomData(currentLobby, "TransitionId");
        if (leaveRequested)
        {
            // Retry an uncertain intent write, never acknowledge a start over it.
            if (!Leaving(room.Players.First(p => p.Id == local)))
                await LobbyService.Instance.UpdatePlayerAsync(id, local, new UpdatePlayerOptions
                { Data = new Dictionary<string, PlayerDataObject> {
                    { "LeaveRequested", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "True") } } });
            return;
        }
        if (!host && !actionPending && HasLocalTicket && acknowledgedStart != ticket)
        {
            // Synchronously starting=true blocks a subsequent manual Leave before
            // this acknowledgement's asynchronous write can reach the service.
            await LobbyService.Instance.UpdatePlayerAsync(id, local, new UpdatePlayerOptions
            { Data = new Dictionary<string, PlayerDataObject> {
                { "StartAck", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ticket) } } });
            if (!CurrentRoom(version, id)) return;
            acknowledgedStart = ticket;
        }
        if (host && startAgreement != null && currentLobby.Players.All(p => p.Id == local ||
            (p.Data != null && p.Data.TryGetValue("StartAck", out var ack) && ack.Value == ticket)))
            startAgreement.TrySetResult(currentLobby);
    }

    private async Task RestoreWaitingRoom()
    {
        int version = revision;
        Lobby room = currentLobby;
        recoveringStart = true;
        if (room == null) { recoveringStart = false; return; }
        if (currentLobby.HostId != AuthenticationService.Instance.PlayerId)
        { recoveringStart = false; return; } // Only the current authority may unlock the room.
        try
        {
            Lobby restored = await LobbyService.Instance.UpdateLobbyAsync(room.Id, new UpdateLobbyOptions
            { IsLocked = false, Data = new Dictionary<string, DataObject> {
                { "RoomState", new DataObject(DataObject.VisibilityOptions.Member, "Waiting") },
                { "TransitionId", new DataObject(DataObject.VisibilityOptions.Member, "") },
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") } } });
            if (!CurrentRoom(version, room.Id)) return;
            currentLobby = restored;
            starting = recoveringStart = false;
            if (!leaveRequested) { roomWaitDeadline = 0; waitingTransition = null; }
            pollTimer = 0f;
        }
        catch (Exception e) { if (CurrentRoom(version, room.Id)) ReportFailure("Restore waiting room (will retry)", e); }
        if (CurrentRoom(version, room.Id)) UpdateControls();
    }

    private async void StartGame()
    {
        if (currentLobby == null || currentLobby.HostId != AuthenticationService.Instance.PlayerId || !AllReady || !HasLocalTicket ||
            currentLobby.IsLocked || MatchConnectionCoordinator.Instance?.CanBegin != true || !BeginAction("Start match")) return;
        int version = revision;
        string roomId = currentLobby.Id;
        MatchConnectionCoordinator coordinator = MatchConnectionCoordinator.Instance;
        starting = true;
        UpdateControls();
        try
        {
            if (coordinator == null || !coordinator.CanBegin)
                throw new InvalidOperationException("The connection manager is missing or still closing a previous connection.");
            startRoster = currentLobby.Players.Select(p => p.Id).OrderBy(id => id).ToArray();
            startHost = currentLobby.HostId;
            startAgreement = new TaskCompletionSource<Lobby>();
            startDeadline = Time.realtimeSinceStartupAsDouble + coordinator.StartPreparationTimeout;
            string ticket = Guid.NewGuid().ToString("N");
            Lobby announced = await LobbyService.Instance.UpdateLobbyAsync(roomId, new UpdateLobbyOptions
            {
                IsLocked = true, Data = new Dictionary<string, DataObject>
                {
                    { "RoomState", new DataObject(DataObject.VisibilityOptions.Member, "Starting") },
                    { "StartingHost", new DataObject(DataObject.VisibilityOptions.Member, startHost) },
                    { "TransitionId", new DataObject(DataObject.VisibilityOptions.Member, ticket) },
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
                }
            });
            if (!CurrentRoom(version, roomId)) return;
            currentLobby = announced;
            pollTimer = 0f;
            ShowNotice("Starting match. Confirming participants...");
            Lobby agreed = await startAgreement.Task;
            if (!CurrentRoom(version, roomId)) return;
            // Transfer ownership only after every remaining client has settled prior
            // actions and acknowledged this exact roster/transition.
            hasJoinedRelay = true;
            UpdateControls();
            await coordinator.StartHostMatchAsync(agreed, localJoinToken, gameSceneName, menuSceneName);
        }
        catch (Exception e)
        {
            if (this != null && !destroyed && revision == version && coordinator?.TransitionActive != true)
            {
                hasJoinedRelay = false;
                await RestoreWaitingRoom();
                ReportFailure("Start match", e);
            }
        }
        finally { if (this != null && !destroyed && revision == version) { startAgreement = null; EndAction(version); } }
    }

    private async void CheckIfGameStarted()
    {
        if (hasJoinedRelay || actionPending || currentLobby?.Data == null ||
            !currentLobby.Data.TryGetValue("RelayCode", out DataObject code) ||
            string.IsNullOrWhiteSpace(code.Value) || code.Value == "0") return;
        if (!HasLocalTicket || leaveRequested || !BeginAction("Connect to match", true)) return;
        int version = revision;
        try
        {
            MatchConnectionCoordinator coordinator = MatchConnectionCoordinator.Instance;
            if (coordinator == null || !coordinator.CanBegin)
                throw new InvalidOperationException("The connection manager is missing or still closing a previous connection.");
            hasJoinedRelay = true;
            UpdateControls();
            await coordinator.JoinMatchAsync(currentLobby, localJoinToken, gameSceneName, menuSceneName);
        }
        catch (Exception e)
        {
            if (this != null && !destroyed && revision == version)
            { hasJoinedRelay = false; ReportFailure("Connect to match", e); }
        }
        finally { EndAction(version); }
    }

    private void OnDestroy()
    {
        destroyed = true;
        revision++;
        startAgreement?.TrySetCanceled();
        if (createRoomButton != null) createRoomButton.onClick.RemoveListener(CreateRoom);
        if (refreshButton != null) refreshButton.onClick.RemoveListener(RefreshLobbyList);
        if (readyButton != null) readyButton.onClick.RemoveListener(ToggleReady);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(StartGame);
        if (leaveRoomButton != null) leaveRoomButton.onClick.RemoveListener(LeaveLobby);
        MatchConnectionCoordinator.Trace("LobbyUI destroyed", hasJoinedRelay ? "transition owns session; no cleanup" : "UI lifetime ended; no cleanup");
    }

    private async void OnApplicationQuit()
    {
        Lobby leaving = currentLobby;
        if (hasJoinedRelay || leaving == null || !SignedIn) return;
        string playerId = AuthenticationService.Instance.PlayerId;
        currentLobby = null;
        try
        {
            if (leaving.HostId == playerId) await LobbyService.Instance.DeleteLobbyAsync(leaving.Id);
            else await LobbyService.Instance.RemovePlayerAsync(leaving.Id, playerId);
        }
        catch (Exception e) { MatchConnectionCoordinator.Trace("Quit lobby cleanup", MatchConnectionCoordinator.ErrorDescription(e), warning: true); }
    }
}
