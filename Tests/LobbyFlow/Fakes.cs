// Only the engine/service boundary is simulated. The three production flow scripts
// are compiled unchanged by this project. This is NOT a Unity/Relay integration test.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;

namespace UnityEngine
{
    public class SerializeField : Attribute { }
    public class HeaderAttribute(string text) : Attribute { }
    public class MinAttribute(float value) : Attribute { }
    public class RangeAttribute(float min, float max) : Attribute { }
    public class RequireComponent(Type type) : Attribute { }
    public class Object
    {
        public string name;
        public static readonly Dictionary<Type, object> Found = new();
        public static T FindAnyObjectByType<T>() where T : class => Found.TryGetValue(typeof(T), out var value) ? (T)value : null;
        public static void Destroy(Object obj) { }
        public static void Destroy(Object obj,float delay) { }
        public static int Instantiations;
        public static Vector3 LastInstantiationPosition;
        public static GameObject Instantiate(GameObject original,Vector3 position,Quaternion rotation)
        { Instantiations++; LastInstantiationPosition=position; return new("clone"); }
        public static void DontDestroyOnLoad(Object obj) { }
        public static GameObject Instantiate(GameObject original, Transform parent) => new("clone");
        public static Func<Object, Transform, Object> PrefabFactory;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => (T)PrefabFactory(original, parent);
    }
    public class Component : Object
    {
        public bool enabled = true;
        public readonly Dictionary<Type, object> Components = new();
        public GameObject gameObject;
        public Transform transform => gameObject?.transform;
        public T GetComponent<T>() where T : class => Components.TryGetValue(typeof(T), out var value) ? (T)value : gameObject?.GetComponent<T>();
        public T GetComponentInParent<T>() where T : class => GetComponent<T>();
        public bool TryGetComponent<T>(out T value) where T : class { value = GetComponent<T>(); return value != null; }
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
    }
    public class MonoBehaviour : Component
    {
        public readonly List<IEnumerator> Routines = new();
        public Coroutine StartCoroutine(IEnumerator routine) { Routines.Add(routine); return new Coroutine{Routine=routine}; }
        public void StopCoroutine(Coroutine routine)=>Routines.Remove(routine.Routine);
        public void StopAllCoroutines()=>Routines.Clear();
    }
    public class Coroutine { public IEnumerator Routine; }
    public class GameObject : Object
    {
        public SceneManagement.Scene scene;
        public bool activeSelf = true;
        public bool activeInHierarchy => activeSelf && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
        public Transform transform;
        private readonly Dictionary<Type, Component> components = new();
        public GameObject(string name = "", params Type[] types)
        {
            this.name = name;
            transform = new RectTransform { gameObject = this };
            foreach (Type type in types) components[type] = (Component)Activator.CreateInstance(type);
            foreach (var component in components.Values) component.gameObject = this;
        }
        public T GetComponent<T>() where T : class => typeof(T) == typeof(RectTransform) ? transform as T :
            components.TryGetValue(typeof(T), out var value) ? value as T : null;
        public T GetComponentInParent<T>() where T : class => GetComponent<T>();
        public void SetActive(bool active) => activeSelf = active;
    }
    public class Transform : Component, IEnumerable<Transform>
    {
        public Vector3 position, forward = new(0,0,1);
        public Vector3 localScale = new(1,1,1);
        public Quaternion rotation;
        public Quaternion localRotation;
        public Transform parent;
        public bool IsChildOf(Transform value) => parent == value || (parent != null && parent.IsChildOf(value));
        public void SetParent(Transform value, bool stay) { parent = value; }
        public void SetSiblingIndex(int index) { }
        public IEnumerator<Transform> GetEnumerator() { yield break; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition; }
    public struct Vector2(float x, float y) { }
    public struct Color(float red, float green, float blue, float alpha = 1)
    { public float r = red, g = green, b = blue, a = alpha; public static Color white => new(1,1,1); }
    public struct Vector3(float x, float y, float z)
    {
        public float x=x,y=y,z=z;
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public static Vector3 zero => default;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public Vector3 normalized => sqrMagnitude > 0 ? this / MathF.Sqrt(sqrMagnitude) : default;
        public static Vector3 up => new(0,1,0);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator /(Vector3 a,float b)=>new(a.x/b,a.y/b,a.z/b);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 ProjectOnPlane(Vector3 a,Vector3 normal)=>a-normal*Dot(a,normal);
        public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a-b).sqrMagnitude);
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float step)=>Distance(a,b)<=step?b:a+(b-a).normalized*step;
    }
    public struct Quaternion
    { public float z; public static Quaternion identity=>default; public static Quaternion LookRotation(Vector3 forward)=>default; public static Quaternion Euler(float x,float y,float z)=>new(){z=z}; public static float Angle(Quaternion a,Quaternion b)=>0; }
    public class Collider : Component { }
    public class Renderer : Component { }
    public static class Mathf
    {
        public const float Rad2Deg = 180f/MathF.PI;
        public static float Clamp01(float value)=>Math.Clamp(value,0,1);
        public static float Clamp(float value,float min,float max)=>Math.Clamp(value,min,max);
        public static float Exp(float value)=>MathF.Exp(value);
        public static float SmoothStep(float a,float b,float t) { t=Clamp01(t); return a+(b-a)*t*t*(3-2*t); }
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Sin(float value)=>MathF.Sin(value);
        public static float Atan2(float a,float b)=>MathF.Atan2(a,b);
        public static float MoveTowards(float a,float b,float step)=>a < b ? Math.Min(b,a+step) : Math.Max(b,a-step);
    }
    public class CanvasGroup : Component { public float alpha; }
    public class Camera : Component { public static Camera main; }
    public class Canvas : Component { }
    public class AsyncOperation { public bool isDone = true; }
    public enum CursorLockMode { None, Locked }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible; }
    public static class Time { public static float unscaledDeltaTime = 0.02f, unscaledTime, time; public static double realtimeSinceStartupAsDouble; }
    public static class Debug
    {
        public static readonly List<string> Messages = new();
        public static void Log(object value) => Messages.Add(value.ToString());
        public static void LogWarning(object value) => Messages.Add(value.ToString());
        public static void LogError(object value, Object context = null) => Messages.Add(value.ToString());
    }
    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new() { IncludeFields = true };
        public static int SerializationCalls;
        public static string ToJson<T>(T value) { SerializationCalls++; return System.Text.Json.JsonSerializer.Serialize(value, Options); }
        public static T FromJson<T>(string value) => System.Text.Json.JsonSerializer.Deserialize<T>(value, Options);
    }
}
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single }
    public struct Scene { public string name; public bool isLoaded; }
    public static class SceneManager
    {
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static string Active = "MainMenuScene";
        public static Scene GetActiveScene() => new() { name = Active, isLoaded = true };
        public static Scene GetSceneByName(string name) => new() { name = name, isLoaded = Active == name };
        public static UnityEngine.AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode)
        { Active = name; sceneLoaded?.Invoke(GetActiveScene(), mode); return new(); }
        public static void MoveGameObjectToScene(UnityEngine.GameObject obj,Scene scene)=>obj.scene=scene;
    }
}
namespace UnityEngine.UI
{
    public class Image : UnityEngine.Component { public float fillAmount; public UnityEngine.Color color; }
    public class GraphicRaycaster : UnityEngine.Component { }
    public class Button : UnityEngine.Component
    {
        public bool interactable = true;
        public readonly ClickEvent onClick = new();
        public class ClickEvent { public void AddListener(Action action) { } public void RemoveListener(Action action) { } }
    }
}
namespace UnityEngine.EventSystems { public class EventSystem : UnityEngine.MonoBehaviour { } }
namespace TMPro
{
    public enum TextAlignmentOptions { Center }
    public class TMP_FontAsset { }
    public static class TMP_Settings { public static TMP_FontAsset defaultFontAsset = new(); }
    public class TMP_Text : UnityEngine.Component
    {
        public string text; public TMP_FontAsset font; public float fontSize; public UnityEngine.Color color;
        public TextAlignmentOptions alignment; public bool richText, raycastTarget;
        public UnityEngine.RectTransform rectTransform => (UnityEngine.RectTransform)transform;
    }
    public class TextMeshProUGUI : TMP_Text { }
    public class TMP_Dropdown : UnityEngine.Component { public int value; public bool interactable; }
}
namespace Unity.Services.Core
{
    public enum ServicesInitializationState { Uninitialized, Initialized }
    public static class UnityServices { public static ServicesInitializationState State = ServicesInitializationState.Initialized; }
}
namespace Unity.Services.Authentication
{
    public class AuthenticationService
    {
        public static AuthenticationService Instance = new();
        public string PlayerId = "host"; public bool IsSignedIn = true;
    }
}
namespace Unity.Services.Lobbies.Models
{
    public class Lobby
    {
        public string Id = "room", HostId = "host";
        public bool IsLocked; public int MaxPlayers = 2;
        public List<Player> Players = new(); public Dictionary<string, DataObject> Data = new();
        public DateTime LastUpdated;
    }
    public class Player { public string Id; public Dictionary<string, PlayerDataObject> Data; }
    public class PlayerDataObject(PlayerDataObject.VisibilityOptions visibility, string value)
    { public enum VisibilityOptions { Member } public string Value = value; }
    public class DataObject(DataObject.VisibilityOptions visibility, string value)
    { public enum VisibilityOptions { Member, Public } public string Value = value; }
    public class QueryResponse { public List<Lobby> Results = new(); }
    public class QueryFilter(QueryFilter.FieldOptions field, string value, QueryFilter.OpOptions operation)
    { public enum FieldOptions { AvailableSlots } public enum OpOptions { GT } }
    public class QueryOrder(bool asc, QueryOrder.FieldOptions field) { public enum FieldOptions { Created } }
}
namespace Unity.Services.Lobbies
{
    public enum LobbyExceptionReason { UnknownErrorCode = 0, LobbyNotFound = 16001, PlayerNotFound = 16002,
        RequestTimeOut = 16408, RateLimited = 16429, InternalServerError = 16500, NetworkError = 16998, Unknown = 16999 }
    public class LobbyServiceException(LobbyExceptionReason reason, string message) : Exception(message)
    { public LobbyExceptionReason Reason => reason; public int ErrorCode => (int)reason; }
    public class CreateLobbyOptions { public bool IsPrivate; public Player Player; public Dictionary<string, DataObject> Data; }
    public class JoinLobbyByIdOptions { public Player Player; }
    public class UpdateLobbyOptions { public bool? IsLocked; public Dictionary<string, DataObject> Data; }
    public class UpdatePlayerOptions { public Dictionary<string, PlayerDataObject> Data; }
    public class QueryLobbiesOptions { public int Count; public List<QueryFilter> Filters; public List<QueryOrder> Order; }
    public class LobbyService
    {
        public static LobbyService Instance = new();
        public Func<Task<List<string>>> Joined = () => Task.FromResult(new List<string>());
        public Func<string, Task<Lobby>> Get = id => Task.FromResult(new Lobby { Id = id });
        public Func<CreateLobbyOptions, Task<Lobby>> Create = options => Task.FromResult(new Lobby { Data = options.Data, Players = new() { options.Player } });
        public Func<JoinLobbyByIdOptions, Task<Lobby>> Join = options => Task.FromResult(new Lobby { Players = new() { options.Player } });
        public Func<Task<QueryResponse>> Query = () => Task.FromResult(new QueryResponse());
        public Func<UpdatePlayerOptions, Task<Lobby>> PlayerUpdate = options => Task.FromResult(new Lobby());
        public Func<UpdateLobbyOptions, Task<Lobby>> Update = options => Task.FromResult(new Lobby());
        public Func<Task> Heartbeat = () => Task.CompletedTask;
        public Func<Task> Delete = () => Task.CompletedTask;
        public Func<Task> Remove = () => Task.CompletedTask;
        public Task<List<string>> GetJoinedLobbiesAsync() => Joined();
        public Task<Lobby> GetLobbyAsync(string id) => Get(id);
        public Task<Lobby> CreateLobbyAsync(string name, int max, CreateLobbyOptions options) => Create(options);
        public Task<Lobby> JoinLobbyByIdAsync(string id, JoinLobbyByIdOptions options) => Join(options);
        public Task<QueryResponse> QueryLobbiesAsync(QueryLobbiesOptions options) => Query();
        public Task<Lobby> UpdatePlayerAsync(string id, string playerId, UpdatePlayerOptions options) => PlayerUpdate(options);
        public Task<Lobby> UpdateLobbyAsync(string id, UpdateLobbyOptions options) => Update(options);
        public Task SendHeartbeatPingAsync(string id) => Heartbeat();
        public Task DeleteLobbyAsync(string id) => Delete();
        public Task RemovePlayerAsync(string id, string player) => Remove();
    }
}
namespace Unity.Services.Relay
{
    public class Allocation
    {
        public class Server { public string IpV4 = "127.0.0.1"; public int Port = 7777; }
        public Server RelayServer = new(); public Guid AllocationId; public byte[] AllocationIdBytes, Key, ConnectionData, HostConnectionData;
    }
    public class RelayService
    {
        public static RelayService Instance = new();
        public Task<Allocation> CreateAllocationAsync(int count) => Task.FromResult(new Allocation());
        public Task<string> GetJoinCodeAsync(Guid id) => Task.FromResult("test-code");
        public Task<Allocation> JoinAllocationAsync(string code) => Task.FromResult(new Allocation());
    }
}
namespace Unity.Netcode
{
    public class NetworkBehaviour : UnityEngine.MonoBehaviour
    {
        public NetworkManager NetworkManager=>NetworkManager.Singleton;
        public bool IsServer,IsClient;
        public ulong NetworkObjectId;
        public bool IsOwner, IsSpawned;
        public ulong OwnerClientId;
        public virtual void OnNetworkSpawn() { }
        public virtual void OnNetworkDespawn() { }
    }
    public class NetworkObject : UnityEngine.Component
    { public ulong NetworkObjectId; public bool IsSpawned,IsPlayerObject; public void SpawnAsPlayerObject(ulong id,bool destroy)=>IsSpawned=IsPlayerObject=true; }
    public class NetworkVariable<T>(T value=default)
    {
        private T storage=value;
        public event Action<T,T> OnValueChanged;
        public int SubscriberCount=>OnValueChanged?.GetInvocationList().Length??0;
        public T Value { get=>storage; set { T old=storage; storage=value; if(!EqualityComparer<T>.Default.Equals(old,value)) OnValueChanged?.Invoke(old,value); } }
    }
    public enum SceneEventProgressStatus { Started, SceneFailedVerification }
    public enum SceneEventType { Synchronize, Load, LoadComplete, SynchronizeComplete, LoadEventCompleted }
    public class SceneEvent { public SceneEventType SceneEventType; public string SceneName; public ulong ClientId; public List<ulong> ClientsThatTimedOut; }
    public class NetworkSceneManager
    {
        public event Action<SceneEvent> OnSceneEvent;
        public SceneEventProgressStatus Result = SceneEventProgressStatus.Started;
        public SceneEventProgressStatus LoadScene(string scene, UnityEngine.SceneManagement.LoadSceneMode mode) => Result;
    }
    public enum SendTo { Everyone,Server,Owner }
    public enum RpcInvokePermission { Server,Owner,Everyone }
    public class RpcAttribute(SendTo target):Attribute { public RpcInvokePermission InvokePermission; }
    public struct RpcParams { public struct Received { public ulong SenderClientId; } public Received Receive; }
    public class NetworkConfig { public UnityEngine.GameObject PlayerPrefab; public bool ConnectionApproval, AutoSpawnPlayerPrefabClientSide; public byte[] ConnectionData; }
    public class NetworkManager : UnityEngine.MonoBehaviour
    {
        public class Spawner
        { public Dictionary<ulong,NetworkObject> Players=new(); public HashSet<NetworkObject> SpawnedObjectsList=new(); public NetworkObject GetPlayerNetworkObject(ulong id)=>Players.GetValueOrDefault(id); }
        public Spawner SpawnManager = new();
        public static NetworkManager Singleton;
        public class Client { public NetworkObject PlayerObject; }
        public class Clock { public double Time; }
        public Client LocalClient = new(); public Clock ServerTime = new();
        public static ulong ServerClientId => 0;
        public ulong LocalClientId;
        public bool IsListening, ShutdownInProgress, IsServer, IsConnectedClient, HostResult = true, ClientResult = true;
        public int ShutdownCalls; public string DisconnectReason;
        public NetworkConfig NetworkConfig = new(); public NetworkSceneManager SceneManager = new();
        public Dictionary<ulong, object> ConnectedClients = new();
        public IEnumerable<ulong> ConnectedClientsIds=>ConnectedClients.Keys;
        public void DisconnectClient(ulong id,string reason)=>ConnectedClients.Remove(id);
        public event Action<ulong> OnClientDisconnectCallback;
        public event Action OnTransportFailure;
        public event Action<ConnectionApprovalRequest, ConnectionApprovalResponse> ConnectionApprovalCallback;
        public class ConnectionApprovalRequest { public byte[] Payload; public ulong ClientNetworkId; }
        public class ConnectionApprovalResponse { public bool Pending, Approved, CreatePlayerObject; public string Reason; }
        public bool StartHost() { IsListening = IsServer = HostResult; if (HostResult) ConnectedClients[0] = new(); return HostResult; }
        public bool StartClient() { IsListening = ClientResult; return ClientResult; }
        public void Shutdown() { ShutdownCalls++; IsListening = IsServer = IsConnectedClient = false; OnClientDisconnectCallback?.Invoke(0); }
    }
}
namespace Unity.Netcode.Transports.UTP
{
    public class UnityTransport
    {
        public void SetHostRelayData(string ip, ushort port, byte[] id, byte[] key, byte[] data) { }
        public void SetClientRelayData(string ip, ushort port, byte[] id, byte[] key, byte[] data, byte[] host) { }
        public void SetRelayServerData(object data) { }
        public void SetConnectionData(string ip, ushort port) { }
    }
}
public class PlayerIdentityManager { public static string LocalPlayerName = "Tester"; public static bool OpenBrowserOnNextMenu; }
public class RoomItemUI : UnityEngine.Component { public void Initialize(Lobby lobby, LobbyManager manager) { } }
#if !UI_FLOW_TESTS
public class LoadingScreenController
{
    public static LoadingScreenController Instance = new();
    public bool IsConfigured = true;
    public bool Visible; public string Message; public event Action ReturnRequested;
    public void Show(string message) { Visible = true; Message = message; }
    public void ShowError(string message, bool canReturn) => Show(message);
    public void Hide() => Visible = false;
}
public class MatchResults { public static object LocalFinal; public static void Clear() => LocalFinal = null; }
#endif
#if !COMBAT_TESTS
public class MatchManager
{
    public static MatchManager Instance; public bool IsSpawned, FinalSnapshotsDelivered = true;
    public bool Running;
    public Unity.Netcode.NetworkVariable<int> phase = new(0), destroyedBases = new(0);
    public Unity.Netcode.NetworkVariable<double> phaseEndsAt = new(0);
#if UI_FLOW_TESTS
    public event Action<CombatFeedback> CombatConfirmed;
    public Unity.Netcode.NetworkVariable<Unity.Collections.FixedString4096Bytes> statistics = new(default);
    public void Emit(CombatFeedback value)=>CombatConfirmed?.Invoke(value);
#endif
    public static bool CanControl(PlayerHealth health)=>MatchConnectionCoordinator.GameplayAllowed && (Instance==null||Instance.Running) && !health.isDead.Value;
    public void BroadcastAbort(string reason) => MatchConnectionCoordinator.Instance.ReturnAfterFailure(reason);
}
#endif
#if !UI_FLOW_TESTS
public class MatchHud { public static MatchHud Instance; public void ShowFinal() { } }
#endif
#if !COMBAT_TESTS
public class PlayerHealth : Unity.Netcode.NetworkBehaviour
{
    public static event Action<PlayerHealth> Spawned;
    public bool isPreparedForLife = true;
    public int maxHealth=100;
    public Unity.Netcode.NetworkVariable<int> currentHealth = new(100);
    public Unity.Netcode.NetworkVariable<bool> isDead = new(false);
    public Unity.Netcode.NetworkVariable<double> respawnAt = new(-1);
}
#endif
public class PlayerState : UnityEngine.Component
{ public Unity.Netcode.NetworkVariable<int> teamIndex = new(0),spawnOrdinal=new(0); public Unity.Netcode.NetworkVariable<bool> isPrepared = new(true); }
#if UI_FLOW_TESTS
public class TankShooter : UnityEngine.Component
{
    public static void TraceShot(bool enabled,ulong shooter,uint shot,ulong target,string stage,double confirmedAt=-1,double elapsed=0) { }
    public event Action WeaponStateChanged;
    public float CooldownRemaining, CooldownDuration=.4f, CooldownEndsAt;
    public bool CanFire=true;
    public void Emit()=>WeaponStateChanged?.Invoke();
}
#endif
public static class PoolManager { public static void Clear() { } public static void ReleaseActiveBullets() { } }
#if COMBAT_TESTS
public class PlayerMovement:UnityEngine.Component
{ public void ClearLifeMotion() { } public void RelocateForReturn(UnityEngine.Vector3 position,UnityEngine.Quaternion rotation) { } }
public class PlayerLook:UnityEngine.Component { public bool LocalCameraReady=true; public void ResetAfterReturn() { } public void ResetPresentation() { } }
public class TankCameraShake : UnityEngine.Component { public int Hits; public void ShakeOnHit()=>Hits++; }
// Only the pooled engine object is simulated; ShotPresentation itself is production code.
public class Bullet : UnityEngine.Component
{
    public ulong Shooter; public uint Sequence; public bool Active=true; public int Releases;
    public Action Disabled;
    public void ReleaseIfShot(ulong shooter,uint sequence)
    { if (!Active || Shooter!=shooter || Sequence!=sequence) return; Active=false; Releases++; Disabled?.Invoke(); }
}
#endif
namespace Unity.Collections
{
    public struct FixedString4096Bytes(string value)
    {
        public int Length => value?.Length??0;
        public override string ToString()=>value??"";
    }
}
