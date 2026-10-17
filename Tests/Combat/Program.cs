using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;

public class MatchHud : Component { public static MatchHud Instance; public void ShowFinal() { } }
static class Program
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Assert(bool condition,string reason){checks++;if(!condition)throw new Exception(reason);}
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
    static (MatchManager match,MatchRules rules,PlayerHealth health,List<CombatFeedback> feed) Fixture(bool start=true,bool four=false)
    {
        NetworkManager.Singleton=new NetworkManager{IsServer=true,IsListening=true};
        NetworkManager.Singleton.ServerTime.Time=20;
        var match=new GameObject("Match",typeof(MatchManager)).GetComponent<MatchManager>();
        match.IsServer=match.IsClient=match.IsSpawned=true;
        typeof(MatchManager).GetProperty("Instance").SetValue(null,match);
        var rules=(MatchRules)typeof(MatchManager).GetField("rules",Hidden).GetValue(match);
        rules.Register(0,100,"Attacker",0,0);rules.Register(1,101,"Victim",1,1);rules.Register(2,102,"Friend",1,3);
        if(four)rules.Register(3,103,"Second client",0,2);
        if(start)rules.Start(10,300);
        var player=new GameObject("Victim",typeof(PlayerHealth),typeof(PlayerState),typeof(PlayerMovement));
        var health=player.GetComponent<PlayerHealth>();health.IsServer=health.IsSpawned=true;health.OwnerClientId=1;health.NetworkObjectId=101;
        player.GetComponent<PlayerState>().teamIndex.Value=1;
        Call(health,"Awake");
        var feed=new List<CombatFeedback>();match.CombatConfirmed+=feed.Add;
        return (match,rules,health,feed);
    }
    static void Main()
    {
        var f=Fixture(false);
        Assert(!f.health.TakeDamage(25,0,100,new Vector3(2,0,0))&&f.feed.Count==0,"Preparation rejects damage and all feedback");
        f=Fixture();
        Assert(!f.health.TakeDamage(25,1,102)&&f.feed.Count==0,"Friendly hit never produces UI confirmation");
        Assert(!f.health.TakeDamage(0,0,100)&&f.feed.Count==0,"Zero damage has no hit feedback");
        f.health.IsServer=false;Assert(!f.health.TakeDamage(25,0,100)&&f.feed.Count==0,"Client replica cannot accept damage or generate confirmations");f.health.IsServer=true;
        Assert(f.health.TakeDamage(25,0,100,new Vector3(7,0,-3))&&f.health.currentHealth.Value==75,"Authoritative accepted damage changes actual HP");
        var hit=f.feed.Single();
        Assert(!hit.IsKill&&!hit.IsBase&&hit.HasAttacker&&hit.AttackerId==0&&hit.VictimId==1,"Hit metadata uses registered network identities");
        Assert(hit.HasSource&&hit.Source.x==7&&hit.Source.z==-3,"Damage direction retains actual projectile origin");
        Assert(f.health.TakeDamage(100,0,100,new Vector3(7,0,-3)),"Lethal damage accepted");
        var kill=f.feed.Last();
        Assert(kill.IsKill&&kill.VictimName=="Victim"&&kill.AttackerName=="Attacker"&&f.rules.Players[0].kills==1,"Kill feed is emitted only after rules credit actual death");
        int before=f.feed.Count;
        Assert(!f.health.TakeDamage(25,0,100)&&f.feed.Count==before,"Repeated damage to dead player cannot duplicate kill");
        f.match.RecordDeath(f.health,100);Assert(f.feed.Count==before,"Repeated death processing cannot duplicate feedback");
        Call(f.match,"CombatFeedbackRpc",kill.Sequence,kill.VictimId,kill.AttackerId,kill.HasAttacker,kill.VictimTeam,kill.AttackerTeam,
            kill.VictimName,kill.AttackerName,kill.IsKill,kill.IsBase,kill.HasSource,kill.Source);
        Assert(f.feed.Count==before,"Duplicate confirmation sequence is ignored on receiver");
        var baseObject=new GameObject("EnemyBase",typeof(TeamBase));var teamBase=baseObject.GetComponent<TeamBase>();
        teamBase.IsServer=teamBase.IsSpawned=true;teamBase.teamIndex=1;
        Assert(teamBase.TakeDamage(25,0,100)&&f.feed.Last().IsBase&&!f.feed.Last().IsKill,"Base damage confirms a hit without a player kill");
        teamBase.TakeDamage(10000,0,100);
        Assert(teamBase.isDestroyed.Value&&f.feed.Count(p=>p.IsKill)==1,"Destroying base never adds a kill-feed record");
        before=f.feed.Count;
        Assert(!teamBase.TakeDamage(25,0,100)&&f.feed.Count==before,"Destroyed base rejects extra feedback");
        f=Fixture();f.health.TakeDamage(100,0);
        Assert(f.feed.Single().IsKill&&!f.feed[0].HasAttacker&&f.feed[0].AttackerName==""&&!f.feed[0].HasSource,
            "Death without known attacker invents neither name nor damage bearing");
        Assert(f.rules.Players[0].kills==0,"Unknown attacker never receives fabricated kill credit");
        f=Fixture();f.health.OwnerClientId=0;f.health.NetworkObjectId=100;f.health.GetComponent<PlayerState>().teamIndex.Value=0;
        f.health.TakeDamage(25,1,101,new Vector3(-2,0,0));
        Assert(f.feed.Single().AttackerId==1&&f.feed[0].VictimId==0,"Client-to-host feedback uses actual sender/victim client identities");
        f=Fixture(four:true);f.health.TakeDamage(25,0,103,new Vector3(3,0,0));
        Assert(f.feed.Single().AttackerId==3&&f.feed[0].VictimId==1,"Client-to-client damage is confirmed on server with distinct client IDs");
        Console.WriteLine($"PASS: {checks} authoritative damage/feedback assertions (simulated engine/RPC delivery).");
    }
}
