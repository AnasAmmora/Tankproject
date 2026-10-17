using System;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Batch-only smoke check of the actual saved scene/prefabs. This deliberately
// bypasses Lobby/Relay and injects server state; it is not a multiplayer match test.
public static class HudPlayModeCheck
{
    private const string Active = "TankHudPlayModeCheck.Active";
    private static NetworkManager network;
    private static PlayerHealth health;
    private static TankShooter shooter;
    private static Canvas canvas;
    private static MatchManager match;
    private static int step;
    private static double due;
    private static double deadline;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate batch Editor; it opens GameScene without saving scene changes.");
        EditorSceneManager.OpenScene("Assets/ProjectsFiles/Scenes/GameScene.unity");
        SessionState.SetBool(Active, true);
        Resume();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        if (!SessionState.GetBool(Active, false)) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("HUD Play Mode check timed out at step " + step);
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
            switch (step)
            {
                case 0:
                    MatchResults.Clear();
                    var networkAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ProjectsFiles/Prefabes/Managers/NetworkManager.prefab");
                    network = UnityEngine.Object.Instantiate(networkAsset).GetComponent<NetworkManager>();
                    network.NetworkConfig.EnableSceneManagement = false;
                    network.NetworkConfig.ConnectionApproval = true;
                    network.ConnectionApprovalCallback = (request, response) =>
                    { response.Approved = true; response.CreatePlayerObject = false; response.Pending = false; };
                    network.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 0, "127.0.0.1");
                    Require(network.StartHost(), "StartHost returned false");
                    Next();
                    break;
                case 1:
                    if (!network.IsConnectedClient || MatchManager.Instance == null) return;
                    match = MatchManager.Instance;
                    var player = UnityEngine.Object.Instantiate(network.NetworkConfig.PlayerPrefab);
                    player.GetComponent<NetworkObject>().SpawnAsPlayerObject(network.LocalClientId);
                    health = player.GetComponent<PlayerHealth>();
                    shooter = player.GetComponent<TankShooter>();
                    player.GetComponent<PlayerState>().teamIndex.Value = 0;
                    player.GetComponent<PlayerState>().isPrepared.Value = true;
                    // Presentation fixture only: server NetworkVariables are the UI's real
                    // inputs. We do not claim to have exercised rules/readiness or damage.
                    match.phase.Value = (int)MatchRules.Stage.Regulation;
                    match.phaseEndsAt.Value = network.ServerTime.Time + 120;
                    canvas = player.transform.Find("PlayerCanvas").GetComponent<Canvas>();
                    Next();
                    break;
                case 2:
                    if (!health.GetComponent<PlayerLook>().LocalCameraReady) return;
                    Require(canvas.gameObject.activeInHierarchy && health.IsOwner, "Owner canvas did not become visible");
                    Require(health.GetComponent<PlayerUI>().enabled, "PlayerUI disabled itself");
                    Require(At<TMP_Text>("PlayerHealth/HealthValue").text == "100 / 100", "Initial health was not read");
                    Require(At<RectTransform>("Combat").gameObject.activeSelf && shooter.CanFire, "Ready combat HUD was not shown");
                    Require(!At<Image>("Combat/ReloadProgress").gameObject.activeSelf, "ReloadProgress is visible before reloading");
                    Require(!At<Image>("Combat/WeaponReady").gameObject.activeSelf, "WeaponReady is visible on initial spawn");
                    typeof(TankShooter).GetMethod("TryFire", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shooter, null);
                    Require(shooter.CooldownRemaining > 0 && !shooter.CanFire, "Actual shot did not start weapon cooldown");
                    Require(At<Image>("Combat/ReloadProgress").fillAmount < 1, "Reload ring did not react to the actual shot");
                    Require(At<Image>("Combat/ReloadProgress").gameObject.activeSelf, "ReloadProgress did not show on the shot event");
                    Require(!At<Image>("Combat/WeaponReady").gameObject.activeSelf, "WeaponReady stayed visible during reload");
                    var ui = health.GetComponent<PlayerUI>();
                    ui.enabled = false;
                    ui.enabled = true;
                    Require(At<Image>("Combat/ReloadProgress").gameObject.activeSelf, "Re-enable did not restore active reload visibility");
                    Debug.Log("[HudPlayModeCheck] PASS owner HUD, initial health and actual weapon cooldown.");
                    Next(0.8);
                    break;
                case 3:
                    Require(shooter.CanFire && At<Image>("Combat/ReloadProgress").fillAmount > 0.99f, "Reload ring did not finish with the weapon");
                    Require(!At<Image>("Combat/ReloadProgress").gameObject.activeSelf, "ReloadProgress stayed visible after cooldown completion");
                    Require(At<Image>("Combat/WeaponReady").gameObject.activeSelf, "Successful cooldown did not show WeaponReady");
                    health.GetComponent<PlayerUI>().enabled = false;
                    health.GetComponent<PlayerUI>().enabled = true;
                    Require(!At<Image>("Combat/ReloadProgress").gameObject.activeSelf, "Re-enable showed a finished reload");
                    Require(!At<Image>("Combat/WeaponReady").gameObject.activeSelf, "Re-enable replayed the completion animation");
                    health.currentHealth.Value = 75;
                    Next();
                    break;
                case 4:
                    Require(At<TMP_Text>("PlayerHealth/HealthValue").text == "75 / 100", "Health change did not update text");
                    Require(Mathf.Abs(At<Image>("PlayerHealth/HealthTrack/HealthFill").fillAmount - 0.75f) < 0.01f, "Health bar did not reach the replicated value");
                    health.currentHealth.Value = 0;
                    health.isDead.Value = true;
                    health.respawnAt.Value = network.ServerTime.Time + 10;
                    Next();
                    break;
                case 5:
                    Require(canvas.gameObject.activeInHierarchy, "Death hid required life presentation");
                    Require(!At<RectTransform>("Combat").gameObject.activeSelf && !At<RectTransform>("SpecialAbilityUnavailable").gameObject.activeSelf,
                        "Death did not hide combat-only widgets");
                    health.RestoreOnServer();
                    Next();
                    break;
                case 6:
                    Require(canvas.gameObject.activeInHierarchy && At<RectTransform>("Combat").gameObject.activeSelf, "Combat HUD did not return after RestoreOnServer");
                    Require(At<TMP_Text>("PlayerHealth/HealthValue").text == "100 / 100" && At<Image>("PlayerHealth/HealthTrack/HealthFill").fillAmount > 0.99f,
                        "Respawn state did not reset health display");
                    Debug.Log("[HudPlayModeCheck] PASS health changes, dead presentation and RestoreOnServer presentation.");
                    MatchResults.Store(JsonUtility.ToJson(new MatchResults { winner = 0, players = new[] {
                        new MatchRules.PlayerRecord { clientId=network.LocalClientId, team=0, name="HUD check", ordinal=0 }
                    }}));
                    Next();
                    break;
                case 7:
                    Require(MatchHud.ResultsVisible && !canvas.gameObject.activeSelf, "Results did not hide the owner canvas");
                    Require(!GameplayHud.Instance.transform.Find("Gameplay").gameObject.activeSelf, "Match overlays still cover Results");
                    Require(health.gameObject.activeInHierarchy && health.IsSpawned, "Hiding results UI disabled the network player");
                    Debug.Log("[HudPlayModeCheck] PASS Results hides player/general HUD without disabling the network player. Local Host-only presentation check; no Relay/client or combat-hit verification.");
                    Finish(0);
                    break;
            }
        }
        catch (Exception error)
        {
            Debug.LogError("[HudPlayModeCheck] FAIL step=" + step + " " + error);
            Finish(1);
        }
    }

    private static T At<T>(string path) where T : Component
    {
        var target = canvas.transform.Find(path);
        if (target == null) throw new InvalidOperationException("Missing saved HUD path: PlayerCanvas/" + path);
        return target.GetComponent<T>();
    }
    private static void Require(bool success, string message) { if (!success) throw new InvalidOperationException(message); }
    private static void Next(double wait = 0.4) { step++; due = EditorApplication.timeSinceStartup + wait; }
    private static void Finish(int exitCode)
    {
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Tick;
        if (network != null && network.IsListening) network.Shutdown();
        EditorApplication.Exit(exitCode);
    }
}
