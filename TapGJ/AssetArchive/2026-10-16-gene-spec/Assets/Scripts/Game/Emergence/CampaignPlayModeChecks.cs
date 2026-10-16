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
    private bool hadSave,hadEndings,cleaned,finalSection;
    private string priorSave;
    private int priorEndings;
    public static void Run()
    {
        if(!Application.isPlaying||CampaignFlow.Current==null)throw new InvalidOperationException("Enter chapter one Play Mode first");
        var obj=new GameObject("Campaign Verification");DontDestroyOnLoad(obj);obj.AddComponent<CampaignPlayModeChecks>().Begin();
    }
    public static void RunFinalSection()
    {
        var obj=new GameObject("Final Watershed Verification");DontDestroyOnLoad(obj);
        var check=obj.AddComponent<CampaignPlayModeChecks>();check.finalSection=true;check.Begin();
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
        var stack=new Stack<IEnumerator>();stack.Push(finalSection?FinalCases():Cases());
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
        int handle=SceneManager.GetActiveScene().handle;
        Stage="continuous traversal";Check(player.transform.position.x<4,"A fresh run starts at the left edge");
        float deadline=Time.time+65,nextJump=0,lastX=player.transform.position.x,nextProbe=Time.time+4;
        while(player.transform.position.x<140)
        {
            if(Time.time>deadline)throw new Exception("Traversal timeout at "+player.transform.position);
            if(player.IsGrounded()&&Time.time>=nextJump){Keys(Key.D,Key.Space);nextJump=Time.time+1.2f;}else Keys(Key.D);
            if(Time.time>=nextProbe){Check(player.transform.position.x>lastX+.5f,"Actual movement clears the corridor near x="+lastX.ToString("0"));lastX=player.transform.position.x;nextProbe=Time.time+4;}
            yield return Wait(.06f);
        }
        Keys();Check(SceneManager.GetActiveScene().handle==handle,"The whole left-to-right route uses one scene with no chapter reload");
        Teleport(new Vector2(3,1));yield return Wait(.2f);
        Stage="living water and player body rules";
        var ecoTest=new GameObject("Living Watershed Verification").AddComponent<EcosystemPlayModeChecks>();ecoTest.Begin();
        while(EcosystemPlayModeChecks.Running)yield return null;
        Results.AddRange(EcosystemPlayModeChecks.Results);Check(EcosystemPlayModeChecks.Succeeded,"Live water, filtration, both power routes, body genes and save reload pass");Destroy(ecoTest.gameObject);
        Bind();keyboard.MakeCurrent();mouse.MakeCurrent();handle=SceneManager.GetActiveScene().handle;
        Stage="death and checkpoint retry";StorySession.Current.SetValidationMode(false);StorySession.Current.SaveCheckpoint(new Vector2(50,1));
        player.SetInvincible(false);player.TakeDamage(999);yield return new WaitForSecondsRealtime(1);
        var retry=Object.FindFirstObjectByType<RunMenuPanel>(FindObjectsInactive.Include);
        Check(player.IsDead&&retry!=null&&retry.gameObject.activeInHierarchy,"Actual fatal damage opens the actual retry interface");
        retry.retryButton.onClick.Invoke();yield return ChapterReady(handle,"GameScene");handle=SceneManager.GetActiveScene().handle;
        Check(!player.IsDead&&Mathf.Abs(player.transform.position.x-50)<.3f&&flow.ecosystem.Completed,"Retry returns to the same-map forest checkpoint and preserves the upstream ecosystem");
        var network=flow.GetComponent<WatershedNetwork>();
        Stage="upstream changes downstream";yield return Until(()=>network.forest.Bloomed,10);
        Check(network.forestBridge.activeSelf,"Upstream clean water irrigates a real downstream organism and grows a real shortcut");
        var shell=flow.reactors.First(n=>n.district==2&&n.rule==CampaignReactor.Rule.Fracture);
        Teleport(new Vector2(77,1));yield return Wait(.2f);
        float fireUntil=Time.time+14;
        while(!shell.Complete&&Time.time<fireUntil){Aim(new Vector2(80,.8f),true);yield return Wait(.08f);}
        Aim(Vector2.right);Check(shell.Complete&&flow.DistrictSolved(2),"Real shots break the optional shell; biology plus fracture connects the forest");
        Stage="reaction relay reward";Teleport(new Vector2(63,1));yield return Wait(.1f);ability.Recall();float before=ability.Energy;
        var a=new GameObject("QA wet boiler");a.transform.position=new Vector2(63,1);var boil=a.AddComponent<EmergenceBody>();boil.material=EmergenceMaterial.Boiler;
        var b=new GameObject("QA living material");b.transform.position=new Vector2(65,1);var living=b.AddComponent<EmergenceBody>();living.material=EmergenceMaterial.Organic;
        int bursts=network.ResonanceBursts;boil.Receive(E_ElementType.Water);boil.Receive(E_ElementType.Fire);living.Receive(E_ElementType.Water);
        var fragile=new GameObject("QA fragile material");fragile.transform.position=new Vector2(64,1);var breakable=fragile.AddComponent<EmergenceBody>();breakable.material=EmergenceMaterial.Fragile;breakable.Damage(20);
        Check(network.ResonanceBursts>bursts,"Three actual distinct material reactions trigger the player's resonance reward");Destroy(a);Destroy(b);Destroy(fragile);
        yield return FinalCases();
    }
    private IEnumerator FinalCases()
    {
        int handle=SceneManager.GetActiveScene().handle;
        Stage="core reactions and battle";
        // A long verification teleport must let the smooth-follow camera settle before converting a world aim into mouse pixels.
        Teleport(new Vector2(99.2f,1));yield return Wait(1.2f);Aim(new Vector2(100.8f,.5f));yield return Frames(2);yield return Press(Key.R);
        var pressure=flow.reactors.First(n=>n.district==3&&n.rule==CampaignReactor.Rule.Pressure);
        yield return Until(()=>pressure.Complete,15);yield return Press(Key.T);
        var plate=flow.reactors.First(n=>n.district==3&&n.rule==CampaignReactor.Rule.Weight);
        Teleport(new Vector2(plate.plate.transform.position.x,1));yield return Wait(.8f);
        Check(flow.WatershedSolved&&flow.level.BattleStarted,"Real full-water-network prerequisites wake the three final guardians");
        Check(SceneManager.GetActiveScene().handle==handle,"Downstream puzzles and guardian activation still preserve the same scene");
        // Use the actual gene workbench and weapon inputs for the final encounter.
        Teleport(new Vector2(110,1));yield return Wait(.2f);yield return Press(Key.E);
        var editor=UIManager.Instance.GetPanel<GeneEditPanel>();Check(editor.gameObject.activeInHierarchy,"The downstream gene terminal opens normally");
        var combat=player.Genes.GenInventory.Where(g=>g.ModifierType!=E_ModifierType.BodyTrait).Take(6).ToArray();
        for(int i=0;i<combat.Length;i++){editor.SelectSlot(i);editor.Equip(player.Genes.GenInventory.ToList().IndexOf(combat[i]));}editor.Confirm();
        foreach(var guardian in flow.level.guardians)
        {
            Teleport(new Vector2(guardian.transform.position.x-3,guardian.transform.position.y+1));yield return Wait(.15f);
            float until=Time.time+30;
            while(guardian!=null&&!guardian.IsDead&&Time.time<until){Aim(guardian.GetComponent<Collider2D>().bounds.center,true);yield return Wait(.08f);}
            Aim(player.transform.position+Vector3.right*3);Check(guardian==null||guardian.IsDead,"Actual water projectiles defeat a final guardian");
        }
        yield return Wait(.2f);Check(flow.level.CoreDefeated&&StorySession.Current.GuardianCleared,"All three actual deaths unlock the final valve");
        StorySession.Current.SetValidationMode(false);Teleport(new Vector2(138,1));yield return Wait(.2f);yield return Press(Key.E);
        var dialogue=StorySession.Current.dialogue;Check(dialogue.IsDialogue,"E at the far-right valve opens the final narrative");
        dialogue.skip.onClick.Invoke();yield return Frames(2);Check(dialogue.IsChoosing,"The final narrative presents the real ending choices");
        dialogue.coexist.onClick.Invoke();yield return Frames(2);dialogue.skip.onClick.Invoke();yield return Frames(2);
        Check(dialogue.IsResult&&RunProgress.Read().completed&&!RunProgress.CanContinue,"The ending persists victory and completes the run without a build");
        Check(dialogue.endingStats.text.Contains("3 / 3 区域恢复"),"The final result counts the real restored watershed instead of obsolete gate counters");
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
