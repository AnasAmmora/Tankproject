using Unity.Netcode;
using Unity.Collections;

public class PlayerState : NetworkBehaviour
{
    public NetworkVariable<int> spawnOrdinal = new NetworkVariable<int>(-1);
    public NetworkVariable<bool> isPrepared = new NetworkVariable<bool>(false);
    public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<int> teamIndex = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            FixedString32Bytes name = default;
            name.CopyFromTruncated(PlayerIdentityManager.LocalPlayerName ?? "Player");
            playerName.Value = name;
        }
    }
}
