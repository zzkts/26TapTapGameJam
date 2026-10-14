#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Real projectile, field, physics, UI and scene-reload checks for the expanded chapter.</summary>
public sealed class EmergencePlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running{get;private set;}
    public static bool Succeeded{get;private set;}
    public static string Stage{get;private set;}
    private PlayerController player;
    private EmergenceProgress progress;
    private Keyboard keyboard,previousKeyboard;
    private InputSettings.BackgroundBehavior originalBackground;
    private readonly List<Behaviour> suspended=new();
    private bool hadSave,hadEndings,cleaned;
    private string previousSave;
    private int previousEndings,maxChain;
    public static void Run()
    {
        if(!Application.isPlaying||StorySession.Current==null)throw new InvalidOperationException("Enter the expanded chapter first");
        if(Running)throw new InvalidOperationException("Already running");
        var runner=new GameObject("Emergence Verification").AddComponent<EmergencePlayModeChecks>();DontDestroyOnLoad(runner.gameObject);runner.Begin();
    }
    private void Begin()
    {
        Results.Clear();Running=true;Succeeded=false;
        hadSave=PlayerPrefs.HasKey(RunProgress.SaveKey);previousSave=PlayerPrefs.GetString(RunProgress.SaveKey);
        hadEndings=PlayerPrefs.HasKey(RunProgress.EndingKey);previousEndings=PlayerPrefs.GetInt(RunProgress.EndingKey);
        previousKeyboard=Keyboard.current;originalBackground=InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();
        EmergenceReactions.Happened+=OnReaction;Bind();StartCoroutine(Execute());
    }
    private void Bind()
    {
        player=StorySession.Current.player;progress=EmergenceProgress.Current;
        StorySession.Current.SetValidationMode(true);player.SetInvincible(true);player.Heal(player.maxHp);
        foreach(Behaviour behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(behaviour.enabled&&(behaviour is EnemyAIController||behaviour is EnemySupportAura||behaviour is EnvironmentElementSource))
            {suspended.Add(behaviour);behaviour.enabled=false;}
    }
    private IEnumerator Execute()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try{IEnumerator step=stack.Peek();if(!step.MoveNext()){stack.Pop();continue;}wait=step.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}}
            catch(Exception error){Results.Add("FAIL: "+Stage+": "+error);break;}
            yield return wait;
        }
        Finish();Destroy(gameObject);
    }
    private IEnumerator Cases()
    {
        Stage="choice";
        Check(progress!=null&&progress.CompletionCount==0&&!progress.coreGate.IsOpen,"The expanded chapter begins with an intact two-route core lock");
        Teleport(new Vector2(19.8f,1));yield return Wait(.35f);int inventory=player.Genes.GenInventory.Count;
        yield return Press(Key.E);GeneDraftPanel draft=Object.FindFirstObjectByType<GeneDraftPanel>();
        Check(draft!=null&&draft.options.Count(b=>b.gameObject.activeInHierarchy)==3&&player.MenuOpen,"E opens three real gene choices and pauses the chapter");
        draft.cancel.onClick.Invoke();yield return null;
        Check(player.Genes.GenInventory.Count==inventory&&!player.MenuOpen,"Cancelling the draft neither consumes the chest nor changes inventory");
        yield return Press(Key.E);draft=Object.FindFirstObjectByType<GeneDraftPanel>();draft.options[0].onClick.Invoke();yield return null;
        Check(player.Genes.GenInventory.Any(g=>g.Definition.name=="ember"&&g.Copies==2)&&!player.MenuOpen,"Choosing a card awards exactly two copies of the selected gene");
        Check(StorySession.Current.WasTriggerCompleted("reward:chest-19.8"),"The chosen reward records its stable checkpoint claim");

        Stage="conductive topology";
        EmergenceBody[] conductors=progress.GetComponentsInChildren<EmergenceBody>().Where(b=>b.Has(EmergenceMaterial.Conductive)).OrderBy(b=>b.transform.position.x).ToArray();
        EnvironmentElementSource source=progress.GetComponentInChildren<EnvironmentElementSource>();
        Teleport(new Vector2(82,1));ActorElementEffects.Conduct(source.transform.position,source.radius);
        Check(!progress.IsComplete("duct"),"A dry gap prevents the electrical source from reaching its receiver");
        foreach(EmergenceBody conductor in conductors){yield return Shoot(conductor,E_ElementType.None);Check(conductor.Wet,"An unenchanted water projectile wets the scene conductor");}
        source.enabled=true;yield return Wait(2.6f);source.enabled=false;
        Check(progress.IsComplete("duct")&&conductors.Last().Charged&&conductors.Last().chargedGate.IsOpen,"The existing electrical source follows the wet conductor chain and opens the mechanical route");
        Check(progress.Discoveries.ContainsKey("conduction"),"The discovered multi-node causal chain appears in the journal");

        Stage="steam physics";
        EmergenceBody boiler=progress.GetComponentsInChildren<EmergenceBody>().Single(b=>b.Has(EmergenceMaterial.Boiler));
        Teleport(new Vector2(103,2));yield return Wait(.4f);yield return Shoot(boiler,E_ElementType.None);
        Check(boiler.Wet,"Ordinary water provides the boiler's first physical state");
        yield return Shoot(boiler,E_ElementType.Fire);yield return Wait(.15f);
        Check(player.GetComponent<Rigidbody2D>().linearVelocity.y>2f&&player.transform.position.y>2f,"A real fire projectile turns stored water into steam that launches the actual player");
        Keys(Key.D);yield return Wait(.65f);Keys();yield return Wait(1.5f);
        Check(player.IsGrounded()&&player.GetComponent<Collider2D>().bounds.min.y>3.8f,"Steam provides enough real lift to reach the upper alternative route");
        // The upper platform is wide; center at its actual control after proving the landing.
        Teleport(new Vector2(106,4.95f));yield return Wait(.2f);yield return Press(Key.E);
        Check(progress.IsComplete("duct"),"The elevated calibration control offers a second solution to the same district");

        Stage="biological cascade";
        Teleport(new Vector2(135,1));
        EmergenceBody[] fungi=progress.GetComponentsInChildren<EmergenceBody>().Where(b=>b.Has(EmergenceMaterial.Organic)).OrderBy(b=>b.transform.position.x).ToArray();
        yield return Shoot(fungi[0],E_ElementType.None);yield return Wait(1.15f);
        Check(fungi[0].Bloomed&&fungi[1].Poisoned,"Water grows a living colony and its real poison field changes a neighbouring material");
        for(int i=1;i<fungi.Length;i++)yield return Shoot(fungi[i],E_ElementType.None);
        yield return Shoot(fungi[0],E_ElementType.Fire);yield return Wait(.3f);
        EmergenceBody fragile=progress.GetComponentsInChildren<EmergenceBody>().Single(b=>b.Has(EmergenceMaterial.Fragile));
        Check(maxChain>=3,"One real ignition propagates through at least three prepared colonies");
        Check(fragile.Broken&&progress.IsComplete("spores"),"The cascade breaks the nearby structure and creates a persistent route change");
        Check(progress.CompletionCount==2&&progress.coreGate.IsOpen,"Any two solved districts release the final core lock");

        Stage="inertia and shared weight";
        Teleport(new Vector2(189,1));EmergenceBody crate=progress.GetComponentsInChildren<EmergenceBody>().First(b=>b.Has(EmergenceMaterial.Movable));
        float before=crate.transform.position.x;yield return Shoot(crate,E_ElementType.None,false);yield return Wait(.1f);
        Check(crate.transform.position.x>before+.02f||crate.GetComponent<Rigidbody2D>().linearVelocity.x>.2f,"A real water projectile transfers physical momentum to the crate");
        Rigidbody2D box=crate.GetComponent<Rigidbody2D>();box.position=new Vector2(197.5f,.1f);box.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
        yield return Shoot(crate,E_ElementType.None,false);yield return Wait(.3f);
        EmergencePlate plate=progress.GetComponentInChildren<EmergencePlate>();
        Check(plate.Pressed&&plate.gate.IsOpen,"The pushed crate's actual mass actuates the pressure gate");
        EmergenceReactions.Emit("inertia",crate.transform.position,"水球冲量 → 箱体移动 → 重量阀板",1);
        box.position=new Vector2(195,1);box.linearVelocity=Vector2.zero;Teleport(new Vector2(198,1));yield return Wait(.3f);
        Check(plate.Pressed,"The same pressure rule also accepts the player's body");
        Teleport(new Vector2(208,1));yield return Wait(.3f);yield return Press(Key.E);
        Check(progress.IsComplete("factory")&&plate.gate.IsOpen,"E at the reached control latches a third route without changing the pressure rule");

        Stage="persistent experiment state";
        RunSaveData saved=StorySession.Current.Capture();saved.checkpointX=139;saved.checkpointY=1;RunProgress.Write(saved);
        Check(RunProgress.Read().completedTriggers.Contains("emergence:spores")&&RunProgress.Read().completedTriggers.Contains("discovery:explosion"),"A normal checkpoint stores material-route changes and discovered rules together");
        int oldScene=SceneManager.GetActiveScene().handle;RunProgress.ContinueRun();yield return NewChapter(oldScene);
        Check(progress.CompletionCount==3&&progress.coreGate.IsOpen,"Continue reconstructs the solved topology and final lock state");
        Check(progress.GetComponentsInChildren<EmergenceBody>().Single(b=>b.Has(EmergenceMaterial.Fragile)).Broken,"A broken structure remains broken after a real scene reload");
        GeneChest claimed=Object.FindObjectsByType<GeneChest>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.name=="第一次构筑选择");
        Check(!claimed.gameObject.activeSelf&&player.Genes.GenInventory.Any(g=>g.Definition.name=="ember"&&g.Copies==2),"Continue keeps both the chosen gene and the consumed draft chest");
        Check(progress.Discoveries.ContainsKey("explosion")&&progress.Discoveries.ContainsKey("steam"),"Discovered causal rules survive Continue");
        yield return Press(Key.Tab);
        Check(player.MenuOpen&&Time.timeScale==0f,"TAB opens the actual emergence journal as a modal");
        yield return Press(Key.Escape);Check(!player.MenuOpen&&Time.timeScale==1f,"Escape closes only the journal and resumes the chapter");

        foreach(int seed in new[]{3000,3001,3002})
        {
            Stage="trial "+seed;oldScene=SceneManager.GetActiveScene().handle;RunProgress.BeginExperimentSeed(seed);yield return NewChapter(oldScene);
            Check(RunProgress.ExperimentMode&&RunProgress.RunSeed==seed&&progress.CompletionCount==0,"A trial starts a fresh topology with the requested reproducible rule seed");
            Check(progress.GetComponentsInChildren<EmergenceTrialEnemy>().Length>=3,"Trial rules add their own encounter composition");
            if(seed%3==0)Check(progress.GetComponentInChildren<EnvironmentElementSource>().interval==1.1f,"The high-frequency trial changes the actual electrical pulse cadence");
            if(seed%3==1)Check(progress.GetComponentsInChildren<EmergenceBody>().First(b=>b.Has(EmergenceMaterial.Organic)).growthCooldown==9f,"The living-colony trial changes the actual biological regrowth interval");
            RunSaveData trialSave=StorySession.Current.Capture();Check(trialSave.experimentMode&&trialSave.runSeed==seed,"Trial mode and its rule seed enter the normal checkpoint snapshot");
        }
        Stage="complete";
    }
    private IEnumerator NewChapter(int oldScene)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while(SceneManager.GetActiveScene().handle==oldScene||StorySession.Current==null||!StorySession.Current.Ready)
        {if(Time.realtimeSinceStartup>deadline)throw new InvalidOperationException("Chapter reload timed out");yield return null;}
        yield return null;yield return null;Bind();yield return null;
    }
    private IEnumerator Shoot(EmergenceBody target,E_ElementType element,bool fromAbove=true)
    {
        BulletSnapshot shot=player.Weapon.CurrentShot.Copy();shot.element=element;shot.speed=12;shot.effectRadius=0;shot.splitCount=0;shot.pierceCount=0;shot.projectileCount=1;
        Vector2 center=target.GetComponent<Collider2D>().bounds.center;
        Vector2 direction=fromAbove?Vector2.down:Vector2.right;
        Vector2 origin=center-direction*1.5f;
        Projectile bullet=Object.Instantiate(shot.projectilePrefab,origin,Quaternion.identity).GetComponent<Projectile>();
        bullet.LaunchSnapshot(origin,direction,LayerMask.GetMask("Enemy"),player.transform,shot,new ShotSpawnBudget(1));
        yield return Wait(.22f);
    }
    private void Teleport(Vector2 position)
    {Keys();var body=player.GetComponent<Rigidbody2D>();body.position=position;body.linearVelocity=Vector2.zero;player.transform.position=position;Physics2D.SyncTransforms();}
    private void Keys(params Key[] values){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(values));}
    private IEnumerator Press(params Key[] values){Keys(values);yield return null;yield return null;Keys();yield return null;yield return null;}
    private static IEnumerator Wait(float seconds){yield return new WaitForSeconds(seconds);}
    private static void Check(bool value,string message){Results.Add((value?"PASS: ":"FAIL: ")+message);if(!value)throw new InvalidOperationException(message);}
    private void OnReaction(EmergenceEvent reaction){if(reaction.kind=="explosion")maxChain=Mathf.Max(maxChain,reaction.count);}
    private void Finish()
    {
        if(cleaned)return;cleaned=true;
        EmergenceReactions.Happened-=OnReaction;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);previousKeyboard?.MakeCurrent();InputSystem.settings.backgroundBehavior=originalBackground;
        foreach(Behaviour component in suspended)if(component!=null)component.enabled=true;
        if(player!=null){player.SetInvincible(false);player.SetMenuOpen(false);}
        StorySession.Current?.SetValidationMode(false);
        if(hadSave)PlayerPrefs.SetString(RunProgress.SaveKey,previousSave);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);
        if(hadEndings)PlayerPrefs.SetInt(RunProgress.EndingKey,previousEndings);else PlayerPrefs.DeleteKey(RunProgress.EndingKey);PlayerPrefs.Save();
        Time.timeScale=1;Succeeded=Results.Count>0&&Results.All(r=>r.StartsWith("PASS:"));Running=false;
        Debug.Log("Emergence checks: "+string.Join("\n",Results));
    }
    private void OnDestroy(){if(!cleaned)Finish();}
}
#endif
