#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>Opt-in, isolated measurements; never touches the real save or builds a player.</summary>
public sealed class ReleasePerformanceChecks : MonoBehaviour
{
    [Serializable] private sealed class TextureMetric {public string path,format;public int width,height;public long bytes;}
    [Serializable] private sealed class Report
    {
        public bool success,threadAllocationCounterSupported;public string[] checks;public string label,context="Unity Editor Game view, not standalone player";
        public TextureMetric[] textures;public long textureBytes,hudBytesPer1000Updates,supportBytesPer1000Scans;
        public double hudMillisecondsPer1000Updates,supportMillisecondsPer1000Scans,meanMainThreadMs,meanFrameMs;
        public long meanFrameGcBytes;public int targetFrameRate,vSync,createdShots,reusedShots;public string error;
    }
    private readonly List<string> checks=new();
    private Report report;
    private const string Folder="Temp/ReleasePerformance/";
    private bool background;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Isolate()
    {if(SessionState.GetBool("ReleasePerf.Run",false))RunProgress.BeginIsolatedValidation();}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartChecks()
    {
        if(!SessionState.GetBool("ReleasePerf.Run",false))return;
        SessionState.SetBool("ReleasePerf.Run",false);
        var host=new GameObject("Release performance checks");DontDestroyOnLoad(host);
        host.AddComponent<ReleasePerformanceChecks>().StartCoroutine("Run");
    }
    private IEnumerator Run()
    {
        report=new Report{label=SessionState.GetString("ReleasePerf.Label","before")};
        background=Application.runInBackground;Application.runInBackground=true;
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try {var current=stack.Peek();if(!current.MoveNext()){stack.Pop();continue;}wait=current.Current;}
            catch(Exception e){report.error=e.ToString();checks.Add("FAIL: "+e.Message);break;}
            if(wait is IEnumerator nested){stack.Push(nested);continue;}yield return wait;
        }
        Check(PlayerPrefs.GetString(RunProgress.SaveKey,"")==SessionState.GetString("ReleasePerf.Save","missing"),"Original save unchanged");
        Check(PlayerPrefs.GetInt(RunProgress.EndingKey,0)==SessionState.GetInt("ReleasePerf.Endings",-1),"Ending history unchanged");
        report.checks=checks.ToArray();report.success=checks.All(s=>s.StartsWith("PASS:"));
        Directory.CreateDirectory(Folder);File.WriteAllText(Folder+report.label+"-runtime.json",JsonUtility.ToJson(report,true));
        Application.runInBackground=background;SessionState.SetBool("ReleasePerf.Finished",true);
    }
    private IEnumerator Cases()
    {
        yield return null;yield return null;
        Check(RunProgress.IsolatedValidation,"Measurements use isolated progress");if(!RunProgress.IsolatedValidation)yield break;
        FindAnyObjectByType<BeginPanel>().btnBegin.onClick.Invoke();
        float deadline=Time.realtimeSinceStartup+30;
        while(Time.realtimeSinceStartup<deadline&&(StorySession.Current==null||!StorySession.Current.Ready))yield return null;
        Check(StorySession.Current!=null&&StorySession.Current.Ready,"GameScene ready");
        if(StorySession.Current==null)yield break;
        StorySession.Current.SetValidationMode(true);var player=StorySession.Current.player;player.SetInvincible(true);
        foreach(var ai in FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None))ai.enabled=false;
        yield return new WaitForSecondsRealtime(.5f);
        report.targetFrameRate=Application.targetFrameRate;report.vSync=QualitySettings.vSyncCount;
        long controlBefore=GC.GetAllocatedBytesForCurrentThread();var control=new byte[1024];GC.KeepAlive(control);
        report.threadAllocationCounterSupported=GC.GetAllocatedBytesForCurrentThread()>controlBefore;
        report.textures=Resources.FindObjectsOfTypeAll<Texture2D>().Select(t=>new {t,path=AssetDatabase.GetAssetPath(t)})
            .Where(a=>a.path.StartsWith("Assets/")).Select(a=>new TextureMetric{path=a.path,width=a.t.width,height=a.t.height,
                format=a.t.format.ToString(),bytes=Profiler.GetRuntimeMemorySizeLong(a.t)}).OrderByDescending(t=>t.bytes).ToArray();
        report.textureBytes=report.textures.Sum(t=>t.bytes);
        var hud=FindAnyObjectByType<PlayerHudPanel>();
        var hudTick=(Action)Delegate.CreateDelegate(typeof(Action),hud,typeof(PlayerHudPanel).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic));
        Measure(hudTick,out report.hudBytesPer1000Updates,out report.hudMillisecondsPer1000Updates);
        var support=FindAnyObjectByType<EnemySupportAura>();
        Check(support!=null,"Monster4 healing support present");
        if(support!=null)
        {
            var scan=(Action)Delegate.CreateDelegate(typeof(Action),support,typeof(EnemySupportAura).GetMethod("RefreshCandidates",BindingFlags.Instance|BindingFlags.NonPublic));
            Measure(scan,out report.supportBytesPer1000Scans,out report.supportMillisecondsPer1000Scans);
        }
        if(!report.threadAllocationCounterSupported)report.hudBytesPer1000Updates=report.supportBytesPer1000Scans=-1;
        using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
        using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
        {
            double sumCpu=0,sumFrames=0;long sumGc=0;int count=180;
            for(int i=0;i<count;i++){yield return null;sumCpu+=main.Valid?main.LastValue/1000000d:0;sumGc+=gc.Valid?gc.LastValue:0;sumFrames+=Time.unscaledDeltaTime*1000d;}
            report.meanMainThreadMs=sumCpu/count;report.meanFrameMs=sumFrames/count;report.meanFrameGcBytes=sumGc/count;
        }
        var template=new BulletSnapshot(Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData"));
        int created=ProjectilePool.CreatedCount,reused=ProjectilePool.ReusedCount,plateau=0;
        for(int wave=0;wave<4;wave++)
        {
            for(int i=0;i<24;i++)
            {
                var data=template.Copy();data.lifeTime=.1f;data.splitCount=0;data.speed=8;
                var at=(Vector2)player.transform.position+new Vector2(.8f+i*.015f,1);
                var shot=ProjectilePool.Rent(data.projectilePrefab,at);shot.LaunchSnapshot(at,Vector2.right,0,player.transform,data,new ShotSpawnBudget(1));
            }
            yield return new WaitForSeconds(.95f);if(wave==1)plateau=ProjectilePool.CreatedCount;
        }
        report.createdShots=ProjectilePool.CreatedCount-created;report.reusedShots=ProjectilePool.ReusedCount-reused;
        Check(ProjectilePool.CreatedCount==plateau,"Repeated 24-shot bursts reuse the warmed projectile pool");
        Check(report.reusedShots>=48,"At least 48 stress shots reused objects");
        if(report.label=="after")
        {
            float hp=player.hp;player.hp=Mathf.Max(1,hp-1);hudTick();
            Check(hud.healthText.text==player.hp.ToString("0.#")+" / "+player.maxHp.ToString("0.#"),"Cached HUD updates after damage");
            player.hp=hp;hudTick();Check(hud.healthText.text==hp.ToString("0.#")+" / "+player.maxHp.ToString("0.#"),"Cached HUD updates after healing");
            yield return HealingRegression();
            Check(Resources.Load<AudioClip>("Audio/WaterDrain/cavern_ambience")!=null,"Prebaked ambience is available");
        }
        ScreenCapture.CaptureScreenshot(Folder+report.label+"-game.png");yield return null;yield return null;
        SceneManager.LoadScene("BeginScene");yield return null;yield return null;
        Check(SceneManager.GetActiveScene().name=="BeginScene","Returned to main menu");
    }
    private static void Measure(Action action,out long bytes,out double milliseconds)
    {
        for(int i=0;i<10;i++)action();
        var timer=new System.Diagnostics.Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();timer.Start();
        for(int i=0;i<1000;i++)action();timer.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-before;milliseconds=timer.Elapsed.TotalMilliseconds;
    }
    private IEnumerator HealingRegression()
    {
        var fixtures=new List<GameObject>();
        try
        {
            var supportPrefab=Resources.Load<GameObject>("Prefab/Enemy/Support/Monster4");
            var enemyPrefab=Resources.Load<GameObject>("Prefab/Enemy/Melee/Monster3");
            Check(supportPrefab!=null&&enemyPrefab!=null,"Healing regression fixtures use production prefabs");
            var root=Instantiate(supportPrefab,new Vector3(200,20,0),Quaternion.identity);fixtures.Add(root);
            root.GetComponent<EnemyAIController>().enabled=false;root.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
            var aura=root.GetComponent<EnemySupportAura>();aura.enabled=false;
            var targets=new List<EnemyAIController>();
            for(int i=0;i<5;i++)
            {
                var obj=Instantiate(enemyPrefab,new Vector3(201+i*.5f,20,0),Quaternion.identity);fixtures.Add(obj);
                var actor=obj.GetComponent<EnemyAIController>();actor.enabled=false;obj.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
                actor.TakeDamage(1);targets.Add(actor);if(i==4)obj.SetActive(false);
            }
            Physics2D.SyncTransforms();
            var scan=(Action)Delegate.CreateDelegate(typeof(Action),aura,typeof(EnemySupportAura).GetMethod("RefreshCandidates",BindingFlags.Instance|BindingFlags.NonPublic));
            var heal=(Action)Delegate.CreateDelegate(typeof(Action),aura,typeof(EnemySupportAura).GetMethod("HealOldestTargets",BindingFlags.Instance|BindingFlags.NonPublic));
            scan();targets[4].gameObject.SetActive(true);targets[4].enabled=false;Physics2D.SyncTransforms();scan();heal();
            Check(targets.Take(4).All(t=>Mathf.Approximately(t.Hp,t.MaxHp))&&targets[4].Hp<targets[4].MaxHp,"Monster4 still heals the four oldest nearby allies");
            Check(aura.GetComponentsInChildren<LineRenderer>().Count(l=>l.name=="HealBeam")==4,"Healing retains four visible beams");
            targets[0].gameObject.SetActive(false);Physics2D.SyncTransforms();scan();heal();
            Check(Mathf.Approximately(targets[4].Hp,targets[4].MaxHp),"Healing target replacement works after an ally leaves");
            targets[4].AddBuff(E_ElementType.Water);
            Check(targets[4].GetComponent<ActorElementEffects>().Has(E_ElementType.Water),"Allocation-free status lookup still detects applied water");
        }
        finally{foreach(var fixture in fixtures)if(fixture!=null)Destroy(fixture);}
        yield return null;
    }
    private void Check(bool value,string message){checks.Add((value?"PASS: ":"FAIL: ")+message);}
}
#endif
