#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TapGJ.WaterProjectiles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>MCP regression against production pools, colliders, element state and UI geometry.</summary>
public sealed class PerformancePlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running{get;private set;}
    public static bool Succeeded{get;private set;}
    public static string Stage{get;private set;}
    public static string Metrics{get;private set;}
    private readonly List<Behaviour> suspended=new();
    private readonly List<GameObject> fixtures=new();
    private PlayerController player;
    private Vector3 priorPosition;
    private bool validation,cleaned;
    private BulletSnapshot template;
    public static void Run()
    {new GameObject("Performance verification").AddComponent<PerformancePlayModeChecks>().Begin();}
    private void Begin()
    {
        Results.Clear();Running=true;Succeeded=false;
        player=CampaignFlow.Current.player;priorPosition=player.transform.position;
        validation=StorySession.Current.ValidationMode;StorySession.Current.SetValidationMode(true);player.SetInvincible(true);
        foreach(var behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(behaviour.enabled&&(behaviour is EnemyAIController||behaviour is EnemySupportAura)){suspended.Add(behaviour);behaviour.enabled=false;}
        template=new BulletSnapshot(Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData"));
        StartCoroutine(Execute());
    }
    private IEnumerator Execute()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try{var item=stack.Peek();if(!item.MoveNext()){stack.Pop();continue;}wait=item.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}}
            catch(Exception e){Results.Add("FAIL: "+Stage+": "+e);break;}
            yield return wait;
        }
        Finish();Destroy(gameObject);
    }
    private Projectile Shot(Vector2 at,float lifetime=.06f,E_ElementType element=E_ElementType.Water,int split=0)
    {
        var data=template.Copy();data.lifeTime=lifetime;data.speed=8;data.element=element;data.splitCount=split;
        data.maxGeneration=1;data.maxSpawn=8;
        var shot=ProjectilePool.Rent(data.projectilePrefab,at);
        shot.LaunchSnapshot(at,Vector2.right,LayerMask.GetMask("Enemy"),player.transform,data,new ShotSpawnBudget(data.maxSpawn));
        return shot;
    }
    private IEnumerator Cases()
    {
        Stage="pooled hit history and visual state";
        var target=new GameObject("Pool damage target");fixtures.Add(target);target.layer=LayerMask.NameToLayer("Enemy");target.transform.position=new Vector2(8,17);
        var actor=target.AddComponent<PoolVerificationActor>();target.AddComponent<BoxCollider2D>().size=Vector2.one*.5f;
        var data=template.Copy();data.lifeTime=.08f;data.speed=8;data.damage=2;data.damageMultiplier=1;data.pierceCount=4;data.reboundCount=3;
        var first=ProjectilePool.Rent(data.projectilePrefab,new Vector2(6,17));
        first.LaunchSnapshot(new Vector2(6,17),Vector2.right,LayerMask.GetMask("Enemy"),player.transform,data,new ShotSpawnBudget(24));
        first.Step(.4f);first.Step(.1f);
        Check(actor.hits==1,"A piercing drop damages the same collider only once");
        int id=first.GetInstanceID();int children=first.transform.childCount;
        yield return Returned(first);
        Check(!first.gameObject.activeSelf,"Expired bullet and its intact tail return to the inactive pool");
        var next=ProjectilePool.Rent(data.projectilePrefab,new Vector2(6,17));
        Check(next.GetInstanceID()==id,"The next shot reuses the projectile instance");
        Check(next.Snapshot==null&&next.Owner==null,"Reused shot releases its old snapshot and owner");
        data.element=E_ElementType.Fire;data.damage=3;data.pierceCount=0;data.reboundCount=0;
        next.LaunchSnapshot(new Vector2(6,17),Vector2.right,LayerMask.GetMask("Enemy"),transform,data,new ShotSpawnBudget(24));next.Step(.4f);
        Check(actor.hits==2&&Mathf.Abs(actor.damageTaken-5)<.01f,"Reused projectile clears hit history and uses the new damage");
        Check(!next.GetComponent<Collider2D>().enabled,"An expired shot cannot damage again while its trail fades");
        yield return Returned(next);
        var water=Shot(new Vector2(12,17));
        Check(water.GetInstanceID()==id&&water.GetComponent<WaterProjectileVisual>().Surface.enabled,"Fire-to-water reuse restores the dedicated liquid body");
        Check(!water.GetComponent<SpriteRenderer>().enabled,"The original sprite stays hidden for liquid rendering");
        Check(water.transform.childCount==children+1,"Reuse retains one water presentation and one optional element ribbon");
        yield return Returned(water);
        for(int i=0;i<4;i++){var repeated=Shot(new Vector2(12,17));yield return Returned(repeated);}
        Check(first.transform.childCount==children+1,"Repeated reuse does not accumulate duplicate droplet systems or trails");
        Stage="bounded split generation";
        var budget=new ShotSpawnBudget(8);data=template.Copy();data.splitCount=3;data.maxGeneration=1;data.lifeTime=2;data.speed=0;
        var parent=ProjectilePool.Rent(data.projectilePrefab,new Vector2(18,17));parent.LaunchSnapshot(new Vector2(18,17),Vector2.right,0,player.transform,data,budget);
        parent.SetUp(new Vector2(18,17),Vector2.right,0,0,1,1,.3f,.05f);
        yield return new WaitForSeconds(.35f);
        var splitShots=Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p=>p.enabled&&p.Generation==1).ToArray();
        Check(splitShots.Length==3,"Expiry splits create exactly three pooled child projectiles");
        Check(splitShots.All(p=>p.Owner==player.transform&&p.Snapshot.splitCount==3),"Split children retain their own copied snapshot and owner");
        yield return new WaitForSeconds(2.8f);
        Check(!Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Any(p=>p.enabled&&p.Generation>0),"Split generations stop at the configured budget and lifetime");
        Stage="enemy projectile reuse";
        Teleport(new Vector2(40,1));yield return new WaitForSeconds(.15f);player.Heal(player.MaxHp);player.SetInvincible(false);
        var enemyPrefab=Resources.Load<GameObject>("Prefab/Enemy/Ranged/Monster6Bullet");
        int enemyId=0;
        for(int i=0;i<2;i++)
        {
            float hp=player.hp;Vector2 center=player.GetComponent<Collider2D>().bounds.center;
            var shot=(JellyfishProjectile)ProjectilePool.Rent(enemyPrefab,center+Vector2.right*1.5f);
            if(i==0)enemyId=shot.GetInstanceID();else Check(shot.GetInstanceID()==enemyId,"Enemy ranged attacks reuse their bullet instance");
            shot.Launch(null,player.transform,center+Vector2.right*1.5f,Vector2.left,LayerMask.GetMask("Ground","Wall"),12,1,.2f,1);
            yield return new WaitForSeconds(.45f);
            Check(player.hp<hp,"A reused enemy bullet resets its spent flag and hits the actual player");
            yield return new WaitForSeconds(1.1f);
        }
        player.SetInvincible(true);player.Heal(player.MaxHp);
        Stage="steady-state burst reuse";
        int startCreated=ProjectilePool.CreatedCount,startReuse=ProjectilePool.ReusedCount,plateau=0;
        for(int wave=0;wave<8;wave++)
        {
            var batch=new Projectile[32];
            for(int i=0;i<32;i++)batch[i]=Shot(new Vector2(12+i*.02f,17));
            foreach(var shot in batch)yield return Returned(shot);
            yield return new WaitForSeconds(.3f);
            if(wave==4)plateau=ProjectilePool.CreatedCount;
        }
        Check(ProjectilePool.CreatedCount==plateau,"After warmup, three further 32-shot bursts create no projectile GameObjects");
        Check(ProjectilePool.ReusedCount-startReuse>=224,"At least 224 of 256 stress shots reuse existing instances");
        Check(ProjectilePool.IdleCount<=ProjectilePool.CapacityPerPrefab*2,"Inactive projectile storage stays within its per-prefab bounds");
        int effects=WaterEffectPool.CreatedCount;for(int i=0;i<16;i++)WaterVfx.SpawnLiquidImpact(new Vector2(10,17),Vector2.up,.3f,10);
        yield return new WaitForSeconds(1.2f);int warmEffects=WaterEffectPool.CreatedCount;
        for(int i=0;i<16;i++)WaterVfx.SpawnLiquidImpact(new Vector2(10,17),Vector2.up,.3f,10);
        yield return new WaitForSeconds(1.2f);
        Check(WaterEffectPool.CreatedCount==warmEffects,"Repeated surface impacts reuse spray particles and animated impact sheets");
        Check(WaterEffectPool.ReusedCount>0,"Transient water effects use their scene-owned pool");
        Metrics="256 stress shots; new projectiles="+(ProjectilePool.CreatedCount-startCreated)+"; reused="+(ProjectilePool.ReusedCount-startReuse)+"; impact warmup="+(warmEffects-effects)+"; steady impact allocations=0 GameObjects";
        Teleport(new Vector2(17,1));yield return new WaitForSeconds(.2f);
    }
    private void Teleport(Vector2 at)
    {player.transform.position=at;var rb=player.GetComponent<Rigidbody2D>();rb.position=at;rb.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    private static IEnumerator Returned(Projectile shot)
    {
        float deadline=Time.time+3;
        while(shot.gameObject.activeSelf)
        {if(Time.time>deadline)throw new InvalidOperationException("Projectile did not return after its tail faded");yield return null;}
    }
    private static void Check(bool yes,string message)
    {Results.Add((yes?"PASS: ":"FAIL: ")+message);if(!yes)throw new InvalidOperationException(message);}
    private void Finish()
    {
        if(cleaned)return;cleaned=true;
        foreach(var b in suspended)if(b!=null)b.enabled=true;
        foreach(var obj in fixtures)if(obj!=null)Destroy(obj);
        if(player!=null){Teleport(priorPosition);player.SetInvincible(false);}
        if(StorySession.Current!=null)StorySession.Current.SetValidationMode(validation);
        Running=false;Succeeded=Results.Count>0&&Results.All(r=>r.StartsWith("PASS:"));
        Debug.Log("Performance verification: "+string.Join("\n",Results)+"\n"+Metrics);
    }
    private void OnDestroy(){if(!cleaned)Finish();}
}
public sealed class PoolVerificationActor:MonoBehaviour,IActor
{
    public int hits;public float damageTaken;
    public void TakeDamage(float damage){hits++;damageTaken+=damage;}
}
#endif
