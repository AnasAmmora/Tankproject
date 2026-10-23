using UnityEngine;
using UnityEngine.SceneManagement;


[DisallowMultipleComponent]
public sealed class TankGrounding : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private Vector3[] samples;
    [SerializeField] private LayerMask groundLayers = 257; 
    [SerializeField, Range(0f, 70f)] private float maxClimbAngle = 40f;
    [SerializeField, Min(0.001f)] private float probeLift = 0.12f;
    [SerializeField, Min(0f)] private float groundDistance = 0.045f;
    [SerializeField, Min(1)] private int minimumSupportSamples = 2;
    [SerializeField, Min(0f)] private float maxSeparatingSpeed = 0.5f;
    [SerializeField, Min(1f)] private float alignmentSpeed = 180f;

    [SerializeField] private bool alignToSlope = true;
    [SerializeField] private bool logDiagnostics;

    private RaycastHit[] hits = new RaycastHit[16];
    private Vector3 smoothedUp = Vector3.up;
    private bool hadSupport;
    private float nextLogTime;
    private string lastGroundName = "none";

    public bool Grounded { get; private set; }
    public int SupportCount { get; private set; }
    public Vector3 Normal { get; private set; } = Vector3.up;
    public float MinimumGroundDot => Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);

    private void Awake()
    {
        if (body == null || samples == null || samples.Length < minimumSupportSamples)
            Debug.LogError("TankGrounding: body and sufficient underside samples must be assigned.", this);
        ResetSupport();
    }

    public void ResetSupport()
    {
        Grounded = hadSupport = false;
        SupportCount = 0;
        Normal = Vector3.up;
        smoothedUp = body != null ? body.rotation * Vector3.up : transform.up;
    }

    public void Sample()
    {
        Grounded = false;
        SupportCount = 0;
        if (body == null || samples == null) return;
        Vector3 sum = Vector3.zero;
        int count = 0;
        var scene = gameObject.scene.GetPhysicsScene();
        foreach (var point in samples)
        {
            Vector3 origin = body.position + body.rotation * Vector3.Scale(point, transform.lossyScale) + Vector3.up * probeLift;
            int hitCount;
            // Requery a full buffer; a truncated unsorted result must never select
            // a farther floor through a nearer obstacle. Allocation only on growth.
            while (true)
            {
                hitCount = scene.Raycast(origin, Vector3.down, hits, probeLift + groundDistance,
                    groundLayers, QueryTriggerInteraction.Ignore);
                if (hitCount < hits.Length) break;
                System.Array.Resize(ref hits, hits.Length * 2);
            }
            float nearest = float.PositiveInfinity;
            RaycastHit selected = default;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hits[i];
                if (hit.rigidbody == body || hit.collider.transform.IsChildOf(transform) ||
                    Physics.GetIgnoreLayerCollision(gameObject.layer, hit.collider.gameObject.layer)) continue;
                // Other dynamic bodies are not terrain or a stable source of normals.
                if (hit.rigidbody != null && !hit.rigidbody.isKinematic) continue;
                if (hit.distance < nearest) { nearest = hit.distance; selected = hit; }
            }
            if (nearest == float.PositiveInfinity || Vector3.Dot(selected.normal, Vector3.up) < MinimumGroundDot) continue;
            sum += selected.normal;
            count++;
            if (logDiagnostics)
                lastGroundName = selected.collider.name + " [" + selected.collider.GetType().Name +
                                 ", layer " + LayerMask.LayerToName(selected.collider.gameObject.layer) + "]";
        }
        SupportCount = count;
        if (count < minimumSupportSamples) { hadSupport = false; LogState(); return; }
        Normal = sum.normalized;
        // An outward impulse is not ground support. Never pull/snap the body back.
        Grounded = Vector3.Dot(body.linearVelocity, Normal) <= maxSeparatingSpeed;
        if (!Grounded) hadSupport = false;
        LogState();
    }

    public Quaternion GetSteeredRotation(float turnDegrees, float dt)
    {
        if (!Grounded) return body.rotation;

        if (!alignToSlope)
            return Quaternion.AngleAxis(turnDegrees, Vector3.up) * body.rotation;

        if (!hadSupport) smoothedUp = body.rotation * Vector3.up;
        hadSupport = true;
        smoothedUp = Vector3.RotateTowards(smoothedUp, Normal, alignmentSpeed * Mathf.Deg2Rad * dt, 0f).normalized;
        Vector3 heading = Quaternion.AngleAxis(turnDegrees, Vector3.up) * (body.rotation * Vector3.forward);
        heading = Vector3.ProjectOnPlane(heading, smoothedUp).normalized;
        return heading.sqrMagnitude > 0.001f ? Quaternion.LookRotation(heading, smoothedUp) : body.rotation;
    }

    private void LogState()
    {
        if (!logDiagnostics || Time.time < nextLogTime) return;
        nextLogTime = Time.time + 0.5f;
        Debug.Log($"[TankGrounding] grounded={Grounded} support={SupportCount}/{samples.Length} " +
                  $"y={body.position.y:F3} vy={body.linearVelocity.y:F2} ground={lastGroundName}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (samples == null) return;
        Gizmos.color = Grounded ? Color.green : Color.yellow;
        foreach (Vector3 point in samples)
        {
            Vector3 origin = transform.TransformPoint(point);
            Gizmos.DrawLine(origin + Vector3.up * probeLift, origin - Vector3.up * groundDistance);
            Gizmos.DrawWireSphere(origin, 0.008f);
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Normal * 0.3f);
    }
}