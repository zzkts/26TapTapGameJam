using UnityEngine;

/// <summary>
/// 场景中已有实例则复用，重复实例自动销毁；跨场景不销毁。
/// </summary>
public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static bool _isQuitting;

    public static T Instance
    {
        get
        {
            if (_isQuitting)
            {
                Debug.LogWarning($"[MonoSingleton] 应用正在退出，{typeof(T).Name} 不再创建实例。");
                return null;
            }

            if (_instance == null)
            {
                // 先找场景中已存在的实例
                _instance = FindAnyObjectByType<T>();

                if (_instance == null)
                {
                    var go = new GameObject($"[{typeof(T).Name}]");
                    _instance = go.AddComponent<T>();
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // 已有实例，销毁重复的
            Debug.LogWarning($"[MonoSingleton] 检测到重复的 {typeof(T).Name}，已自动销毁。");
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        DontDestroyOnLoad(gameObject);
        OnSingletonInit();
    }

    /// <summary>
    /// 单例初始化回调（相当于安全的 Awake）
    /// </summary>
    protected virtual void OnSingletonInit() { }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}

/*
// ==================== 使用示例 ====================

public class AudioManager : MonoSingleton<AudioManager>
{
    private AudioSource _source;

    protected override void OnSingletonInit()
    {
        _source = gameObject.AddComponent<AudioSource>();
    }

    public void Play(AudioClip clip) => _source.PlayOneShot(clip);
}

// 任意地方调用：
// AudioManager.Instance.Play(someClip);
// 也可以先在场景里手动挂一个 AudioManager，运行时会自动复用场景中的实例。

// ===============================================
*/
