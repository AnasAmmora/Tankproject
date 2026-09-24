using UnityEngine;

[RequireComponent(typeof(TankInputHandler))]
public class PlayerLook : MonoBehaviour
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

    private float turretYaw;                
    private float referenceYaw;             
    private Quaternion headInitialRotation;  

    private void Awake()
    {
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
            turretYaw = referenceYaw;
        }
    }

    private void Update()
    {
        Vector2 mouseDelta = inputHandler.LookInput;

        RotateCameraTarget(mouseDelta);
        AimTurretWithCamera();
    }

    private void RotateCameraTarget(Vector2 mouseDelta)
    {
        if (cameraTarget == null) return;

        cameraYaw += mouseDelta.x * cameraSensitivity;
        cameraPitch -= mouseDelta.y * cameraSensitivity;

        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
    }

    private void AimTurretWithCamera()
    {
        if (tankHead == null) return;

        float targetYaw = cameraYaw + aimOffset;

        turretYaw = Mathf.MoveTowardsAngle(turretYaw, targetYaw, headRotationSpeed * Time.deltaTime);

        float delta = Mathf.DeltaAngle(referenceYaw, turretYaw);
        if (invertTurretRotation) delta = -delta;

        tankHead.rotation = Quaternion.AngleAxis(delta, Vector3.up) * headInitialRotation;
    }
}