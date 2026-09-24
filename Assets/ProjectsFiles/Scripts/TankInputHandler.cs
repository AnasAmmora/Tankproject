using UnityEngine;
using UnityEngine.InputSystem;

public class TankInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction; 

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
    }

    private void Update()
    {
        if (moveAction != null) MoveInput = moveAction.action.ReadValue<Vector2>();
        if (lookAction != null) LookInput = lookAction.action.ReadValue<Vector2>(); 
    }
}