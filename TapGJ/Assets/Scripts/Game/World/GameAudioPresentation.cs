using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Authored WAV cues, shared volume controls and bounded, distance-aware reaction feedback.</summary>
[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
public sealed class GameAudioPresentation : MonoBehaviour
{
    private const int SampleRate=22050;
    private static GameAudioPresentation current;
    private GlobalManager volumes;
    private AudioSource ambience,effects;
    private AudioListener fallbackListener;
    private AudioClip room;
    private PlayerController player;
    private GeneInventory inventory;
    private int hurts,jumps,landings,variant;
    private bool wasDashing,wasModal,wasDead;
    private float nextPlayerSearch,lastClick=-1;
    private readonly Dictionary<string,AudioClip> clips=new();
    private readonly Dictionary<string,float> nextCue=new();
    private readonly Dictionary<string,int> played=new();
    private readonly List<float> voices=new();
    private readonly List<RaycastResult> uiHits=new();
    private readonly List<AudioClip> owned=new();
    public static GameAudioPresentation Current=>current;
    public string LastCue { get; private set; }
    public int PlayedCount { get; private set; }
    public int LoadedClipCount=>clips.Count;
    public int CountFor(string cue)=>played.TryGetValue(cue,out int count)?count:0;
    public static readonly string[] CueNames={"water_shot_01","water_shot_02","water_shot_03","water_impact_01","water_impact_02","water_impact_03","jump","land","dash","gene","heal","hurt","death","steam","conduction","explosion","chapter","victory","chest","ui","fire_shot","poison_shot"};
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession(){current=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if(current!=null)return;
        var existing=FindAnyObjectByType<GameAudioPresentation>();
        if(existing!=null){current=existing;return;}
        new GameObject("Water Drain Audio").AddComponent<GameAudioPresentation>();
    }
    private void Awake()
    {
        if(current!=null&&current!=this){Destroy(gameObject);return;}
        current=this;DontDestroyOnLoad(gameObject);volumes=GlobalManager.Instance;
        ambience=volumes.BackgroundAudioSource;effects=volumes.GameAudioSource;
        ambience.playOnAwake=effects.playOnAwake=false;
        ambience.spatialBlend=effects.spatialBlend=0;
        ambience.ignoreListenerPause=effects.ignoreListenerPause=true;
        foreach(string name in CueNames)
        {
            var clip=Resources.Load<AudioClip>("Audio/WaterDrain/"+name);
            if(clip!=null)clips[name]=clip;
        }
        // Keep the quiet cavern bed underneath the separate, scene-specific music sources.
        room=CreateRoom();owned.Add(room);ambience.clip=room;ambience.loop=true;ambience.Play();
        if(GetComponent<SceneMusicPlayer>()==null)gameObject.AddComponent<SceneMusicPlayer>();
        SceneManager.sceneLoaded+=OnSceneLoaded;EmergenceReactions.Happened+=OnReaction;
        EnsureListener();BindPlayer();
    }
    public static bool PlayCue(string cue,float gain=1)
    {return Application.isPlaying&&current!=null&&current.PlayLocal(cue,gain);}
    public static bool PlayWorld(string cue,Vector2 at,float gain=1)
    {
        if(!Application.isPlaying||current==null||Time.timeScale<=0||current.player!=null&&(current.player.MenuOpen||current.player.IsDead))return false;
        Vector2 ear=current.player!=null?(Vector2)current.player.transform.position:Camera.main!=null?(Vector2)Camera.main.transform.position:at;
        float distance=Vector2.Distance(ear,at);if(distance>=22)return false;
        return current.PlayLocal(cue,gain*Mathf.Lerp(1,0,Mathf.InverseLerp(6,22,distance)));
    }
    private bool PlayLocal(string cue,float gain)
    {
        string key=cue;
        if(cue=="water_shot"||cue=="water_impact")key=cue+"_"+((variant++%3)+1).ToString("00");
        if(effects==null||!clips.TryGetValue(key,out AudioClip clip)||clip==null)return false;
        // A muted cue is handled: callers must not fall back to an unmuted, separate source.
        if(effects.volume<=0||AudioListener.volume<=0||gain<=0)return true;
        float now=Time.unscaledTime;
        if(nextCue.TryGetValue(cue,out float until)&&now<until)return true;
        voices.RemoveAll(end=>end<=now);if(voices.Count>=16)return true;
        nextCue[cue]=now+(cue=="water_impact"?.07f:cue=="water_shot"?.075f:cue=="steam"||cue=="conduction"?.22f:.09f);
        effects.PlayOneShot(clip,Mathf.Clamp01(gain));voices.Add(now+clip.length);
        LastCue=cue;PlayedCount++;played[cue]=CountFor(cue)+1;return true;
    }
    private void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        effects.Stop();voices.Clear();nextCue.Clear();EnsureListener();BindPlayer();
        nextPlayerSearch=Time.unscaledTime+.3f;wasModal=false;
    }
    private void EnsureListener()
    {
        bool found=false;
        foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            if(listener!=fallbackListener&&listener.enabled&&listener.gameObject.activeInHierarchy)found=true;
        if(found){if(fallbackListener!=null)fallbackListener.enabled=false;}
        else{if(fallbackListener==null)fallbackListener=gameObject.AddComponent<AudioListener>();fallbackListener.enabled=true;}
    }
    private void BindPlayer()
    {
        if(inventory!=null)inventory.Acquired-=OnAcquired;
        player=FindAnyObjectByType<PlayerController>();inventory=player!=null?player.Genes:null;
        if(inventory!=null)inventory.Acquired+=OnAcquired;SampleCounters();
        wasDashing=player!=null&&player.IsDashing;wasDead=player!=null&&player.IsDead;
    }
    private void SampleCounters()
    {
        var p=player!=null?player.Presentation:null;if(p==null)return;
        hurts=p.HurtCount;jumps=p.JumpCount;landings=p.LandingCount;
    }
    private void Update()
    {
        if(player==null&&Time.unscaledTime>=nextPlayerSearch){nextPlayerSearch=Time.unscaledTime+.5f;BindPlayer();}
        bool modal=player!=null?player.MenuOpen||player.IsDead:Time.timeScale<=0;
        if(modal&&!wasModal){effects.Stop();voices.Clear();}wasModal=modal;
        var p=player!=null?player.Presentation:null;
        if(p!=null)
        {
            if(!modal)
            {
                if(p.HurtCount!=hurts)PlayCue("hurt");
                if(p.JumpCount!=jumps)PlayCue("jump");
                if(p.LandingCount!=landings)PlayCue("land");
                if(player.IsDashing&&!wasDashing)PlayCue("dash");
            }
            if(player.IsDead&&!wasDead)PlayCue("death");
            SampleCounters();wasDashing=player.IsDashing;wasDead=player.IsDead;
        }
        PlayUIClick();
    }
    private void OnReaction(EmergenceEvent reaction)
    {
        string key=reaction.kind;
        if(key=="steam"||key=="conduction"||key=="explosion")PlayWorld(key,reaction.position,.85f);
    }
    private void PlayUIClick()
    {
        var events=EventSystem.current;if(events==null||Time.unscaledTime-lastClick<.06f)return;
        bool clicked=false;var mouse=Mouse.current;
        if(mouse!=null&&mouse.leftButton.wasPressedThisFrame)
        {
            uiHits.Clear();events.RaycastAll(new PointerEventData(events){position=mouse.position.ReadValue()},uiHits);
            var button=uiHits.Count>0?uiHits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>():null;
            clicked=button!=null&&button.IsActive()&&button.IsInteractable();
        }
        var keys=Keyboard.current;var selected=events.currentSelectedGameObject;
        var selectedButton=selected!=null?selected.GetComponent<UnityEngine.UI.Button>():null;
        bool submitted=keys!=null&&selectedButton!=null&&selectedButton.IsInteractable()&&(keys.enterKey.wasPressedThisFrame||keys.spaceKey.wasPressedThisFrame);
        if(!clicked&&!submitted)return;lastClick=Time.unscaledTime;PlayCue("ui");
    }
    private void OnAcquired(Modifier gene)
    {
        if(gene!=null&&player!=null&&!player.IsDead&&(StorySession.Current==null||StorySession.Current.Ready)&&
            (CampaignFlow.Current==null||CampaignFlow.Current.Ready))PlayCue("gene",.85f);
    }
    private static AudioClip CreateRoom()
    {
        const float duration = 24f;
        int count = (int)(duration * SampleRate);
        float[] samples = new float[count];
        // Whole cycles over the loop keep the sustained notes seamless.
        float[] notes = { 98f, 146.832f, 233.082f, 293.665f };
        for (int n = 0; n < notes.Length; n++) notes[n] = Mathf.Round(notes[n] * duration) / duration;
        var random = new System.Random(26);
        float water = 0f;
        for (int i = 0; i < count; i++)
        {
            double time = i / (double)SampleRate;
            double cycle = time / duration * Math.PI * 2;
            double breathing = .68 + .16 * Math.Sin(cycle) + .12 * Math.Cos(cycle * 2);
            double pad = 0;
            for (int n = 0; n < notes.Length; n++)
                pad += Math.Sin(time * notes[n] * Math.PI * 2) * (.010 / (1 + n * .8));
            water += ((float)random.NextDouble() * 2f - 1f - water) * .025f;
            double waterWindow = .5 - .5 * Math.Cos(cycle);
            samples[i] = (float)(pad * breathing + water * waterWindow * .025);
        }
        float[] starts = { 2f, 5.5f, 8f, 12.5f, 16f, 21f };
        float[] chimes = { 587.33f, 698.46f, 880f, 698.46f, 587.33f, 440f };
        for (int n = 0; n < starts.Length; n++)
        {
            int start = (int)(starts[n] * SampleRate), length = (int)(1.5f * SampleRate);
            for (int i = 0; i < length; i++)
            {
                double t = i / (double)SampleRate;
                double envelope = Math.Sin(Math.PI * i / (length - 1)) * Math.Exp(-3.4 * t);
                double tone = Math.Sin(t * chimes[n] * Math.PI * 2) + .22 * Math.Sin(t * chimes[n] * Math.PI * 4);
                samples[start + i] += (float)(tone * envelope * .027);
            }
        }
        return MakeClip("Mellow cavern loop", samples);
    }

    private static AudioClip MakeClip(string label, float[] samples)
    {
        AudioClip clip = AudioClip.Create(label, samples.Length, 1, SampleRate, false);
        clip.hideFlags = HideFlags.DontSave;
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if(current!=this)return;
        SceneManager.sceneLoaded-=OnSceneLoaded;EmergenceReactions.Happened-=OnReaction;
        if(inventory!=null)inventory.Acquired-=OnAcquired;
        if(ambience!=null&&ambience.clip==room){ambience.Stop();ambience.clip=null;}
        if(effects!=null)effects.Stop();
        foreach(var clip in owned)if(clip!=null)Destroy(clip);
        // Imported AudioClips belong to the asset database, not to this presentation object's lifetime.
        clips.Clear();current=null;
    }
}
