using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// One bounded server-only history for the locally simulated Host. No live body is rewound.
public sealed class HostShotHistory : NetworkBehaviour
{
    public static HostShotHistory Current { get; private set; }
    private struct Pose { public double Time; public Vector3 Position; public Quaternion Rotation; }
    private readonly Pose[] poses = new Pose[128];
    private int count, head;
    public uint Generation { get; private set; }
    public double GenerationStartedAt { get; private set; }
    private PlayerHealth health;
    private PlayerState state;
    private Rigidbody body, proxy;
    private Collider[] originals;
    private Scene scene;
    private PhysicsScene physics;
    private NetworkSceneManager.VerifySceneBeforeLoadingDelegateHandler previousFilter, filter;
    private readonly RaycastHit[] hits = new RaycastHit[8];
    private readonly Collider[] overlaps = new Collider[8];
    private Collider[] copies;
    public bool Alive => IsSpawned && !health.isDead.Value;
    private Pose At(int i) => poses[(head-count+i+poses.Length)%poses.Length];
    public override void OnNetworkSpawn()
    {
        if (!IsServer || OwnerClientId != NetworkManager.ServerClientId) return;
        Current=this; health=GetComponent<PlayerHealth>();state=GetComponent<PlayerState>();body=GetComponent<Rigidbody>();
        originals=GetComponentsInChildren<Collider>();
        health.isDead.OnValueChanged+=LifeChanged;state.teamIndex.OnValueChanged+=TeamChanged;
        previousFilter=NetworkManager.SceneManager.VerifySceneBeforeLoading;
        filter=(index,name,mode)=>name!="__HostShotQueries"&&(previousFilter==null||previousFilter(index,name,mode));
        NetworkManager.SceneManager.VerifySceneBeforeLoading=filter;
        scene=SceneManager.CreateScene("__HostShotQueries",new CreateSceneParameters(LocalPhysicsMode.Physics3D));physics=scene.GetPhysicsScene();
        var root=new GameObject("Host collision query only");SceneManager.MoveGameObjectToScene(root,scene);
        proxy=root.AddComponent<Rigidbody>();proxy.isKinematic=true;proxy.useGravity=false;copies=new Collider[originals.Length];
        for(int i=0;i<originals.Length;i++)
        {
            var source=originals[i];var child=new GameObject(source.name);child.transform.SetParent(root.transform,false);
            child.transform.localPosition=Quaternion.Inverse(transform.rotation)*(source.transform.position-transform.position);
            child.transform.localRotation=Quaternion.Inverse(transform.rotation)*source.transform.rotation;child.transform.localScale=source.transform.lossyScale;
            if(source is BoxCollider box){var c=child.AddComponent<BoxCollider>();c.center=box.center;c.size=box.size;copies[i]=c;}
            else if(source is CapsuleCollider capsule){var c=child.AddComponent<CapsuleCollider>();c.center=capsule.center;c.radius=capsule.radius;c.height=capsule.height;c.direction=capsule.direction;copies[i]=c;}
            else Debug.LogError("HostShotHistory: unsupported authored collider",this);
        }
        Physics.SyncTransforms();ResetHistory();
    }
    private void LifeChanged(bool a,bool b)=>ResetHistory();
    private void TeamChanged(int a,int b)=>ResetHistory();
    private void ResetHistory(){count=head=0;Generation++;GenerationStartedAt=NetworkManager.ServerTime.Time;}
    private void LateUpdate()
    {
        if(Current!=this||!Alive)return;
        double time=NetworkManager.ServerTime.Time;
        if(count>0&&time<=At(count-1).Time)return;
        if(count>0&&(body.position-At(count-1).Position).sqrMagnitude>4f)ResetHistory();
        poses[head]=new Pose{Time=time,Position=body.position,Rotation=body.rotation};head=(head+1)%poses.Length;count=Math.Min(count+1,poses.Length);
    }
    public bool Sample(double time,out Vector3 p,out Quaternion q)
    {
        p=default;q=Quaternion.identity;
        if(!Alive||count==0||!Finite(time)||time<At(0).Time)return false;
        var last=At(count-1);
        if(time>=last.Time){if(time-last.Time>.04)return false;p=last.Position;q=last.Rotation;return true;}
        for(int i=count-1;i>0;i--){var a=At(i-1);var b=At(i);if(time<a.Time)continue;if(b.Time-a.Time>.1)return false;float t=(float)((time-a.Time)/(b.Time-a.Time));p=Vector3.Lerp(a.Position,b.Position,t);q=Quaternion.Slerp(a.Rotation,b.Rotation,t);return true;}return false;
    }
    public bool Cast(uint generation,Vector3 from,Vector3 to,Vector3 extents,Quaternion rotation,double start,double end,int layer,out Collider original,out Vector3 point,out Vector3 normal,out float fraction)
    {
        original=null;point=normal=default;fraction=float.PositiveInfinity;
        if(generation!=Generation||!Sample(start,out Vector3 a,out Quaternion ar)||!Sample(end,out Vector3 b,out Quaternion br))return false;
        proxy.position=a;proxy.rotation=Quaternion.Slerp(ar,br,.5f);
        for(int i=0;i<copies.Length;i++)if(copies[i]!=null){bool enabled=originals[i].enabled&&!originals[i].isTrigger&&!Physics.GetIgnoreLayerCollision(layer,originals[i].gameObject.layer);if(copies[i].enabled!=enabled)copies[i].enabled=enabled;}
        int n=physics.OverlapBox(from,extents,overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
        if(n>0){original=Original(overlaps[0]);point=from;normal=(from-to).normalized;fraction=0;return original!=null;}
        Vector3 relative=to-from-(b-a);float length=relative.magnitude;if(length<.000001f)return false;
        n=physics.BoxCast(from,extents,relative/length,hits,rotation,length,~0,QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++){float t=hits[i].distance/length;if(t>=fraction)continue;original=Original(hits[i].collider);fraction=t;point=hits[i].point+(b-a)*t;normal=hits[i].normal;}return original!=null;
    }
    private Collider Original(Collider c){for(int i=0;i<copies.Length;i++)if(copies[i]==c)return originals[i];return null;}
    public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    public static double PhysicalNow(NetworkManager n)=>n.IsServer?n.ServerTime.Time:n.LocalTime.Time-n.NetworkTimeSystem.LocalBufferSec;
    public override void OnNetworkDespawn()
    {
        if(Current!=this)return;
        health.isDead.OnValueChanged-=LifeChanged;state.teamIndex.OnValueChanged-=TeamChanged;
        if(NetworkManager.SceneManager!=null&&NetworkManager.SceneManager.VerifySceneBeforeLoading==filter)NetworkManager.SceneManager.VerifySceneBeforeLoading=previousFilter;
        if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);Current=null;
    }
}
