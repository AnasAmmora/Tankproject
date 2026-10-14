using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class RoomItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    private Lobby lobby;
    private LobbyManager lobbyManager;

    public void Initialize(Lobby _lobby, LobbyManager _manager)
    {
        lobby = _lobby;
        lobbyManager = _manager;

        if (roomNameText == null || playerCountText == null || joinButton == null)
        {
            Debug.LogError($"RoomItemUI on '{name}': missing Inspector reference '{(roomNameText == null ? nameof(roomNameText) : playerCountText == null ? nameof(playerCountText) : nameof(joinButton))}'.", this);
            return;
        }
        string hostName = lobby.Data != null && lobby.Data.ContainsKey("HostName") ? lobby.Data["HostName"].Value : "Unknown";
        roomNameText.text = hostName + "'s Room";

        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        joinButton.onClick.RemoveListener(OnJoin);
        joinButton.onClick.AddListener(OnJoin);
    }

    private void OnJoin() => lobbyManager.JoinLobby(lobby);
    private void OnDestroy() { if (joinButton != null) joinButton.onClick.RemoveListener(OnJoin); }
}
