using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerNameDisplay : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private PlayerState playerState;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        playerState.playerName.OnValueChanged += (oldVal, newVal) =>
        {
            nameText.text = newVal.ToString();
        };

        nameText.text = playerState.playerName.Value.ToString();
    }

    private void LateUpdate()
    {
        if (!IsOwner && Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}