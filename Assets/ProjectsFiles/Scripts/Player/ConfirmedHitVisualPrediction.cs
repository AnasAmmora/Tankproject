using UnityEngine;

// A presentation-only trajectory. It never writes the network root or a Rigidbody.
public sealed class ConfirmedHitVisualPrediction
{
    public bool IsActive { get; private set; }
    public Vector3 Offset { get; private set; }
    private Vector3 position;
    private Vector3 velocity;
    private Vector3 driveVelocity;
    private Vector3 previousForward;
    private float leadRemaining;

    public void Begin(Vector3 rootPosition, Vector3 rootVelocity, Vector3 deltaVelocity, float lead,
        Vector3 forward, float maxDriveSpeed)
    {
        // Preserve the displayed position when another confirmed hit arrives.
        position = rootPosition + Offset;
        if (!IsActive)
        {
            previousForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
            // A previous hit in the observed velocity must not become permanent drive.
            driveVelocity = previousForward * Mathf.Clamp(Vector3.Dot(rootVelocity, previousForward), -maxDriveSpeed, maxDriveSpeed);
            velocity = driveVelocity;
        }
        velocity += Vector3.ProjectOnPlane(deltaVelocity, Vector3.up);
        leadRemaining = Mathf.Max(leadRemaining, lead);
        IsActive = true;
    }

    public Vector3 Tick(Vector3 rootPosition, float acceleration, float deltaTime,
        float maxOffset, float reconcileTime, Vector3 forward)
    {
        if (!IsActive || deltaTime <= 0f) return Offset;
        Vector3 nextForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
        // Follow received steering, without treating arbitrary root translation as
        // an acknowledgement of the hit. Keep the impulse in world space.
        Vector3 nextDrive = nextForward * Vector3.Dot(driveVelocity, previousForward);
        velocity += nextDrive - driveVelocity;
        driveVelocity = nextDrive;
        previousForward = nextForward;
        velocity = Vector3.MoveTowards(velocity, driveVelocity, acceleration * deltaTime);
        leadRemaining -= deltaTime;

        // Subtract the entire received root movement. Adding the same hit displacement
        // on top of a moving replica would produce the old double-push artifact.
        Vector3 error = position - rootPosition;
        error.y = 0f;
        float previousDistance = error.magnitude;
        error += velocity * deltaTime;
        float weight = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(1f + leadRemaining / Mathf.Max(0.01f, reconcileTime)));
        // Ease into the existing safety bound instead of clipping to a flat plateau.
        // Feed the bounded trajectory back so hidden displacement cannot build up
        // behind the cap and reappear when a later packet or hit arrives.
        float distance = error.magnitude;
        float radius = Mathf.Max(0f, maxOffset);
        float softStart = radius * 0.5f;
        if (distance > softStart && distance > previousDistance)
        {
            float range = Mathf.Max(0.0001f, radius - softStart);
            // Apply the exponential to this frame's outward travel, not repeatedly
            // to the stored offset. Constant velocity then behaves alike at 30/120 FPS.
            float start = Mathf.Clamp(previousDistance, softStart, radius);
            float travel = distance - Mathf.Max(previousDistance, softStart);
            float bounded = radius <= 0f ? 0f : radius - (radius - start) * Mathf.Exp(-travel / range);
            error *= bounded / distance;
        }
        else if (distance > radius) error *= radius / distance;
        position = rootPosition + error;
        Offset = error * weight;
        if (weight <= 0f) Reset();
        return Offset;
    }

    public void Reset()
    {
        IsActive = false;
        Offset = Vector3.zero;
        leadRemaining = 0f;
        position = velocity = driveVelocity = previousForward = Vector3.zero;
    }

    public void Constrain(Vector3 rootPosition, Vector3 permittedOffset)
    {
        Vector3 blockedDirection = (Offset - permittedOffset).normalized;
        float intoWall = Vector3.Dot(velocity, blockedDirection);
        if (intoWall > 0f) velocity -= blockedDirection * intoWall;
        Offset = permittedOffset;
        position = rootPosition + permittedOffset;
    }
}
