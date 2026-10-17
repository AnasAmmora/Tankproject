using System.Collections.Generic;
using UnityEngine;

// Match-scoped identities outlive pooled projectile instances. Terminal records are
// retained until match teardown so a late/duplicate message cannot resurrect a shot.
public sealed class ShotPresentation
{
    private sealed class Shot
    {
        public bool Registered, Resolved, Presented, Impact;
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

    public bool Register(ulong shooter, uint sequence, GameObject effect, float effectLifetime, TankCameraShake shake)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Registered) return false;
        shot.Registered = true;
        shot.Effect = effect;
        shot.EffectLifetime = effectLifetime;
        shot.Shake = shake;
        Present(shot);
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

    public void Resolve(ulong shooter, uint sequence, bool impact, Vector3 point, Vector3 normal)
    {
        Shot shot = Get(shooter, sequence);
        if (shot.Resolved) return;
        shot.Resolved = true;
        shot.Impact = impact;
        shot.Point = point;
        shot.Normal = normal;
        Bullet active = shot.Active;
        shot.Active = null;
        if (active != null) active.ReleaseIfShot(shooter, sequence);
        Present(shot);
    }

    private static void Present(Shot shot)
    {
        if (!shot.Registered || !shot.Resolved || shot.Presented) return;
        shot.Presented = true;
        if (shot.Impact)
        {
            // Unity's destroyed-object check also covers a shooter that disconnected.
            if (shot.Shake != null) shot.Shake.ShakeOnHit();
            if (shot.Effect != null)
            {
                Quaternion rotation = shot.Normal.sqrMagnitude > 0f ? Quaternion.LookRotation(shot.Normal) : Quaternion.identity;
                GameObject effect = Object.Instantiate(shot.Effect, shot.Point, rotation);
                Object.Destroy(effect, shot.EffectLifetime);
            }
        }
        shot.Effect = null;
        shot.Shake = null;
    }

    public void Clear()
    {
        foreach (var pair in shots)
            if (pair.Value.Active != null) pair.Value.Active.ReleaseIfShot(pair.Key.Item1, pair.Key.Item2);
        shots.Clear();
    }
}
