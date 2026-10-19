using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent, streaming scene music. Fades never overwrite the user's volume setting.</summary>
[DisallowMultipleComponent]
public sealed class SceneMusicPlayer : MonoBehaviour
{
    public const string MenuMusicPath = "Audio/Music/baby_black_magi_loop";
    public const string GameMusicPath = "Audio/Music/chee_zee_lab";
    private const float FadeSeconds = 1.6f;
    private readonly AudioSource[] sources = new AudioSource[2];
    private readonly float[] gains = new float[2];
    private readonly float[] fadeStart = new float[2];
    private AudioClip menuMusic, gameMusic;
    private GlobalManager volumes;
    private PlayerController player;
    private int target = -1;
    private float fadeTime, duck = 1f;
    public AudioClip ActiveClip => target >= 0 ? sources[target].clip : null;
    public bool Transitioning => fadeTime < FadeSeconds;

    private void Awake()
    {
        volumes = GlobalManager.Instance;
        menuMusic = Resources.Load<AudioClip>(MenuMusicPath);
        gameMusic = Resources.Load<AudioClip>(GameMusicPath);
        for (int i = 0; i < sources.Length; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            sources[i] = source;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.priority = 32;
            source.volume = 0f;
        }
        SceneManager.sceneLoaded += SceneLoaded;
        SelectScene(SceneManager.GetActiveScene());
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) SelectScene(scene);
    }

    private void SelectScene(Scene scene)
    {
        player = FindAnyObjectByType<PlayerController>();
        AudioClip clip = scene.name == "BeginScene" ? menuMusic : scene.name == "GameScene" ? gameMusic : null;
        if (target >= 0 && sources[target].clip == clip) return;
        int next = -1;
        if (clip != null)
        {
            // Reuse a fading-out track if the player returns before the transition finishes.
            for (int i = 0; i < sources.Length; i++)
                if (sources[i].clip == clip && sources[i].isPlaying) next = i;
            if (next < 0)
            {
                next = target == 0 ? 1 : 0;
                sources[next].Stop();
                sources[next].clip = clip;
                gains[next] = 0f;
                sources[next].volume = 0f;
                sources[next].Play();
            }
        }
        for (int i = 0; i < sources.Length; i++) fadeStart[i] = gains[i];
        target = next;
        fadeTime = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fadeTime = Mathf.Min(FadeSeconds, fadeTime + dt);
        float blend = Mathf.SmoothStep(0f, 1f, fadeTime / FadeSeconds);
        float desiredDuck = player != null && (player.MenuOpen || player.IsDead) ? .55f : 1f;
        duck = Mathf.MoveTowards(duck, desiredDuck, dt * 1.5f);
        float master = volumes != null ? volumes.BackgroundVolume : 0f;
        for (int i = 0; i < sources.Length; i++)
        {
            gains[i] = Mathf.Lerp(fadeStart[i], i == target ? 1f : 0f, blend);
            float mix = sources[i].clip == menuMusic ? .38f : .30f;
            sources[i].volume = master * mix * gains[i] * duck;
            if (fadeTime >= FadeSeconds && i != target && sources[i].isPlaying) sources[i].Stop();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        foreach (var source in sources)
            if (source != null) { source.Stop(); Destroy(source); }
    }
}
