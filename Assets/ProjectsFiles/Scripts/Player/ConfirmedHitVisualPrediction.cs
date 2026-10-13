using UnityEngine;

// A presentation-only trajectory. It never writes the network root or a Rigidbody.
public sealed class ConfirmedHitVisualPrediction
{
    public bool IsActive { get; private set; }
    public Vector3 Offset { get; private set; }
    private Vector3 position;
    private Vector3 velocity;
    private Vector3 driveVelocity;
    private float leadRemaining;

    public void Begin(Vector3 rootPosition, Vector3 rootVelocity, Vector3 deltaVelocity, float lead)
    {
        // Preserve the displayed position when another confirmed hit arrives.
        position = rootPosition + Offset;
        if (!IsActive)
        {
            driveVelocity = Vector3.ProjectOnPlane(rootVelocity, Vector3.up);
            velocity = driveVelocity;
        }
        velocity += Vector3.ProjectOnPlane(deltaVelocity, Vector3.up);
        leadRemaining = Mathf.Max(leadRemaining, lead);
        IsActive = true;
    }

    public Vector3 Tick(Vector3 rootPosition, float acceleration, float deltaTime,
        float maxOffset, float reconcileTime)
    {
        if (!IsActive || deltaTime <= 0f) return Offset;
        velocity = Vector3.MoveTowards(velocity, driveVelocity, acceleration * deltaTime);
        position += velocity * deltaTime;
        leadRemaining -= deltaTime;

        // Subtract the entire received root movement. Adding the same hit displacement
        // on top of a moving replica would produce the old double-push artifact.
        Vector3 error = position - rootPosition;
        error.y = 0f;
        float weight = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(1f + leadRemaining / Mathf.Max(0.01f, reconcileTime)));
        Offset = Vector3.ClampMagnitude(error, maxOffset) * weight;
        if (weight <= 0f) Reset();
        return Offset;
    }

    public void Reset()
    {
        IsActive = false;
        Offset = Vector3.zero;
        leadRemaining = 0f;
        position = velocity = driveVelocity = Vector3.zero;
    }

    public void Constrain(Vector3 rootPosition, Vector3 permittedOffset)
    {
        Offset = permittedOffset;
        position = rootPosition + permittedOffset;
    }
}
