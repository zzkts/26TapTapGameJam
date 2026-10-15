using System.Collections.Generic;
using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    /// <summary>Small scene-local pools for recurring impacts; never retain a previous scene's effects.</summary>
    public sealed class WaterEffectPool : MonoBehaviour
    {
        public const int CapacityPerKind = 48;
        private static WaterEffectPool current;
        private readonly Dictionary<string, Stack<GameObject>> idle = new();
        private readonly Dictionary<int, string> keys = new();
        private readonly List<Expiry> active = new();
        private struct Expiry { public GameObject obj; public float until; }
        public static int CreatedCount { get; private set; }
        public static int ReusedCount { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current = null; CreatedCount = ReusedCount = 0; }
        public static GameObject Rent(string kind, out bool created)
        {
            if (current == null) current = new GameObject("Impact pool (scene owned)").AddComponent<WaterEffectPool>();
            if (!current.idle.TryGetValue(kind, out var stack)) current.idle[kind] = stack = new Stack<GameObject>();
            GameObject obj = null;
            while (stack.Count > 0 && obj == null) obj = stack.Pop();
            created = obj == null;
            if (created)
            {
                obj = new GameObject(kind); obj.transform.SetParent(current.transform);
                current.keys[obj.GetInstanceID()] = kind; CreatedCount++;
            }
            else ReusedCount++;
            obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            obj.transform.localScale = Vector3.one;
            obj.SetActive(true);
            return obj;
        }
        public static void ReturnAfter(GameObject obj, float seconds)
        {
            current.active.Add(new Expiry { obj = obj, until = Time.time + seconds });
        }
        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var entry = active[i];
                if (entry.obj != null && Time.time < entry.until) continue;
                active.RemoveAt(i);
                if (entry.obj == null) continue;
                int id = entry.obj.GetInstanceID();
                entry.obj.SetActive(false);
                if (keys.TryGetValue(id, out string kind) && idle[kind].Count < CapacityPerKind) idle[kind].Push(entry.obj);
                else { keys.Remove(id); Destroy(entry.obj); }
            }
        }
        private void OnDestroy() { if (current == this) current = null; }
    }
}
