using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankInputHandler))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 120f;
    private Rigidbody rb;
    private TankInputHandler inputHandler;

    [Header("Visual Fixes (Lag & Float)")]
    [Tooltip("اسحب مجسم الدبابة الداخلي (TankModel) هنا")]
    public Transform tankModel;
    public float joltDistance = 0.5f;
    public float joltSpeed = 15f;

    private Vector3 originalModelLocalPos;
    private bool isJolting = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputHandler = GetComponent<TankInputHandler>();
    }

    public override void OnNetworkSpawn()
    {
        if (tankModel != null)
        {
            originalModelLocalPos = tankModel.localPosition;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        MoveTank();
        RotateTank();
    }

    private void MoveTank()
    {
        float moveAmount = inputHandler.MoveInput.y * moveSpeed * Time.fixedDeltaTime;
        Vector3 movement = transform.forward * moveAmount;
        rb.MovePosition(rb.position + movement);
    }

    private void RotateTank()
    {
        float turnAmount = inputHandler.MoveInput.x * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
    }

    // 🟢 اهتزاز بصري فوري لمطلق النار دون التأثير على الارتفاع
    public void TriggerVisualJolt(Vector3 direction)
    {
        if (!IsOwner && tankModel != null && gameObject.activeInHierarchy && !isJolting)
        {
            StartCoroutine(JoltRoutine(direction));
        }
    }

    private System.Collections.IEnumerator JoltRoutine(Vector3 dir)
    {
        isJolting = true;

        Vector3 localDir = transform.InverseTransformDirection(dir).normalized;
        Vector3 targetPos = originalModelLocalPos + (localDir * joltDistance);

        // اندفاع سريع للخلف
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * joltSpeed;
            tankModel.localPosition = Vector3.Lerp(originalModelLocalPos, targetPos, t);
            yield return null;
        }

        // عودة سلسة للمركز
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * (joltSpeed / 2f);
            tankModel.localPosition = Vector3.Lerp(targetPos, originalModelLocalPos, t);
            yield return null;
        }

        tankModel.localPosition = originalModelLocalPos;
        isJolting = false;
    }
}