using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class TankCameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float fireForce = 1f;

    [SerializeField] private float hitForce = 2f;


    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

//#if UNITY_EDITOR
//    private void Update()
//    {
//        if (Keyboard.current == null) return;

//        if (Keyboard.current.spaceKey.wasPressedThisFrame) ShakeOnFire();
//        if (Keyboard.current.hKey.wasPressedThisFrame) ShakeOnHit();
//    }
//#endif

    public void ShakeOnFire()
    {
        Debug.Log($"[TankCameraShake] Fire shake, force = {fireForce}");
        impulseSource.GenerateImpulse(fireForce);
    }

    public void ShakeOnHit()
    {
        Debug.Log($"[TankCameraShake] Hit shake, force = {hitForce}");
        impulseSource.GenerateImpulse(hitForce);
    }
}