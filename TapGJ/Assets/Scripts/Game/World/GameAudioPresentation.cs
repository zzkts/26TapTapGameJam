using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Small, asset-free sound palette shared by the menu and the playable chapter.</summary>
[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
public sealed class GameAudioPresentation : MonoBehaviour
{
    private const int SampleRate = 22050;
    private static GameAudioPresentation current;
    private GlobalManager volumes;
    private AudioSource ambience, effects;
    private AudioListener fallbackListener;
    private AudioClip room, shot, pickup, hurt, dash, click, jump, land;
    private PlayerController player;
    private GeneInventory inventory;
    private int shots, hurts, jumps, landings;
    private bool wasDashing, wasModal;
    private float nextPlayerSearch, lastClick = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { current = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (current != null) return;
        GameAudioPresentation existing = FindAnyObjectByType<GameAudioPresentation>();
        if (existing != null) { current = existing; return; }
        new GameObject("Water Drain Audio").AddComponent<GameAudioPresentation>();
    }

    private void Awake()
    {
        if (current != null && current != this) { Destroy(gameObject); return; }
        current = this;
        DontDestroyOnLoad(gameObject);
        volumes = GlobalManager.Instance;
        ambience = volumes.BackgroundAudioSource;
        effects = volumes.GameAudioSource;
        ambience.playOnAwake = effects.playOnAwake = false;
        ambience.spatialBlend = effects.spatialBlend = 0f;
        ambience.ignoreListenerPause = effects.ignoreListenerPause = true;
        room = CreateRoom();
        shot = CreateEffect("Water shot", .105f, 690f, 290f, .13f, .16f);
        pickup = CreateEffect("Gene bubbles", .32f, 520f, 1100f, .14f, .015f);
        hurt = CreateEffect("Soft impact", .17f, 175f, 65f, .14f, .16f);
        dash = CreateEffect("Drain dash", .19f, 440f, 120f, .055f, .17f);
        click = CreateEffect("UI droplet", .075f, 840f, 570f, .095f, .015f);
        jump = CreateEffect("Little hop", .12f, 260f, 620f, .065f, .025f);
        land = CreateEffect("Soft landing", .09f, 145f, 75f, .07f, .055f);
        ambience.clip = room;
        ambience.loop = true;
        ambience.Play();
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureListener();
        BindPlayer();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // GlobalManager and its sources persist. A new camera owns the scene listener.
        EnsureListener();
        BindPlayer();
        nextPlayerSearch = Time.unscaledTime + .3f;
        wasModal = false;
    }

    private void EnsureListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool sceneHasListener = false;
        for (int i = 0; i < listeners.Length; i++)
            if (listeners[i] != fallbackListener && listeners[i].enabled && listeners[i].gameObject.activeInHierarchy)
                sceneHasListener = true;
        if (sceneHasListener)
        {
            if (fallbackListener != null) fallbackListener.enabled = false;
        }
        else
        {
            if (fallbackListener == null) fallbackListener = gameObject.AddComponent<AudioListener>();
            fallbackListener.enabled = true;
        }
    }

    private void BindPlayer()
    {
        if (inventory != null) inventory.Acquired -= OnAcquired;
        player = FindAnyObjectByType<PlayerController>();
        inventory = player != null ? player.Genes : null;
        if (inventory != null) inventory.Acquired += OnAcquired;
        SampleCounters();
        wasDashing = player != null && player.IsDashing;
    }

    private void SampleCounters()
    {
        PlayerPresentation p = player != null ? player.Presentation : null;
        if (p == null) return;
        shots = p.ShotCount; hurts = p.HurtCount;
        jumps = p.JumpCount; landings = p.LandingCount;
    }

    private void Update()
    {
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + .5f;
            BindPlayer();
        }
        // A brief combat hit-stop should retain its impact sound; menus suppress world sounds.
        bool modal = player != null ? player.MenuOpen || player.IsDead : Time.timeScale <= 0f;
        if (modal && !wasModal && effects != null) effects.Stop();
        wasModal = modal;
        PlayerPresentation p = player != null ? player.Presentation : null;
        if (p != null)
        {
            if (!modal)
            {
                if (p.ShotCount != shots) Play(shot);
                if (p.HurtCount != hurts) Play(hurt);
                if (p.JumpCount != jumps) Play(jump);
                if (p.LandingCount != landings) Play(land);
                if (player.IsDashing && !wasDashing) Play(dash);
            }
            SampleCounters();
            wasDashing = player.IsDashing;
        }
        PlayUIClick();
    }

    private void PlayUIClick()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Time.unscaledTime - lastClick < .06f) return;
        Mouse mouse = Mouse.current;
        bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame && eventSystem.IsPointerOverGameObject();
        Keyboard keys = Keyboard.current;
        GameObject selected = eventSystem.currentSelectedGameObject;
        bool submitted = keys != null && selected != null &&
            (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) &&
            selected.GetComponent<UnityEngine.UI.Button>() != null;
        if (!clicked && !submitted) return;
        lastClick = Time.unscaledTime;
        Play(click);
    }

    private void OnAcquired(Modifier gene)
    {
        if (gene != null && player != null && !player.IsDead &&
            (StorySession.Current == null || StorySession.Current.Ready)) Play(pickup);
    }

    private void Play(AudioClip clip)
    {
        if (effects != null && clip != null && effects.volume > 0f) effects.PlayOneShot(clip);
    }

    private static AudioClip CreateEffect(string label, float seconds, float from, float to, float gain, float noiseGain)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        float[] samples = new float[count];
        var random = new System.Random(label.Length * 71 + (int)from);
        double phase = 0;
        float smoothNoise = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            phase += 2.0 * Math.PI * Mathf.Lerp(from, to, t) / SampleRate;
            float envelope = Mathf.Sin(Mathf.PI * t) * Mathf.Exp(-3.8f * t);
            smoothNoise = Mathf.Lerp(smoothNoise, (float)random.NextDouble() * 2f - 1f, .2f);
            samples[i] = envelope * ((float)Math.Sin(phase) * gain + smoothNoise * noiseGain);
        }
        return MakeClip(label, samples);
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
        if (current != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (inventory != null) inventory.Acquired -= OnAcquired;
        if (ambience != null && ambience.clip == room) { ambience.Stop(); ambience.clip = null; }
        if (effects != null) effects.Stop();
        Destroy(room); Destroy(shot); Destroy(pickup); Destroy(hurt);
        Destroy(dash); Destroy(click); Destroy(jump); Destroy(land);
        current = null;
    }
}
