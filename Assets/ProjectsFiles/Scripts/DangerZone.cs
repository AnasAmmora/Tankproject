using UnityEngine;

/// <summary>Server-only lethal environmental contact, using the normal health/death pipeline.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class DangerZone : MonoBehaviour
{
    private void Awake()
    {
        var box = GetComponent<BoxCollider>();
        if (!box.isTrigger || box.size.x <= 0f || box.size.y <= 0f || box.size.z <= 0f)
        {
            Debug.LogError($"DangerZone on '{name}': BoxCollider must be a trigger with positive size on every axis.", this);
            enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other) => Touch(other);
    // Also handles players enabled/spawned inside the zone and a later life in the zone.
    private void OnTriggerStay(Collider other) => Touch(other);

    private void Touch(Collider other)
    {
        if (!isActiveAndEnabled || other == null) return;
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || !health.IsSpawned || !health.IsServer || health.isDead.Value) return;
        // ApplyDamage marks isDead synchronously before notifying the match. Further
        // child-collider callbacks are ignored, with no persistent per-player cache.
        health.TakeEnvironmentalDamage(health.currentHealth.Value);
    }
}
