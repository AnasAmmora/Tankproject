using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TankInputHandler))]
public class PlayerLook : NetworkBehaviour
{
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
    private Quaternion headInitialRotation;

    public NetworkVariable<float> netTurretYaw = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner 
    );

    public override void OnNetworkSpawn()
    {
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
            return;
        }

        CinemachineCamera vCam = FindAnyObjectByType<CinemachineCamera>();

        if (vCam != null && cameraTarget != null)
        {
            vCam.Follow = cameraTarget;
            vCam.LookAt = cameraTarget;
        }

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
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            Vector2 mouseDelta = inputHandler.LookInput;
            RotateCameraTarget(mouseDelta);
            CalculateTurretAim();
        }

        ApplyTurretRotation();
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

        netTurretYaw.Value = Mathf.MoveTowardsAngle(netTurretYaw.Value, targetYaw, headRotationSpeed * Time.deltaTime);
    }

    private void ApplyTurretRotation()
    {
        if (tankHead == null) return;

        float delta = Mathf.DeltaAngle(referenceYaw, netTurretYaw.Value);
        if (invertTurretRotation) delta = -delta;

        tankHead.rotation = Quaternion.AngleAxis(delta, Vector3.up) * headInitialRotation;
    }
}