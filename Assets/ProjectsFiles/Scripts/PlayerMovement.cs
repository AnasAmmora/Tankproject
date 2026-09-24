using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;      
    public float rotationSpeed = 120f; 

    private Rigidbody rb;
    private TankInputHandler inputHandler;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputHandler = GetComponent<TankInputHandler>();
    }

    private void FixedUpdate()
    {
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
}