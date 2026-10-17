using System;
using UnityEngine;

static partial class Program
{
    static void PresentationTests()
    {
        var shots = new ShotPresentation();
        var effect = new GameObject("Authored impact");
        var shake = new TankCameraShake();
        var pooled = new Bullet { Shooter=100, Sequence=1 };
        int fx = UnityEngine.Object.Instantiations;
        Assert(shots.Register(100,1,effect,2,shake), "First presentation registration starts flight");
        Assert(!shots.Register(100,1,effect,2,shake), "Duplicate spawn cannot start a second flight");
        shots.Attach(100,1,pooled);
        // Local collision, release and reuse, all before the server's result.
        shots.Detach(100,1,pooled);
        pooled.Sequence=2;
        shots.Register(100,2,effect,2,shake); shots.Attach(100,2,pooled);
        shots.Resolve(100,1,true,new Vector3(7,0,2),Vector3.up);
        Assert(pooled.Active && pooled.Releases==0,"Late result cannot release a pooled instance now serving another shot");
        Assert(UnityEngine.Object.Instantiations==fx+1 && shake.Hits==1,"Late authoritative contact emits exactly one impact/shake");
        Assert(UnityEngine.Object.LastInstantiationPosition.x==7,"Explosion uses server contact, not old local contact");
        shots.Resolve(100,1,true,Vector3.zero,Vector3.up);
        Assert(UnityEngine.Object.Instantiations==fx+1 && shake.Hits==1,"Duplicate result never repeats impact/shake");
        shots.Resolve(100,2,false,Vector3.zero,Vector3.zero);
        Assert(!pooled.Active && pooled.Releases==1 && shake.Hits==1,"Rejected/expired shot stops flight without hit or explosion");

        shots.Resolve(100,3,true,new Vector3(9,0,0),Vector3.up);
        Assert(!shots.Register(100,3,effect,2,shake),"Result preceding launch cannot resurrect projectile");
        Assert(UnityEngine.Object.Instantiations==fx+2,"Out-of-order impact presents once when its authored descriptor arrives");
        shots.Resolve(100,4,false,Vector3.zero,Vector3.zero);
        Assert(!shots.Register(100,4,effect,2,shake) && UnityEngine.Object.Instantiations==fx+2,"Out-of-order rejection never presents impact");

        pooled.Active=true; pooled.Sequence=6;
        shots.Register(100,5,effect,2,shake); shots.Attach(100,5,pooled);
        shots.Resolve(100,5,false,Vector3.zero,Vector3.zero);
        Assert(pooled.Active,"Generation check protects reuse even if a stale active reference survives");
        shots.Register(100,6,effect,2,shake); shots.Attach(100,6,pooled);
        pooled.Disabled=()=>shots.Detach(100,6,pooled);
        shots.Clear();
        Assert(!pooled.Active,"Match teardown releases remaining active flight, tolerating detach callbacks");
        Assert(shots.Register(100,1,effect,2,shake),"New match clears old terminal identities");

        var f=Fixture();
        Assert(f.match.TryResolveProjectile(100,1) && !f.match.TryResolveProjectile(100,1),"Server claims terminal processing once per shot");
        Assert(f.match.TryResolveProjectile(101,1),"Sequence is scoped by shooter identity");
        f.match.IsServer=false;
        Assert(!f.match.TryResolveProjectile(100,2),"Non-authority cannot claim hit processing");
        f.match.IsServer=true;
        int serializations=JsonUtility.SerializationCalls;
        Call(f.match,"PublishState");
        for(int i=0;i<120;i++) { f.rules.Resolve(20,120); Call(f.match,"PublishState"); }
        Assert(JsonUtility.SerializationCalls==serializations+1,"120 unchanged frames do not rebuild statistics JSON");
        f.health.TakeDamage(100,0,100); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+2,"Death/kills publish a changed snapshot");
        f.rules.BeginReturn(1); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+3,"Relocation state invalidates snapshot");
        Assert(f.rules.ConfirmReturn(1,1,24),"Expected life can return"); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+4,"Return publishes alive state");
        f.rules.Disconnect(1); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+5,"Disconnect publishes retained player record");
        f.rules.Disconnect(1); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+5,"Duplicate disconnect does not dirty statistics");
        f.rules.Disconnect(2); f.rules.Resolve(25,120); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+6,"Winner change publishes final snapshot");
        f=Fixture(); f.health.TakeDamage(100,0,100); Call(f.match,"PublishState");
        serializations=JsonUtility.SerializationCalls;
        f.rules.DestroyBase(1); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+1 && f.rules.Players[1].respawnAt==-1,
            "Base destruction publishes cancellation of a pending return");
        f=Fixture(); f.health.TakeDamage(100,0,100); Call(f.match,"PublishState");
        serializations=JsonUtility.SerializationCalls;
        f.rules.Resolve(310,120); Call(f.match,"PublishState");
        Assert(JsonUtility.SerializationCalls==serializations+1 && f.rules.Players[1].respawnAt==-1,
            "Overtime publishes cancelled returns even without another death");

        Vector3 forward=new(0,0,1), impulse=new(0,0,5);
        float? referenceOffset=null;
        foreach(int fps in new[]{30,60,120})
        {
            var limiter=new ConfirmedHitVisualPrediction();
            limiter.Begin(Vector3.zero,Vector3.zero,impulse,.6f,forward,1.5f);
            for(int i=0;i<fps/5;i++) limiter.Tick(Vector3.zero,0,1f/fps,.5f,.15f,forward);
            if(referenceOffset.HasValue)
                Assert(Math.Abs(limiter.Offset.z-referenceOffset.Value)<.00001f,"Soft bound uses elapsed travel, not render frame count");
            referenceOffset=limiter.Offset.z;
        }
        foreach(int fps in new[]{30,60,120})
        {
            var prediction=new ConfirmedHitVisualPrediction();
            prediction.Begin(Vector3.zero,Vector3.zero,impulse,.6f,forward,1.5f);
            float dt=1f/fps;
            for(int i=0;i<fps;i++)
            {
                Vector3 offset=prediction.Tick(Vector3.zero,12,dt,.5f,.15f,forward);
                if(float.IsNaN(offset.z) || offset.magnitude>.50001f) throw new Exception("Nonfinite/unbounded visual trajectory");
            }
            Assert(!prediction.IsActive && prediction.Offset.magnitude==0,$"Bounded prediction converges at {fps} FPS");
        }
        var p=new ConfirmedHitVisualPrediction();
        p.Begin(Vector3.zero,Vector3.zero,impulse,.2f,forward,1.5f);
        Vector3 root=Vector3.zero; float speed=5;
        for(int i=0;i<12;i++)
        {
            speed=Math.Max(0,speed-12*.02f); root+=forward*(speed*.02f);
            p.Tick(root,12,.02f,.5f,.15f,forward);
        }
        Assert(p.Offset.magnitude<.0001f,"Already displayed authoritative trajectory is subtracted, not pushed twice");
        p.Reset(); p.Begin(Vector3.zero,Vector3.zero,impulse,.6f,forward,1.5f);
        p.Tick(Vector3.zero,12,.02f,.5f,.15f,forward);
        Vector3 before=p.Offset;
        p.Begin(Vector3.zero,Vector3.zero,impulse,.6f,forward,1.5f);
        Assert(Vector3.Distance(before,p.Offset)==0,"Successive hit preserves current visual position");
        p.Constrain(Vector3.zero,Vector3.zero);
        Vector3 afterWall=p.Tick(Vector3.zero,12,.02f,.5f,.15f,forward);
        Assert(afterWall.magnitude<.0001f,"Blocked impulse does not accumulate behind a wall");
        p.Reset(); p.Begin(Vector3.zero,forward*1.5f,Vector3.zero,.2f,forward,1.5f);
        Vector3 turned=new(1,0,0);
        Assert(p.Tick(turned*.03f,12,.02f,.5f,.15f,turned).magnitude<.0001f,"Baseline follows received steering without rotating the world-space impulse");
        p.Reset();
        Assert(!p.IsActive && p.Offset.magnitude==0,"Death/relocation reset leaves no pending offset");
    }
}
