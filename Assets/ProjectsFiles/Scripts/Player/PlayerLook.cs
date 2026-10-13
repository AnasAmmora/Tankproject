using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TankInputHandler))]
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
        if (!IsOwner)
        {
            if (cameraTarget != null)
            {
                Camera cam = cameraTarget.GetComponentInChildren<Camera>();
                if (cam != null) cam.gameObject.SetActive(false);
            }

            if (tankHead != null)
            {
                headInitialRotation = tankHead.rotation;
                referenceYaw = transform.eulerAngles.y;
            }

            localTurretYaw = referenceYaw;
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
            headInitialRotation = tankHead.rotation;
            referenceYaw = transform.eulerAngles.y;
            netTurretYaw.Value = referenceYaw;
            localTurretYaw = referenceYaw;
        }
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

        ApplyTurretRotation();
    }

    public override void OnNetworkDespawn()
    {
        LocalCameraReady = false;
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

        float currentYaw = IsOwner ? localTurretYaw : netTurretYaw.Value;

        float delta = Mathf.DeltaAngle(referenceYaw, currentYaw);
        if (invertTurretRotation) delta = -delta;

        tankHead.rotation = Quaternion.AngleAxis(delta, Vector3.up) * headInitialRotation;
    }
}
