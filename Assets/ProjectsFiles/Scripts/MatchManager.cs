using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class MatchManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("0 و 1 للفريق الأزرق | 2 و 3 للفريق الأحمر")]
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    // عداد لمعرفة عدد اللاعبين الذين تم توزيعهم حتى الآن
    private int playersSpawned = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 1. نقل اللاعبين الموجودين فوراً (مثل الـ Host)
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                StartCoroutine(AssignSpawnPointRoutine(clientId));
            }

            // 2. الاستماع لدخول أي لاعب جديد (مثل الكلاينت المتأخر)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        // تنظيف الحدث
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        StartCoroutine(AssignSpawnPointRoutine(clientId));
    }

    private IEnumerator AssignSpawnPointRoutine(ulong clientId)
    {
        // ننتظر اللحظة التي يتم فيها إنشاء مجسم الدبابة الخاص بهذا اللاعب في السيرفر
        NetworkObject playerObj = null;
        while (playerObj == null)
        {
            playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            yield return null;
        }

        Transform targetSpawn = null;

        // توزيع ذكي وتلقائي: الأول أزرق، الثاني أحمر، الثالث أزرق، الرابع أحمر
        if (playersSpawned == 0) targetSpawn = spawnPoints[0];
        else if (playersSpawned == 1) targetSpawn = spawnPoints[2];
        else if (playersSpawned == 2) targetSpawn = spawnPoints[1];
        else if (playersSpawned == 3) targetSpawn = spawnPoints[3];

        playersSpawned++; // زيادة العداد للاعب القادم

        if (targetSpawn != null)
        {
            // تغيير الموقع في السيرفر
            playerObj.transform.position = targetSpawn.position;
            playerObj.transform.rotation = targetSpawn.rotation;

            // أمر الكلاينت بتحديث الفيزياء لتجنب تأثير المطاط (Rubber-banding)
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
            };

            TeleportPlayerClientRpc(targetSpawn.position, targetSpawn.rotation, clientRpcParams);
        }
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams rpcParams = default)
    {
        StartCoroutine(WaitAndTeleport(position, rotation));
    }

    private IEnumerator WaitAndTeleport(Vector3 pos, Quaternion rot)
    {
        while (NetworkManager.Singleton.LocalClient.PlayerObject == null)
        {
            yield return null;
        }

        Transform localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.transform;

        Rigidbody rb = localPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        localPlayer.position = pos;
        localPlayer.rotation = rot;
    }
}