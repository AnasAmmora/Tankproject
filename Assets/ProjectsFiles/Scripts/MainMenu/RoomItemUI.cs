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

        // استخراج اسم صاحب الغرفة من البيانات الإضافية
        string hostName = lobby.Data.ContainsKey("HostName") ? lobby.Data["HostName"].Value : "Unknown";
        roomNameText.text = hostName + "'s Room";

        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

        // عند الضغط على Join، نخبر الـ LobbyManager بالانضمام لهذه الغرفة
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => lobbyManager.JoinLobby(lobby));
    }
}