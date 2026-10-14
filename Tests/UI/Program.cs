using System;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

static class Program
{
    static int checks;
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Assert(bool condition, string reason) { checks++; if (!condition) throw new Exception(reason); }
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, Private).SetValue(obj, value);
    static void Call(object obj, string method, params object[] args) => obj.GetType().GetMethod(method, Private).Invoke(obj, args);
    static T Component<T>() where T : Component => new GameObject("authored", typeof(T)).GetComponent<T>();
    static LoadingScreenController Loading()
    {
        var root = new GameObject("LoadingCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(LoadingScreenController));
        var screen = root.GetComponent<LoadingScreenController>();
        Set(screen, "overlay", root.GetComponent<Canvas>());
        Set(screen, "inputBlocker", root.GetComponent<GraphicRaycaster>());
        Set(screen, "status", Component<TMP_Text>()); Set(screen, "returnButton", Component<Button>());
        return screen;
    }
    static MatchHud Hud()
    {
        var root = new GameObject("MatchCanvas", typeof(Canvas), typeof(GraphicRaycaster), typeof(MatchHud));
        var hud = root.GetComponent<MatchHud>();
        Set(hud, "matchCanvas", root.GetComponent<Canvas>());
        Set(hud, "uiRaycaster", root.GetComponent<GraphicRaycaster>());
        foreach (string field in new[] { "timer", "lifeStatus", "resultTitle", "firstTeam", "secondTeam" }) Set(hud, field, Component<TMP_Text>());
        Set(hud, "resultsPanel", new GameObject("Results")); Set(hud, "back", Component<Button>());
        Set(hud, "sceneEventSystem", Component<EventSystem>());
        return hud;
    }
    static (PlayerCanvasController ui, Canvas canvas, GameObject root) Player(bool owner)
    {
        var root = new GameObject("Player", typeof(PlayerCanvasController), typeof(NetworkObject));
        var ui = root.GetComponent<PlayerCanvasController>();
        var canvas = Component<Canvas>(); canvas.transform.parent = root.transform;
        Set(ui, "playerCanvas", canvas); Call(ui, "Awake");
        ui.IsSpawned = true; ui.IsOwner = owner; ui.OnNetworkSpawn();
        return (ui, canvas, root);
    }
    static void Main()
    {
        var screen = Loading(); Call(screen, "Awake");
        Assert(screen.IsConfigured && LoadingScreenController.Instance == screen, "Authored loading singleton registered");
        Assert(screen.gameObject.activeSelf && !screen.GetComponent<Canvas>().enabled, "Hide keeps root/controller alive");
        screen.Show("Connecting"); Assert(screen.GetComponent<Canvas>().enabled && screen.GetComponent<GraphicRaycaster>().enabled, "Loading blocks input when shown");
        var duplicate = Loading(); Call(duplicate, "Awake");
        Assert(LoadingScreenController.Instance == screen && !duplicate.GetComponent<Canvas>().enabled, "Duplicate loading instance never replaces visible one");
        Call(duplicate, "OnDestroy");
        screen.Show("Waiting for players"); screen.Hide();
        Assert(!screen.GetComponent<GraphicRaycaster>().enabled, "Hidden loading does not block recovered menu");

        MatchResults.Clear();
        var hud = Hud(); Call(hud, "Awake");
        Assert(!MatchHud.ResultsVisible, "No results during initial preparation");
        var local = Player(true); var remote = Player(false);
        Assert(local.canvas.gameObject.activeSelf && !remote.canvas.gameObject.activeSelf, "Only owner gets PlayerCanvas");
        var network = NetworkManager.Singleton = new NetworkManager();
        network.LocalClient.PlayerObject = local.root.GetComponent<NetworkObject>();
        var result = new MatchResults { winner = 0, players = new[] { new MatchRules.PlayerRecord { name = "A", team = 0, kills = 2, deaths = 1 } } };
        MatchResults.Store(JsonUtility.ToJson(result)); hud.ShowFinal();
        Assert(MatchHud.ResultsVisible && !local.canvas.gameObject.activeSelf, "Showing Results immediately hides local PlayerCanvas");
        Assert(local.root.activeSelf && local.ui.enabled && local.ui.IsSpawned, "Player and network behaviour stay active");
        var late = Player(true);
        Assert(!late.canvas.gameObject.activeSelf, "Player spawned after Results never shows UI");
        late.canvas.gameObject.SetActive(true); Call(late.ui, "LateUpdate");
        Assert(!late.canvas.gameObject.activeSelf, "Late UI activation is corrected from retained results");
        var secondHud = Hud(); Call(secondHud, "Awake");
        Assert(MatchHud.Instance == hud && !secondHud.GetComponent<Canvas>().enabled, "Only one MatchCanvas renders");
        Call(secondHud, "OnDestroy");
        Call(hud, "OnSceneLoaded", new Scene { name = "MainMenuScene" }, LoadSceneMode.Single);
        Assert(!hud.GetComponent<Canvas>().enabled, "Returning menu hides old MatchCanvas before destroy");
        Assert(!hud.GetComponent<GraphicRaycaster>().enabled, "Old results cannot block menu input during deferred destroy");
        Call(hud, "OnDestroy"); Assert(MatchHud.Instance == null, "Returning menu releases HUD singleton");
        MatchResults.Clear();
        var nextHud = Hud(); Call(nextHud, "Awake"); var nextPlayer = Player(true);
        Assert(MatchHud.Instance == nextHud && !MatchHud.ResultsVisible && nextPlayer.canvas.gameObject.activeSelf, "Next match starts with fresh HUD and visible local UI");
        Call(nextHud, "OnDestroy"); Call(screen, "OnDestroy");
        var missing = Loading(); Set(missing, "status", null); Call(missing, "Awake");
        Assert(!missing.IsConfigured && Debug.Messages.Any(m => m.Contains("missing Inspector reference 'status'")), "Missing reference is reported without creating UI");
        Console.WriteLine($"PASS: {checks} UI lifecycle assertions (simulated engine boundary).");
    }
}
