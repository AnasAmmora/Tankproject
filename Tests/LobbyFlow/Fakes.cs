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
    public class RequireComponent(Type type) : Attribute { }
    public class Object
    {
        public static readonly Dictionary<Type, object> Found = new();
        public static T FindAnyObjectByType<T>() where T : class => Found.TryGetValue(typeof(T), out var value) ? (T)value : null;
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object obj) { }
        public static GameObject Instantiate(GameObject original, Transform parent) => new("clone");
    }
    public class Component : Object
    {
        public readonly Dictionary<Type, object> Components = new();
        public GameObject gameObject;
        public Transform transform => gameObject?.transform;
        public T GetComponent<T>() where T : class => Components.TryGetValue(typeof(T), out var value) ? (T)value : gameObject?.GetComponent<T>();
        public T GetComponentInParent<T>() where T : class => GetComponent<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
    }
    public class MonoBehaviour : Component
    {
        public readonly List<IEnumerator> Routines = new();
        public object StartCoroutine(IEnumerator routine) { Routines.Add(routine); return routine; }
    }
    public class GameObject : Object
    {
        public bool activeSelf = true;
        public Transform transform;
        private readonly Dictionary<Type, Component> components = new();
        public GameObject(string name = "", params Type[] types)
        {
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
        public void SetParent(Transform parent, bool stay) { }
        public IEnumerator<Transform> GetEnumerator() { yield break; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition; }
    public struct Vector2(float x, float y) { }
    public struct Color { public static Color white => default; }
    public class Canvas : Component { }
    public class AsyncOperation { public bool isDone = true; }
    public enum CursorLockMode { None, Locked }
    public static class Cursor { public static CursorLockMode lockState; public static bool visible; }
    public static class Time { public static float unscaledDeltaTime = 0.02f; public static double realtimeSinceStartupAsDouble; }
    public static class Debug
    {
        public static readonly List<string> Messages = new();
        public static void Log(object value) => Messages.Add(value.ToString());
        public static void LogWarning(object value) => Messages.Add(value.ToString());
    }
    public static class JsonUtility
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, Options);
        public static T FromJson<T>(string value) => System.Text.Json.JsonSerializer.Deserialize<T>(value, Options);
    }
}
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single }
    public struct Scene { public string name; public bool isLoaded; }
    public static class SceneManager
    {
        public static string Active = "MainMenuScene";
        public static Scene GetActiveScene() => new() { name = Active, isLoaded = true };
        public static Scene GetSceneByName(string name) => new() { name = name, isLoaded = Active == name };
        public static UnityEngine.AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode) { Active = name; return new(); }
    }
}
namespace UnityEngine.UI
{
    public class Button : UnityEngine.Component
    {
        public bool interactable = true;
        public readonly ClickEvent onClick = new();
        public class ClickEvent { public void AddListener(Action action) { } public void RemoveListener(Action action) { } }
    }
}
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
    public enum SceneEventProgressStatus { Started, SceneFailedVerification }
    public enum SceneEventType { Synchronize, Load, LoadComplete, SynchronizeComplete, LoadEventCompleted }
    public class SceneEvent { public SceneEventType SceneEventType; public string SceneName; public ulong ClientId; public List<ulong> ClientsThatTimedOut; }
    public class NetworkSceneManager
    {
        public event Action<SceneEvent> OnSceneEvent;
        public SceneEventProgressStatus Result = SceneEventProgressStatus.Started;
        public SceneEventProgressStatus LoadScene(string scene, UnityEngine.SceneManagement.LoadSceneMode mode) => Result;
    }
    public class NetworkConfig { public bool ConnectionApproval, AutoSpawnPlayerPrefabClientSide; public byte[] ConnectionData; }
    public class NetworkManager : UnityEngine.MonoBehaviour
    {
        public static ulong ServerClientId => 0;
        public ulong LocalClientId;
        public bool IsListening, ShutdownInProgress, IsServer, IsConnectedClient, HostResult = true, ClientResult = true;
        public int ShutdownCalls; public string DisconnectReason;
        public NetworkConfig NetworkConfig = new(); public NetworkSceneManager SceneManager = new();
        public Dictionary<ulong, object> ConnectedClients = new();
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
public class LoadingScreenController
{
    public static LoadingScreenController Instance = new();
    public bool Visible; public string Message; public event Action ReturnRequested;
    public void Show(string message) { Visible = true; Message = message; }
    public void ShowError(string message, bool canReturn) => Show(message);
    public void Hide() => Visible = false;
}
public class MatchResults { public static object LocalFinal; public static void Clear() => LocalFinal = null; }
public class MatchManager
{
    public static MatchManager Instance; public bool IsSpawned, FinalSnapshotsDelivered = true;
    public void BroadcastAbort(string reason) => MatchConnectionCoordinator.Instance.ReturnAfterFailure(reason);
}
public class MatchHud { public static MatchHud Instance; public void ShowFinal() { } }
public static class PoolManager { public static void Clear() { } }
