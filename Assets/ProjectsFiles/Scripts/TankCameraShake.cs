using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class TankCameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("قوة اهتزاز الكاميرا عند إطلاق النار (جرب 1 إلى 2 للتأكد، وبعدها قللها)")]
    [SerializeField] private float fireForce = 1f;

    [Tooltip("قوة اهتزاز الكاميرا عند التعرض لضربة أو انفجار")]
    [SerializeField] private float hitForce = 2f;

    [Header("Debug")]
    [Tooltip("تفعيل مفاتيح التجربة داخل الـ Editor فقط: Space للإطلاق و H للضربة")]
    [SerializeField] private bool enableTestKeys = true;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!enableTestKeys || Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame) ShakeOnFire();
        if (Keyboard.current.hKey.wasPressedThisFrame) ShakeOnHit();
    }
#endif

    [ContextMenu("Shake on Fire")]
    public void ShakeOnFire()
    {
        Debug.Log($"[TankCameraShake] Fire shake, force = {fireForce}");
        impulseSource.GenerateImpulse(fireForce);
    }

    [ContextMenu("Shake on Hit")]
    public void ShakeOnHit()
    {
        Debug.Log($"[TankCameraShake] Hit shake, force = {hitForce}");
        impulseSource.GenerateImpulse(hitForce);
    }
}