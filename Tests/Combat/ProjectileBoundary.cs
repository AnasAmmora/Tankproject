// Physics, engine lifetime and RPC delivery are simulated. Production Bullet and
// ShotPresentation are compiled unchanged; this cannot validate PhysX or NGO timing.
using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)] public class RequireComponent(Type type):Attribute { }
    public class SerializeField:Attribute { }
    public class HeaderAttribute(string text):Attribute { }
    public class TooltipAttribute(string text):Attribute { }
    public class MinAttribute(float value):Attribute { }
    public class RangeAttribute(float min,float max):Attribute { }
    public class CreateAssetMenuAttribute:Attribute { public string menuName; }
    public class ScriptableObject:Object { }
    public static class Debug { public static void LogError(string message,Object context) { } }
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Clamp(float value,float min,float max)=>Math.Clamp(value,min,max);
        public static float Clamp01(float value)=>Math.Clamp(value,0,1);
    }
    public class Object
    {
        public static int Effects;
        public static GameObject Instantiate(GameObject prefab,Vector3 p,Quaternion q) { Effects++;return new GameObject(); }
        public static void Destroy(Object obj,float delay) { }
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T:class=>GetComponent<T>();
    }
    public class MonoBehaviour:Component
    {
        public bool TimerScheduled;
        public void CancelInvoke(string method)=>TimerScheduled=false;
        public void Invoke(string method,float seconds)=>TimerScheduled=true;
    }
    public class GameObject:Object
    {
        public readonly Dictionary<Type,Component> Components=new();
        public Transform transform=new(); public int layer; public bool active=true;
        public T GetComponent<T>() where T:class=>Components.TryGetValue(typeof(T),out var c)?c as T:null;
        public T Add<T>() where T:Component,new() { var c=new T{gameObject=this};Components[typeof(T)]=c;return c; }
    }
    public class Transform { public Vector3 position; public Quaternion rotation; }
    public struct Vector3(float x,float y,float z)
    {
        public float x=x,y=y,z=z;
        public float sqrMagnitude=>x*x+y*y+z*z; public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>0?this*(1/magnitude):zero;
        public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 zero=>default; public static Vector3 up=>new(0,1,0);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a)=>zero-a;
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
    }
    public struct Quaternion { public static Quaternion identity=>default; public static Quaternion LookRotation(Vector3 forward)=>default; }
    public enum CollisionDetectionMode { ContinuousDynamic }
    public enum QueryTriggerInteraction { Ignore }
    public class Rigidbody:Component
    {
        public Vector3 position,linearVelocity,angularVelocity; public Quaternion rotation;
        public bool useGravity;public CollisionDetectionMode collisionDetectionMode;
        public RaycastHit[] Hits=Array.Empty<RaycastHit>();
        public RaycastHit[] SweepTestAll(Vector3 direction,float distance,QueryTriggerInteraction interaction)=>Hits;
    }
    public class Collider:Component { public bool isTrigger,enabled=true;public Vector3 ClosestPoint(Vector3 p)=>transform.position; }
    public struct RaycastHit { public Collider collider;public Vector3 point,normal;public float distance; }
    public static class Physics { public static Vector3 gravity=new(0,-9.81f,0);public static bool GetIgnoreLayerCollision(int a,int b)=>false; }
    public static class Time { public static float fixedDeltaTime=.02f; }
}
namespace Unity.Netcode
{
    public class NetworkManager { public static NetworkManager Singleton=new();public bool IsServer; }
    public class NetworkObject:UnityEngine.Component { public ulong NetworkObjectId; }
}
public class TestVariable<T> { public T Value; }
public class PlayerState:UnityEngine.Component { public TestVariable<int> teamIndex=new(); }
public class PlayerHealth:UnityEngine.Component
{
    public TestVariable<bool> isDead=new();public ulong NetworkObjectId=101;public int DamageCalls,HP=100,LastDamage;
    public UnityEngine.Vector3 Source,Impact;
    public bool TakeDamage(int damage,int team,ulong shooter,UnityEngine.Vector3 source,UnityEngine.Vector3 impact)
    { if(damage<=0)return false; DamageCalls++;LastDamage=damage;Source=source;Impact=impact;HP-=damage;isDead.Value=HP<=0;return true; }
}
public class PlayerMovement:UnityEngine.Component
{
    public ulong NetworkObjectId=101;public int Impulses;
    public void ApplyHitImpulse(UnityEngine.Vector3 force,ulong shooter,uint sequence,bool diagnostics)=>Impulses++;
}
public class TeamBase:UnityEngine.Component
{
    public int teamIndex,DamageCalls,LastDamage;public TestVariable<bool> isDestroyed=new();
    public void TakeDamage(int damage,int team,ulong shooter,UnityEngine.Vector3 impact) { DamageCalls++;LastDamage=damage; }
}
public class TankCameraShake:UnityEngine.Component { public int Hits;public void ShakeOnHit()=>Hits++; }
public static class TankShooter
{ public static void TraceShot(bool trace,ulong shooter,uint shot,ulong target,string stage) { } }
public static class MatchResults { public static object LocalFinal; }
public class MatchManager
{
    public static MatchManager Instance;public bool Running=true;
    public ShotPresentation Projectiles=new();public int Results;
    private readonly HashSet<(ulong,uint)> resolved=new();
    public bool TryGetShotTeam(ulong shooter,out int team) { team=0;return true; }
    public bool TryResolveProjectile(ulong shooter,uint sequence)=>Unity.Netcode.NetworkManager.Singleton.IsServer && resolved.Add((shooter,sequence));
    public void PublishProjectileResult(ulong shooter,uint sequence,ulong target,bool impact,UnityEngine.Vector3 point,UnityEngine.Vector3 normal,bool diagnostics)
    { Results++;Projectiles.Resolve(shooter,sequence,impact,point,normal); }
}
public static class PoolManager
{
    public static int Releases;
    public static void Release(UnityEngine.GameObject obj)
    {
        Releases++;obj.active=false;
        typeof(Bullet).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj.GetComponent<Bullet>(),null);
    }
}
