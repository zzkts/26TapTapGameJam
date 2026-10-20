using UnityEngine;

/// <summary>Touch commands use the same controller and combat rules as keyboard/mouse.</summary>
public static class MobileGameInput
{
    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            return Application.isMobilePlatform || UnityEditor.SessionState.GetBool("WaterDrain.TouchPreview", false);
#else
            return Application.isMobilePlatform;
#endif
        }
    }
    public enum Command { Jump, Dash, Enchant, Interact, Drop, Pause }
    private static readonly bool[] pending = new bool[6];
    public static Vector2 Move { get; private set; }
    public static Vector2 Aim { get; private set; } = Vector2.right;
    public static Vector2 AimScreenPosition { get; private set; }
    public static bool Firing { get; private set; }
    public static bool JumpHeld { get; private set; }
    private static bool fireRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() { Aim = Vector2.right; Reset(); }
    public static void SetMove(Vector2 value) => Move = Vector2.ClampMagnitude(value, 1);
    public static void BeginScreenAim(Vector2 position)
    {
        SetScreenAim(position, true);
        // UGUI can deliver a short tap entirely between two player Updates.
        fireRequested = true;
    }
    public static void SetScreenAim(Vector2 position, bool held)
    { AimScreenPosition = position; Firing = held; if (!held) fireRequested = false; }
    public static void EndScreenAim() => Firing = false;
    public static bool ConsumeFireRequest()
    { bool value = fireRequested; fireRequested = false; return value; }
    public static void SetAimDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > .01f) Aim = direction.normalized;
    }
    public static void Press(Command command)
    {
        pending[(int)command] = true;
        if (command == Command.Jump) JumpHeld = true;
    }
    public static void Release(Command command) { if (command == Command.Jump) JumpHeld = false; }
    public static bool Consume(Command command)
    {
        bool value = pending[(int)command]; pending[(int)command] = false; return value;
    }
    public static void Reset()
    {
        Move = Vector2.zero; Firing = JumpHeld = fireRequested = false;
        System.Array.Clear(pending, 0, pending.Length);
    }
    public static string InteractHint => Enabled ? "点击交互" : "按 E";
    public static string EnchantHint => Enabled ? "点击附魔" : "右键";
    public static string AdaptHint(string text)
    {
        if (!Enabled || string.IsNullOrEmpty(text)) return text;
        return text.Replace("按 E", "点击交互").Replace("E 拾取/终端", "交互拾取/终端")
            .Replace("E 选择", "交互选择").Replace("右键", "附魔键").Replace("左键", "点击场景");
    }
}
