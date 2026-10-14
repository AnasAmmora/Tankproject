using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
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
    static T Get<T>(object obj, string field) => (T)obj.GetType().GetField(field, Private).GetValue(obj);
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
        foreach (string field in new[] { "timer", "lifeStatus", "resultTitle" }) Set(hud, field, Component<TMP_Text>());
        Set(hud, "blueRows", new GameObject("BlueRows").transform);
        Set(hud, "redRows", new GameObject("RedRows").transform);
        Set(hud, "rowPrefab", Row());
        Set(hud, "resultsPanel", new GameObject("Results")); Set(hud, "back", Component<Button>());
        Set(hud, "sceneEventSystem", Component<EventSystem>());
        return hud;
    }
    static ResultRow Row()
    {
        var row = new GameObject("ResultRow", typeof(ResultRow)).GetComponent<ResultRow>();
        foreach (string field in new[] { "playerName", "kills", "deaths" }) Set(row, field, Component<TMP_Text>());
        foreach (string field in new[] { "evenBackground", "oddBackground", "localMarker" }) Set(row, field, new GameObject(field));
        Call(row, "Awake");
        return row;
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
        UnityEngine.Object.PrefabFactory = (template, parent) => { var row = Row(); row.transform.parent = parent; return row; };
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
        network.IsConnectedClient = true;
        network.LocalClient.PlayerObject = local.root.GetComponent<NetworkObject>();
        var result = new MatchResults { winner = 0, players = new[] { new MatchRules.PlayerRecord { name = "A", team = 0, kills = 2, deaths = 1 } } };
        MatchResults.Store(JsonUtility.ToJson(result)); hud.ShowFinal();
        Assert(MatchHud.ResultsVisible && !local.canvas.gameObject.activeSelf, "Showing Results immediately hides local PlayerCanvas");
        Assert(local.root.activeSelf && local.ui.enabled && local.ui.IsSpawned, "Player and network behaviour stay active");
        var blue = Get<Dictionary<ulong, ResultRow>>(hud, "blueEntries");
        var red = Get<Dictionary<ulong, ResultRow>>(hud, "redEntries");
        Assert(blue.Count == 1 && red.Count == 0, "Actual snapshot determines row count");
        ResultRow first = blue[0];
        Assert(Get<TMP_Text>(first,"playerName").text == "A" && Get<TMP_Text>(first,"kills").text == "2" &&
            Get<TMP_Text>(first,"deaths").text == "1", "Row cells contain real player stats");
        Assert(Get<GameObject>(first,"localMarker").activeSelf, "Local player highlighted by actual client ID");
        hud.ShowFinal(); Assert(blue.Count == 1 && blue[0] == first, "Repeated result presentation reuses row");
        result.players = new[] {
            new MatchRules.PlayerRecord { clientId=0, name="A", team=0, ordinal=0, kills=3, deaths=2 },
            new MatchRules.PlayerRecord { clientId=1, name="B", team=1, ordinal=1 },
            new MatchRules.PlayerRecord { clientId=2, name="C", team=0, ordinal=2 },
            new MatchRules.PlayerRecord { clientId=3, name="D", team=1, ordinal=3 }
        };
        MatchResults.Store(JsonUtility.ToJson(result)); hud.ShowFinal();
        Assert(blue.Count == 2 && red.Count == 2 && blue[0] == first, "Four-player update adds only missing rows");
        Assert(Get<TMP_Text>(first,"kills").text == "3" && Get<TMP_Text>(first,"deaths").text == "2", "Existing row stats refresh");
        Assert(Get<GameObject>(blue[0],"evenBackground").activeSelf && Get<GameObject>(blue[2],"oddBackground").activeSelf,
            "Rows alternate by stable ordinal order");
        Assert(!Get<GameObject>(blue[2],"localMarker").activeSelf && !Get<GameObject>(red[1],"localMarker").activeSelf,
            "Only local record is highlighted");
        network.IsConnectedClient = false; network.LocalClientId = 77;
        result.players = new[] { result.players[0], result.players[1] };
        MatchResults.Store(JsonUtility.ToJson(result)); hud.ShowFinal();
        Assert(blue.Count == 1 && red.Count == 1, "Removed records do not leave stale rows");
        Assert(Get<GameObject>(first,"localMarker").activeSelf, "Highlight survives network shutdown ID reset");
        result.players = new[] { result.players[0], result.players[0], result.players[1] };
        MatchResults.Store(JsonUtility.ToJson(result)); hud.ShowFinal();
        Assert(blue.Count == 1 && red.Count == 1, "Duplicate snapshot records cannot duplicate rows");
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
        Assert(blue.Count == 0 && red.Count == 0, "Old match clears row bookkeeping");
        MatchResults.Clear();
        var nextHud = Hud(); Call(nextHud, "Awake"); var nextPlayer = Player(true);
        Assert(MatchHud.Instance == nextHud && !MatchHud.ResultsVisible && nextPlayer.canvas.gameObject.activeSelf, "Next match starts with fresh HUD and visible local UI");
        Assert(Get<Dictionary<ulong,ResultRow>>(nextHud,"blueEntries").Count == 0, "New match has no old result rows");
        Call(nextHud, "OnDestroy"); Call(screen, "OnDestroy");
        var missing = Loading(); Set(missing, "status", null); Call(missing, "Awake");
        Assert(!missing.IsConfigured && Debug.Messages.Any(m => m.Contains("missing Inspector reference 'status'")), "Missing reference is reported without creating UI");
        Console.WriteLine($"PASS: {checks} UI lifecycle assertions (simulated engine boundary).");
    }
}
