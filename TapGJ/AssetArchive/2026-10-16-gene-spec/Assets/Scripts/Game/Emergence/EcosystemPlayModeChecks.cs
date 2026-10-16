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

public sealed class EcosystemPlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    public static string Stage { get; private set; }
    private PlayerController player;
    private CampaignFlow flow;
    private PumpLoopSystem eco;
    private PlayerReservoir body;
    private Keyboard keyboard,oldKeyboard;
    private Mouse mouse,oldMouse;
    private InputSettings.BackgroundBehavior oldBackground;
    private InputSettings.EditorInputBehaviorInPlayMode oldEditor;
    private readonly List<Behaviour> suspended=new();
    private readonly List<GameObject> fixtures=new();
    public void Begin()
    {
        DontDestroyOnLoad(gameObject);Results.Clear();Running=true;Succeeded=false;
        oldKeyboard=Keyboard.current;oldMouse=Mouse.current;oldBackground=InputSystem.settings.backgroundBehavior;oldEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();System.IO.Directory.CreateDirectory("Docs/EcosystemValidation");Bind();StartCoroutine(Run());
    }
    private void Bind()
    {
        flow=CampaignFlow.Current;player=flow.player;eco=flow.ecosystem;body=player.GetComponent<PlayerReservoir>();
        StorySession.Current.SetValidationMode(true);player.SetInvincible(true);
        foreach(var a in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))if(a.enabled&&(a is EnemyAIController||a is EnemySupportAura)){suspended.Add(a);a.enabled=false;}
    }
    private IEnumerator Run()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait=null;
            try{var e=stack.Peek();if(!e.MoveNext()){stack.Pop();continue;}wait=e.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}}
            catch(Exception e){Results.Add("FAIL: "+e.Message);break;}
            yield return wait;
        }
        foreach(var f in fixtures)if(f!=null)Object.Destroy(f);foreach(var b in suspended)if(b!=null)b.enabled=true;
        if(eco!=null){eco.enabled=true;eco.fungus.enabled=true;}if(player!=null)player.SetInvincible(false);
        InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        Running=false;Succeeded=Results.All(r=>r.StartsWith("PASS:"));Stage="complete";Debug.Log("Ecosystem checks: "+string.Join("\n",Results));
    }
    private IEnumerator Cases()
    {
        Stage="Real body-water inputs";
        Check(eco!=null&&flow.reactors.Where(r=>r.district==1).All(r=>r.ecosystem==eco),"Chapter one monitors a live ecosystem instead of latched switches");
        Check(player.Genes.GenInventory.Count(g=>g.ModifierType==E_ModifierType.BodyTrait)==3,"The actual terminal inventory contains all three body genes");
        eco.enabled=false;eco.fungus.enabled=false;eco.pool.SetState(4,.8f);body.Restore(2,0);
        Teleport(new Vector2(26.4f,.3f));Aim(new Vector2(27.5f,-.5f));yield return Wait(.15f);yield return Press(Key.S,Key.F);yield return Wait(.12f);
        Check(Mathf.Abs(body.Stored-4)<.02f&&Mathf.Abs(eco.pool.Volume-2)<.02f,"Real S+F moves two water units from the pool into the hero");
        Check(Mathf.Abs(body.Pollution-.4f)<.02f,"Absorbed dirty water mixes with the carried clean water");
        yield return Wait(2.05f);Aim(new Vector2(28,-.6f));yield return Press(Key.F);yield return Wait(.1f);
        Check(Mathf.Abs(body.Stored-2)<.02f&&Mathf.Abs(eco.pool.Volume-4)<.02f,"Real F returns the carried volume without duplicating bulk water");
        Check(eco.pool.Pollution<.65f,"Exchanging water changes the pool's actual pollution");
        Teleport(new Vector2(23,1));yield return Wait(.12f);float before=body.Stored;
        Check(!body.Absorb()&&Mathf.Abs(body.Stored-before)<.001f,"Solid basin walls prevent absorption through terrain");
        Teleport(new Vector2(17,1));eco.enabled=true;eco.pool.SetState(3,.1f);eco.cistern.SetState(8,.9f);float volume=eco.pool.Volume;
        yield return Wait(.65f);
        Check(eco.pool.Volume>volume&&eco.pool.Pollution>.1f,"High dirty water physically changes the lower pool over time");
        float slow=eco.PipeRate;Teleport(new Vector2(31.6f,1));yield return Wait(.35f);
        Check(eco.bypass.Pressed&&eco.PipeRate>slow*1.8f,"Actual body weight accelerates the shared incoming water pipe");
        Teleport(new Vector2(17,1));yield return Wait(.2f);

        Stage="Buoyancy and shared biology";eco.enabled=false;eco.fungus.enabled=true;eco.pool.SetState(8,.8f);eco.fungus.SetGrowth(.2f);
        var cargo=flow.GetComponentsInChildren<EmergenceBody>().First(b=>b.Has(EmergenceMaterial.Movable));var rb=cargo.GetComponent<Rigidbody2D>();rb.position=new Vector2(26,-1.1f);rb.linearVelocity=Vector2.zero;
        yield return Wait(.8f);Check(rb.position.y>-.9f,"The real movable crate rises with a filled pool");
        eco.pool.SetState(1,.8f);float elevated=rb.position.y;yield return Wait(.7f);Check(rb.position.y<elevated-.1f,"Draining water removes buoyant support from the same crate");
        eco.pool.SetState(8,.8f);Teleport(new Vector2(31.6f,1));Aim(new Vector2(30.2f,-.2f),true);yield return Wait(2.5f);Aim(new Vector2(30.2f,-.2f));
        Check(eco.fungus.Growth>.3f,"Actual water shots and pool moisture feed the living fungus");
        // Continue the same production growth at normal rate; no completion flags are assigned.
        yield return Until(()=>eco.fungus.BridgeGrown,12);
        Check(eco.fungus.bridge.activeSelf,"A mature watered organism creates real one-way crossing platforms");
        float pollution=eco.pool.Pollution;yield return Wait(1);Check(eco.pool.Pollution<pollution-.01f,"The same growing organism removes pollution from the water");
        eco.fungus.SetGrowth(1);eco.enabled=true;yield return Wait(.2f);float unclogged=eco.PipeRate;
        eco.fungus.SetGrowth(1.3f);yield return Wait(.2f);float clogged=eco.PipeRate;Check(eco.fungus.Clogged&&clogged<unclogged*.6f,"Overgrowth restricts the live water pipe");
        eco.fungus.GetComponent<EmergenceBody>().Receive(E_ElementType.Fire);yield return Wait(.1f);Check(!eco.fungus.Clogged&&eco.PipeRate>clogged*1.8f,"The shared fire rule prunes roots and restores flow");
        var healer=flow.GetComponentsInChildren<EnemySupportAura>().First();var enemy=healer.GetComponent<EnemyAIController>();
        enemy.transform.position=eco.fungus.transform.position+Vector3.right*1.5f;eco.fungus.enabled=true;
        eco.fungus.SetGrowth(.55f);float growth=eco.fungus.Growth;typeof(EnemySupportAura).GetMethod("HealOldestTargets",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(healer,null);
        Check(eco.fungus.Growth>growth,"Monster4's real healing pulse also nourishes nearby fungus");eco.fungus.enabled=true;

        Stage="Real body-gene workbench";Teleport(new Vector2(14,1));yield return Wait(.2f);yield return Press(Key.E);
        var editor=UIManager.Instance.GetPanel<GeneEditPanel>();Check(editor.gameObject.activeInHierarchy,"The actual workbench opens for body gene assembly");
        for(int i=0;i<3;i++){editor.SelectSlot(i);editor.Equip(player.Genes.GenInventory.ToList().FindIndex(g=>g.ModifierType==E_ModifierType.BodyTrait&&Mathf.RoundToInt(g.ModifierValue)==i+1));}
        editor.Confirm();Check(body.HasTrait(1)&&body.HasTrait(2)&&body.HasTrait(3),"Confirming actual gene slots activates all three body rules");
        yield return Wait(2);Teleport(new Vector2(17,1));body.Restore(6,0);Aim(new Vector2(17,-2));
        float releaseVelocity=0;
        Action<BackflowPulseReport> recordLaunch=pulse=>{if(!pulse.pulling)releaseVelocity=player.YVelocity;};
        player.Backflow.Pulsed+=recordLaunch;
        yield return Press(Key.F);yield return Wait(.08f);player.Backflow.Pulsed-=recordLaunch;
        Check(releaseVelocity>=6,"The spray gene converts real downward F release into upward movement");
        Check(EcoFungus.Active.Any(f=>f.temporary),"The symbiosis gene leaves a temporary living colony");
        Check(ElementField.Active.Any(f=>f.element==E_ElementType.Water),"Adhesive release leaves a persistent wet circuit trace");
        foreach(var f in EcoFungus.Active.ToArray())if(f.temporary)Object.Destroy(f.gameObject);

        Stage="Electrical supply through a player seed";eco.pool.SetState(8,.8f);Teleport(new Vector2(17,6.8f));yield return Wait(.15f);Aim(new Vector2(16.4f,6.9f));yield return Press(Key.R);
        yield return Until(()=>eco.relay.Charged,8);Check(eco.Power&&eco.PowerName=="水路导电","A real player-sown wet circuit supplies the pump electrically");
        yield return Press(Key.T);yield return Wait(5.5f);
        Stage="Alternative steam supply and sustained clean output";eco.pool.SetState(8,.2f);eco.fungus.SetGrowth(1);Teleport(new Vector2(3.5f,1));yield return Wait(.2f);
        Aim(new Vector2(6,.5f));yield return Press(Key.R);yield return Wait(.4f);yield return Press(Key.Q);Aim(new Vector2(6.4f,.55f));yield return Press(Key.R);
        yield return Until(()=>eco.SteamSeconds>0,8);Check(eco.Power,"Player water and fire seeds create the alternative steam power supply");
        float finishDeadline=Time.time+35,nextSteam=Time.time+22;
        while(!eco.Completed)
        {
            if(Time.time>finishDeadline)throw new Exception("Sustained pump did not complete: "+eco.Goal+" · "+eco.PowerName);
            if(Time.time>=nextSteam)
            {
                // Steam throws its ingredients upward too: replenish them with actual player inputs.
                yield return Press(Key.Q);yield return Press(Key.Q);Aim(new Vector2(6,.5f));yield return Press(Key.R);yield return Wait(.4f);
                yield return Press(Key.Q);Aim(new Vector2(6.4f,.55f));yield return Press(Key.R);nextSteam=Time.time+22;
            }
            yield return Wait(.1f);
        }
        Check(eco.StableSeconds>=20&&flow.ChapterSolved,"Real supply, purity and power sustain clean output for the full 20 seconds");
        ScreenCapture.CaptureScreenshot("Docs/EcosystemValidation/Stable-Pump.png");
        string ids=string.Join("|",player.Genes.GenInventory.Select(g=>g.InstanceId));float carried=body.Stored;
        StorySession.Current.SetValidationMode(false);StorySession.Current.SaveCheckpointState();
        var saved=RunProgress.Read();Check(saved.ecosystem!=null&&saved.ecosystem.complete,"The existing checkpoint contains the ecosystem's completed state");
        int handle=SceneManager.GetActiveScene().handle;RunProgress.ContinueRun();yield return Changed(handle,"GameScene");
        Check(eco.Completed&&Mathf.Abs(body.Stored-carried)<.01f,"Actual scene reload restores the pump and carried water");
        Check(string.Join("|",player.Genes.GenInventory.Select(g=>g.InstanceId))==ids,"Reload preserves the exact body-gene instances and loadout");
        handle=SceneManager.GetActiveScene().handle;Teleport(new Vector2(50,1));yield return Wait(.3f);
        Check(flow.chapter==2&&SceneManager.GetActiveScene().handle==handle&&!player.MenuOpen,"Entering the next district preserves the very same scene and player input");
        Check(Mathf.Abs(body.Stored-carried)<.01f&&body.HasTrait(3),"Water and equipped body rules persist while walking into the forest");
        Check(ChapterArrivalPanel.Current!=null,"The chapter arrival panel appears without a modal next button");
        yield return new WaitForSecondsRealtime(5.2f);Check(ChapterArrivalPanel.Current==null&&Time.timeScale==1,"The chapter panel destroys itself automatically without pausing gameplay");

    }
    private IEnumerator Changed(int handle,string scene)
    {float timeout=Time.realtimeSinceStartup+15;while(SceneManager.GetActiveScene().handle==handle||SceneManager.GetActiveScene().name!=scene||CampaignFlow.Current==null||!CampaignFlow.Current.Ready){if(Time.realtimeSinceStartup>timeout)throw new Exception("Scene transition timeout");yield return null;}yield return null;Bind();yield return Wait(.1f);}
    private IEnumerator Until(Func<bool> condition,float timeout){float until=Time.time+timeout;while(!condition()){if(Time.time>until)throw new Exception("Timed out at "+Stage);yield return null;}}
    private void Teleport(Vector2 at){Keys();var rb=player.GetComponent<Rigidbody2D>();rb.position=at;rb.linearVelocity=Vector2.zero;player.transform.position=at;Physics2D.SyncTransforms();}
    private void Aim(Vector2 at,bool fire=false){mouse.MakeCurrent();var state=new MouseState{position=Camera.main.WorldToScreenPoint(at)};if(fire)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
    private void Keys(params Key[] keys){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    private IEnumerator Press(params Key[] keys){Keys(keys);yield return null;yield return null;Keys();yield return null;yield return null;}
    private static IEnumerator Wait(float seconds){yield return new WaitForSeconds(seconds);}
    private static void Check(bool result,string text){Results.Add((result?"PASS: ":"FAIL: ")+text);if(!result)throw new Exception(text);}
    private void OnDestroy()
    {
        if(!Running)return;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditor;
        Running=false;
    }
}
#endif
