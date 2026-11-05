using System.Collections.Generic;
using UnityEngine;

// Match-scoped identities outlive pooled projectile instances. Terminal records are
// retained until match teardown so a late/duplicate message cannot resurrect a shot.
public sealed class ShotPresentation
{
    private sealed class Shot
    {
        public bool Registered, Resolved, Presented, Impact, FeedbackPresented, Diagnostics;
        public ulong Shooter;
        public uint Sequence;
        public Bullet Active;
        public GameObject Effect;
        public TankCameraShake Shake;
        public float EffectLifetime;
        public Vector3 Point, Normal;
    }

    private readonly Dictionary<(ulong, uint), Shot> shots = new Dictionary<(ulong, uint), Shot>();

    private Shot Get(ulong shooter, uint sequence)
    {
        var key = (shooter, sequence);
        if (!shots.TryGetValue(key, out Shot shot)) shots.Add(key, shot = new Shot());
        return shot;
    }

    public bool Register(ulong shooter, uint sequence, GameObject effect, float effectLifetime, TankCameraShake shake,
        bool diagnostics = false)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Registered) return false;
        shot.Registered = true;
        shot.Shooter = shooter;
        shot.Sequence = sequence;
        shot.Diagnostics = diagnostics;
        shot.Effect = effect;
        shot.EffectLifetime = effectLifetime;
        shot.Shake = shake;
        ConfirmFeedback(shot);
        if (shot.Resolved && shot.Impact) PresentImpact(shot, shot.Point, shot.Normal);
        return !shot.Resolved;
    }

    public void Attach(ulong shooter, uint sequence, Bullet bullet)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Resolved) bullet.ReleaseIfShot(shooter, sequence);
        else shot.Active = bullet;
    }

    public void Detach(ulong shooter, uint sequence, Bullet bullet)
    {
        if (shots.TryGetValue((shooter, sequence), out Shot shot) && shot.Active == bullet) shot.Active = null;
    }

    // Cosmetic only: no health, damage numbers, kill feedback or gameplay force.
    // A predicted burst is consumed permanently, even if the server later disagrees.
    // Moving/replaying a completed explosion would duplicate its particles/audio.
    public void PredictImpact(ulong shooter, uint sequence, Vector3 point, Vector3 normal)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Resolved && !shot.Impact) return;
        PresentImpact(shot, point, normal);
    }

    public void Resolve(ulong shooter, uint sequence, bool impact, Vector3 point, Vector3 normal,
        float flightAge = -1f)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Resolved) return;
        shot.Resolved = true;
        shot.Impact = impact;
        shot.Point = point;
        shot.Normal = normal;
        ConfirmFeedback(shot);
        Bullet active = shot.Active;
        // Observers can receive confirmation before their independently launched
        // flight arrives. Keep that flight until contact or the server's flight age.
        if (impact && active != null && !shot.Presented &&
            active.ConfirmImpactIfShot(shooter, sequence, point, normal, flightAge)) return;
        shot.Active = null;
        if (active != null) active.ReleaseIfShot(shooter, sequence);
        if (impact) PresentImpact(shot, point, normal);
        else shot.Effect = null;
        shot.Shake = null;
    }

    private static void ConfirmFeedback(Shot shot)
    {
        if (!shot.Registered || !shot.Resolved || shot.FeedbackPresented) return;
        shot.FeedbackPresented = true;
        if (shot.Impact && shot.Shake != null) shot.Shake.ShakeOnHit();
        shot.Shake = null;
    }

    private static void PresentImpact(Shot shot, Vector3 point, Vector3 normal)
    {
        if (!shot.Registered || shot.Presented) return;
        shot.Presented = true;
        if (shot.Effect != null)
        {
            Quaternion rotation = normal.sqrMagnitude > 0f ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GameObject effect = Object.Instantiate(shot.Effect, point, rotation);
            Object.Destroy(effect, shot.EffectLifetime);
        }
        TankShooter.TraceShot(shot.Diagnostics, shot.Shooter, shot.Sequence, ulong.MaxValue, "impact-effect-played");
        shot.Effect = null;
    }

    public void Clear()
    {
        foreach (var pair in shots)
            if (pair.Value.Active != null) pair.Value.Active.ReleaseIfShot(pair.Key.Item1, pair.Key.Item2);
        shots.Clear();
    }
}
