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
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private bool Current(int version) => this != null && !destroyed && revision == version && !hasJoinedRelay;

    private bool BeginAction(string operation)
    {
        if (destroyed || actionPending || hasJoinedRelay) return false;
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
        if (!SignedIn || currentLobby == null || hasJoinedRelay || actionPending || pollPending) return;
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
            if (pollWarningShown) ShowNotice("");
            pollWarningShown = false;
            UpdateWaitingRoomUI();
            CheckIfGameStarted();
        }
        catch (Exception e)
        {
            if (!Current(version) || currentLobby?.Id != id) return;
            if (e is LobbyServiceException service && service.Reason == LobbyExceptionReason.LobbyNotFound)
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
                { "ConnectionTokenHash", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, MatchParticipantRoster.TokenHash(localJoinToken)) }
            }
        };
    }

    // Check membership before another Create/Join. A lost response does not prove
    // that the service failed to create the room. A failed check stops creation.
    private async Task<bool> RecoverMembershipAsync(int version)
    {
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
            if (existing.IsLocked)
            {
                ShowNotice("A previous room is still locked. Leave it before creating or joining another room.");
                return true;
            }
            Player registration = GetPlayer();
            Lobby updated = await LobbyService.Instance.UpdatePlayerAsync(id, member.Id,
                new UpdatePlayerOptions { Data = registration.Data });
            if (!Current(version)) return true;
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
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
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
        bool available = !actionPending && !hasJoinedRelay;
        if (createRoomButton != null) createRoomButton.interactable = available && currentLobby == null;
        if (refreshButton != null) refreshButton.interactable = available && !refreshPending && currentLobby == null;
        if (maxPlayersDropdown != null) maxPlayersDropdown.interactable = available && currentLobby == null;
        if (readyButton != null) readyButton.interactable = available && currentLobby != null && !currentLobby.IsLocked;
        if (leaveRoomButton != null) leaveRoomButton.interactable = available && currentLobby != null;
        if (startGameButton != null)
        {
            bool host = SignedIn && currentLobby?.HostId == AuthenticationService.Instance.PlayerId;
            startGameButton.gameObject.SetActive(host);
            startGameButton.interactable = available && host && !currentLobby.IsLocked && HasLocalTicket && AllReady;
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
            else await LobbyService.Instance.RemovePlayerAsync(leaving.Id, AuthenticationService.Instance.PlayerId);
            if (Current(version)) ResetLobbyUI("Left room.");
        }
        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound || e.Reason == LobbyExceptionReason.PlayerNotFound)
        { if (Current(version)) ResetLobbyUI("Room membership has ended."); }
        catch (Exception e) { if (Current(version)) ReportFailure("Leave room", e); }
        finally { EndAction(version); }
    }

    private void ResetLobbyUI(string reason)
    {
        MatchConnectionCoordinator.Trace("ResetLobbyUI", "reason=" + reason, Role);
        revision++;
        actionPending = false;
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

    private async void StartGame()
    {
        if (currentLobby == null || currentLobby.HostId != AuthenticationService.Instance.PlayerId || !AllReady || !HasLocalTicket ||
            currentLobby.IsLocked || !BeginAction("Start match")) return;
        int version = revision;
        try
        {
            MatchConnectionCoordinator coordinator = MatchConnectionCoordinator.Instance;
            if (coordinator == null || !coordinator.CanBegin)
                throw new InvalidOperationException("The connection manager is missing or still closing a previous connection.");
            hasJoinedRelay = true; // Transfer cleanup ownership before any await or scene change.
            UpdateControls();
            await coordinator.StartHostMatchAsync(currentLobby, localJoinToken, gameSceneName, menuSceneName);
        }
        catch (Exception e)
        {
            if (this != null && !destroyed && revision == version)
            { hasJoinedRelay = false; ReportFailure("Start match", e); }
        }
        finally { EndAction(version); }
    }

    private async void CheckIfGameStarted()
    {
        if (hasJoinedRelay || actionPending || currentLobby?.Data == null ||
            !currentLobby.Data.TryGetValue("RelayCode", out DataObject code) ||
            string.IsNullOrWhiteSpace(code.Value) || code.Value == "0") return;
        if (!HasLocalTicket || !BeginAction("Connect to match")) return;
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
