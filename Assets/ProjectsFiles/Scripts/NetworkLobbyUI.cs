using UnityEngine;
using Unity.Netcode;

public class NetworkLobbyUI : MonoBehaviour
{
    private void Start()
    {
        // نتأكد من وجود Singleton قبل إضافة الـ Callback
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (NetworkManager.Singleton.ConnectedClients.Count >= 4)
        {
            response.Approved = false;
            response.Reason = "Lobby is full!";
        }
        else
        {
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
    }

    private void OnGUI()
    {
        // حماية واجهة المستخدم من الانهيار إذا لم يكن هناك NetworkManager
        if (NetworkManager.Singleton == null)
        {
            GUILayout.Label("NetworkManager is missing in the scene!");
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        try
        {
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Start Host (Player 1)"))
                {
                    NetworkManager.Singleton.StartHost();
                }
                if (GUILayout.Button("Join Client (Player 2,3,4)"))
                {
                    NetworkManager.Singleton.StartClient();
                }
            }
            else
            {
                GUILayout.Label($"Players connected: {NetworkManager.Singleton.ConnectedClients.Count} / 4");
            }
        }
        finally
        {
            // نضمن دائماً إغلاق الـ Area حتى لو حدث خطأ في الأعلى
            GUILayout.EndArea();
        }
    }
}