using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;

public class MatchHud : Component { public static MatchHud Instance; public void ShowFinal() { } }
static partial class Program
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Assert(bool condition,string reason){checks++;if(!condition)throw new Exception(reason);}
    static void Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Hidden).Invoke(target,args);
    static (MatchManager match,MatchRules rules,PlayerHealth health,List<CombatFeedback> feed) Fixture(bool start=true,bool four=false)
    {
        NetworkManager.Singleton=new NetworkManager{IsServer=true,IsListening=true};
        NetworkManager.Singleton.ServerTime.Time=20;
        foreach(ulong id in new ulong[]{0,1,2,3})NetworkManager.Singleton.ConnectedClients[id]=new object();
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
            kill.VictimName,kill.AttackerName,kill.IsKill,kill.IsBase,kill.HasSource,kill.Source,
            kill.AppliedDamage,kill.HasImpact,kill.ImpactPosition,default(RpcParams));
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
        DamageNumberTests();
        PresentationTests();
        Console.WriteLine($"PASS: {checks} combat/presentation assertions (simulated engine/RPC delivery).");
    }

    static void DamageNumberTests()
    {
        var point=new Vector3(5,2,3);
        var f=Fixture();f.health.currentHealth.Value=6;
        Assert(f.health.TakeDamage(18,0,100,Vector3.zero,point),"Lethal ranged hit is accepted");
        var feedback=f.feed.Single();
        Assert(feedback.AppliedDamage==6&&feedback.HasImpact&&feedback.ImpactPosition.x==5,"Number carries six actual HP, not calculated eighteen");
        Assert(feedback.IsKill&&f.rules.Players[0].kills==1,"Actual-damage payload preserves kill attribution");
        Assert(feedback.CanShowDamageNumber(0)&&!feedback.CanShowDamageNumber(1)&&!feedback.CanShowDamageNumber(2)&&!feedback.CanShowDamageNumber(3),
            "Host shooter eligible; victim, both teams and observers rejected");
        Assert(f.match.RpcTarget.Calls.Count==2&&f.match.RpcTarget.Calls[0].SequenceEqual(new ulong[]{0})&&f.match.RpcTarget.Calls[1].SequenceEqual(new ulong[]{1,2,3}),
            "Host receives single-recipient payload; other confirmation targets exclude host");
        f=Fixture(four:true);f.health.TakeDamage(18,0,103,Vector3.zero,point);feedback=f.feed.Single();
        Assert(feedback.AppliedDamage==18&&feedback.CanShowDamageNumber(3)&&!feedback.CanShowDamageNumber(0)&&!feedback.CanShowDamageNumber(1),
            "Remote shooter eligible; host observer and victim rejected");
        Assert(f.match.RpcTarget.Calls[0].SequenceEqual(new ulong[]{3})&&f.match.RpcTarget.Calls[1].SequenceEqual(new ulong[]{0,1,2}),
            "Remote shooter targets are disjoint from observer group");
        f=Fixture();var baseObject=new GameObject("Base",typeof(TeamBase));var teamBase=baseObject.GetComponent<TeamBase>();
        teamBase.IsServer=true;teamBase.teamIndex=1;teamBase.currentHealth.Value=4;teamBase.TakeDamage(25,0,100,point);
        Assert(f.feed.Single().AppliedDamage==4&&f.feed[0].IsBase&&f.feed[0].CanShowDamageNumber(0),"Base lethal hit reports only remaining four HP");
        var empty=feedback;empty.AppliedDamage=0;Assert(!empty.CanShowDamageNumber(3),"Zero damage cannot display a number");
        empty=feedback;empty.HasImpact=false;Assert(!empty.CanShowDamageNumber(3),"Damage without confirmed impact does not invent a position");
        f=Fixture();for(int i=0;i<10;i++)f.health.TakeDamage(1,0,100,Vector3.zero,point);
        Assert(f.feed.Count==10&&f.feed.Select(x=>x.Sequence).Distinct().Count()==10&&f.feed.All(x=>x.AppliedDamage==1),"Rapid accepted hits have ten unique damage event identities");
    }
}
