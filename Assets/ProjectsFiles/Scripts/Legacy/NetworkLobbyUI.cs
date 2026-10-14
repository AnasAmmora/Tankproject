using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

// SampleScene's direct-network test controls, authored in a UI prefab.
public class NetworkLobbyUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_Text connectionStatus;
    private NetworkManager network;
    private bool subscribed;
    private string failure;

    private void Start()
    {
        if (hostButton == null || clientButton == null || connectionStatus == null)
        {
            Debug.LogError($"NetworkLobbyUI on '{name}': missing Inspector reference '{(hostButton == null ? nameof(hostButton) : clientButton == null ? nameof(clientButton) : nameof(connectionStatus))}'.", this);
            enabled = false;
            return;
        }
        network = NetworkManager.Singleton;
        if (network == null)
        {
            Debug.LogError("NetworkLobbyUI: NetworkManager is missing from SampleScene.", this);
            connectionStatus.text = "NetworkManager is missing from the scene.";
            hostButton.interactable = clientButton.interactable = false;
            enabled = false;
            return;
        }
        hostButton.onClick.AddListener(StartHost);
        clientButton.onClick.AddListener(StartClient);
        network.ConnectionApprovalCallback += ApprovalCheck;
        subscribed = true;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = network.ConnectedClients.Count < 4;
        response.Reason = response.Approved ? "" : "Lobby is full!";
        response.CreatePlayerObject = response.Approved;
    }

    private void StartHost()
    {
        if (network.IsListening) return;
        failure = network.StartHost() ? "" : "Could not start Host. See the Netcode log.";
    }

    private void StartClient()
    {
        if (network.IsListening) return;
        failure = network.StartClient() ? "" : "Could not start Client. See the Netcode log.";
    }

    private void Update()
    {
        if (network == null) return;
        bool connected = network.IsClient || network.IsServer;
        hostButton.gameObject.SetActive(!connected);
        clientButton.gameObject.SetActive(!connected);
        hostButton.interactable = clientButton.interactable = !network.IsListening && !network.ShutdownInProgress;
        connectionStatus.text = connected ? $"Players connected: {network.ConnectedClients.Count} / 4" : failure ?? "Start Host or join as Client.";
    }

    private void OnDestroy()
    {
        if (hostButton != null) hostButton.onClick.RemoveListener(StartHost);
        if (clientButton != null) clientButton.onClick.RemoveListener(StartClient);
        if (subscribed && network != null) network.ConnectionApprovalCallback -= ApprovalCheck;
        subscribed = false;
    }
}
