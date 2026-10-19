using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode.Components;
using Q=System.Numerics.Quaternion;

static class Program
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Assert(bool ok,string why) { checks++;if(!ok)throw new Exception(why); }
    static void Near(float a,float b,string why)=>Assert(Math.Abs(a-b)<.0001f,why+$" ({a} vs {b})");
    static void Call(object obj,string name)=>obj.GetType().GetMethod(name,Private).Invoke(obj,null);
    static void Set(object obj,string name,object value)=>obj.GetType().GetField(name,Private).SetValue(obj,value);
    static T Get<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Private).GetValue(obj);

    sealed class Tank
    {
        public GameObject Root=new(); public TankTrackVisuals Visual;public PlayerMovement Movement;
        public Transform[] Wheels=new Transform[6];public Renderer Track;public Material Source,Instance;
        public TankInputHandler Input;public PlayerHealth Health;
        public Tank(bool owner=true,Material shared=null)
        {
            Source=shared??new Material();
            Visual=Root.Add<TankTrackVisuals>();Movement=Root.Add<PlayerMovement>();
            Root.Add<Rigidbody>();Root.Add<NetworkTransform>();Health=Root.Add<PlayerHealth>();Input=Root.Add<TankInputHandler>();
            for(int i=0;i<6;i++)
            {
                Wheels[i]=new GameObject().transform;Wheels[i].parent=Root.transform;
                Wheels[i].localRotation=Quaternion.Euler(-90,13+i,7);
            }
            Track=new GameObject().Add<Renderer>();Track.transform.parent=Root.transform;
            Track.sharedMaterials=new[]{new Material(),Source};
            Set(Visual,"wheels",Wheels);Set(Visual,"trackRenderer",Track);Set(Visual,"materialSlot",1);
            Set(Visual,"wheelDegreesPerSecond",90f);Set(Visual,"trackCyclesPerSecond",.25f);
            Set(Movement,"trackVisuals",Visual);Movement.moveSpeed=5;Movement.rotationSpeed=60;
            Movement.IsOwner=owner;Movement.IsSpawned=true;Movement.IsServer=owner;
            Call(Visual,"Awake");Call(Movement,"Awake");Movement.OnNetworkSpawn();
            Instance=Track.sharedMaterials[1];Frame(.02f);
        }
        public void Frame(float dt) { Time.deltaTime=dt;Call(Movement,"LateUpdate");Call(Visual,"LateUpdate"); }
        public float Angle=>Get<float>(Visual,"wheelAngle");
    }

    static void Main()
    {
        foreach(var (drive,turn,direction) in new[]{(1f,0f,1),(-1f,0f,-1),(0f,1f,1),(0f,-1f,1),(1f,1f,1),(1f,-1f,1),(-1f,1f,-1),(-1f,-1f,-1)})
        {
            var tank=new Tank();Quaternion initial=tank.Wheels[0].localRotation;
            tank.Input.Input=new Vector2(turn,drive);tank.Frame(.2f);
            Near(tank.Angle,direction>0?18:342,$"Arcade wheel direction for {drive}/{turn}");
            Near(tank.Instance.Offset.y,.33f+direction*.05f,"Texture Y follows wheel direction");
            Quaternion expected=initial*Quaternion.AngleAxis(direction*18,Vector3.right);
            Assert(Math.Abs(Q.Dot(expected.Value,tank.Wheels[0].localRotation.Value))>.99999f,"Local X spin preserves authored orientation");
            for(int i=1;i<6;i++)
            {
                Quaternion rest=Quaternion.Euler(-90,13+i,7);
                Assert(Math.Abs(Q.Dot((rest*Quaternion.AngleAxis(direction*18,Vector3.right)).Value,tank.Wheels[i].localRotation.Value))>.99999f,
                    "All six wheels spin together without losing individual rest rotation");
            }
            tank.Input.Input=new Vector2();float angle=tank.Angle,offset=tank.Instance.Offset.y;tank.Frame(.2f);
            Near(tank.Angle,angle,"Idle holds wheel pose");Near(tank.Instance.Offset.y,offset,"Idle holds UV offset");
        }
        foreach(int fps in new[]{30,60,120})
        {
            var tank=new Tank();tank.Input.Input=new Vector2(0,.5f);
            for(int i=0;i<fps;i++)tank.Frame(1f/fps);
            Near(tank.Angle,45,"Wheel speed is delta-time based");Near(tank.Instance.Offset.y,.455f,"Texture speed is delta-time based");
        }
        var weighted=new Tank();Set(weighted.Visual,"driveContribution",.5f);Set(weighted.Visual,"turnContribution",.25f);
        weighted.Input.Input=new Vector2(-1,0);weighted.Frame(.2f);Near(weighted.Angle,4.5f,"Turn contribution is independently adjustable");
        weighted.Input.Input=new Vector2(1,-1);weighted.Frame(.2f);Near(weighted.Angle,355.5f,"Reverse dominates direction and uses configured drive magnitude");

        var first=new Tank();var second=new Tank(shared:first.Source);
        object block=first.Track.PropertyBlock;int clones=Material.Copies;
        first.Instance.Color=.12f;first.Input.Input=new Vector2(0,1);first.Frame(.2f);
        Near(first.Source.Offset.y,.33f,"Shared Material asset stays unchanged");Near(second.Instance.Offset.y,.33f,"Another tank's track stays still");
        Near(first.Instance.Offset.x,.2f,"Initial Offset X is retained");Near(first.Instance.Tiling.x,2,"Tiling X is retained");Near(first.Instance.Tiling.y,3,"Tiling Y is retained");
        Near(first.Instance.Color,.12f,"Runtime material tint is retained");Assert(ReferenceEquals(block,first.Track.PropertyBlock),"Renderer property blocks are untouched");
        for(int i=0;i<2000;i++)first.Frame(.2f);
        Assert(Math.Abs(first.Instance.Offset.y-.33f)<1,"Long-running UV phase stays bounded");Assert(Material.Copies==clones,"No per-frame Material creation");
        Call(first.Visual,"OnDisable");float held=first.Angle;Call(first.Visual,"LateUpdate");Near(first.Angle,held,"Disable stops presentation without reset");
        Call(first.Visual,"OnEnable");first.Frame(.2f);Assert(first.Angle!=held,"Re-enable resumes from current pose");
        Call(first.Visual,"OnDestroy");Assert(first.Instance.Destroyed && first.Track.sharedMaterials[1]==first.Source,"Destroy restores selected slot and destroys owned instance");

        foreach(float drive in new[]{-1f,1f})
        {
            var remote=new Tank(owner:false);remote.Input.Input=new Vector2(1,-drive);
            remote.Root.transform.position=new Vector3(0,0,drive*.5f);remote.Frame(.1f);
            Near(remote.Angle,drive>0?9:351,"Replica derives travel from synchronized root, not non-owner input");
            Assert(remote.Input.Reads==0,"Replica never reads input");Call(remote.Movement,"FixedUpdate");
            Assert(remote.Root.GetComponent<Rigidbody>().ForceCalls==0,"Replica presentation never runs physics driving");
        }
        foreach(float yaw in new[]{-6f,6f})
        {
            var remote=new Tank(owner:false);remote.Root.transform.rotation=Quaternion.Euler(0,yaw,0);remote.Frame(.1f);
            Near(remote.Angle,9,"Both stationary turn directions animate forward");
        }
        var wrap=new Tank(owner:false);wrap.Root.transform.rotation=Quaternion.Euler(0,359,0);wrap.Frame(.1f);
        float before=wrap.Angle;wrap.Root.transform.rotation=Quaternion.Euler(0,1,0);wrap.Frame(.1f);
        Near(wrap.Angle-before,3,"Replica yaw uses shortest arc around 360/0");
        var jump=new Tank(owner:false);jump.Root.transform.position=new Vector3(100,0,100);jump.Frame(.1f);
        Near(jump.Angle,0,"Position teleport cannot spin wheels");jump.Root.transform.rotation=Quaternion.Euler(0,100,0);jump.Frame(.1f);
        Near(jump.Angle,0,"Large rotation teleport cannot spin wheels");
        jump.Root.transform.position+=jump.Root.transform.forward*.5f;jump.Frame(.1f);Near(jump.Angle,9,"Replica resumes on subsequent ordinary movement");

        var life=new Tank();life.Input.Input=new Vector2(1,1);life.Frame(.1f);before=life.Angle;
        life.Health.Dead=true;life.Movement.ClearLifeMotion();life.Frame(.1f);Near(life.Angle,before,"Death stops tracks without resetting pose");
        life.Movement.RelocateForReturn(new Vector3(100,0,100),Quaternion.Euler(0,90,0));life.Health.Dead=false;life.Frame(.1f);
        Near(life.Angle,before,"Respawn relocation does not produce a wheel burst");life.Frame(.1f);Near(life.Angle,before+9,"Tracks resume after respawn sampling initializes");
        before=life.Angle;MatchResults.LocalFinal=new object();life.Frame(.1f);Near(life.Angle,before,"Results stops tracks immediately");
        MatchResults.LocalFinal=null;MatchManager.Running=false;life.Frame(.1f);Near(life.Angle,before,"Non-playing match phase stops tracks");MatchManager.Running=true;
        life.Movement.OnNetworkDespawn();life.Movement.IsSpawned=false;life.Frame(.1f);Near(life.Angle,before,"Despawn stops pending presentation");
        var broken=new GameObject().Add<TankTrackVisuals>();Call(broken,"Awake");
        Assert(!broken.enabled && Debug.Errors.Count==1 && Debug.Errors[0].Contains("wheels[0]"),"Missing reference emits named error and disables visuals");
        Console.WriteLine($"PASS: {checks} track visual / movement integration assertions (Unity and transport simulated).");
    }
}
