using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode.Components;

static class Program
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
    static void Near(Vector3 a, Vector3 b, string label) => Check((a-b).magnitude < .0001f,label);
    static void Call(object target,string name,params object[] args) => target.GetType().GetMethod(name,Private).Invoke(target,args);
    static void Set(object target,string name,object value) => target.GetType().GetField(name,Private).SetValue(target,value);
    static T Get<T>(object target,string name) => (T)target.GetType().GetField(name,Private).GetValue(target);

    sealed class Tank
    {
        public GameObject Root=new();public Rigidbody Body;public PlayerMovement Motor;public TankRecoil Recoil;
        public TankShooter Shooter;public PlayerHealth Health;public TankGrounding Ground;public TankInputHandler Input;
        public TankNetworkTransform Motion;
        public Tank(bool owner=true,bool server=false)
        {
            Time.time=0;MatchManager.Allowed=true;MatchManager.Instance=new();MatchResults.LocalFinal=null;PoolManager.Fail=false;
            Unity.Netcode.NetworkManager.Singleton=new();
            Body=Root.Add<Rigidbody>();Motor=Root.Add<PlayerMovement>();Recoil=Root.Add<TankRecoil>();Shooter=Root.Add<TankShooter>();
            Health=Root.Add<PlayerHealth>();Ground=Root.Add<TankGrounding>();Input=Root.Add<TankInputHandler>();
            Motion=Root.Add<TankNetworkTransform>();Root.Add<TankTrackVisuals>();
            Motor.IsOwner=Shooter.IsOwner=owner;Motor.IsServer=Shooter.IsServer=server;
            Motor.IsSpawned=Shooter.IsSpawned=true;Shooter.IsClient=true;Shooter.NetworkObjectId=42;
            Motion.IsOwner=owner;Motion.IsServer=server;Motion.IsSpawned=true;
            Body.isKinematic=!owner;
            Set(Motor,"grounding",Ground);Set(Motor,"trackVisuals",Root.GetComponent<TankTrackVisuals>());
            Set(Recoil,"movement",Motor);Set(Shooter,"recoil",Recoil);
            Set(Shooter,"muzzlePoint",Root.transform);
            var prefab=new GameObject();prefab.Add<Bullet>();Set(Shooter,"bulletPrefab",prefab);
            Call(Motor,"Awake");Call(Recoil,"Awake");Call(Shooter,"Awake");
            Call(Motion,"Awake");
        }
        public Vector3 Pending=>Get<Vector3>(Motor,"pendingImpulse");
        public void Fire(Vector3 direction)
        {
            var camera=new GameObject().Add<Camera>();camera.TestRay=new Ray { direction=direction };
            Set(Shooter,"aimCamera",camera);
            Time.time+=1;Unity.Netcode.NetworkManager.Singleton.ServerTime.Time=Time.time;
            Call(Shooter,"TryFire");
        }
        public void Confirm(uint id,Vector3 direction,double firedAt=0,Vector3 impulse=default)=>
            Call(Shooter,"FireClientRpc",Vector3.zero,direction,id,false,firedAt,impulse);
        public void Step()=>Call(Motor,"FixedUpdate");
        public Transform AddModel()
        {
            var model=new GameObject().transform;model.parent=Root.transform;
            Set(Motor,"visualModel",model);Call(Motor,"Awake");return model;
        }
    }

    static void Main()
    {
        foreach(var pair in new[] {
            (new Vector3(0,0,1),new Vector3(0,0,-10)),
            (new Vector3(1,0,0),new Vector3(-10,0,0)),
            (new Vector3(-1,0,0),new Vector3(10,0,0)),
            (new Vector3(3,8,4),new Vector3(-6,0,-8)) })
        {
            var t=new Tank();t.Root.transform.rotation=Quaternion.Euler(0,130,0);
            t.Fire(pair.Item1);Near(t.Pending,pair.Item2,"opposite normalized horizontal launch, independent of hull");
            t.Step();Near(t.Body.linearVelocity,pair.Item2/10,"impulse scales by mass");
            Check(t.Body.Forces.Count==1 && t.Body.Forces[0].Mode==ForceMode.Impulse,"motor correction skipped on recoil step");
            Near(t.Pending,Vector3.zero,"impulse consumed once");
        }
        {
            var t=new Tank();t.Recoil.IsActive=false;t.Fire(new(0,0,1));Near(t.Pending,Vector3.zero,"IsActive off");
            t.Recoil.IsActive=true;t.Recoil.recoilForce=0;t.Fire(new(0,0,1));Near(t.Pending,Vector3.zero,"zero force");
            t.Recoil.recoilForce=-5;Call(t.Recoil,"OnValidate");Check(t.Recoil.recoilForce==0,"nonnegative Inspector validation");
            t.Recoil.recoilForce=10;t.Fire(new(0,1,0));Near(t.Pending,Vector3.zero,"vertical shot has no horizontal recoil");
            t.Recoil.enabled=false;t.Fire(new(0,0,1));Near(t.Pending,Vector3.zero,"disabled component");
        }
        {
            var t=new Tank();t.Fire(new(1,0,0));t.Confirm(1,new(-1,0,0));Near(t.Pending,new(-10,0,0),"confirmation does not repeat or redirect recoil");
            t.Fire(new(1,0,0));Near(t.Pending,new(-20,0,0),"distinct shots accumulate without waiting for physics");
            t.Motor.ClearLifeMotion();Near(t.Pending,Vector3.zero,"death/respawn clears queued recoil");
            t.Confirm(2,new(1,0,0));Near(t.Pending,Vector3.zero,"old confirmation still ignored after life reset");
            t.Fire(new(1,0,0));Check(t.Pending.x==-10,"next life firing works");
        }
        foreach(bool server in new[]{false,true})
        {
            var observer=new Tank(false,server);observer.Confirm(1,new(0,0,1));observer.Recoil.ApplyRecoil(new(0,0,1));
            Near(observer.Pending,Vector3.zero,"remote client/server cannot queue owner recoil");
            observer.Step();Check(observer.Body.ForceCalls==0,"remote body unchanged");
        }
        {
            var host=new Tank(true,true);int shots=0,empty=0;host.Shooter.ShotFired+=()=>shots++;host.Shooter.EmptyFireAttempted+=()=>empty++;
            Call(host.Shooter,"TryFire");Near(host.Pending,new(0,0,-10),"host accepted shot recoils once");
            Check(shots==1,"host projectile feedback once");Call(host.Shooter,"TryFire");
            Check(empty==1 && shots==1,"reload attempt only empty feedback");Near(host.Pending,new(0,0,-10),"reload does not recoil");
            host.Motor.ClearLifeMotion();host.Recoil.IsActive=false;Time.time=1;Unity.Netcode.NetworkManager.Singleton.ServerTime.Time=1;
            Call(host.Shooter,"TryFire");Check(shots==2,"IsActive off preserves firing");Near(host.Pending,Vector3.zero,"off preserves no recoil");
        }
        {
            var t=new Tank();Set(t.Shooter,"nextServerFireAt",10d);t.Fire(new(0,0,1));
            Near(t.Pending,new(0,0,-10),"predicted local recoil remains after server rejection, no snap or inverse force");
            Check(MatchManager.Instance.Rejections==1,"server rejection path executed");
            t.Step();Near(t.Pending,Vector3.zero,"predicted recoil consumed after rejection");
            t.Confirm(1,new(0,0,1));Check(t.Body.ForceCalls==1 && t.Pending==Vector3.zero,"late result/echo cannot repeat rejection recoil");
            MatchManager.Allowed=false;Call(t.Shooter,"TryFire");Near(t.Pending,Vector3.zero,"blocked control does not recoil");
        }
        {
            var t=new Tank(true,true);PoolManager.Fail=true;Call(t.Shooter,"TryFire");Near(t.Pending,Vector3.zero,"failed projectile launch has no recoil");
            Check(Get<uint>(t.Shooter,"lastServerShot")==0,"failed local launch never reaches server request");
        }
        {
            var t=new Tank(true,true);MatchManager.Instance.Projectiles.Accept=false;Call(t.Shooter,"TryFire");
            Near(t.Pending,Vector3.zero,"duplicate/terminal projectile registration does not recoil");
            Check(Get<uint>(t.Shooter,"lastServerShot")==0,"failed registration never requests firing");
            t=new Tank(true,true);Set(t.Shooter,"bulletPrefab",null);Call(t.Shooter,"TryFire");
            Near(t.Pending,Vector3.zero,"missing projectile definition does not recoil");
            t=new Tank();MatchResults.LocalFinal=new();t.Confirm(1,new(0,0,1));Near(t.Pending,Vector3.zero,"results suppress late confirmation");
        }
        {
            var t=new Tank();t.Body.linearVelocity=new(0,2,2);t.Input.Input=new(0,1);t.Ground.Normal=new(0,.8660254f,-.5f);
            Set(t.Motor,"pendingImpulse",new Vector3(0,3,0));t.Fire(new(1,2,0));t.Step();
            Near(t.Body.linearVelocity,new(-1,2.3f,2),"moving/slope context preserves velocity and adds hit + horizontal recoil");
            Check(t.Body.Forces.Count==1 && t.Ground.Samples==1,"shared accumulator, one physics impulse, grounding still sampled");
            t.Ground.Grounded=false;t.Motor.QueueOwnerImpulse(new(0,0,10));t.Step();
            Near(t.Body.linearVelocity,new(-1,2.3f,3),"airborne recoil does not require grounding");
        }
        {
            var t=new Tank();t.Body.isKinematic=true;t.Recoil.ApplyRecoil(new(0,0,1));Near(t.Pending,Vector3.zero,"kinematic authority guard");
            t.Body.isKinematic=false;t.Health.Dead=true;t.Recoil.ApplyRecoil(new(0,0,1));Near(t.Pending,Vector3.zero,"dead owner guard");
            t.Health.Dead=false;t.Motor.IsSpawned=false;t.Recoil.ApplyRecoil(new(0,0,1));Near(t.Pending,Vector3.zero,"despawned guard");
        }
        foreach(bool host in new[]{true,false})
        foreach(double confirmationDelay in new[]{0d,.1d,.5d})
        {
            var t=new Tank(true,host);double presentedAt=-1;
            Time.realtimeSinceStartupAsDouble=.010;
            t.Shooter.ShotFired+=()=>presentedAt=Time.realtimeSinceStartupAsDouble;
            bool queuedBeforeServerValidation=false;
            Unity.Netcode.NetworkManager.Singleton.ServerTime.OnRead=()=>queuedBeforeServerValidation=t.Pending!=Vector3.zero;
            t.Fire(new(0,0,1));
            Check(queuedBeforeServerValidation,"recoil queued before the outbound server flow can validate/confirm");
            Check(t.Body.ForceCalls==0,"only schedules, no out-of-step force");
            t.Root.transform.rotation=Quaternion.Euler(0,90,0);
            Time.realtimeSinceStartupAsDouble=.020;t.Step();
            Near(t.Body.Forces[0].Force,new(0,0,-10),"direction captured before later turret/hull change");
            Check(Math.Abs(t.Body.ForceTimes[0]-presentedAt-.010)<.000001,"scripted 10 ms shot-to-next-physics interval");
            Time.realtimeSinceStartupAsDouble=Math.Max(.020,presentedAt+confirmationDelay);
            t.Confirm(1,new(1,0,0));t.Confirm(1,new(1,0,0));
            Check(t.Body.ForceCalls==1 && t.Pending==Vector3.zero,"delayed duplicate confirmations never apply recoil");
            Console.WriteLine($"Timing boundary: {(host?"host owner":"client owner")} presentation={presentedAt*1000:F0}ms impulse={t.Body.ForceTimes[0]*1000:F0}ms echoDelay={confirmationDelay*1000:F0}ms");
        }
        {
            var viewer=new Tank(false);var model=viewer.AddModel();
            Unity.Netcode.NetworkManager.Singleton.ServerTime.Time=5;
            Check(viewer.Motion.TryGetRenderTime(out double displayed) && displayed<5,"client presentation clock precedes host shot");
            viewer.Confirm(1,new(0,0,1),5,new(0,0,-50));
            var prediction=Get<ConfirmedHitVisualPrediction>(viewer.Motor,"visualPrediction");
            Check(prediction.IsActive,"server-accepted shot begins anticipation before buffered movement");
            Vector3 velocity=Get<Vector3>(prediction,"velocity");
            viewer.Confirm(1,new(1,0,0),5,new(-50,0,0));
            Near(Get<Vector3>(prediction,"velocity"),velocity,"duplicate pooled flight/shot cannot add another visual impulse");
            Time.deltaTime=.02f;Call(viewer.Motor,"LateUpdate");
            Check(model.localPosition.z<0,"recoil starts visually with the remote shot");
            Near(viewer.Root.transform.position,Vector3.zero,"anticipation never moves network root");
            Check(viewer.Body.ForceCalls==0,"anticipation never applies a replica force");
            viewer.Motor.ClearLifeMotion();Near(model.localPosition,Vector3.zero,"life reset clears anticipated model offset");
            Check(!prediction.IsActive,"life reset clears pending trajectory");
        }
        foreach(bool host in new[]{false,true})
        {
            var viewer=new Tank(false,host);viewer.AddModel();Unity.Netcode.NetworkManager.Singleton.ServerTime.Time=5;
            viewer.Motor.PresentFiringRecoil(new(0,0,-50),4.5);
            Check(!Get<ConfirmedHitVisualPrediction>(viewer.Motor,"visualPrediction").IsActive,"no anticipation if timeline already covers the shot");
        }
        {
            var viewer=new Tank(false);viewer.AddModel();Unity.Netcode.NetworkManager.Singleton.ServerTime.Time=5;
            Call(viewer.Motor,"ConfirmHitRpc",new Vector3(10,0,0),99ul,7u,5d,.1f,false);
            viewer.Confirm(1,new(0,0,1),5,new(0,0,-50));
            var prediction=Get<ConfirmedHitVisualPrediction>(viewer.Motor,"visualPrediction");
            Near(Get<Vector3>(prediction,"velocity"),new(1,0,-5),"hit and firing visuals share one trajectory and add once");
            Check(viewer.Body.ForceCalls==0,"combined visual impulses still never simulate remote physics");
            viewer.Motor.ClearLifeMotion();
            viewer.Motor.PresentFiringRecoil(Vector3.zero,5);
            Check(!prediction.IsActive,"source disabled/zero recoil has no visual anticipation");
            viewer.Motor.PresentFiringRecoil(new(0,0,-10),double.NaN);
            Check(!prediction.IsActive,"invalid timestamp cannot poison visual state");
            viewer.Motion.PositionInterpolationType=NetworkTransform.InterpolationTypes.Lerp;
            viewer.Motor.PresentFiringRecoil(new(0,0,-10),5);
            Check(!prediction.IsActive,"unsupported timeline profile is never guessed");
        }
        Console.WriteLine($"PASS {checks} recoil assertions (simulated boundary; no Unity physics/NGO transport).");
    }
}
