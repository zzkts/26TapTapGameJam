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

public sealed class CampaignPlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running{get;private set;}
    public static bool Succeeded{get;private set;}
    public static string Stage{get;private set;}
    private Keyboard keyboard,oldKeyboard;
    private Mouse mouse,oldMouse;
    private InputSettings.BackgroundBehavior oldBackground;
    private InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    private readonly List<Behaviour> suspended=new();
    private PlayerController player;
    private PlayerSynthesis ability;
    private CampaignFlow flow;
    private bool hadSave,hadEndings,cleaned;
    private string priorSave;
    private int priorEndings;
    public static void Run()
    {
        if(!Application.isPlaying||CampaignFlow.Current==null)throw new InvalidOperationException("Enter chapter one Play Mode first");
        var obj=new GameObject("Campaign Verification");DontDestroyOnLoad(obj);obj.AddComponent<CampaignPlayModeChecks>().Begin();
    }
    private void Begin()
    {
        Results.Clear();Running=true;Succeeded=false;
        hadSave=PlayerPrefs.HasKey(RunProgress.SaveKey);priorSave=PlayerPrefs.GetString(RunProgress.SaveKey);
        hadEndings=PlayerPrefs.HasKey(RunProgress.EndingKey);priorEndings=PlayerPrefs.GetInt(RunProgress.EndingKey);
        oldKeyboard=Keyboard.current;oldMouse=Mouse.current;
        oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();Bind();StartCoroutine(Execute());
    }
    private void Bind()
    {
        flow=CampaignFlow.Current;player=flow.player;ability=player.GetComponent<PlayerSynthesis>();
        StorySession.Current.SetValidationMode(true);player.SetInvincible(true);player.Heal(player.MaxHp);
        foreach(Behaviour b in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(b.enabled&&(b is EnemyAIController||b is EnemySupportAura)){suspended.Add(b);b.enabled=false;}
    }
    private IEnumerator Execute()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try{var item=stack.Peek();if(!item.MoveNext()){stack.Pop();continue;}wait=item.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}}
            catch(Exception error){Results.Add("FAIL: "+Stage+": "+error);break;}
            yield return wait;
        }
        Finish();Destroy(gameObject);
    }
    private IEnumerator Cases()
    {
        Stage="chapter one and clear navigation";
        Check(flow.chapter==1&&flow.Powered==0&&flow.reactors.Length==3,"Chapter one begins with three independent branch goals");
        Check(flow.exit.GetComponentsInChildren<Collider2D>().All(c=>c.isTrigger)&&flow.GetComponentsInChildren<EmergenceGate>().Length==0,"Progression objects cannot form a solid full-height roadblock");
        Check(player.DashUnlocked&&ability!=null&&player.CoyoteTime>=.22f,"The actual hero begins with dash, synthesis and the enlarged movement grace window");
        Check(StorySession.Current.objective.target.text.Contains("任意两座"),"The real HUD tells the player exactly how to finish this chapter");
        yield return Press(Key.M);Check(CampaignUI.Current.MapOpen&&player.MenuOpen&&Time.timeScale==0,"M opens the actual goal map and pauses gameplay");
        yield return Press(Key.Escape);Check(!player.MenuOpen&&Time.timeScale==1,"Escape resumes from the map without opening another menu");
        Teleport(new Vector2(17,1));yield return Wait(.2f);Keys(Key.D);yield return Wait(1.8f);Keys();
        Check(player.transform.position.x>21.5f&&!flow.ChapterSolved,"Real D input crosses the still-unsolved central exit without being blocked");
        Teleport(new Vector2(17,1));yield return Wait(.25f);Aim(new Vector2(17,5));yield return Frames(2);yield return Press(Key.LeftShift);
        Check(player.IsDashing&&player.YVelocity>7,"Real Shift input dashes upward toward the aim point rather than forcing a horizontal dash");
        yield return Wait(1.2f);

        Stage="hero air step and seed momentum";
        Teleport(new Vector2(17,1));yield return Wait(.25f);yield return Press(Key.Space);yield return Wait(.18f);
        int steps=ability.AirSteps;yield return Press(Key.Space);
        Check(ability.AirSteps==steps+1&&player.YVelocity>3,"A second real Space press produces the bubble air step");
        yield return Press(Key.Space);Check(ability.AirSteps==steps+1,"A third airborne press cannot become an unlimited air jump");
        yield return Wait(1.4f);
        Teleport(new Vector2(7,1));yield return Wait(.25f);Aim(new Vector2(6.2f,.5f));yield return Frames(2);yield return Press(Key.R);
        Check(ability.Seeds.Count(s=>s!=null)==1&&ability.Seeds[0].Body.Wet,"Real R input creates the player's own wet conductive material");
        yield return Until(()=>flow.reactors[0].Complete,7);
        Check(flow.reactors[0].Complete&&flow.Powered==1,"The deployed water seed's real fields fill and complete the pressure reactor");
        ReactionSeed first=ability.Seeds.First(s=>s!=null);float before=first.transform.position.x;
        Aim(first.transform.position);yield return Frames(2);yield return Press(Key.F);yield return Wait(.15f);
        Check(Mathf.Abs(first.transform.position.x-before)>.04f,"F applies real momentum to an owned wet seed through the shared material rule");
        yield return Press(Key.T);Check(ability.Seeds.Count==0,"T recalls owned materials and clears the live seed list");

        Stage="weight alternatives and chapter exit";
        float plateX=flow.reactors[1].plate.transform.position.x;
        Teleport(new Vector2(plateX-1.8f,1));yield return Wait(.25f);
        Check(!flow.reactors[1].plate.Pressed,"The exposed pressure plate is initially free of the player's mass");
        Aim(new Vector2(plateX,.42f));yield return Frames(2);yield return Press(Key.R);
        Check(ability.Seeds.Any(s=>s!=null),"Sowing from underneath a one-way catwalk follows its pass-through rule");
        yield return Until(()=>flow.reactors[1].Complete,7);
        Check(flow.reactors[1].plate.Pressed&&flow.ChapterSolved,"A deployed seed's real rigidbody mass supplies a second, independent route solution");
        yield return Press(Key.T);Teleport(new Vector2(plateX,1));yield return Wait(.3f);
        Check(flow.reactors[1].plate.Pressed,"The same exposed plate also accepts the actual player's body");
        Check(flow.Goal.Contains("中央出口"),"The objective changes from solving nodes to returning to the central exit");
        Stage="real vertical branch and portable circuit";
        Teleport(new Vector2(11.2f,1));yield return Wait(.3f);
        yield return JumpTo(Key.D,.7f,1);yield return JumpTo(Key.D,.45f,2);yield return JumpTo(Key.D,.8f,3);
        yield return JumpTo(Key.D,.8f,4);yield return JumpTo(Key.A,.6f,5);yield return JumpTo(Key.None,.7f,6);
        Aim(new Vector2(17.2f,6.42f));yield return Frames(2);yield return Press(Key.R);
        yield return Until(()=>flow.reactors[2].Complete,8);
        Check(flow.Powered==3&&flow.reactors[2].material.Charged,"A player-sown water material completes the optional upper electrical circuit");
        Check(EmergenceProgress.Current.Discoveries.ContainsKey("conduction"),"The portable circuit produces an actual multi-node conduction discovery");
        yield return Press(Key.T);
        string ids=string.Join("|",player.Genes.GenInventory.Select(g=>g.InstanceId));
        Teleport(new Vector2(20,1));yield return Wait(.3f);yield return Press(Key.E);
        Check(player.MenuOpen&&CampaignUI.Current.advanceButton.gameObject.activeInHierarchy,"Real E opens an explicit chapter-complete screen");
        int handle=SceneManager.GetActiveScene().handle;CampaignUI.Current.advanceButton.onClick.Invoke();yield return ChapterReady(handle,"SporeScene");
        Check(flow.chapter==2&&flow.Powered==0,"The chapter-complete button enters a separate second environment with fresh local goals");
        Check(string.Join("|",player.Genes.GenInventory.Select(g=>g.InstanceId))==ids,"Chapter transition preserves the exact acquired gene instances");
        Check(RunProgress.Read().scene=="SporeScene"&&RunProgress.Read().campaignLayout,"The checkpoint names the actual next chapter scene");

        Stage="player-authored biological cascade";
        Teleport(new Vector2(6,1));yield return Wait(.25f);Aim(new Vector2(5.2f,.5f));yield return Frames(2);yield return Press(Key.R);
        yield return Until(()=>flow.reactors[0].Complete,6);
        Check(flow.reactors[0].material.Bloomed,"A player-deployed water seed grows the actual mother colony in chapter two");
        yield return Press(Key.T);Teleport(new Vector2(30,1));yield return Wait(.25f);
        yield return Press(Key.Q);yield return Press(Key.Q);Check(ability.Selected==E_ElementType.Poison,"Two real Q presses select the poison seed");
        Aim(new Vector2(31.7f,.5f));yield return Frames(2);yield return Press(Key.R);yield return Wait(.45f);
        yield return Press(Key.Q);yield return Press(Key.Q);Check(ability.Selected==E_ElementType.Fire,"The same cyclic selection reaches the fire seed");
        Aim(new Vector2(30.8f,.5f));yield return Frames(2);yield return Press(Key.R);
        yield return Until(()=>flow.reactors[1].Complete,7);
        Check(flow.reactors[1].material.Broken&&flow.Powered==2,"The player's own fire and poison materials create a real cascade that breaks the colony shell");
        Check(EmergenceProgress.Current.Discoveries.ContainsKey("explosion"),"The self-authored reaction records its discovery in the shared handbook");

        Stage="death retry in the second chapter";
        StorySession.Current.SetValidationMode(false);StorySession.Current.SaveCheckpointState();
        player.SetInvincible(false);player.TakeDamage(100);yield return new WaitForSecondsRealtime(.85f);
        var menu=Object.FindFirstObjectByType<RunMenuPanel>(FindObjectsInactive.Include);
        Check(player.IsDead&&menu.gameObject.activeInHierarchy,"Death opens the real retry menu inside chapter two");
        handle=SceneManager.GetActiveScene().handle;menu.retryButton.onClick.Invoke();yield return ChapterReady(handle,"SporeScene");
        Check(flow.chapter==2&&flow.Powered==2&&player.Hp==player.MaxHp,"Actual Retry restores the same chapter, completed objectives and full health");
        Check(flow.reactors[1].material.Broken,"A shattered shell stays shattered after a real death scene reload");
        Teleport(new Vector2(20,1));yield return Wait(.25f);yield return Press(Key.E);
        handle=SceneManager.GetActiveScene().handle;CampaignUI.Current.advanceButton.onClick.Invoke();yield return ChapterReady(handle,"CoreScene");
        Check(flow.chapter==3&&flow.Powered==0&&!flow.level.BattleStarted,"The second exit enters the final chapter before its encounter is activated");
        Check(flow.Goal.Contains("唤醒守卫"),"The final HUD describes the node-to-battle prerequisite");

        Stage="core circuit and final victory";
        Teleport(new Vector2(6,1));yield return Wait(.2f);Aim(new Vector2(5.2f,.5f));yield return Frames(2);yield return Press(Key.R);
        yield return Until(()=>flow.reactors[0].Complete,7);yield return Press(Key.T);
        plateX=flow.reactors[1].plate.transform.position.x;Teleport(new Vector2(plateX,1));yield return Wait(.8f);
        Check(flow.ChapterSolved&&flow.level.BattleStarted&&flow.level.guardians.All(g=>g.gameObject.activeInHierarchy),"Two real reactor solutions wake all three assigned core guardians");
        Teleport(new Vector2(14,1));yield return Wait(.2f);yield return Press(Key.E);
        GeneEditPanel editor=UIManager.Instance.GetPanel<GeneEditPanel>();
        Check(editor.gameObject.activeInHierarchy,"The central terminal still opens the actual gene workbench in chapter three");
        for(int i=0;i<Mathf.Min(6,player.Genes.GenInventory.Count);i++){editor.SelectSlot(i);editor.Equip(i);}editor.Confirm();
        foreach(var guardian in flow.level.guardians)
        {
            Teleport(new Vector2(guardian.transform.position.x-3,guardian.transform.position.y+1));yield return Wait(.15f);
            float deadline=Time.realtimeSinceStartup+25;
            while(guardian!=null&&!guardian.IsDead&&Time.realtimeSinceStartup<deadline)
            {Aim(guardian.GetComponent<Collider2D>().bounds.center,true);yield return Wait(.08f);}
            Aim(player.transform.position+Vector3.right*3,false);
            Check(guardian==null||guardian.IsDead,"Real left-mouse projectiles defeat the activated core guardian");
        }
        yield return Wait(.2f);
        Check(flow.level.CoreDefeated&&StorySession.Current.GuardianCleared,"The encounter's actual death events satisfy the final win prerequisite");
        Check(flow.Goal.Contains("选择"),"The completed encounter explicitly directs the player back to the final decision");
        StorySession.Current.SetValidationMode(false);Teleport(new Vector2(20,1));yield return Wait(.2f);yield return Press(Key.E);
        var dialogue=StorySession.Current.dialogue;Check(dialogue.IsDialogue,"E at the cleared central valve opens the final narrative");
        dialogue.skip.onClick.Invoke();yield return Frames(2);Check(dialogue.IsChoosing,"Completing the narrative presents the actual final choice buttons");
        dialogue.coexist.onClick.Invoke();yield return Frames(2);dialogue.skip.onClick.Invoke();yield return Frames(2);
        Check(dialogue.IsResult&&RunProgress.Read().completed&&!RunProgress.CanContinue,"The choice reaches a persisted victory result and disables Continue for that finished run");
        Check(dialogue.endingTitle.text=="水的归处","The full three-chapter route reaches its correct ending title");
        Stage="complete";
    }
    private IEnumerator ChapterReady(int old,string scene)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while(SceneManager.GetActiveScene().handle==old||SceneManager.GetActiveScene().name!=scene||CampaignFlow.Current==null||!CampaignFlow.Current.Ready)
        {if(Time.realtimeSinceStartup>deadline)throw new InvalidOperationException("Scene transition timed out: "+scene);yield return null;}
        yield return Frames(2);Bind();yield return Frames(2);
    }
    private IEnumerator Until(Func<bool> predicate,float seconds)
    {float deadline=Time.time+seconds;while(!predicate()){if(Time.time>deadline)throw new InvalidOperationException("Actual mechanism did not complete within "+seconds+" seconds");yield return null;}}
    private IEnumerator JumpTo(Key direction,float seconds,float top)
    {
        if(direction==Key.None)Keys(Key.Space);else Keys(direction,Key.Space);
        yield return Wait(seconds);Keys();yield return Wait(1.1f);
        float foot=player.GetComponent<Collider2D>().bounds.min.y;
        Check(player.IsGrounded()&&foot>=top-.05f&&foot<top+.15f,"Real jump input reaches vertical branch platform at "+top+" metres (x="+player.transform.position.x.ToString("0.00")+", foot="+foot.ToString("0.00")+")");
    }
    private void Teleport(Vector2 at){Keys();var rb=player.GetComponent<Rigidbody2D>();rb.position=at;rb.linearVelocity=Vector2.zero;player.transform.position=at;Physics2D.SyncTransforms();}
    private void Aim(Vector2 point,bool fire=false){mouse.MakeCurrent();var at=Camera.main.WorldToScreenPoint(point);MouseState state=new(){position=at};if(fire)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
    private void Keys(params Key[] keys){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    private IEnumerator Press(params Key[] keys){Keys(keys);yield return Frames(2);Keys();yield return Frames(2);}
    private static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    private static IEnumerator Wait(float seconds){yield return new WaitForSeconds(seconds);}
    private static void Check(bool yes,string text){Results.Add((yes?"PASS: ":"FAIL: ")+text);if(!yes)throw new InvalidOperationException(text);}
    private void Finish()
    {
        if(cleaned)return;cleaned=true;
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
        foreach(var b in suspended)if(b!=null)b.enabled=true;
        if(player!=null){player.SetInvincible(false);player.SetMenuOpen(false);}StorySession.Current?.SetValidationMode(false);
        if(hadSave)PlayerPrefs.SetString(RunProgress.SaveKey,priorSave);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);
        if(hadEndings)PlayerPrefs.SetInt(RunProgress.EndingKey,priorEndings);else PlayerPrefs.DeleteKey(RunProgress.EndingKey);PlayerPrefs.Save();
        Time.timeScale=1;Running=false;Succeeded=Results.Count>0&&Results.All(r=>r.StartsWith("PASS:"));Debug.Log("Campaign verification: "+string.Join("\n",Results));
    }
    private void OnDestroy(){if(!cleaned)Finish();}
}
#endif
