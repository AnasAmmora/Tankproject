using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;

public class PlayerNameDisplay : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private PlayerState playerState;
    private bool subscribed;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        if (nameText == null || playerState == null)
        {
            Debug.LogError($"PlayerNameDisplay on '{name}': missing Inspector reference '{(nameText == null ? nameof(nameText) : nameof(playerState))}'.", this);
            return;
        }
        if (!subscribed)
        {
            playerState.playerName.OnValueChanged += OnNameChanged;
            subscribed = true;
        }

        nameText.text = playerState.playerName.Value.ToString();
    }

    private void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current) => nameText.text = current.ToString();

    public override void OnNetworkDespawn()
    {
        if (subscribed && playerState != null) playerState.playerName.OnValueChanged -= OnNameChanged;
        subscribed = false;
    }

    private void LateUpdate()
    {
        if (!IsOwner && Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}
