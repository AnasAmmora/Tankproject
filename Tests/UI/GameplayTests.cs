using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

static partial class Program
{
    static TeammateRow Mate()
    {
        var row = Component<TeammateRow>();
        foreach (string field in new[] { "playerName", "healthText", "lifeText" }) Set(row,field,Component<TMP_Text>());
        Set(row,"healthFill",Component<Image>());
        return row;
    }
    static KillFeedRow Feed()
    {
        var row=Component<KillFeedRow>();
        foreach(string field in new[]{"killer","action","victim"})Set(row,field,Component<TMP_Text>());
        Set(row,"fade",Component<CanvasGroup>());return row;
    }
    static GameplayHud Gameplay()
    {
        var hud=Component<GameplayHud>();
        foreach(string field in new[]{"gameplayRoot","teammatePanel"})Set(hud,field,new GameObject(field));
        foreach(string field in new[]{"timer","phaseLabel","lifeStatus"})Set(hud,field,Component<TMP_Text>());
        foreach(string field in new[]{"timerAccent","baseNotice","overtimeNotice"})Set(hud,field,Component<CanvasGroup>());
        Set(hud,"teammateRows",new GameObject().transform);Set(hud,"feedRows",new GameObject().transform);
        Set(hud,"teammatePrefab",Mate());Set(hud,"feedPrefab",Feed());Call(hud,"Awake");return hud;
    }
    static PlayerUI Owner()
    {
        var actor=new GameObject("Owner",typeof(PlayerUI),typeof(PlayerHealth),typeof(PlayerState),typeof(TankShooter));
        var ui=actor.GetComponent<PlayerUI>();
        var health=actor.GetComponent<PlayerHealth>();health.IsOwner=health.IsSpawned=true;health.OwnerClientId=0;
        Set(ui,"health",health);Set(ui,"state",actor.GetComponent<PlayerState>());Set(ui,"shooter",actor.GetComponent<TankShooter>());
        foreach(string field in new[]{"combat","abilitySlot","weaponReady","hitMarker","killMarker"})Set(ui,field,new GameObject(field));
        foreach(string field in new[]{"reload","healthFill"})Set(ui,field,Component<Image>());
        foreach(string field in new[]{"healthText","killText"})Set(ui,field,Component<TMP_Text>());
        foreach(string field in new[]{"lowHealth","confirmation"})Set(ui,field,Component<CanvasGroup>());
        var indicators=new DamageIndicatorView[3];
        for(int i=0;i<3;i++){indicators[i]=Component<DamageIndicatorView>();Set(indicators[i],"bearing",Component<RectTransform>());Set(indicators[i],"fade",Component<CanvasGroup>());}
        Set(ui,"damageIndicators",indicators);ui.OnPlayerSpawned();return ui;
    }
    static void VerifyGameplay()
    {
        MatchResults.Clear();Time.unscaledTime=Time.time=10;Camera.main=Component<Camera>();
        var match=MatchManager.Instance=new MatchManager{IsSpawned=true,Running=true};
        match.phase.Value=(int)MatchRules.Stage.Regulation;match.phaseEndsAt.Value=125;
        var network=NetworkManager.Singleton=new NetworkManager{IsConnectedClient=true};network.ServerTime.Time=5;
        UnityEngine.Object.PrefabFactory=(template,parent)=>{
            Component row=template is TeammateRow ? Mate() : template is KillFeedRow ? Feed() : Row();
            row.transform.parent=parent;return row;
        };
        var hud=Gameplay();Call(hud,"Update");var ui=Owner();Call(ui,"Update");Call(hud,"Update");
        Assert(Get<TMP_Text>(hud,"timer").text=="02:00","HUD uses authoritative deadline and server time");
        Assert(Get<TMP_Text>(hud,"lifeStatus").text=="Alive","Local life comes from replicated state");
        var health=ui.Health;var weapon=Get<TankShooter>(ui,"shooter");
        Assert(!Get<Image>(ui,"reload").gameObject.activeSelf,"ReloadProgress starts hidden with no cooldown");
        var ready=Get<GameObject>(ui,"weaponReady");
        Assert(!ready.activeSelf,"WeaponReady does not show on initial spawn");
        weapon.CooldownRemaining=.2f;weapon.CooldownEndsAt=10.2f;weapon.CanFire=false;weapon.Emit();
        Assert(Get<Image>(ui,"reload").gameObject.activeSelf,"Weapon event immediately shows ReloadProgress during cooldown");
        Assert(Math.Abs(Get<Image>(ui,"reload").fillAmount-.5f)<.001f&&!Get<GameObject>(ui,"weaponReady").activeSelf,"Reload reads weapon timing and actual ready gate");
        Time.time=10.2f;weapon.CooldownRemaining=0;weapon.CanFire=true;Call(ui,"Update");
        Assert(!Get<Image>(ui,"reload").gameObject.activeSelf,"Cooldown completion hides ReloadProgress on the same update");
        Assert(ready.activeSelf&&ready.transform.localScale.x==0,"Successful completion starts ready pulse at zero scale");
        Time.unscaledTime=10.25f;Call(ui,"Update");
        Assert(ready.transform.localScale.x>0&&ready.transform.localScale.x<1,"Ready pulse scales in during the first half");
        Time.unscaledTime=10.5f;Call(ui,"Update");
        Assert(Math.Abs(ready.transform.localScale.x-1f)<.001f,"Ready pulse reaches its original scale at half a second");
        Time.unscaledTime=10.75f;Call(ui,"Update");
        Assert(ready.transform.localScale.x>0&&ready.transform.localScale.x<1,"Ready pulse scales out towards zero during the second half");
        Time.unscaledTime=11;Call(ui,"Update");
        Assert(!ready.activeSelf&&ready.transform.localScale.x==1,"Ready pulse hides and resets after exactly one second");
        weapon.CooldownRemaining=.2f;weapon.CooldownEndsAt=10.4f;weapon.Emit();
        weapon.CooldownRemaining=0;weapon.Emit();
        Assert(!Get<Image>(ui,"reload").gameObject.activeSelf,"Cancelled cooldown event hides ReloadProgress immediately");
        Assert(!ready.activeSelf,"Early cancellation never shows WeaponReady even when firing is allowed");
        weapon.CooldownRemaining=.2f;weapon.CooldownEndsAt=10.4f;weapon.Emit();
        Time.time=10.5f;weapon.CooldownEndsAt=10.5f;weapon.CooldownRemaining=0;weapon.Emit();
        Assert(!ready.activeSelf,"Replacing a deadline cannot masquerade as completion after the old deadline passes");
        weapon.CooldownRemaining=.2f;weapon.CooldownEndsAt=10.7f;weapon.Emit();
        Time.time=10.7f;weapon.CooldownRemaining=0;Call(ui,"Update");
        Time.unscaledTime=11.25f;Call(ui,"Update");
        Assert(ready.activeSelf&&ready.transform.localScale.x>0&&ready.transform.localScale.x<1,"A repeated completion starts a fresh ready animation");
        weapon.CooldownRemaining=.2f;weapon.CooldownEndsAt=10.9f;weapon.Emit();
        Assert(!ready.activeSelf&&ready.transform.localScale.x==1,"A new reload immediately cancels and resets the ready animation");
        Time.time=10.9f;weapon.CooldownRemaining=0;Call(ui,"Update");
        Call(ui,"OnDisable");
        Assert(!ready.activeSelf&&ready.transform.localScale.x==1,"Disabling UI cleans up the active ready animation");
        Call(ui,"OnEnable");
        Assert(!ready.activeSelf,"Re-enabling an already ready weapon cannot replay completion");
        health.currentHealth.Value=75;Call(ui,"Update");
        Assert(Get<TMP_Text>(ui,"healthText").text=="75 / 100"&&Get<Image>(ui,"healthFill").fillAmount>.75f,"Health label updates immediately while fill moves smoothly");
        health.currentHealth.Value=20;Call(ui,"Update");Assert(Get<CanvasGroup>(ui,"lowHealth").alpha>0,"Low health warning uses actual health");

        var hit=new CombatFeedback{VictimId=0,AttackerId=1,HasAttacker=true,VictimTeam=0,AttackerTeam=1,HasSource=true,Source=new Vector3(1,0,0)};
        match.Emit(hit);Call(ui,"Update");var arrows=Get<DamageIndicatorView[]>(ui,"damageIndicators");
        Assert(Math.Abs(Get<RectTransform>(arrows[0],"bearing").localRotation.z+90)<.01f,"Right-hand damage points right relative to camera");
        Camera.main.transform.forward=new Vector3(1,0,0);Call(ui,"Update");
        Assert(Math.Abs(Get<RectTransform>(arrows[0],"bearing").localRotation.z)<.01f,"Indicator follows camera yaw after hit");
        hit.Source=new Vector3(0,0,1);match.Emit(hit);hit.Source=new Vector3(-1,0,0);match.Emit(hit);Call(ui,"Update");
        Assert(Get<CanvasGroup>(arrows[1],"fade").alpha>0&&Get<CanvasGroup>(arrows[2],"fade").alpha>0,"Consecutive hits retain multiple bearings");
        Time.unscaledTime+=1;Call(ui,"Update");Assert(Get<CanvasGroup>(arrows[0],"fade").alpha==0,"Damage indicator expires locally");

        var outgoing=new CombatFeedback{VictimId=1,AttackerId=0,HasAttacker=true,VictimTeam=1,AttackerTeam=0,VictimName="Enemy",AttackerName="Owner"};
        match.Emit(outgoing);Call(ui,"Update");
        Assert(Get<GameObject>(ui,"hitMarker").activeSelf&&Get<CanvasGroup>(ui,"confirmation").alpha>0,"Confirmed damage shows shooter marker");
        var feed=Get<List<KillFeedRow>>(hud,"feed");Assert(feed.Count==0,"Normal damage never creates a kill feed entry");
        outgoing.IsBase=true;match.Emit(outgoing);Assert(feed.Count==0,"Base damage never counts as player kill");
        outgoing.IsBase=false;outgoing.IsKill=true;match.Emit(outgoing);Call(ui,"Update");
        Assert(Get<GameObject>(ui,"killMarker").activeSelf&&Get<TMP_Text>(ui,"killText").text.Contains("Enemy"),"Confirmed kill names the victim and uses distinct cue");
        Assert(feed.Count==1,"Only confirmed player death creates a feed row");
        outgoing.HasAttacker=false;match.Emit(outgoing);
        Assert(!Get<TMP_Text>(feed[1],"killer").gameObject.activeSelf&&Get<TMP_Text>(feed[1],"action").text=="Eliminated:","Unknown killer has no invented name");
        for(int i=0;i<5;i++)match.Emit(outgoing);Assert(feed.Count==4,"Kill feed is bounded by Inspector limit");
        Time.unscaledTime+=6;Call(hud,"Update");Assert(feed.Count==0,"Feed rows expire and are removed");

        var roster=new MatchResults{players=new[]{new MatchRules.PlayerRecord{clientId=0,team=0,name="Owner",ordinal=0},new MatchRules.PlayerRecord{clientId=1,team=1,name="Enemy",ordinal=1}}};
        match.statistics.Value=new FixedString4096Bytes(JsonUtility.ToJson(roster));
        Assert(!Get<GameObject>(hud,"teammatePanel").activeSelf,"1v1 has no teammate panel or reserved space");
        var friend=new GameObject("Friend",typeof(PlayerHealth),typeof(NetworkObject));var friendHealth=friend.GetComponent<PlayerHealth>();
        friendHealth.IsSpawned=true;friendHealth.maxHealth=150;friendHealth.currentHealth.Value=105;
        roster.players=new[]{roster.players[0],roster.players[1],new MatchRules.PlayerRecord{clientId=2,team=0,name="Actual friend",ordinal=2},new MatchRules.PlayerRecord{clientId=3,team=1,name="Other enemy",ordinal=3}};
        match.statistics.Value=new FixedString4096Bytes(JsonUtility.ToJson(roster));Call(hud,"Update");
        var lateRows=Get<Dictionary<ulong,TeammateRow>>(hud,"teammates");
        var lateRow=lateRows[2];
        Assert(Get<TMP_Text>(lateRow,"healthText").text=="—","Roster can arrive before the teammate PlayerObject");
        network.SpawnManager.Players[2]=friend.GetComponent<NetworkObject>();
        Call(hud,"Update"); // No second roster/Spawned event to rescue the binding.
        Assert(lateRows.Count==1&&lateRows[2]==lateRow,"Late teammate registration reuses the authored row");
        var mates=Get<Dictionary<ulong,TeammateRow>>(hud,"teammates");var mate=mates[2];
        Assert(mates.Count==1&&Get<TMP_Text>(mate,"healthText").text=="105 / 150","Teammate reads other player's real max/current health");
        friendHealth.currentHealth.Value=90;Assert(Get<TMP_Text>(mate,"healthText").text=="90 / 150","Remote health changes update the teammate row");
        friendHealth.isDead.Value=true;friendHealth.respawnAt.Value=8;Call(hud,"Update");
        Assert(Get<TMP_Text>(mate,"lifeText").text=="Respawn in 3s","Teammate countdown uses remote deadline");
        match.destroyedBases.Value=2;Call(hud,"Update");Assert(Get<CanvasGroup>(hud,"baseNotice").alpha==0,"Enemy base destruction does not notify local team");
        match.destroyedBases.Value=3;Call(hud,"Update");var until=Get<float>(hud,"baseUntil");
        Assert(Get<CanvasGroup>(hud,"baseNotice").alpha>0&&Get<TMP_Text>(mate,"lifeText").text.Contains("Eliminated"),"Own base notice and elimination reflect persistent base flags");
        Time.unscaledTime+=1;hud.RefreshLocal();Assert(Get<float>(hud,"baseUntil")==until,"Repeated state reads cannot repeat base destruction notice");
        match.phase.Value=(int)MatchRules.Stage.Overtime;Call(hud,"Update");var overtime=Get<float>(hud,"overtimeUntil");
        Assert(Get<CanvasGroup>(hud,"overtimeNotice").alpha>0&&Get<TMP_Text>(hud,"phaseLabel").text=="OVERTIME","Actual overtime transition shows notification");
        Time.unscaledTime+=1;hud.RefreshLocal();Assert(Get<float>(hud,"overtimeUntil")==overtime,"Overtime notification occurs once per match");
        roster.players[2].disconnected=true;match.statistics.Value=new FixedString4096Bytes(JsonUtility.ToJson(roster));Call(hud,"Update");
        Assert(Get<TMP_Text>(mate,"lifeText").text=="Disconnected","Disconnected teammate retained in roster has correct status");

        health.isDead.Value=true;health.currentHealth.Value=0;health.respawnAt.Value=10;Call(ui,"Update");
        Assert(!Get<GameObject>(ui,"combat").activeSelf&&!Get<GameObject>(ui,"abilitySlot").activeSelf,"Death hides combat/ability widgets only");
        outgoing.HasAttacker=true;outgoing.AttackerId=0;match.Emit(outgoing);Call(ui,"Update");
        Assert(Get<CanvasGroup>(ui,"confirmation").alpha>0,"A projectile that kills after shooter death still confirms its credited kill");
        health.currentHealth.Value=100;health.isDead.Value=false;Call(ui,"Update");
        Assert(Get<Image>(ui,"healthFill").fillAmount==1&&Get<CanvasGroup>(arrows[2],"fade").alpha==0,"Respawn resets health display and old damage effects");
        foreach (var currentRow in mates.Values) Call(currentRow,"OnDestroy");
        Call(ui,"OnDisable");
        health.currentHealth.Value=60;
        weapon.CooldownRemaining=.2f;
        Call(ui,"OnEnable");
        Assert(Get<Image>(ui,"reload").gameObject.activeSelf,"Re-enable immediately reads an active weapon cooldown");
        foreach (var currentRow in mates.Values) Call(currentRow,"OnDestroy");
        Call(ui,"OnDisable");weapon.CooldownRemaining=0;Call(ui,"OnEnable");
        Assert(!Get<Image>(ui,"reload").gameObject.activeSelf,"Re-enable immediately reads a completed cooldown");
        Call(ui,"Update");
        Assert(Get<TMP_Text>(ui,"healthText").text=="60 / 100"&&health.currentHealth.SubscriberCount==1,
            "Re-enable binds once and reads health changed while unsubscribed");
        Call(ui,"Update");
        Assert(health.currentHealth.SubscriberCount==1,"Repeated updates cannot duplicate owner subscriptions");
        // The fake Destroy does not dispatch Unity's end-of-frame OnDestroy.
        foreach (var currentRow in mates.Values) Call(currentRow,"OnDestroy");
        health.IsOwner=false;Call(ui,"Update");
        Assert(health.currentHealth.SubscriberCount==0,"Losing ownership removes local presentation subscriptions");
        health.IsOwner=true;Call(ui,"Update");
        Assert(health.currentHealth.SubscriberCount==1,"Late ownership rebinds current state");
        foreach (var currentRow in mates.Values) Call(currentRow,"OnDestroy");
        hud.HideForResults();
        Assert(!Get<GameObject>(hud,"gameplayRoot").activeSelf&&mates.Count==0,"Results hide all match overlays and temporary rows");
        ui.OnPlayerDespawned();Call(hud,"OnDestroy");
        Assert(health.currentHealth.SubscriberCount==0&&friendHealth.currentHealth.SubscriberCount==0&&match.phase.SubscriberCount==0,"Owner, teammate and match subscriptions cleaned");
        MatchManager.Instance=null;MatchResults.Clear();
        var secondMatch = MatchManager.Instance = new MatchManager { IsSpawned=true, Running=true };
        secondMatch.phase.Value=(int)MatchRules.Stage.Regulation;
        var secondHud=Gameplay();Call(secondHud,"Update");var secondOwner=Owner();Call(secondOwner,"Update");
        Assert(Get<CanvasGroup>(secondHud,"baseNotice").alpha==0 &&
            Get<List<KillFeedRow>>(secondHud,"feed").Count==0 &&
            Get<GameObject>(secondHud,"gameplayRoot").activeSelf,
            "Second match starts without old notices or feed and with an active gameplay root");
        secondOwner.OnPlayerDespawned();Call(secondHud,"OnDestroy");MatchManager.Instance=null;
        Assert(HudPresentation.Clock(59.1)=="01:00"&&HudPresentation.Clock(-1)=="00:00","Timer formatting clamps actual network countdown");
    }
}
