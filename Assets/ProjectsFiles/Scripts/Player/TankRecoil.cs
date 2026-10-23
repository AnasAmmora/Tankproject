using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class TankRecoil : MonoBehaviour
{
    [InspectorName("IsActive")] public bool IsActive = true;

    [Min(0f)] public float recoilForce = 10f;
    [SerializeField] private PlayerMovement movement;

    private void OnValidate() => recoilForce = Mathf.Max(0f, recoilForce);

    public Vector3 ApplyRecoil(Vector3 shotDirection, uint shotId = 0)
    {
        if (!isActiveAndEnabled || !IsActive || recoilForce <= 0f ||
            movement == null || movement.gameObject != gameObject) return Vector3.zero;

        Vector3 horizontal = Vector3.ProjectOnPlane(shotDirection, Vector3.up);
        if (horizontal.sqrMagnitude < 0.000001f) return Vector3.zero;
        Vector3 impulse = -horizontal.normalized * recoilForce;
        return movement.QueueOwnerImpulse(impulse, shotId) ? impulse : Vector3.zero;
    }
}
