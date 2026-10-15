using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned pools: keep fading tails alive, then reuse the complete projectile hierarchy.</summary>
public sealed class ProjectilePool : MonoBehaviour
{
    public const int CapacityPerPrefab = 128;
    private static ProjectilePool current;
    private readonly Dictionary<int, Stack<Projectile>> idle = new();
    private readonly List<Tail> fading = new();
    private struct Tail { public Projectile shot; public float until; }
    public static int CreatedCount { get; private set; }
    public static int ReusedCount { get; private set; }
    public static int ReturnedCount { get; private set; }
    public static int IdleCount
    {
        get { int count = 0; if (current != null) foreach (var pool in current.idle.Values) count += pool.Count; return count; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { current = null; CreatedCount = ReusedCount = ReturnedCount = 0; }
    private static ProjectilePool GetPool()
    {
        if (current == null) current = new GameObject("Projectile pool (scene owned)").AddComponent<ProjectilePool>();
        return current;
    }
    public static Projectile Rent(GameObject prefab, Vector2 position)
    {
        if (prefab == null) return null;
        if (!Application.isPlaying) return Instantiate(prefab, position, Quaternion.identity).GetComponent<Projectile>();
        var pool = GetPool();
        int key = prefab.GetInstanceID();
        if (!pool.idle.TryGetValue(key, out var stack)) pool.idle[key] = stack = new Stack<Projectile>();
        Projectile shot = null;
        while (stack.Count > 0 && shot == null) shot = stack.Pop();
        if (shot == null)
        {
            shot = Instantiate(prefab, position, Quaternion.identity, pool.transform).GetComponent<Projectile>();
            if (shot == null) throw new System.InvalidOperationException("Projectile prefab has no Projectile: " + prefab.name);
            shot.Pool = pool; shot.PoolKey = key; CreatedCount++;
        }
        else ReusedCount++;
        shot.transform.SetPositionAndRotation(position, Quaternion.identity);
        shot.ResetForReuse();
        shot.gameObject.SetActive(true);
        shot.enabled = true;
        return shot;
    }
    public static void Return(Projectile shot)
    {
        if (Application.isPlaying && shot.Pool != null)
        {
            shot.PrepareTail();
            shot.Pool.fading.Add(new Tail { shot = shot, until = Time.time + .65f });
            ReturnedCount++;
        }
        else Destroy(shot.gameObject);
    }
    private void Update()
    {
        for (int i = fading.Count - 1; i >= 0; i--)
        {
            Tail tail = fading[i];
            if (tail.shot != null && Time.time < tail.until) continue;
            fading.RemoveAt(i);
            if (tail.shot == null) continue;
            var shot = tail.shot;
            shot.gameObject.SetActive(false);
            shot.transform.localScale = Vector3.one;
            if (idle.TryGetValue(shot.PoolKey, out var stack) && stack.Count < CapacityPerPrefab) stack.Push(shot);
            else Destroy(shot.gameObject);
        }
    }
    private void OnDestroy() { if (current == this) current = null; }
}
