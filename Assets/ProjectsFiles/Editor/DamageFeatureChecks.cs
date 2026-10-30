using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Invoked explicitly through Unity MCP. Synthetic contacts exercise the production
// server/NGO/feedback/pool path; they do not constitute remote-client or PhysX testing.
public static class DamageFeatureChecks
{
    private const string Root = "Assets/ProjectsFiles/";
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Active = "DamageFeatureChecks.Active";
    private const string Restore = "DamageFeatureChecks.Restore";
    private const string Result = "DamageFeatureChecks.Result";
    private static int checks, step;
    private static double deadline;
    private static NetworkManager network;

    private static void Require(bool condition, string message)
    {
        checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Hidden).GetValue(owner);

    public static string RunAssetChecks()
    {
        checks = 0;
        var profile = AssetDatabase.LoadAssetAtPath<DamageFalloff>(Root + "Settings/StandardShellDamage.asset");
        Require(profile != null, "Saved damage profile missing");
        Require(profile.Calculate(0) == 25 && profile.Calculate(20) == 25, "Full-strength range");
        Require(profile.Calculate(35) == 23 && profile.Calculate(50) == 18 && profile.Calculate(65) == 12, "Intermediate smoothstep values and half-up rounding");
        Require(profile.Calculate(80) == 10 && profile.Calculate(1000) == 10, "Far minimum plateau");
        Require(profile.Calculate(new Vector3(100, 0, 0), new Vector3(150, 0, 0)) == 18, "Launch-to-impact distance");
        int previous = 25;
        for (int distance = 0; distance <= 200; distance++)
        {
            int damage = profile.Calculate(distance);
            Require(damage >= 10 && damage <= previous, "Bounded monotonic damage at " + distance);
            previous = damage;
        }
        Require(profile.Calculate(float.NaN) == 0 && profile.Calculate(float.PositiveInfinity) == 10, "Non-finite distance handling");
        var malformed = ScriptableObject.CreateInstance<DamageFalloff>();
        try
        {
            typeof(DamageFalloff).GetField("baseDamage", Hidden).SetValue(malformed, -1);
            typeof(DamageFalloff).GetField("fullDamageDistance", Hidden).SetValue(malformed, float.MaxValue);
            typeof(DamageFalloff).GetField("maximumFalloffDistance", Hidden).SetValue(malformed, -5f);
            typeof(DamageFalloff).GetField("minimumDamageMultiplier", Hidden).SetValue(malformed, float.NaN);
            Require(malformed.Calculate(100000f) == 0, "Malformed runtime configuration is safe");
            typeof(DamageFalloff).GetMethod("OnValidate", Hidden).Invoke(malformed, null);
            Require(Field<float>(malformed, "maximumFalloffDistance") > Field<float>(malformed, "fullDamageDistance"), "Validation fixes reversed/degenerate range");
            typeof(DamageFalloff).GetField("baseDamage", Hidden).SetValue(malformed, int.MaxValue);
            typeof(DamageFalloff).GetField("minimumDamageMultiplier", Hidden).SetValue(malformed, 1f);
            Require(malformed.Calculate(0) == int.MaxValue, "Maximum integer damage cannot overflow rounding");
            typeof(DamageFalloff).GetField("baseDamage", Hidden).SetValue(malformed, 1);
            typeof(DamageFalloff).GetField("minimumDamageMultiplier", Hidden).SetValue(malformed, 0f);
            Require(malformed.Calculate(1000000f) == 0, "Zero minimum permits zero far damage");
        }
        finally { Object.DestroyImmediate(malformed); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabes/UI/FloatingDamageNumber.prefab");
        var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabes/Bullet.prefab");
        var hud = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabes/UI/MatchCanvas.prefab").GetComponent<GameplayHud>();
        Require(prefab != null && prefab.GetComponent<FloatingDamageNumber>().ValidateReferences(), "Saved floating number label reference");
        Require(prefab.GetComponentInChildren<TextMeshPro>().font != null, "Saved 3D TMP font");
        Require(prefab.GetComponentsInChildren<NetworkObject>(true).Length == 0 && prefab.GetComponentsInChildren<Canvas>(true).Length == 0, "Number is local world text without Canvas or NetworkObject");
        Require(Field<FloatingDamageNumber>(hud, "floatingDamageNumberPrefab") == prefab.GetComponent<FloatingDamageNumber>(), "Saved MatchCanvas prefab reference");
        Require(Field<DamageFalloff>(bullet.GetComponent<Bullet>(), "damageFalloff") == profile, "Saved Bullet profile reference");
        var scene = EditorSceneManager.OpenPreviewScene(Root + "Scenes/GameScene.unity");
        try
        {
            var hudInstances = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameplayHud>(true)).ToArray();
            Require(hudInstances.Length == 1 && Field<FloatingDamageNumber>(hudInstances[0], "floatingDamageNumberPrefab") == prefab.GetComponent<FloatingDamageNumber>(), "GameScene's single authored MatchCanvas inherits the reference");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        string result = "PASS: " + checks + " Unity damage/configuration/saved-asset assertions.";
        Debug.Log(result);
        return result;
    }

    public static string StartHostSmoke()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Editor must be idle");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        if (setup.Length != 1 || string.IsNullOrEmpty(setup[0].path) || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Smoke test requires one saved, clean scene so it can restore the workspace");
        SessionState.SetString(Restore, setup[0].path);
        SessionState.SetString(Result, "Running");
        EditorSceneManager.OpenScene(Root + "Scenes/GameScene.unity");
        SessionState.SetBool(Active, true);
        Resume();
        EditorApplication.EnterPlaymode();
        return "Started local-host smoke check; query DamageFeatureChecks.LastResult after Play Mode exits.";
    }
    public static string LastResult => SessionState.GetString(Result, "Not run");

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (SessionState.GetBool(Active, false))
        {
            step = checks = 0;
            deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
        else if (!string.IsNullOrEmpty(SessionState.GetString(Restore, "")))
            EditorApplication.delayCall += RestoreScene;
    }
    private static void RestoreScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string path = SessionState.GetString(Restore, "");
        SessionState.EraseString(Restore);
        if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Host smoke timed out at step " + step);
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (step == 0)
            {
                MatchResults.Clear();
                var obj = new GameObject("Damage test network");
                network = obj.AddComponent<NetworkManager>();
                network.NetworkConfig = new NetworkConfig();
                var transport = obj.AddComponent<UnityTransport>();
                network.NetworkConfig.NetworkTransport = transport;
                network.NetworkConfig.EnableSceneManagement = false;
                transport.SetConnectionData("127.0.0.1", 0, "127.0.0.1");
                Require(network.StartHost(), "Real NGO StartHost");
                step++;
                return;
            }
            if (!network.IsConnectedClient || MatchManager.Instance == null || !MatchManager.Instance.IsSpawned) return;
            RunHostAssertions();
            Finish("PASS: " + checks + " actual Unity/NGO local-host damage, feedback and visual-pool assertions. Contacts were synthetic; no remote client was connected.");
        }
        catch (Exception e) { Finish("FAIL: " + e); }
    }

    private static void RunHostAssertions()
    {
        var match = MatchManager.Instance;
        var rules = Field<MatchRules>(match, "rules");
        rules.Register(network.LocalClientId, 100, "Host shooter", 0, 0);
        rules.Register(1, 101, "Fixture opponent", 1, 1);
        rules.Start(network.ServerTime.Time, 300);
        match.phase.Value = (int)MatchRules.Stage.Regulation;
        var cameraObject = new GameObject("Damage test camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        var view = cameraObject.GetComponent<Camera>();
        view.transform.position = new Vector3(0, 4, -10);
        var hud = Object.FindAnyObjectByType<GameplayHud>();
        typeof(GameplayHud).GetMethod("BindMatch", Hidden).Invoke(hud, null);
        var feedback = new List<CombatFeedback>();
        match.CombatConfirmed += feedback.Add;
        var target = Object.FindObjectsByType<TeamBase>().Single(b => b.teamIndex == 1);
        var friendly = Object.FindObjectsByType<TeamBase>().Single(b => b.teamIndex == 0);
        target.currentHealth.Value = 1000; // Isolate range checks from the scene's low-health base tuning.
        var collider = target.GetComponentInChildren<Collider>();
        Require(collider != null, "Base fixture collider");
        var bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabes/Bullet.prefab");
        var definition = bulletPrefab.GetComponent<Bullet>();
        Vector3 point = collider.bounds.center;
        uint shot = 0;
        Bullet previous = null;
        Action<float, Collider> contact = (distance, hit) =>
        {
            uint id = ++shot;
            Vector3 firingPosition = point - Vector3.forward * distance;
            match.Projectiles.Register(100, id, definition.ImpactEffectPrefab, definition.EffectLifetime, null);
            var projectile = PoolManager.Get(bulletPrefab, firingPosition, Quaternion.identity).GetComponent<Bullet>();
            if (previous != null) Require(previous == projectile, "Projectile pool reused the instance");
            projectile.Launch(Vector3.forward, Array.Empty<Collider>(), 0, 100, id, false);
            projectile.transform.position = point; // Later flight position must not change the captured origin.
            typeof(Bullet).GetMethod("HandleImpact", Hidden).Invoke(projectile, new object[] { hit, point, Vector3.back, Vector3.forward });
            if (projectile.gameObject.activeSelf) projectile.ReleaseIfShot(100, id);
            Require(Field<Vector3>(projectile, "sourcePosition") == Vector3.zero, "Returned projectile clears firing position");
            previous = projectile;
        };
        contact(5, collider); contact(50, collider); contact(120, collider);
        Require(feedback.Select(f => f.AppliedDamage).SequenceEqual(new[] { 25, 18, 10 }),
            "Near/intermediate/far server-confirmed damage: received=[" + string.Join(",", feedback.Select(f => f.AppliedDamage)) +
            "], base HP=" + target.currentHealth.Value + ", server=" + target.IsServer + ", running=" + rules.Running);
        Require(feedback.All(f => f.CanShowDamageNumber(network.LocalClientId)), "Host receives its own targeted confirmation");
        int before = feedback.Count;
        contact(5, friendly.GetComponentInChildren<Collider>());
        Require(feedback.Count == before, "Friendly projectile produces no damage confirmation");
        var wall = new GameObject("Damage test wall", typeof(BoxCollider));
        contact(5, wall.GetComponent<Collider>());
        Require(feedback.Count == before, "Blocked projectile produces no damage confirmation");
        for (int i = 0; i < 10; i++) contact(5, collider);
        Require(feedback.Count == 13 && feedback.Select(f => f.Sequence).Distinct().Count() == 13, "Rapid hits keep distinct confirmation identities");
        target.currentHealth.Value = 6;
        contact(50, collider);
        Require(feedback.Last().AppliedDamage == 6 && target.currentHealth.Value == 0, "Lethal base hit reports remaining health");
        Require(!target.TakeDamage(25, 0, 100, point) && feedback.Count == 14, "Repeated callback after destruction cannot confirm twice");
        var active = Field<List<FloatingDamageNumber>>(hud, "damageNumbers");
        Require(active.Count == 14 && active.Distinct().Count() == 14 && active.All(n => n.gameObject.activeSelf), "Fourteen simultaneous local numbers are independent");
        Require(Field<TMP_Text>(active.Last(), "label").text == "6", "Actual overkill-adjusted value reaches TMP");

        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabes/UI/FloatingDamageNumber.prefab");
        var visual = PoolManager.Get(visualPrefab, Vector3.zero, Quaternion.identity).GetComponent<FloatingDamageNumber>();
        visual.Show(9, Vector3.zero, view);
        Vector3 initial = visual.transform.position;
        float initialScale = visual.transform.localScale.x;
        visual.Tick(0.08f, view);
        Require(visual.transform.position.y > initial.y && visual.transform.localScale.x > initialScale, "Text rises and pops");
        Require(Quaternion.Angle(visual.transform.rotation, view.transform.rotation) < 0.01f, "Text faces the local camera");
        visual.Tick(0.6f, view);
        Require(Field<TMP_Text>(visual, "label").alpha < 0.5f, "Text fades");
        Require(!visual.Tick(1f, view), "Animation completes");
        PoolManager.Release(visual.gameObject);
        Require(Field<TMP_Text>(visual, "label").text == "", "Pool release clears old text");
        var reused = PoolManager.Get(visualPrefab, Vector3.zero, Quaternion.identity).GetComponent<FloatingDamageNumber>();
        Require(reused == visual, "Visual pool reuses instance");
        reused.Show(3, Vector3.zero, view);
        Require(Field<TMP_Text>(reused, "label").text == "3" && Field<TMP_Text>(reused, "label").alpha == 1f && Field<float>(reused, "elapsed") == 0f, "Visual reuse resets text/alpha/timer");
        PoolManager.Release(reused.gameObject);
        hud.HideForResults();
        Require(active.Count == 0, "Results release every active number");
        match.CombatConfirmed -= feedback.Add;
    }
    private static void Finish(string result)
    {
        SessionState.SetString(Result, result);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        if (result.StartsWith("FAIL")) Debug.LogError(result); else Debug.Log(result);
        if (network != null) network.Shutdown();
        EditorApplication.playModeStateChanged += OnExited;
        EditorApplication.ExitPlaymode();
    }
    private static void OnExited(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= OnExited;
        RestoreScene();
    }
}
