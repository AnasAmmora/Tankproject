using Unity.Netcode;
using UnityEngine;
using TMPro;
using Unity.Collections;
using UnityEngine.Rendering;

public class PlayerNameDisplay : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private PlayerState playerState;
    [SerializeField] private PlayerNameDistanceScale distanceScale;
    [SerializeField] private Canvas nameCanvas;
    private bool subscribed;
    private bool presenting;

    public override void OnNetworkSpawn()
    {
        if (nameText == null || playerState == null || distanceScale == null || nameCanvas == null)
        {
            string missing = nameText == null ? nameof(nameText) : playerState == null ? nameof(playerState) :
                distanceScale == null ? nameof(distanceScale) : nameof(nameCanvas);
            Debug.LogError($"PlayerNameDisplay on '{name}': missing Inspector reference '{missing}'.", this);
            return;
        }
        if (nameCanvas.renderMode != RenderMode.WorldSpace || !nameText.transform.IsChildOf(nameCanvas.transform))
        {
            Debug.LogError($"PlayerNameDisplay on '{name}': 'nameText' requires its authored world-space Canvas.", this);
            return;
        }
        // Hide only the owner's visual; leave this NetworkBehaviour active for reuse.
        nameCanvas.enabled = false; // Wait for the first local camera's distance check.
        distanceScale.ResetPresentation();
        if (!IsClient || IsOwner) return;
        if (!subscribed)
        {
            playerState.playerName.OnValueChanged += OnNameChanged;
            subscribed = true;
        }

        nameText.text = playerState.playerName.Value.ToString();
        BeginPresentation();
    }

    private void OnNameChanged(FixedString32Bytes previous, FixedString32Bytes current) => nameText.text = current.ToString();

    public override void OnNetworkDespawn()
    {
        EndPresentation();
        if (subscribed && playerState != null) playerState.playerName.OnValueChanged -= OnNameChanged;
        subscribed = false;
        if (nameCanvas != null) nameCanvas.enabled = false;
    }

    private void OnEnable()
    {
        if (IsSpawned && IsClient && !IsOwner && subscribed) BeginPresentation();
    }

    private void OnDisable() => EndPresentation();

    private void BeginPresentation()
    {
        if (presenting) return;
        RenderPipelineManager.beginCameraRendering += BeforeCamera;
        presenting = true;
    }

    private void EndPresentation()
    {
        if (presenting) RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        presenting = false;
        if (distanceScale != null) distanceScale.ResetPresentation();
    }

    private void BeforeCamera(ScriptableRenderContext context, Camera view)
    {
        if (!IsSpawned || !IsClient || IsOwner || view == null || !view.isActiveAndEnabled ||
            view.cameraType != CameraType.Game || !view.CompareTag("MainCamera")) return;
        transform.rotation = view.transform.rotation;
        nameCanvas.enabled = distanceScale.Present(view);
    }
}
