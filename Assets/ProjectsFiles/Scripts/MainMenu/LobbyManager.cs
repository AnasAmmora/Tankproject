using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

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

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    private Lobby currentLobby;
    private float heartbeatTimer;
    private float pollTimer;
    private bool isReady = false;
    private bool hasJoinedRelay = false;

    private void Start()
    {
        createRoomButton.onClick.AddListener(CreateRoom);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        readyButton.onClick.AddListener(ToggleReady);
        startGameButton.onClick.AddListener(StartGame);
        leaveRoomButton.onClick.AddListener(LeaveLobby); 

        waitingRoomPanel.SetActive(false);
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (currentLobby != null && !hasJoinedRelay)
        {
            pollTimer -= Time.deltaTime;
            if (pollTimer < 0f)
            {
                pollTimer = 1.5f;
                try
                {
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
                    UpdateWaitingRoomUI();
                    CheckIfGameStarted();
                }
                catch (LobbyServiceException e)
                {
                    if (e.Reason == LobbyExceptionReason.LobbyNotFound)
                    {
                        ResetLobbyUI();
                    }
                }
            }
        }
    }

    private Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, PlayerIdentityManager.LocalPlayerName) },
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "False") }
            }
        };
    }

    private async void CreateRoom()
    {
        string lobbyName = PlayerIdentityManager.LocalPlayerName + "'s Room";
        int maxPlayers = maxPlayersDropdown.value == 0 ? 2 : 4;

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = GetPlayer(),
            Data = new Dictionary<string, DataObject>
            {
                { "HostName", new DataObject(DataObject.VisibilityOptions.Public, PlayerIdentityManager.LocalPlayerName) },
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        ShowWaitingRoom();
    }

    public async void JoinLobby(Lobby lobby)
    {
        JoinLobbyByIdOptions options = new JoinLobbyByIdOptions { Player = GetPlayer() };
        currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, options);
        ShowWaitingRoom();
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in response.Results)
            {
                GameObject roomItem = Instantiate(roomItemPrefab, roomListContainer);
                roomItem.GetComponent<RoomItemUI>().Initialize(lobby, this);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to refresh lobby list: {e.Message}");
        }
    }

    private async void ToggleReady()
    {
        isReady = !isReady;
        UpdatePlayerOptions options = new UpdatePlayerOptions
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, isReady ? "True" : "False") }
            }
        };
        currentLobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, options);
    }

    private void UpdateWaitingRoomUI()
    {
        string playersText = "";
        bool allReady = true;

        foreach (Player p in currentLobby.Players)
        {
            bool playerIsReady = p.Data != null && p.Data.ContainsKey("IsReady") && p.Data["IsReady"].Value == "True";
            string pName = p.Data != null && p.Data.ContainsKey("PlayerName") ? p.Data["PlayerName"].Value : "Unknown";

            playersText += pName + (playerIsReady ? " <color=green>[Ready]</color>\n" : " <color=red>[Not Ready]</color>\n");
            if (!playerIsReady) allReady = false;
        }

        playersListText.text = playersText;

        if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = allReady && currentLobby.Players.Count > 1;
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    private void ShowWaitingRoom()
    {
        roomBrowserPanel.SetActive(false);
        waitingRoomPanel.SetActive(true);
    }

    private async void LeaveLobby()
    {
        if (currentLobby == null) return;

        try
        {
            if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"{e.Message}");
        }

        ResetLobbyUI();
    }

    private void ResetLobbyUI()
    {
        currentLobby = null;
        isReady = false;
        hasJoinedRelay = false;
        waitingRoomPanel.SetActive(false);
        roomBrowserPanel.SetActive(true);
        RefreshLobbyList();
    }

 
    private void OnDestroy()
    {
        if (currentLobby != null && !hasJoinedRelay)
        {
            if (currentLobby.HostId == AuthenticationService.Instance.PlayerId)
                LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            else
                LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
        }
    }

    private async void StartGame()
    {
        startGameButton.interactable = false;
        try
        {
            hasJoinedRelay = true;
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(currentLobby.MaxPlayers - 1);
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UpdateLobbyOptions options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> { { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) } }
            };
            currentLobby = await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, options);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error starting game: " + e.Message);
            startGameButton.interactable = true;
            hasJoinedRelay = false;
        }
    }

    private async void CheckIfGameStarted()
    {
        if (currentLobby.Data != null && currentLobby.Data.ContainsKey("RelayCode"))
        {
            string relayCode = currentLobby.Data["RelayCode"].Value;
            if (relayCode != "0" && !hasJoinedRelay)
            {
                hasJoinedRelay = true;
                try
                {
                    JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayCode);
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                        joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
                        joinAllocation.AllocationIdBytes, joinAllocation.Key,
                        joinAllocation.ConnectionData, joinAllocation.HostConnectionData
                    );

                    NetworkManager.Singleton.StartClient();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error joining Relay: " + e.Message);
                    hasJoinedRelay = false;
                }
            }
        }
    }
}