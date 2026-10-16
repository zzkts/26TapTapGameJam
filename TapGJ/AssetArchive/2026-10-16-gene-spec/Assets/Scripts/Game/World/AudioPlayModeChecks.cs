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

/// <summary>Verifies the audio bank through real player actions and the shared reaction event contract.</summary>
public sealed class AudioPlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    public static string Stage { get; private set; }
    private Keyboard keyboard,priorKeyboard;
    private Mouse mouse,priorMouse;
    private InputSettings.BackgroundBehavior priorBackground;
    private InputSettings.EditorInputBehaviorInPlayMode priorEditor;
    private readonly List<Behaviour> suspended=new();
    private readonly List<GameObject> fixtures=new();
    private GameAudioPresentation audio;
    private PlayerController player;
    private float priorEffects,priorListener,priorMusic;
    private bool priorEditorMute;
    private bool hadSave,cleaned;
    private string priorSave;
    public static void Run()
    {
        if(!Application.isPlaying||CampaignFlow.Current==null||!CampaignFlow.Current.Ready)throw new Exception("Enter GameScene first");
        var obj=new GameObject("Water Audio Verification");DontDestroyOnLoad(obj);obj.AddComponent<AudioPlayModeChecks>().Begin();
    }
    private void Begin()
    {
        Results.Clear();Running=true;Succeeded=false;
        hadSave=PlayerPrefs.HasKey(RunProgress.SaveKey);priorSave=PlayerPrefs.GetString(RunProgress.SaveKey);
        priorEffects=GlobalManager.Instance.GameVolume;priorListener=AudioListener.volume;priorMusic=GlobalManager.Instance.BackgroundVolume;
        priorEditorMute=UnityEditor.EditorUtility.audioMasterMute;UnityEditor.EditorUtility.audioMasterMute=false;
        GlobalManager.Instance.SetGameVolume(.75f);AudioListener.volume=1;
        priorKeyboard=Keyboard.current;priorMouse=Mouse.current;
        priorBackground=InputSystem.settings.backgroundBehavior;priorEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();Bind();StartCoroutine(Execute());
    }
    private void Bind()
    {
        audio=GameAudioPresentation.Current;player=CampaignFlow.Current.player;
        StorySession.Current.SetValidationMode(true);player.SetInvincible(true);player.Heal(player.maxHp);
        foreach(var item in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(item.enabled&&(item is EnemyAIController||item is EnemySupportAura||item is PumpLoopSystem))
            {suspended.Add(item);item.enabled=false;}
    }
    private IEnumerator Execute()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try{var e=stack.Peek();if(!e.MoveNext()){stack.Pop();continue;}wait=e.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}}
            catch(Exception e){Results.Add("FAIL: "+Stage+": "+e.Message);break;}
            yield return wait;
        }
        Finish();Destroy(gameObject);
    }
    private IEnumerator Cases()
    {
        Stage="asset playback";
        Check(audio!=null&&audio.LoadedClipCount==31,"All 31 imported WAV cues are loaded by the real runtime audio service");
        var output=new float[2048];AudioListener.GetOutputData(output,0);
        yield return Wait(.3f);GlobalManager.Instance.SetBackgroundVolume(0);int before=audio.CountFor("victory");GameAudioPresentation.PlayCue("victory",.5f);yield return Wait(.15f);
        Check(audio.CountFor("victory")==before+1&&GlobalManager.Instance.GameAudioSource.isPlaying,"An authored cue plays through the shared game-effects source");
        AudioListener.GetOutputData(output,0);GlobalManager.Instance.SetBackgroundVolume(priorMusic);
        Check(output.Any(x=>Mathf.Abs(x)>.00001f),"The Unity game-effects source produces non-silent DSP output");
        Stage="actual water shots and impacts";Teleport(new Vector2(3,1));yield return Wait(.7f);
        int shots=audio.CountFor("water_shot"),impacts=audio.CountFor("water_impact");
        float until=Time.realtimeSinceStartup+.6f;
        while(Time.realtimeSinceStartup<until){Aim(new Vector2(5,-.5f),true);yield return Wait(.05f);}Aim(new Vector2(5,-.5f));yield return Wait(.8f);
        Check(audio.CountFor("water_shot")>shots,"Real left-mouse attacks play water-shot variations");
        Check(audio.CountFor("water_impact")>impacts,"Actual ground contacts play separate wet-impact cues");
        Stage="actual water transfer";
        var tank=CampaignFlow.Current.ecosystem.pool;tank.SetState(4,.7f);
        var body=player.GetComponent<PlayerReservoir>();body.Restore(2,0);Teleport(new Vector2(26.4f,.3f));yield return Wait(.7f);
        before=audio.CountFor("absorb");Aim(new Vector2(27.5f,-.5f));yield return Press(Key.S,Key.F);yield return Wait(.12f);
        Check(body.Stored>3.9f&&audio.CountFor("absorb")==before+1,"Successful S+F absorption plays exactly one gurgling intake cue");
        yield return Wait(2.1f);before=audio.CountFor("release");Aim(new Vector2(28,-.5f));yield return Press(Key.F);yield return Wait(.1f);
        Check(body.Stored<2.1f&&audio.CountFor("release")==before+1,"Successful F release plays the distinct pouring cue");
        body.Restore(0,0);yield return Wait(.15f);before=audio.CountFor("release");
        Check(!body.Release(new Vector2(28,-.5f))&&audio.CountFor("release")==before,"An empty-water attempt does not play a false success sound");
        Stage="actual movement and seeds";Teleport(new Vector2(3,1));yield return Wait(.7f);
        before=audio.CountFor("jump");yield return Press(Key.Space);yield return Wait(.08f);
        Check(audio.CountFor("jump")>before,"A real ground jump produces its soft bubble cue");
        before=audio.CountFor("air_step");yield return Press(Key.Space);yield return Wait(.1f);
        Check(audio.CountFor("air_step")>before,"The actual extra air step has its own bubble cue");
        yield return Wait(1.5f);before=audio.CountFor("dash");Aim(new Vector2(6,1));yield return Press(Key.LeftShift);yield return Wait(.1f);
        Check(audio.CountFor("dash")>before,"A real eight-direction dash produces a wet whoosh");
        Teleport(new Vector2(3,1));yield return Wait(.7f);Aim((Vector2)player.GetComponent<Collider2D>().bounds.center+new Vector2(2,1.8f));
        before=audio.CountFor("seed");shots=audio.CountFor("water_shot");yield return Press(Key.R);
        Check(audio.CountFor("seed")==before+1&&audio.CountFor("water_shot")==shots,"Actual sowing plays its seed cue without being mistaken for weapon fire");
        before=audio.CountFor("recall");yield return Press(Key.T);
        Check(audio.CountFor("recall")==before+1,"Real seed recall plays its returning-water cue");
        yield return Wait(.4f);
        var wall=new GameObject("Audio QA placement wall");fixtures.Add(wall);wall.layer=LayerMask.NameToLayer("Wall");
        wall.transform.position=(Vector2)player.transform.position+new Vector2(.55f,1);
        wall.AddComponent<BoxCollider2D>().size=new Vector2(.2f,4);Physics2D.SyncTransforms();
        before=audio.CountFor("seed");bool blocked=player.GetComponent<PlayerSynthesis>().Sow((Vector2)player.transform.position+new Vector2(3,1));
        Check(!blocked&&audio.CountFor("seed")==before,"The corrected ground-level placement still respects a nearby solid wall and stays silent on failure");
        Destroy(wall);yield return Frames(2);
        Stage="reaction event routing";
        foreach(string kind in new[]{"steam","conduction","growth","explosion","fracture"})
        {
            before=audio.CountFor(kind);EmergenceReactions.Emit(kind,player.transform.position,"Audio event contract",1);
            Check(audio.CountFor(kind)==before+1,"The shared "+kind+" reaction routes to its matching cue");yield return Wait(.3f);
        }
        before=audio.CountFor("steam");EmergenceReactions.Emit("steam",(Vector2)player.transform.position+Vector2.right*60,"Out of earshot",1);
        Check(audio.CountFor("steam")==before,"Off-screen distant reactions do not flood the player's sound field");
        yield return Wait(.3f);before=audio.CountFor("conduction");
        for(int i=0;i<20;i++)EmergenceReactions.Emit("conduction",player.transform.position,"Same-frame cascade",1);
        Check(audio.CountFor("conduction")==before+1,"Twenty same-frame conduction events are coalesced to one cue");
        Stage="pause and mute";player.SetMenuOpen(true);yield return Wait(.1f);
        before=audio.CountFor("explosion");GameAudioPresentation.PlayWorld("explosion",player.transform.position);
        Check(audio.CountFor("explosion")==before,"World sound cues are suppressed during a modal pause");
        before=audio.CountFor("ui");GameAudioPresentation.PlayCue("ui");yield return Wait(.02f);
        Check(audio.CountFor("ui")==before+1,"UI cues remain usable while gameplay is paused");
        float music=GlobalManager.Instance.BackgroundVolume;GlobalManager.Instance.SetGameVolume(0);before=audio.PlayedCount;
        Check(GameAudioPresentation.PlayCue("chest")&&GameAudioPresentation.PlayCue("water_shot"),"Muted sounds are handled without falling back to separate unmuted sources");
        Check(audio.PlayedCount==before&&GlobalManager.Instance.BackgroundVolume==music,"The effects slider mutes cues without changing the music volume");
        GlobalManager.Instance.SetGameVolume(.75f);player.SetMenuOpen(false);yield return Wait(.15f);
        Stage="result UI cue";before=audio.CountFor("victory");StorySession.Current.dialogue.ShowResult(true,60,6);yield return Wait(.05f);
        Check(audio.CountFor("victory")==before+1,"The actual result panel requests the victory cue");StorySession.Current.SetValidationMode(true);
        Stage="scene reload lifecycle";int handle=SceneManager.GetActiveScene().handle;int audioId=audio.GetInstanceID();
        StorySession.Current.SetValidationMode(false);StorySession.Current.SaveCheckpoint(new Vector2(3,1));RunProgress.ContinueRun();
        float timeout=Time.realtimeSinceStartup+15;
        while(SceneManager.GetActiveScene().handle==handle||CampaignFlow.Current==null||!CampaignFlow.Current.Ready)
        {if(Time.realtimeSinceStartup>timeout)throw new Exception("Reload timeout");yield return null;}
        Bind();yield return Wait(.2f);
        Check(audio.GetInstanceID()==audioId&&audio.LoadedClipCount==31,"Restart preserves one audio service and the imported bank");
        Check(Object.FindObjectsByType<GameAudioPresentation>(FindObjectsSortMode.None).Length==1,"Reload does not create overlapping audio services");
        Check(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l=>l.enabled)==1,"Reload retains exactly one enabled listener");
        before=audio.CountFor("growth");EmergenceReactions.Emit("growth",player.transform.position,"After reload",1);
        Check(audio.CountFor("growth")==before+1,"The reaction subscription remains single after reload");Stage="complete";
    }
    private void Teleport(Vector2 at){Keys();var rb=player.GetComponent<Rigidbody2D>();rb.position=at;rb.linearVelocity=Vector2.zero;player.transform.position=at;Physics2D.SyncTransforms();}
    private void Aim(Vector2 at,bool fire=false){mouse.MakeCurrent();var state=new MouseState{position=Camera.main.WorldToScreenPoint(at)};if(fire)state=state.WithButton(MouseButton.Left);InputSystem.QueueStateEvent(mouse,state);}
    private void Keys(params Key[] keys){keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
    private IEnumerator Press(params Key[] keys){Keys(keys);yield return Frames(3);Keys();yield return Frames(2);}
    private static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
    private static IEnumerator Wait(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)yield return null;}
    private static void Check(bool condition,string message){Results.Add((condition?"PASS: ":"FAIL: ")+message);if(!condition)throw new Exception(message);}
    private void Finish()
    {
        if(cleaned)return;cleaned=true;
        foreach(var fixture in fixtures)if(fixture!=null)Destroy(fixture);
        if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);
        priorKeyboard?.MakeCurrent();priorMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=priorBackground;InputSystem.settings.editorInputBehaviorInPlayMode=priorEditor;
        GlobalManager.Instance.SetGameVolume(priorEffects);GlobalManager.Instance.SetBackgroundVolume(priorMusic);AudioListener.volume=priorListener;UnityEditor.EditorUtility.audioMasterMute=priorEditorMute;
        foreach(var item in suspended)if(item!=null)item.enabled=true;
        if(player!=null){player.SetInvincible(false);player.SetMenuOpen(false);}StorySession.Current?.SetValidationMode(false);
        if(hadSave)PlayerPrefs.SetString(RunProgress.SaveKey,priorSave);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);PlayerPrefs.Save();
        Time.timeScale=1;Running=false;Succeeded=Results.Count>0&&Results.All(r=>r.StartsWith("PASS:"));
        Debug.Log("Audio verification: "+string.Join("\n",Results));
    }
    private void OnDestroy(){if(!cleaned)Finish();}
}
#endif
