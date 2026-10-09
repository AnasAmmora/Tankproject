using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class MatchManager : NetworkBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints = new Transform[4];
    private int playersSpawned = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                StartCoroutine(AssignSpawnPointRoutine(clientId));
            }

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    public override void OnNetworkDespawn()
    {
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
        NetworkObject playerObj = null;
        while (playerObj == null)
        {
            playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
            yield return null;
        }

        Transform targetSpawn = null;

        if (playersSpawned == 0) targetSpawn = spawnPoints[0];
        else if (playersSpawned == 1) targetSpawn = spawnPoints[2];
        else if (playersSpawned == 2) targetSpawn = spawnPoints[1];
        else if (playersSpawned == 3) targetSpawn = spawnPoints[3];

        playersSpawned++;

        if (targetSpawn != null)
        {
            playerObj.transform.position = targetSpawn.position;
            playerObj.transform.rotation = targetSpawn.rotation;

            // 🟢 تحديد الفريق: اللاعب 1 و 3 فريق أزرق (0)، واللاعب 2 و 4 فريق أحمر (1)
            if (playerObj.TryGetComponent<PlayerState>(out PlayerState pState))
            {
                pState.teamIndex.Value = (playersSpawned == 1 || playersSpawned == 3) ? 0 : 1;
            }

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