using UnityEngine;

[DefaultExecutionOrder(-100)]
public class GlobalManager : MonoSingleton<GlobalManager>
{
    private const string BackgroundVolumeKey = "Audio.BackgroundVolume";
    private const string GameVolumeKey = "Audio.GameVolume";

    [SerializeField] private AudioSource backgroundAudioSource;
    [SerializeField] private AudioSource gameAudioSource;

    public AudioSource BackgroundAudioSource => backgroundAudioSource;
    public AudioSource GameAudioSource => gameAudioSource;
    public float BackgroundVolume => backgroundAudioSource.volume;
    public float GameVolume => gameAudioSource.volume;

    protected override void OnSingletonInit()
    {
        backgroundAudioSource.volume = PlayerPrefs.GetFloat(BackgroundVolumeKey, 1f);
        gameAudioSource.volume = PlayerPrefs.GetFloat(GameVolumeKey, 1f);
    }

    //修改背景音乐音量
    public void SetBackgroundVolume(float volume)
    {
        backgroundAudioSource.volume = volume;
    }

    //修改游戏声音音量
    public void SetGameVolume(float volume)
    {
        gameAudioSource.volume = volume;
    }
    //存档
    public void SaveMusic()
    {
        PlayerPrefs.SetFloat(GameVolumeKey, GameVolume);
        PlayerPrefs.SetFloat(BackgroundVolumeKey, BackgroundVolume);
        PlayerPrefs.Save();
    }
    protected override void OnApplicationQuit()
    {
        PlayerPrefs.Save();
        base.OnApplicationQuit();
    }

}
