#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

/// <summary>Isolated regression: real scene colliders, drops, draft/commit, chain, and final encounter.</summary>
public sealed class GeneSpecPlayModeChecks:MonoBehaviour
{
    [Serializable] private class Report { public bool success; public List<string> checks=new();public List<string> errors=new();public List<string> editorWarnings=new(); }
    private readonly Report report=new();
    private PlayerController player;
    private CampaignFlow flow;
    private Keyboard keys;
    private Keyboard previousKeyboard;
    private InputSettings.BackgroundBehavior previousBackground;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private readonly List<Behaviour> enemies=new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void StartIsolated()
    {
        if(!UnityEditor.SessionState.GetBool("GeneSpecQA.Run",false))return;
        RunProgress.BeginIsolatedValidation();
        new GameObject("Isolated gene-spec checks").AddComponent<GeneSpecPlayModeChecks>();
    }
    private void Awake(){DontDestroyOnLoad(gameObject);Application.logMessageReceived+=OnLog;}
    private void OnLog(string text,string stack,LogType type)
    {
        if(type!=LogType.Exception&&type!=LogType.Error)return;
        if(stack.Contains("UnityEditor.Search.SearchDatabase")){report.editorWarnings.Add(text);return;}
        report.errors.Add(text+"\n"+stack);
    }
    private IEnumerator Start()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try{var task=stack.Peek();if(!task.MoveNext()){stack.Pop();continue;}wait=task.Current;if(wait is IEnumerator next){stack.Push(next);continue;}}
            catch(Exception e){report.errors.Add(e.ToString());break;}
            yield return wait;
        }
        if(keys!=null)InputSystem.RemoveDevice(keys);
        previousKeyboard?.MakeCurrent();
        InputSystem.settings.backgroundBehavior=previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode=previousEditorInput;
        report.success=report.errors.Count==0;
        Directory.CreateDirectory("GeneSpecResults");
        File.WriteAllText("GeneSpecResults/play-result.json",JsonUtility.ToJson(report,true));
        Debug.Log("GeneSpecQA result: "+JsonUtility.ToJson(report,true));
        Application.logMessageReceived-=OnLog;
    }
    private void Check(bool valid,string text)
    {report.checks.Add((valid?"PASS: ":"FAIL: ")+text);Debug.Log(report.checks.Last());if(!valid)throw new InvalidOperationException(text);}
    private IEnumerator Cases()
    {
        float deadline=Time.realtimeSinceStartup+25;
        while(StorySession.Current==null||!StorySession.Current.Ready)
        {if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("GameScene startup");yield return null;}
        player=StorySession.Current.player;flow=CampaignFlow.Current;
        StorySession.Current.SetValidationMode(true);player.SetInvincible(true);
        Check(RunProgress.IsolatedValidation,"Save access is isolated before StorySession starts");
        Check(player.Genes.GenInventory.Count==0,"A new run starts without free elements or body traits");
        string[] retired={"PlayerReservoir","PlayerSynthesis","PlayerBackflowPulse","WaterReservoir2D","EcoFungus","PumpLoopSystem","CampaignReactor","EmergenceProgress","EmergenceGate","WatershedNetwork"};
        Check(!Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).Any(c=>c!=null&&retired.Contains(c.GetType().Name)),"No removed gameplay component survives in the scene");
        Check(!Resources.LoadAll<Modifier>("SO/Genes").Any(g=>(int)g.ModifierType==15),"Body gene assets are removed from Resources");
        var support=Object.FindObjectsByType<EnemySupportAura>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Check(support.Length>=3,"Monster4 healers remain in all three districts");
        foreach(var b in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(b.enabled&&(b is EnemyAIController||b is EnemySupportAura)){enemies.Add(b);b.enabled=false;}
        var enemyColliders=Object.FindObjectsByType<EnemyAIController>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .SelectMany(e=>e.GetComponentsInChildren<Collider2D>(true)).Where(c=>c.enabled).ToArray();
        foreach(var collider in enemyColliders)collider.enabled=false;
        previousKeyboard=Keyboard.current;
        previousBackground=InputSystem.settings.backgroundBehavior;
        previousEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keys=InputSystem.AddDevice<Keyboard>();keys.MakeCurrent();
        // Use real horizontal movement across the entire unchanged main route. No teleports within each segment.
        foreach(var range in new[]{new Vector2(3,45),new Vector2(49,93),new Vector2(97,140)})
        {
            Teleport(new Vector2(range.x,1));yield return new WaitForSeconds(.35f);
            InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));
            float end=Time.realtimeSinceStartup+25;float movedAt=Time.realtimeSinceStartup,lastX=player.transform.position.x;
            while(player.transform.position.x<range.y)
            {
                if(Time.realtimeSinceStartup>end)throw new TimeoutException("Main route traversal at "+player.transform.position);
                if(player.transform.position.x>lastX+.1f){lastX=player.transform.position.x;movedAt=Time.realtimeSinceStartup;}
                if(Time.realtimeSinceStartup-movedAt>2)throw new InvalidOperationException("Main route blocked at "+player.transform.position);
                yield return null;
            }
            InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
            Check(player.transform.position.y>-.5f,"Main route walked without extra abilities: "+range.x+" → "+range.y);
        }
        var chest=Object.FindObjectsByType<GeneChest>(FindObjectsSortMode.None).OrderBy(c=>c.transform.position.x).First();
        foreach(var collider in enemyColliders)if(collider!=null)collider.enabled=true;
        Teleport(chest.transform.position+Vector3.right*.6f);yield return new WaitForSeconds(.2f);
        int before=Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Length;
        Check(chest.Interact(player),"A real cold storage opens");yield return null;
        var drops=Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None);
        Check(drops.Length-before==2,"Cold storage scatters two pickups instead of opening a draft-choice panel");
        foreach(var pickup in drops){Teleport(pickup.transform.position);Check(pickup.Interact(player),"A dropped gene can be picked up");}
        foreach(string key in new[]{"water","ember","speed"})player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/"+key));
        var terminal=Object.FindObjectsByType<GeneEditStation>(FindObjectsSortMode.None).OrderBy(s=>s.transform.position.x).First();
        Teleport(terminal.transform.position+Vector3.right*.5f);yield return new WaitForSeconds(.2f);
        var panel=Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include);
        Modifier[] old=player.Genes.CopyChain();panel.Open(player,terminal);
        int water=player.Genes.GenInventory.ToList().FindIndex(g=>g.Definition.name=="water");
        panel.Equip(water);Check(player.Genes.GenChain.SequenceEqual(old),"Editing remains a draft until confirmed");
        panel.Cancel();Check(player.Genes.GenChain.SequenceEqual(old),"Cancel preserves the active build");
        panel.Open(player,terminal);panel.Equip(water);yield return null;
        InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));yield return null;
        InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
        int ember=player.Genes.GenInventory.ToList().FindIndex(g=>g.Definition.name=="ember");panel.Equip(ember);
        Check(panel.CopyDraft()[1]!=null&&panel.CopyDraft()[1].Definition.name=="ember","D selects the next gene slot");
        InputSystem.QueueStateEvent(keys,new KeyboardState(Key.Space));yield return null;
        InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
        Check(!player.MenuOpen&&player.Genes.GenChain[0]?.Definition.name=="water"&&player.Genes.GenChain[1]?.Definition.name=="ember","Space confirms the draft at a terminal");
        var chain=player.GetComponent<GeneChainController>();
        Check(chain.TryActivate()&&chain.ActiveAttribute.Element==E_ElementType.Water,"First right-click activation starts the first attribute");
        chain.Tick(chain.Remaining+.01f);
        Check(chain.State==GeneChainController.Phase.Ready&&chain.ActiveAttribute==null&&chain.CurrentAttribute.Element==E_ElementType.Fire,"The next attribute waits for another activation");
        Check(chain.TryActivate(),"Second attribute can be manually activated");chain.Tick(chain.Remaining+.01f);
        Check(chain.State==GeneChainController.Phase.Reloading,"Finishing the chain starts reloading");chain.Tick(3.1f);
        Check(chain.State==GeneChainController.Phase.Ready&&chain.AttributeIndex==0,"Three-second reload returns to the first attribute");
        var saved=new RunSaveData{campaignLayout=true,checkpointX=98,checkpointY=1,dashUnlocked=true};
        saved.genes.Add(new SavedGene{definitionName="water",geneId=player.Genes.GenInventory[water].GeneId,instanceId="preserved-water",level=2,copies=3});
        saved.genes.Add(new SavedGene{definitionName="body_jet",geneId=102,instanceId="obsolete-body"});
        saved.genes.Add(new SavedGene{definitionName="ember",geneId=player.Genes.GenInventory[ember].GeneId,instanceId="preserved-fire",copies=2});
        saved.equipped=new[]{2,1,0,-1,-1,-1};
        RunProgress.RestorePlayer(player,saved);
        Check(player.Genes.GenChain[0]?.InstanceId=="preserved-fire"&&player.Genes.GenChain[1]==null&&player.Genes.GenChain[2]?.InstanceId=="preserved-water","Legacy body-gene slots become empty without shifting surviving equipped instances");
        Check(player.transform.position.x==98&&player.Genes.GenChain[2].Level==2&&player.Genes.GenChain[0].Copies==2,"Continue preserves checkpoint, evolution and copy counts");
        Teleport(new Vector2(110,1));yield return null;
        // The earlier traversal already activated the guardians with no water-network prerequisites.
        Check(flow.level.BattleStarted,"Reaching the core starts guardians without any ecology prerequisite");
        foreach(var guardian in flow.level.guardians)if(guardian!=null)guardian.TakeDamage(10000f);
        yield return null;yield return null;
        Check(flow.level.CoreDefeated&&StorySession.Current.GuardianCleared,"Defeating all three guardians satisfies the final combat goal");
        Teleport(flow.exit.transform.position+Vector3.left*.5f);yield return new WaitForSeconds(.15f);
        Check(flow.exit.Interact(player),"The final valve is accessible after defeating guardians");
        StorySession.Current.ChooseEnding(true);yield return null;
        Check(StorySession.Current.StoryFlags!=0,"The final story decision remains connected");
        StorySession.Current.SetValidationMode(true);player.SetMenuOpen(false);
        Teleport(new Vector2(18,1));yield return new WaitForSeconds(.5f);
        PerformancePlayModeChecks.Run();
        while(PerformancePlayModeChecks.Running)yield return null;
        foreach(string result in PerformancePlayModeChecks.Results)report.checks.Add(result);
        Check(PerformancePlayModeChecks.Succeeded,"Projectile and water-effect pooling regressions still pass");
    }
    private void Teleport(Vector2 at)
    {player.transform.position=at;var body=player.GetComponent<Rigidbody2D>();body.position=at;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
}
#endif
