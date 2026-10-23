// Simulated Unity/NGO boundary. Production shooter, motor and recoil are compiled
// unchanged. This is not a Unity PhysicsScene or an NGO transport test.
using System;
using System.Collections.Generic;
using N=System.Numerics;
namespace UnityEngine
{
    public class SerializeField:Attribute { }
    public class TooltipAttribute(string text):Attribute { }
    public class InspectorNameAttribute(string text):Attribute { }
    public class DisallowMultipleComponent:Attribute { }
    public class HeaderAttribute(string name):Attribute { }
    public class MinAttribute(float min):Attribute { }
    public class RangeAttribute(float min,float max):Attribute { }
    public class DefaultExecutionOrder(int order):Attribute { }
    [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)] public class RequireComponent(Type type):Attribute { }
    public class Object
    {
        public string name;
        public bool Destroyed;
        public static void Destroy(Object obj)=>obj.Destroyed=true;
        public static void Destroy(Object obj,float delay)=>obj.Destroyed=true;
        public static GameObject Instantiate(GameObject obj,Vector3 pos,Quaternion rot)=>new();
        public static T FindAnyObjectByType<T>() where T:class=>null;
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public T GetComponentInChildren<T>(bool includeInactive) where T:class=>GetComponent<T>();
        public T[] GetComponentsInChildren<T>() where T:class=>Array.Empty<T>();
        public bool TryGetComponent<T>(out T result) where T:class { result=GetComponent<T>();return result!=null; }
    }
    public class MonoBehaviour:Component { public bool enabled=true;public bool isActiveAndEnabled=>enabled && gameObject.Active; }
    public class GameObject:Object
    {
        private readonly Dictionary<Type,Component> components=new();
        public int layer;
        public bool Active=true;
        public Transform transform;
        public GameObject() { transform=new Transform{gameObject=this}; }
        public T Add<T>() where T:Component,new() { var c=new T{gameObject=this};components[typeof(T)]=c;return c; }
        public T GetComponent<T>() where T:class
        { foreach(var component in components.Values) if(component is T result)return result;return null; }
        public bool TryGetComponent<T>(out T result) where T:class { result=GetComponent<T>();return result!=null; }
    }
    public class Transform:Component
    {
        public Transform parent;
        public Vector3 position,localPosition,localScale=new(1,1,1);
        public Quaternion rotation=Quaternion.identity,localRotation=Quaternion.identity;
        public Vector3 forward=>N.Vector3.Transform(new N.Vector3(0,0,1),rotation.Value);
        public Vector3 eulerAngles=>new(0,MathF.Atan2(2*(rotation.Value.W*rotation.Value.Y+rotation.Value.X*rotation.Value.Z),1-2*(rotation.Value.Y*rotation.Value.Y+rotation.Value.X*rotation.Value.X))*180/MathF.PI,0);
        public Vector3 InverseTransformVector(Vector3 value)=>value;
        public bool IsChildOf(Transform candidate)=>this==candidate || (parent!=null && parent.IsChildOf(candidate));
    }
    public struct Vector2(float x,float y) { public float x=x,y=y; }
    public struct Vector3(float x,float y,float z)
    {
        public float x=x,y=y,z=z;
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>0?this/magnitude:zero;
        public static Vector3 zero=>default;
        public static Vector3 up=>new(0,1,0);public static Vector3 right=>new(1,0,0);public static Vector3 forward=>new(0,0,1);
        public static implicit operator N.Vector3(Vector3 v)=>new(v.x,v.y,v.z);
        public static implicit operator Vector3(N.Vector3 v)=>new(v.X,v.Y,v.Z);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>(N.Vector3)a+(N.Vector3)b;
        public static Vector3 operator -(Vector3 a,Vector3 b)=>(N.Vector3)a-(N.Vector3)b;
        public static Vector3 operator -(Vector3 a)=>zero-a;
        public static Vector3 operator *(Vector3 a,float n)=>(N.Vector3)a*n;
        public static Vector3 operator /(Vector3 a,float n)=>(N.Vector3)a/n;
        public static bool operator ==(Vector3 a,Vector3 b)=>a.Equals(b);
        public static bool operator !=(Vector3 a,Vector3 b)=>!a.Equals(b);
        public override bool Equals(object obj)=>obj is Vector3 b && x==b.x && y==b.y && z==b.z;
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
        public static float Dot(Vector3 a,Vector3 b)=>N.Vector3.Dot(a,b);
        public static Vector3 ProjectOnPlane(Vector3 a,Vector3 n)=>a-n*Dot(a,n);
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float step)=>(b-a).magnitude<=step?b:a+(b-a).normalized*step;
        public static Vector3 ClampMagnitude(Vector3 a,float max)=>a.magnitude>max?a.normalized*max:a;
    }
    public struct Quaternion
    {
        public N.Quaternion Value;
        public static Quaternion identity=>new(){Value=N.Quaternion.Identity};
        public static Quaternion AngleAxis(float degrees,Vector3 axis)=>new(){Value=N.Quaternion.CreateFromAxisAngle(axis.normalized,degrees*MathF.PI/180)};
        public static Quaternion Euler(float x,float y,float z)=>AngleAxis(y,Vector3.up)*AngleAxis(x,Vector3.right)*AngleAxis(z,new Vector3(0,0,1));
        public static Quaternion LookRotation(Vector3 direction)=>identity;
        public static Quaternion operator *(Quaternion a,Quaternion b)=>new(){Value=a.Value*b.Value};
        public static Vector3 operator *(Quaternion a,Vector3 b)=>N.Vector3.Transform(b,a.Value);
    }
    public static class Mathf
    {
        public static float Abs(float n)=>MathF.Abs(n);
        public static float Clamp(float n,float min,float max)=>Math.Clamp(n,min,max);
        public static float Clamp01(float n)=>Clamp(n,0,1);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Exp(float n)=>MathF.Exp(n);
        public static float Repeat(float n,float length)=>Clamp(n-MathF.Floor(n/length)*length,0,length);
        public static float DeltaAngle(float a,float b) { float d=Repeat(b-a,360);return d>180?d-360:d; }
        public static float SmoothStep(float a,float b,float t) { t=Clamp01(t);return a+(b-a)*t*t*(3-2*t); }
    }
    public class Material:Object
    {
        public static int Copies;
        public Vector2 Offset=new(.2f,.33f),Tiling=new(2,3);
        public float Color=.7f;
        public Material() { name="Shared track"; }
        public Material(Material other) { Copies++;Offset=other.Offset;Tiling=other.Tiling;Color=other.Color;name=other.name; }
        public bool HasProperty(string property)=>property=="_BaseMap";
        public Vector2 GetTextureOffset(int id)=>Offset;
        public void SetTextureOffset(int id,Vector2 value)=>Offset=value;
    }
    public class Renderer:Component
    {
        private Material[] materials=Array.Empty<Material>();
        public object PropertyBlock=new();
        public Material[] sharedMaterials { get=>(Material[])materials.Clone();set=>materials=(Material[])value.Clone(); }
    }
    public static class Shader { public static int PropertyToID(string property)=>property.GetHashCode(); }
    public static class Debug { public static List<string> Errors=new();public static void LogError(string message,Object context)=>Errors.Add(message);public static void Log(string s,Object context=null) { } }
    public static class Time { public static float time,deltaTime=1f/60,fixedDeltaTime=.02f;public static double realtimeSinceStartupAsDouble; }
    public struct LayerMask { public static implicit operator LayerMask(int n)=>default; }
    public enum RigidbodyConstraints { FreezeAll }
    public enum RigidbodyInterpolation { None,Interpolate }
    public enum ForceMode { VelocityChange,Impulse }
    public enum QueryTriggerInteraction { Ignore }
    public class Rigidbody:Component
    {
        public RigidbodyConstraints constraints;public bool isKinematic;public float mass=10;
        public RigidbodyInterpolation interpolation=RigidbodyInterpolation.Interpolate;
        public Vector3 linearVelocity,angularVelocity,position;public Quaternion rotation=Quaternion.identity;
        public int ForceCalls;
        public List<(Vector3 Force,ForceMode Mode)> Forces=new();
        public List<double> ForceTimes=new();
        public void AddForce(Vector3 force,ForceMode mode) { ForceCalls++;Forces.Add((force,mode));ForceTimes.Add(Time.realtimeSinceStartupAsDouble);linearVelocity+=mode==ForceMode.Impulse?force/mass:force; }
        public void MoveRotation(Quaternion value)=>rotation=value;
        public RaycastHit[] SweepTestAll(Vector3 dir,float distance,QueryTriggerInteraction mode)=>Array.Empty<RaycastHit>();
    }
    public class Collider:Component { }
    public struct RaycastHit { public Collider collider;public float distance;public Vector3 point; }
    public struct ContactPoint { public Vector3 normal; }
    public class Collision { public int contactCount=>0;public ContactPoint GetContact(int i)=>default; }
    public struct Ray { public Vector3 origin,direction; }
    public class Camera:Component { public static Camera main;public Ray? TestRay;public Ray ViewportPointToRay(Vector3 p)=>TestRay??new(){direction=transform.forward}; }
    public static class Physics
    {
        public static Vector3 gravity=new(0,-9.81f,0);
        public static bool GetIgnoreLayerCollision(int a,int b)=>false;
        public static RaycastHit[] RaycastAll(Ray ray,float max,LayerMask mask,QueryTriggerInteraction q)=>Array.Empty<RaycastHit>();
    }
}
namespace UnityEngine.InputSystem
{
    public class InputActionReference { public InputAction action=new(); }
    public class InputAction
    {
        public struct CallbackContext { }
        public event Action<CallbackContext> performed;
        public void Enable() { }public void Disable() { }
    }
}
namespace Unity.Netcode
{
    public enum SendTo { Everyone }
    public enum RpcInvokePermission { Server }
    public class RpcAttribute(SendTo target):Attribute { public RpcInvokePermission InvokePermission; }
    public class ServerRpcAttribute:Attribute { }public class ClientRpcAttribute:Attribute { }
    public class NetworkObject:UnityEngine.Component { }
    public class NetworkBehaviour:UnityEngine.MonoBehaviour
    {
        public bool IsOwner,IsSpawned,IsServer,IsClient;
        public ulong OwnerClientId,NetworkObjectId;
        public NetworkManager NetworkManager=>NetworkManager.Singleton;
        public virtual void OnNetworkSpawn() { } public virtual void OnNetworkDespawn() { }
    }
    public class NetworkManager
    {
        public static NetworkManager Singleton=new();public const ulong ServerClientId=0;public ulong LocalClientId;
        public class Clock { private double time;public Action OnRead;public double Time { get { OnRead?.Invoke();return time; } set=>time=value; } }public Clock ServerTime=new();
        public class Transport { public ulong GetCurrentRtt(ulong client)=>0; }
        public class Config { public uint TickRate=30;public Transport NetworkTransport=new(); }public Config NetworkConfig=new();
    }
}
namespace Unity.Netcode.Components
{
    public class NetworkTransform:Unity.Netcode.NetworkBehaviour
    {
        public enum InterpolationTypes { LegacyLerp,Lerp,SmoothDampening }
        public InterpolationTypes PositionInterpolationType;public bool PositionLerpSmoothing;
        public static int InterpolationBufferTickOffset;
        public bool CanCommitToTransform=>IsOwner;
        public struct NetworkTransformState
        { public bool HasPositionChange;public int Tick;public UnityEngine.Vector3 Position;public int GetNetworkTick()=>Tick;public UnityEngine.Vector3 GetPosition()=>Position; }
        protected virtual void Awake() { }
        protected virtual void OnAuthorityPushTransformState(ref NetworkTransformState state) { }
        protected virtual void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState,ref NetworkTransformState newState) { }
        public int GetPositionBufferCount()=>0;
        public bool Interpolate=true;public float PositionMaxInterpolationTime=.1f;
        public UnityEngine.Vector3 GetSpaceRelativePosition(bool authority)=>transform.position;
        public void Teleport(UnityEngine.Vector3 p,UnityEngine.Quaternion r,UnityEngine.Vector3 s) { transform.position=p;transform.rotation=r; }
    }
}
public class PlayerHealth:UnityEngine.Component { public bool Dead; }
public class PlayerLook:UnityEngine.Component { public void ResetPresentation() { } }
public class TankInputHandler:UnityEngine.Component
{
    public UnityEngine.Vector2 Input;public int Reads;
    public UnityEngine.Vector2 MoveInput { get { Reads++;return Input; } }
}
public class MatchManager
{
    public static MatchManager Instance=new();public bool Running=true;public static bool Allowed=true;
    public ShotPresentation Projectiles=new();public int Rejections;
    public static bool CanControl(PlayerHealth h)=>Allowed && (h==null || !h.Dead);
    public bool TryResolveProjectile(ulong source,uint shot)=>true;
    public void PublishProjectileResult(ulong source,uint shot,ulong target,bool impact,UnityEngine.Vector3 p,UnityEngine.Vector3 n,bool trace)=>Rejections++;
}
public static class MatchResults { public static object LocalFinal; }
public class TankGrounding:UnityEngine.Component
{
    public bool Grounded=true;public UnityEngine.Vector3 Normal=UnityEngine.Vector3.up;
    public float MinimumGroundDot=>.766f;public int Samples;
    public void Sample()=>Samples++;public void ResetSupport() { }
    public UnityEngine.Quaternion GetSteeredRotation(float turn,float dt)=>UnityEngine.Quaternion.identity;
}
public class TankTrackVisuals:UnityEngine.Component { public void SetMotion(float forward,float turn) { } }
public class TankCameraShake:UnityEngine.Component { public void ShakeOnFire() { } }
public class Variable<T> { public T Value; }
public class PlayerState:UnityEngine.Component { public Variable<int> teamIndex=new(); }
public class Bullet:UnityEngine.Component
{
    public UnityEngine.GameObject ImpactEffectPrefab;public float EffectLifetime=1;public int Launches;public UnityEngine.Vector3 Direction;
    public void Launch(UnityEngine.Vector3 direction,UnityEngine.Collider[] owner,int team,ulong source,uint shot,bool trace) { Launches++;Direction=direction; }
}
public class ShotPresentation
{
    public bool Accept=true;private HashSet<(ulong,uint)> registered=new();
    public bool Register(ulong tank,uint shot,UnityEngine.GameObject effect,float life,TankCameraShake shake)=>Accept && registered.Add((tank,shot));
}
public static class PoolManager
{
    public static bool Fail;public static List<Bullet> Launched=new();
    public static UnityEngine.GameObject Get(UnityEngine.GameObject prefab,UnityEngine.Vector3 p,UnityEngine.Quaternion r)
    {
        if(Fail)return null;var go=new UnityEngine.GameObject();var bullet=go.Add<Bullet>();Launched.Add(bullet);return go;
    }
}
