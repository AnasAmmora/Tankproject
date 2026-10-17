using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Read-only Editor check: deserialize the real assets, not hand-built test doubles.
public static class HudAssetAudit
{
    private const string Base = "Assets/ProjectsFiles/Prefabes/";

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("HudAssetAudit.v1", false)) return;
            SessionState.SetBool("HudAssetAudit.v1", true);
            Run();
        };
    }

    [MenuItem("Tools/TankProject/Audit authored gameplay HUD")]
    public static void Run() => Audit();

    // Explicit Editor operation: save the existing authored player, unload it,
    // force import, and load it again to verify what is actually on disk.
    [MenuItem("Tools/TankProject/Save and reopen authored player HUD")]
    public static void SaveAndReopen()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("SaveAndReopen must run outside Play Mode.");
        string path = Base + "Player.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var canvas = contents.transform.Find("PlayerCanvas");
            if (canvas == null || canvas.childCount != 4 || contents.GetComponent<PlayerUI>() == null)
                throw new InvalidOperationException("Player HUD has not been authored; refusing to save an incomplete prefab.");
            canvas.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(contents, path, out bool success);
            if (!success) throw new InvalidOperationException("Unity failed to save Player.prefab.");
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var reopened = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Debug.Log($"[HudAssetAudit] Saved and reopened {path}; PlayerUI count={reopened.GetComponents<PlayerUI>().Length}, PlayerCanvas children={reopened.transform.Find("PlayerCanvas").childCount}.");
        }
        finally { PrefabUtility.UnloadPrefabContents(reopened); }
        if (Audit() != 0) throw new InvalidOperationException("Saved HUD failed its reference audit; see [HudAssetAudit] errors.");
    }

    private static int Audit()
    {
        int failures = 0;
        Action<bool, string> check = (ok, message) =>
        {
            if (ok) return;
            failures++;
            Debug.LogError("[HudAssetAudit] " + message);
        };
        var player = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "Player.prefab");
        var match = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "UI/MatchCanvas.prefab");
        check(player != null && match != null, "Player or MatchCanvas prefab failed to import.");
        if (player == null || match == null) return failures;
        var manager = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "Managers/NetworkManager.prefab");
        var network = manager != null ? manager.GetComponent<Unity.Netcode.NetworkManager>() : null;
        check(network != null && network.NetworkConfig.PlayerPrefab == player,
            "NetworkManager.PlayerPrefab does not point to the audited Player asset.");
        foreach (var root in new[] { player, match })
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0,
                    root.name + "/" + t.name + ": missing script.");
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                check(text.font != null, root.name + "/" + text.name + ": missing TMP font asset.");
        }
        var canvas = player.transform.Find("PlayerCanvas");
        check(canvas != null && canvas.childCount >= 4 && canvas.localScale != Vector3.zero,
            "PlayerCanvas is empty or has zero scale.");
        CheckFields(player.GetComponent<PlayerUI>(), check, "health", "shooter", "state", "combat", "abilitySlot",
            "reload", "weaponReady", "healthFill", "healthText", "lowHealth", "confirmation", "hitMarker", "killMarker", "killText");
        CheckFields(player.GetComponent<PlayerCanvasController>(), check, "playerCanvas");
        foreach (var indicator in player.GetComponentsInChildren<DamageIndicatorView>(true))
            CheckFields(indicator, check, "bearing", "fade");
        var ui = player.GetComponent<PlayerUI>();
        if (ui != null)
        {
            var array = new SerializedObject(ui).FindProperty("damageIndicators");
            check(array != null && array.arraySize == 3 && Enumerable.Range(0, array.arraySize)
                .All(i => array.GetArrayElementAtIndex(i).objectReferenceValue != null), "PlayerUI.damageIndicators must reference three authored indicators.");
        }
        foreach (var image in player.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            if (new[] { "ReloadTrack", "ReloadProgress", "WeaponReady", "HealthFill", "Arrow", "HitMarker", "KillMarker" }.Contains(image.name))
                check(image.sprite != null, "PlayerCanvas/" + image.name + ": missing authored sprite.");
        CheckFields(match.GetComponent<GameplayHud>(), check, "gameplayRoot", "timer", "phaseLabel", "lifeStatus",
            "timerAccent", "baseNotice", "overtimeNotice", "teammatePanel", "teammateRows", "teammatePrefab", "feedRows", "feedPrefab");
        CheckFields(match.GetComponent<MatchHud>(), check, "gameplayHud", "resultsPanel", "back");
        var teammate = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "UI/TeammateRow.prefab");
        var feed = AssetDatabase.LoadAssetAtPath<GameObject>(Base + "UI/KillFeedRow.prefab");
        CheckFields(teammate != null ? teammate.GetComponent<TeammateRow>() : null, check, "playerName", "healthText", "lifeText", "healthFill");
        CheckFields(feed != null ? feed.GetComponent<KillFeedRow>() : null, check, "killer", "action", "victim", "fade");
        Debug.Log($"[HudAssetAudit] Completed real prefab import/reference audit: failures={failures}, PlayerCanvas children={(canvas != null ? canvas.childCount : 0)}. This is NOT a Play Mode or multiplayer test.");
        return failures;
    }

    private static void CheckFields(Behaviour component, Action<bool, string> check, params string[] names)
    {
        check(component != null, "Missing component for fields: " + string.Join(", ", names));
        if (component == null) return;
        check(component.enabled, component.GetType().Name + " is disabled in the prefab.");
        var serialized = new SerializedObject(component);
        foreach (string name in names)
        {
            var property = serialized.FindProperty(name);
            check(property != null && property.objectReferenceValue != null,
                component.GetType().Name + "." + name + " is missing.");
        }
    }
}
