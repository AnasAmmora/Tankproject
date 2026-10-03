using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode; 

public class TankInputHandler : NetworkBehaviour 
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }


    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return; 

        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return; 

        if (moveAction != null) MoveInput = moveAction.action.ReadValue<Vector2>();
        if (lookAction != null) LookInput = lookAction.action.ReadValue<Vector2>();
    }
}