using System;
using System.Collections.Generic;


/// <summary>
/// 事件中心
/// </summary>
public class EventCenter
{
    /// <summary>
    /// 取消订阅句柄
    /// </summary>
    public delegate void IDisposable();

    private static EventCenter instance;
    public static EventCenter Instance
    {
        get
        {
            if (instance == null)
                instance = new EventCenter();
            return instance;
        }
    }

    private Dictionary<E_EventType, Delegate> pool;
    private EventCenter()
    {
        pool = new Dictionary<E_EventType, Delegate>();
    }


    #region Subscribe
    public IDisposable SubscribeEvent(E_EventType eventType, Action action)
    {
        if (!pool.ContainsKey(eventType))
            pool.Add(eventType, null);
        pool[eventType] = Delegate.Combine(pool[eventType], action);
        return new IDisposable(() => { UnsubscribeEvent(eventType, action); });
    }

    public IDisposable SubscribeEvent<T>(E_EventType eventType, Action<T> action)
    {
        if (!pool.ContainsKey(eventType))
            pool.Add(eventType, null);
        pool[eventType] = Delegate.Combine(pool[eventType], action);
        return new IDisposable(() => { UnsubscribeEvent<T>(eventType, action); });
    }

    public IDisposable SubscribeEvent<T, T2>(E_EventType eventType, Action<T, T2> action)
    {
        if (!pool.ContainsKey(eventType))
            pool.Add(eventType, null);
        pool[eventType] = Delegate.Combine(pool[eventType], action);
        return new IDisposable(() => { UnsubscribeEvent<T, T2>(eventType, action); });
    }

    public IDisposable SubscribeEvent<T, T2, T3>(E_EventType eventType, Action<T, T2, T3> action)
    {
        if (!pool.ContainsKey(eventType))
            pool.Add(eventType, null);
        pool[eventType] = Delegate.Combine(pool[eventType], action);
        return new IDisposable(() => { UnsubscribeEvent<T, T2, T3>(eventType, action); });
    }

    public IDisposable SubscribeEvent<T, T2, T3, T4>(E_EventType eventType, Action<T, T2, T3, T4> action)
    {
        if (!pool.ContainsKey(eventType))
            pool.Add(eventType, null);
        pool[eventType] = Delegate.Combine(pool[eventType], action);
        return new IDisposable(() => { UnsubscribeEvent<T, T2, T3, T4>(eventType, action); });
    }
    #endregion

    #region Trigger
    public void TriggerEvent(E_EventType eventType)
    {
        if (!pool.ContainsKey(eventType))
            return;
        (pool[eventType] as Action)?.Invoke();
    }

    public void TriggerEvent<T>(E_EventType eventType, T t)
    {
        if (!pool.ContainsKey(eventType))
            return;
        (pool[eventType] as Action<T>)?.Invoke(t);
    }

    public void TriggerEvent<T, T2>(E_EventType eventType, T t, T2 t2)
    {
        if (!pool.ContainsKey(eventType))
            return;
        (pool[eventType] as Action<T, T2>)?.Invoke(t, t2);
    }

    public void TriggerEvent<T, T2, T3>(E_EventType eventType, T t, T2 t2, T3 t3)
    {
        if (!pool.ContainsKey(eventType))
            return;
        (pool[eventType] as Action<T, T2, T3>)?.Invoke(t, t2, t3);
    }

    public void TriggerEvent<T, T2, T3, T4>(E_EventType eventType, T t, T2 t2, T3 t3, T4 t4)
    {
        if (!pool.ContainsKey(eventType))
            return;
        (pool[eventType] as Action<T, T2, T3, T4>)?.Invoke(t, t2, t3, t4);
    }
    #endregion

    #region Unsubscribe
    private void UnsubscribeEvent(E_EventType eventType, Action action)
    {
        if (!pool.ContainsKey(eventType))
            return;
        pool[eventType] = Delegate.Remove(pool[eventType], action);
    }

    private void UnsubscribeEvent<T>(E_EventType eventType, Action<T> action)
    {
        if (!pool.ContainsKey(eventType))
            return;
        pool[eventType] = Delegate.Remove(pool[eventType], action);
    }
    private void UnsubscribeEvent<T, T2>(E_EventType eventType, Action<T, T2> action)
    {
        if (!pool.ContainsKey(eventType))
            return;
        pool[eventType] = Delegate.Remove(pool[eventType], action);
    }

    private void UnsubscribeEvent<T, T2, T3>(E_EventType eventType, Action<T, T2, T3> action)
    {
        if (!pool.ContainsKey(eventType))
            return;
        pool[eventType] = Delegate.Remove(pool[eventType], action);
    }

    private void UnsubscribeEvent<T, T2, T3, T4>(E_EventType eventType, Action<T, T2, T3, T4> action)
    {
        if (!pool.ContainsKey(eventType))
            return;
        pool[eventType] = Delegate.Remove(pool[eventType], action);
    }
    #endregion
}
