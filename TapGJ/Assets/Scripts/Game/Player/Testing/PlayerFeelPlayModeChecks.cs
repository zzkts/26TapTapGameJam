#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

/// <summary>Real input and physics checks at movement tolerances and backflow material boundaries.</summary>
public sealed class PlayerFeelPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results = new List<string>();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    public static string Stage { get; private set; }
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private readonly List<GameObject> fixtures = new List<GameObject>();
    private readonly List<ElementField> fields = new List<ElementField>();
    private GameObject rig;
    private PlayerController player;
    private Keyboard keyboard, priorKeyboard;
    private Mouse mouse, priorMouse;
    private InputSettings.BackgroundBehavior priorBackground;
    private InputSettings.EditorInputBehaviorInPlayMode priorEditorInput;
    private int rigIndex, inertiaEvents;
    private bool cleaned, hadSave, priorValidation;
    private string priorSave;
    private StorySession sceneStory;
    private float tapPeak;

    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Running) throw new InvalidOperationException("Player feel checks are already running.");
        new GameObject("Player Feel Verification").AddComponent<PlayerFeelPlayModeChecks>().Begin();
    }
    private void Begin()
    {
        Results.Clear(); Running = true; Succeeded = false;
        hadSave = PlayerPrefs.HasKey(RunProgress.SaveKey); priorSave = PlayerPrefs.GetString(RunProgress.SaveKey);
        priorKeyboard = Keyboard.current; priorMouse = Mouse.current;
        priorBackground = InputSystem.settings.backgroundBehavior;
        priorEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
        sceneStory = StorySession.Current;
        if (sceneStory != null) { priorValidation = sceneStory.ValidationMode; sceneStory.SetValidationMode(true); }
        foreach (Behaviour item in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
            if (item.enabled && (item is PlayerController || item is PlayerInteraction || item is PlayerHudPanel ||
                item is EnemyAIController || item is EnemySupportAura || item is EnvironmentElementSource))
            { suspended.Add(item); item.enabled = false; }
        EmergenceReactions.Happened += OnReaction; StartCoroutine(Execute());
    }
    private void OnReaction(EmergenceEvent reaction) { if (reaction.kind == "inertia" && reaction.position.x > 900f) inertiaEvents++; }
    private IEnumerator Execute()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Cases());
        while (stack.Count > 0)
        {
            object wait;
            try
            {
                IEnumerator step = stack.Peek();
                if (!step.MoveNext()) { stack.Pop(); continue; }
                wait = step.Current;
                if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            }
            catch (Exception error) { Results.Add("FAIL: " + Stage + ": " + error); break; }
            yield return wait;
        }
        Finish(); Destroy(gameObject);
    }
    private IEnumerator Cases()
    {
        Stage = "coyote time";
        NewRig(4f); yield return Wait(.25f);
        Check(player.IsGrounded() && player.CoyoteTime >= .14f && player.JumpBufferTime >= .16f,
            "The actual player settles and loads the increased coyote/buffer windows");
        yield return LeaveLedge(); yield return Wait(.18f); Keys(Key.Space); yield return Frames(2); Keys();
        Check(player.YVelocity > 2f && player.Presentation.JumpCount == 1,
            "A real jump press 180 ms after leaving a ledge still launches once");
        NewRig(4f); yield return Wait(.25f); yield return LeaveLedge(); yield return Wait(.34f);
        Keys(Key.Space); yield return Frames(2); Keys();
        Check(player.YVelocity < 0f && player.Presentation.JumpCount == 0,
            "A late jump beyond the coyote window cannot become an unlimited air jump");

        Stage = "landing buffer";
        NewRig(); yield return Wait(.25f);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>(); body.position += Vector2.up * .28f;
        body.linearVelocity = Vector2.down * 3f; Physics2D.SyncTransforms();
        Keys(Key.Space); yield return Frames(2); Keys(); yield return Wait(.12f);
        Check(player.YVelocity > 1.5f && player.Presentation.JumpCount == 1,
            "A press before landing survives release and becomes exactly one buffered jump");
        NewRig(); yield return Wait(.25f);
        yield return MeasureJump(false); float shortPeak = tapPeak;
        NewRig(); yield return Wait(.25f); yield return MeasureJump(true);
        Check(shortPeak >= .76f && tapPeak > shortPeak + .07f,
            "A tap preserves the .75 m route jump while holding gives a slightly higher arc");

        Stage = "environment launch";
        NewRig(); yield return Wait(.25f); body = player.GetComponent<Rigidbody2D>(); float gravity = body.gravityScale;
        Keys(); player.LaunchFromEnvironment(7.6f); yield return Wait(.12f);
        Check(player.YVelocity > 5.8f && Mathf.Abs(body.gravityScale - gravity) < .01f,
            "Releasing Space never cuts the 7.6 m/s steam launch or applies normal falling acceleration to it");

        Stage = "wet-material pulse push and pull";
        NewRig(); yield return Wait(.25f);
        EmergenceBody wet = Crate("Wet Test Crate", new Vector2(1.5f, -.4f), true);
        EmergenceBody dry = Crate("Dry Test Crate", new Vector2(3f, -.4f), false);
        yield return Wait(.15f); Aim(wet.transform.position); yield return Frames(2); yield return Press(Key.F);
        Check(wet.GetComponent<Rigidbody2D>().linearVelocityX > 1f &&
            Mathf.Abs(dry.GetComponent<Rigidbody2D>().linearVelocityX) < .02f && player.Backflow.LastMovedBodies == 1,
            "Real F input transfers physical momentum to the wet crate while the dry crate remains untouched");
        Check(inertiaEvents == 1 && !player.Backflow.TryPulse(),
            "A genuinely moved wet crate records inertia discovery and the pulse enforces cooldown");
        yield return Wait(player.Backflow.CooldownRemaining + .1f);
        Aim(wet.transform.position); yield return Frames(2); yield return Press(Key.S, Key.F);
        Check(wet.GetComponent<Rigidbody2D>().linearVelocityX < -1f && player.Backflow.LastPulling && inertiaEvents == 1,
            "S + F pulls the wet crate back without duplicating its first inertia discovery");

        Stage = "water transport and walls";
        NewRig(); yield return Wait(.25f);
        ElementField water = Field(E_ElementType.Water, new Vector2(.5f, .2f), .8f);
        ElementField poison = Field(E_ElementType.Poison, new Vector2(.5f, 1.6f), 4f);
        Vector3 waterStart = water.transform.position, poisonStart = poison.transform.position;
        Wall(new Vector2(1f, .5f));
        EmergenceBody blocked = Crate("Wet Crate Behind Wall", new Vector2(2f, -.4f), true);
        Aim(player.transform.position + Vector3.right * 5f); yield return Frames(2); yield return Press(Key.F);
        Check(Mathf.Abs(blocked.GetComponent<Rigidbody2D>().linearVelocityX) < .02f,
            "A solid wall blocks the pulse's influence on a wet crate behind it");
        Check(water.transform.position.x > waterStart.x && water.transform.position.x < .96f + rig.transform.position.x &&
            Vector3.Distance(water.transform.position, waterStart) <= .751f && player.Backflow.LastShiftedWaterFields == 1 &&
            poison.transform.position == poisonStart,
            "The existing water field shifts by at most .75 m, stops before the wall and leaves poison fields unchanged");
        yield return Wait(.95f);
        Check(water == null && poison != null, "Transport retains the original water field lifetime instead of refreshing it");

        Stage = "ability gates";
        NewRig(); yield return Wait(.25f); player.SetMenuOpen(true);
        Check(!player.Backflow.TryPulse() && player.Backflow.PulseCount == 0, "Paused UI prevents backflow input and world changes");
        player.SetMenuOpen(false); player.TakeDamage(1f);
        Check(player.IsHurt && !player.Backflow.TryPulse(), "Hurt reaction prevents a pulse from escaping its action recovery");
        yield return Wait(.45f); player.TakeDamage(100f);
        // The hurt invulnerability period is still active; wait before the fatal probe.
        if (!player.IsDead) { yield return Wait(.5f); player.TakeDamage(100f); }
        Check(player.IsDead && !player.Backflow.TryPulse(), "Death prevents pulse activation");
        Stage = "complete";
    }
    private IEnumerator LeaveLedge()
    {
        float deadline = Time.realtimeSinceStartup + 2f; Keys(Key.D);
        while (player.IsGrounded() && Time.realtimeSinceStartup < deadline) yield return null;
        Keys(); Check(!player.IsGrounded(), "Real D movement leaves the fixture ledge");
    }
    private IEnumerator MeasureJump(bool hold)
    {
        float start = player.transform.position.y, until = Time.realtimeSinceStartup + .85f;
        tapPeak = 0f; Keys(Key.Space); yield return Frames(2); if (!hold) Keys();
        while (Time.realtimeSinceStartup < until)
        { tapPeak = Mathf.Max(tapPeak, player.transform.position.y - start); yield return null; }
        Keys();
    }
    private void NewRig(float width = 40f)
    {
        Keys(); InputSystem.QueueStateEvent(mouse, new MouseState()); Time.timeScale = 1f;
        if (rig != null) rig.SetActive(false);
        rig = new GameObject("Player Feel Fixture " + rigIndex); fixtures.Add(rig);
        rig.transform.position = new Vector3(1000f + 30f * rigIndex++, 0f, 0f);
        var floor = new GameObject("Feel Ground"); floor.transform.SetParent(rig.transform, false);
        floor.transform.localPosition = new Vector3(0f, -1f); floor.layer = LayerMask.NameToLayer("Ground");
        floor.AddComponent<BoxCollider2D>().size = new Vector2(width, .2f);
        player = Object.Instantiate(Resources.Load<GameObject>("Prefab/Player/Player"), rig.transform).GetComponent<PlayerController>();
        player.transform.localPosition = Vector3.zero; Physics2D.SyncTransforms(); Aim(player.transform.position + Vector3.right * 5f);
    }
    private EmergenceBody Crate(string name, Vector2 local, bool wet)
    {
        var obj = new GameObject(name); obj.transform.SetParent(rig.transform, false); obj.transform.localPosition = local;
        obj.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1f);
        Rigidbody2D body = obj.AddComponent<Rigidbody2D>(); body.freezeRotation = true; body.mass = 1f;
        EmergenceBody material = obj.AddComponent<EmergenceBody>(); material.material = EmergenceMaterial.Movable;
        if (wet) material.Receive(E_ElementType.Water); Physics2D.SyncTransforms(); return material;
    }
    private void Wall(Vector2 local)
    {
        var obj = new GameObject("Feel Blocking Wall"); obj.transform.SetParent(rig.transform, false); obj.transform.localPosition = local;
        obj.layer = LayerMask.NameToLayer("Ground"); obj.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 4f); Physics2D.SyncTransforms();
    }
    private ElementField Field(E_ElementType type, Vector2 local, float lifetime)
    {
        ElementField field = ElementField.Create(type, (Vector2)rig.transform.position + local, new Vector2(.3f, .15f), lifetime, 0);
        fields.Add(field); return field;
    }
    private void Aim(Vector3 target)
    {
        if (Camera.main != null) InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.WorldToScreenPoint(target) });
    }
    private IEnumerator Press(params Key[] values) { Keys(values); yield return Frames(3); Keys(); yield return Frames(2); }
    private void Keys(params Key[] values) { if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState(values)); }
    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    private static IEnumerator Wait(float seconds) { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return null; }
    private static void Check(bool condition, string message)
    { Results.Add((condition ? "PASS: " : "FAIL: ") + message); if (!condition) throw new InvalidOperationException(message); }
    private void Finish()
    {
        if (cleaned) return; cleaned = true; EmergenceReactions.Happened -= OnReaction;
        foreach (GameObject fixture in fixtures) if (fixture != null) { fixture.SetActive(false); Destroy(fixture); }
        foreach (ElementField field in fields) if (field != null) Destroy(field.gameObject);
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        priorKeyboard?.MakeCurrent(); priorMouse?.MakeCurrent();
        InputSystem.settings.backgroundBehavior = priorBackground; InputSystem.settings.editorInputBehaviorInPlayMode = priorEditorInput;
        foreach (Behaviour item in suspended) if (item != null) item.enabled = true;
        if (sceneStory != null) sceneStory.SetValidationMode(priorValidation);
        if (hadSave) PlayerPrefs.SetString(RunProgress.SaveKey, priorSave); else PlayerPrefs.DeleteKey(RunProgress.SaveKey); PlayerPrefs.Save();
        Time.timeScale = 1f; Succeeded = Results.Count > 0 && Results.All(r => r.StartsWith("PASS:")); Running = false;
        Debug.Log("Player feel checks: " + string.Join("\n", Results));
    }
    private void OnDestroy() { if (!cleaned) Finish(); }
}
#endif
