using System;
using UnityEngine;

/// <summary>Reusable weapon damage definition. Only authoritative damage callers use Calculate.</summary>
[CreateAssetMenu(menuName = "Tank Project/Damage Falloff")]
public sealed class DamageFalloff : ScriptableObject
{
    [SerializeField, Min(0)] private int baseDamage = 25;
    [SerializeField, Min(0f)] private float fullDamageDistance = 20f;
    [SerializeField, Min(0.01f)] private float maximumFalloffDistance = 80f;
    [SerializeField, Range(0f, 1f)] private float minimumDamageMultiplier = 0.4f;

    // Limits also protect against malformed assets and non-finite Inspector values.
    private static float Finite(float value, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0, baseDamage);
        fullDamageDistance = Mathf.Clamp(Finite(fullDamageDistance, 20f), 0f, 100000f);
        maximumFalloffDistance = Mathf.Clamp(Finite(maximumFalloffDistance, 80f), fullDamageDistance + 0.01f, 1000000f);
        minimumDamageMultiplier = Mathf.Clamp01(Finite(minimumDamageMultiplier, 0.4f));
    }

    public int Calculate(Vector3 firingPosition, Vector3 impactPosition) =>
        Calculate(Vector3.Distance(firingPosition, impactPosition));

    /// <summary>
    /// Smoothstep falloff over straight-line launch-to-impact distance (world units).
    /// Round once to the nearest integer; exact halves round up. Zero is allowed.
    /// The health recipient subsequently caps this to its remaining health.
    /// </summary>
    public int Calculate(float distance)
    {
        if (float.IsNaN(distance)) return 0;
        int damage = Mathf.Max(0, baseDamage);
        float start = Mathf.Clamp(Finite(fullDamageDistance, 20f), 0f, 100000f);
        float end = Mathf.Clamp(Finite(maximumFalloffDistance, 80f), start + 0.01f, 1000000f);
        double minimum = Mathf.Clamp01(Finite(minimumDamageMultiplier, 0.4f));
        double t = Mathf.Clamp01((distance - start) / (end - start));
        double smooth = t * t * (3d - 2d * t);
        double result = damage * (1d + (minimum - 1d) * smooth);
        return (int)Math.Min(damage, Math.Max(0d, Math.Floor(result + 0.5d)));
    }
}
