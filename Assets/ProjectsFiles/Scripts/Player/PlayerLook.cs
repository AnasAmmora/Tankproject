using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TankInputHandler))]
[DefaultExecutionOrder(100)] // Present world-space turret rotation after the body's presentation.
public class PlayerLook : NetworkBehaviour
{
    public bool LocalCameraReady { get; private set; }
    [Header("References")]
    public Transform tankHead;

    [Tooltip("Cinemachine (CameraTarget)")]
    public Transform cameraTarget;

    private TankInputHandler inputHandler;

    [Header("Camera Settings")]
    public float cameraSensitivity = 0.5f;
    public float minPitch = -10f;
    public float maxPitch = 60f;

    [Header("Turret Settings")]
    public float headRotationSpeed = 150f;
    public bool invertTurretRotation = false;
    public float aimOffset = 0f;
    [SerializeField, Min(0.001f)] private float remoteYawSmoothTime = 0.05f;
    [SerializeField, Min(0.1f)] private float presentationTeleportDistance = 2f;
    private float displayedTurretYaw;
    private float displayedYawVelocity;
    private Vector3 previousPresentationPosition;
    private PlayerHealth health;
    private bool snapNextReceivedYaw;

    private float cameraPitch;
    private float cameraYaw;

    private float referenceYaw;

    private float localTurretYaw;
    private Quaternion headInitialRotation;

    public NetworkVariable<float> netTurretYaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner 
    );

    public override void OnNetworkSpawn()
    {
        LocalCameraReady = false;
        health = GetComponent<PlayerHealth>();
        netTurretYaw.OnValueChanged += OnTurretYawChanged;
        if (!IsOwner)
        {
            if (cameraTarget != null)
            {
                Camera cam = cameraTarget.GetComponentInChildren<Camera>();
                if (cam != null) cam.gameObject.SetActive(false);
            }

            if (tankHead != null)
            {
                headInitialRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * tankHead.rotation;
                referenceYaw = transform.eulerAngles.y;
            }

            localTurretYaw = referenceYaw;
            ResetPresentation();
            return;
        }

        StartCoroutine(SetupCameraRoutine());

        inputHandler = GetComponent<TankInputHandler>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTarget != null)
        {
            cameraYaw = cameraTarget.eulerAngles.y;
            cameraPitch = Mathf.DeltaAngle(0f, cameraTarget.eulerAngles.x);
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
        }

        if (tankHead != null)
        {
            headInitialRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * tankHead.rotation;
            referenceYaw = transform.eulerAngles.y;
            netTurretYaw.Value = referenceYaw;
            localTurretYaw = referenceYaw;
        }
        ResetPresentation();
    }

    private System.Collections.IEnumerator SetupCameraRoutine()
    {
        CinemachineCamera vCam = null;
        double deadline = Time.realtimeSinceStartupAsDouble + 30f;

        while (vCam == null && IsSpawned && Time.realtimeSinceStartupAsDouble < deadline)
        {
            vCam = FindAnyObjectByType<CinemachineCamera>();
            yield return null;
        }
        if (!IsSpawned) yield break;
        if (vCam == null || cameraTarget == null || Camera.main == null)
        {
            if (MatchConnectionCoordinator.Instance != null && MatchConnectionCoordinator.Instance.TransitionActive)
                MatchConnectionCoordinator.Instance.Fail("The local tank camera could not be prepared.");
            yield break;
        }

        if (cameraTarget != null)
        {
            vCam.Follow = cameraTarget;
            vCam.LookAt = cameraTarget;
            vCam.PreviousStateIsValid = false;
        }
        // A frame also advances in headless builds, unlike WaitForEndOfFrame.
        yield return null;
        LocalCameraReady = IsSpawned && vCam != null && vCam.Follow == cameraTarget && vCam.LookAt == cameraTarget;
    }

    private void Update()
    {
        if (IsOwner && inputHandler != null && MatchManager.CanControl(GetComponent<PlayerHealth>()))
        {
            Vector2 mouseDelta = inputHandler.LookInput;
            RotateCameraTarget(mouseDelta);
            CalculateTurretAim();
        }

        // Keep owner aiming immediate; only replica presentation is smoothed.
        if (IsOwner) ApplyTurretRotation();
    }

    private void LateUpdate()
    {
        if (!IsSpawned) return;
        if (!IsOwner)
        {
            if ((transform.position - previousPresentationPosition).sqrMagnitude >
                presentationTeleportDistance * presentationTeleportDistance || (health != null && health.isDead.Value))
                ResetPresentation();
            else
                displayedTurretYaw = Mathf.SmoothDampAngle(displayedTurretYaw, netTurretYaw.Value,
                    ref displayedYawVelocity, remoteYawSmoothTime, Mathf.Infinity, Time.deltaTime);
        }
        previousPresentationPosition = transform.position;
        // Body pitch/roll must not tilt the camera's world-space aim.
        if (IsOwner && cameraTarget != null)
            cameraTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        ApplyTurretRotation();
    }

    // Life changes and explicit relocation clear presentation history, never aim data.
    public void ResetPresentation()
    {
        displayedTurretYaw = IsOwner ? localTurretYaw : netTurretYaw.Value;
        displayedYawVelocity = 0f;
        previousPresentationPosition = transform.position;
        snapNextReceivedYaw = !IsOwner;
    }

    private void OnTurretYawChanged(float previous, float current)
    {
        // Life/teleport state can arrive before the owner's new yaw. Snap that first
        // sample as well, instead of interpolating out of the previous life.
        if (IsOwner || !snapNextReceivedYaw) return;
        displayedTurretYaw = current;
        displayedYawVelocity = 0f;
        snapNextReceivedYaw = false;
    }

    public override void OnNetworkDespawn()
    {
        LocalCameraReady = false;
        netTurretYaw.OnValueChanged -= OnTurretYawChanged;
        StopAllCoroutines();
    }

    public void ResetAfterReturn()
    {
        if (!IsOwner) return;
        cameraYaw = transform.eulerAngles.y;
        cameraPitch = 0f;
        if (cameraTarget != null) cameraTarget.rotation = Quaternion.Euler(0f, cameraYaw, 0f);
        localTurretYaw = cameraYaw;
        netTurretYaw.Value = localTurretYaw;
        ResetPresentation();
        LocalCameraReady = false;
        StartCoroutine(SetupCameraRoutine());
    }

    private void RotateCameraTarget(Vector2 mouseDelta)
    {
        if (cameraTarget == null) return;

        cameraYaw += mouseDelta.x * cameraSensitivity;
        cameraPitch -= mouseDelta.y * cameraSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    private void CalculateTurretAim()
    {
        if (tankHead == null) return;
        float targetYaw = cameraYaw + aimOffset;

        localTurretYaw = Mathf.MoveTowardsAngle(localTurretYaw, targetYaw, headRotationSpeed * Time.deltaTime);

        netTurretYaw.Value = localTurretYaw;
    }

    private void ApplyTurretRotation()
    {
        if (tankHead == null) return;

        float currentYaw = IsOwner ? localTurretYaw : displayedTurretYaw;

        float delta = Mathf.DeltaAngle(referenceYaw, currentYaw);
        if (invertTurretRotation) delta = -delta;

        tankHead.rotation = Quaternion.FromToRotation(Vector3.up, transform.up) *
            Quaternion.AngleAxis(delta, Vector3.up) * headInitialRotation;
    }
}
