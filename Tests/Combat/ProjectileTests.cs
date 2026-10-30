using System;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
static class ProjectileTests
{
    static int checks;
    static void Check(bool ok,string reason) { checks++;if(!ok)throw new Exception(reason); }
    static void Call(object obj,string method,params object[] args)=>obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);
    static Bullet Create(uint shot)
    {
        var obj=new GameObject();obj.Add<Rigidbody>();obj.Add<Collider>();var bullet=obj.Add<Bullet>();
        typeof(Bullet).GetField("damageFalloff",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(bullet,new DamageFalloff());
        Call(bullet,"Awake");Launch(bullet,shot);return bullet;
    }
    static void Launch(Bullet bullet,uint shot)
    {
        bullet.gameObject.active=true;
        MatchManager.Instance.Projectiles.Register(100,shot,new GameObject(),2,null);
        bullet.Launch(new Vector3(0,0,1),Array.Empty<Collider>(),0,100,shot,false);
    }
    static GameObject Victim(int team=1,int hp=100)
    {
        var obj=new GameObject();obj.Add<Collider>();obj.Add<PlayerHealth>().HP=hp;
        obj.Add<PlayerState>().teamIndex.Value=team;obj.Add<PlayerMovement>();obj.Add<NetworkObject>().NetworkObjectId=101;return obj;
    }
    static void Main()
    {
        MatchManager.Instance=new MatchManager();NetworkManager.Singleton.IsServer=false;
        var victim=Victim();var bullet=Create(1);int fx=UnityEngine.Object.Effects;
        Call(bullet,"OnTriggerEnter",victim.GetComponent<Collider>());
        Check(!bullet.gameObject.active && victim.GetComponent<PlayerHealth>().DamageCalls==0,"Cosmetic collision releases flight but cannot damage");
        Check(UnityEngine.Object.Effects==fx && victim.GetComponent<PlayerMovement>().Impulses==0,"Cosmetic collision cannot explode or push");
        Launch(bullet,2);
        MatchManager.Instance.Projectiles.Resolve(100,1,true,new Vector3(5,0,0),Vector3.up);
        Check(bullet.gameObject.active && UnityEngine.Object.Effects==fx+1,"Late confirmation after actual Bullet.OnDisable/relaunch preserves new flight");
        bullet.ReleaseIfShot(100,1);
        Check(bullet.gameObject.active,"Actual Bullet generation guard rejects stale release");
        MatchManager.Instance.Projectiles.Resolve(100,2,false,Vector3.zero,Vector3.zero);
        Check(!bullet.gameObject.active && !bullet.TimerScheduled,"Rejection clears active flight and lifetime callback");

        MatchManager.Instance=new MatchManager();NetworkManager.Singleton.IsServer=true;
        bullet=Create(1);victim=Victim();var friendly=Victim(0);var farther=Victim();
        bullet.GetComponent<Rigidbody>().Hits=new[]{
            new RaycastHit{collider=farther.GetComponent<Collider>(),distance=3,point=new Vector3(3,0,0)},
            new RaycastHit{collider=friendly.GetComponent<Collider>(),distance=.1f},
            new RaycastHit{collider=victim.GetComponent<Collider>(),distance=1,point=new Vector3(1,0,0)}};
        int releases=PoolManager.Releases;fx=UnityEngine.Object.Effects;
        Call(bullet,"FixedUpdate");
        Check(victim.GetComponent<PlayerHealth>().DamageCalls==1 && farther.GetComponent<PlayerHealth>().DamageCalls==0 && friendly.GetComponent<PlayerHealth>().DamageCalls==0,
            "Unsorted sweep chooses nearest eligible collider, excluding friendlies");
        Check(victim.GetComponent<PlayerMovement>().Impulses==1 && MatchManager.Instance.Results==1,"One server hit requests one authoritative impulse and terminal result");
        Check(PoolManager.Releases==releases+1 && !bullet.gameObject.active && UnityEngine.Object.Effects==fx+1,"Host local terminal result releases bullet and shows impact once");
        Call(bullet,"OnTriggerEnter",victim.GetComponent<Collider>());Call(bullet,"Expire");
        Check(victim.GetComponent<PlayerHealth>().DamageCalls==1 && PoolManager.Releases==releases+1,"Trigger/lifetime after sweep cannot repeat processing or enqueue twice");
        MatchManager.Instance.Projectiles.Resolve(100,1,true,Vector3.zero,Vector3.up);
        Check(UnityEngine.Object.Effects==fx+1,"Duplicate server outcome cannot explode again");
        var lethal=Create(2);var low=Victim(hp:25);Call(lethal,"OnTriggerEnter",low.GetComponent<Collider>());
        Check(low.GetComponent<PlayerHealth>().isDead.Value && low.GetComponent<PlayerMovement>().Impulses==0,"Lethal damage preserves existing no-impulse-on-dead rule");
        var miss=Create(3);fx=UnityEngine.Object.Effects;Call(miss,"Expire");
        Check(!miss.gameObject.active && UnityEngine.Object.Effects==fx && MatchManager.Instance.Results==3,"Server expiry publishes terminal miss without explosion");

        foreach(var sample in new[]{(distance:5f,expected:25),(distance:50f,expected:18),(distance:120f,expected:10)})
        {
            uint sequence=(uint)(10+sample.distance);var ranged=Create(sequence);
            // Relaunch the same pooled object from a different muzzle, then move its flight.
            ranged.ReleaseIfShot(100,sequence);ranged.transform.position=new Vector3(100,0,0);Launch(ranged,sequence+1000);
            ranged.transform.position=new Vector3(500,0,0);
            var target=Victim();target.transform.position=new Vector3(100+sample.distance,0,0);
            Call(ranged,"OnTriggerEnter",target.GetComponent<Collider>());
            var hp=target.GetComponent<PlayerHealth>();
            Check(hp.LastDamage==sample.expected && hp.Source.x==100 && hp.Impact.x==100+sample.distance,
                "Damage uses captured launch position and confirmed point, including after pool reuse");
            var reset=(Vector3)typeof(Bullet).GetField("sourcePosition",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ranged);
            Check(reset.sqrMagnitude==0,
                "Release clears the retained launch origin");
        }
        var baseShot=Create(2000);var baseObject=new GameObject();baseObject.Add<Collider>();var enemyBase=baseObject.Add<TeamBase>();enemyBase.teamIndex=1;
        baseObject.transform.position=new Vector3(50,0,0);Call(baseShot,"OnTriggerEnter",baseObject.GetComponent<Collider>());
        Check(enemyBase.DamageCalls==1 && enemyBase.LastDamage==18,"Bases use the same falloff calculation");
        var wallShot=Create(2001);var wall=new GameObject();wall.Add<Collider>();Call(wallShot,"OnTriggerEnter",wall.GetComponent<Collider>());
        Check(!wallShot.gameObject.active,"Blocked shot resolves without a damage recipient");
        Console.WriteLine($"PASS: {checks} production projectile lifecycle assertions (physics/RPC boundaries simulated).");
    }
}
