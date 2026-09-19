using System;

/// <summary>
/// 线程安全，首次访问时才创建实例（懒加载）。
/// 用法：public class GameManager : Singleton<GameManager> { ... }
/// </summary>
public abstract class Singleton<T> where T : Singleton<T>, new()
{
    private static readonly Lazy<T> _instance = new Lazy<T>(() =>
    {
        var instance = new T();
        instance.OnInit();
        return instance;
    });

    public static T Instance => _instance.Value;

    /// <summary>
    /// 实例是否已创建（不会触发创建）
    /// </summary>
    public static bool IsCreated => _instance.IsValueCreated;

    /// <summary>
    /// 实例创建后回调，相当于构造后的初始化
    /// </summary>
    protected virtual void OnInit() { }

    // 防止外部 new 出多个实例
    protected Singleton() { }
}

/*
// ==================== 使用示例 ====================

public class GameManager : Singleton<GameManager>
{
    private int _score;

    protected override void OnInit()
    {
        _score = 0;
        UnityEngine.Debug.Log("GameManager 初始化");
    }

    public void AddScore(int v) => _score += v;
    public int Score => _score;
}

// 任意地方调用：
GameManager.Instance.AddScore(10);

// ===============================================
*/
