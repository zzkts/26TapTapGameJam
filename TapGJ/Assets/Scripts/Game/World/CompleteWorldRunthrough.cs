#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

/// <summary>Exercises the finished scene using real player input, colliders, rewards and projectiles.</summary>
public sealed class CompleteWorldRunthrough : MonoBehaviour
{
    public static readonly List<string> Results = new();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    private readonly List<Behaviour> suspended = new();
    private PlayerController player;
    private CompleteLevelFlow level;
    private Keyboard keyboard, previousKeyboard;
    private Mouse mouse, previousMouse;
    private InputSettings.BackgroundBehavior originalBackground;
    private Component story;
    private MethodInfo validationMode;

    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Running) throw new InvalidOperationException("Complete chapter checks are already running.");
        new GameObject("Complete Chapter Verification").AddComponent<CompleteWorldRunthrough>().Begin();
    }
    private void Begin()
    {
        Results.Clear(); Running = true; Succeeded = false;
        level = Object.FindFirstObjectByType<CompleteLevelFlow>();
        player = level != null ? level.player : null;
        if (player == null) { Results.Add("FAIL: no playable complete chapter"); Finish(); return; }
        previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
        originalBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
        foreach (Behaviour behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour.enabled && (behaviour is EnemyAIController || behaviour is EnemySupportAura))
            { suspended.Add(behaviour); behaviour.enabled = false; }
            if (behaviour.GetType().Name == "StorySession") story = behaviour;
        }
        validationMode = story != null ? story.GetType().GetMethod("SetValidationMode", BindingFlags.Public | BindingFlags.Instance) : null;
        validationMode?.Invoke(story, new object[] { true });
        player.SetMenuOpen(false); player.SetInvincible(true); player.Heal(player.maxHp);
        StartCoroutine(Execute());
    }
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
            catch (Exception error) { Results.Add("FAIL: " + error.Message); break; }
            yield return wait;
        }
        Finish();
    }
    private IEnumerator Cases()
    {
        CompleteWorldExit exit = Object.FindFirstObjectByType<CompleteWorldExit>();
        Check(exit != null && !level.CoreDefeated && !level.ReachExit(player), "Core exit is locked before defeating its guardians");
        Teleport(new Vector2(3f, 1f)); yield return Wait(.6f);
        Check(player.IsGrounded(), "The player settles on the new chapter's solid floor");
        yield return WalkTo(8f);
        Keys(Key.Space); yield return null; Keys(); yield return Wait(1.15f);
        Check(player.IsGrounded() && player.GetComponent<Collider2D>().bounds.min.y > .6f,
            "A real Space jump reaches the .7m teaching platform");
        Keys(Key.S, Key.Space); yield return null; Keys(); yield return Wait(.6f);
        Check(player.GetComponent<Collider2D>().bounds.max.y < 1.6f && player.IsGrounded(),
            "S + Space passes down through the one-way platform and reaches the floor");
        yield return WalkTo(13.5f);
        CompleteGeneSample sample = Object.FindObjectsByType<CompleteGeneSample>(FindObjectsSortMode.None).Single(s => s.name == "Water Sample");
        yield return Press(Key.E);
        Check(sample == null && player.Genes.GenInventory.Count > 0, "E collects the first fixed sample into the actual player inventory");
        Check(story is StorySession session && session.WasTriggerCompleted("reward:sample-water"),
            "The fixed sample records a persistent claim for continue and death retry");
        yield return WalkTo(16f);
        GeneEditStation station = Object.FindObjectsByType<GeneEditStation>(FindObjectsSortMode.None).OrderBy(s => s.transform.position.x).First();
        GeneEditPanel editor = UIManager.Instance.GetPanel<GeneEditPanel>();
        yield return Press(Key.E);
        Check(player.MenuOpen && Time.timeScale == 0f, "The laboratory terminal opens the real gene editor and pauses gameplay");
        editor.Equip(0); editor.Confirm();
        Check(!player.MenuOpen && player.Genes.GenChain[0] != null, "Confirm equips the collected instance and resumes the chapter");
        yield return WalkTo(22f); yield return JumpOnto(22f, .75f);
        yield return WalkTo(23f); yield return JumpOnto(25.7f, 1.5f);
        GeneChest laboratoryChest = Object.FindObjectsByType<GeneChest>(FindObjectsSortMode.None).Single(chest => chest.name == "Laboratory Branch Cold Storage");
        yield return Press(Key.E);
        GeneDraftPanel draft=Object.FindFirstObjectByType<GeneDraftPanel>();
        if(draft!=null){draft.Choose(0);yield return null;}
        Check(laboratoryChest.Opened, "Normal jumps reach the optional laboratory shelf and E opens its reward");
        yield return DropToFloor();
        yield return WalkTo(31f);
        PlayerSupply module = Object.FindObjectsByType<PlayerSupply>(FindObjectsSortMode.None).Single(s => s.kind == PlayerSupply.SupplyKind.Dash);
        yield return Press(Key.E);
        Check(module == null && player.DashUnlocked, "E collects the fixed laboratory module and unlocks dash");
        Aim(player.transform.position + Vector3.right * 4f); Keys(Key.D, Key.LeftShift); yield return null; yield return new WaitForFixedUpdate();
        Check(player.IsDashing, "Shift activates the actual player's dash on the new map");
        Keys(); yield return Wait(.25f);
        yield return WalkTo(46f); yield return JumpOnto(46f, .75f);
        yield return WalkTo(47f); yield return JumpOnto(49.5f, 1.5f);
        yield return WalkTo(50.5f); yield return JumpOnto(53f, 2.25f);
        GeneChest ecologyChest = Object.FindObjectsByType<GeneChest>(FindObjectsSortMode.None).Single(chest => chest.name == "Ecology Upper Branch Cold Storage");
        yield return Press(Key.E);
        draft=Object.FindFirstObjectByType<GeneDraftPanel>();
        if(draft!=null){draft.Choose(0);yield return null;}
        Check(ecologyChest.Opened, "The three-step ecology branch is physically reachable and contains a usable cold store");
        yield return DropToFloor();
        yield return WalkTo(54.25f); int falls = level.FallRecoveries;
        yield return JumpAcross(58.5f);
        Check(level.FallRecoveries == falls && player.IsGrounded(), "The west 2.4m fissure is traversable with a normal running jump");
        yield return WalkTo(65.25f);
        yield return JumpAcross(69.5f);
        Check(level.FallRecoveries == falls && player.IsGrounded(), "The east 2.4m fissure is traversable with a normal running jump");
        level.SetCheckpoint(new Vector2(41f, 1f));
        Teleport(new Vector2(56f, -6f)); yield return new WaitForFixedUpdate(); yield return Wait(.3f);
        Check(level.FallRecoveries == falls + 1 && Mathf.Abs(player.transform.position.x - 41f) < .1f && !player.IsDead,
            "Falling below a fissure restores the safe checkpoint before the death plane");
        // Resume beyond the already-verified jumps; test both the real arena and projectile damage.
        Teleport(new Vector2(level.guardianEntryX+1.5f, 1f)); yield return Wait(.2f); level.BeginGuardianBattle(); yield return null;
        Check(level.BattleStarted && level.guardians.All(g => g != null && g.gameObject.activeInHierarchy && g.Hp > 0f),
            "The core encounter activates three living guardians");
        foreach (EnemyAIController guardian in level.guardians)
        {
            float hp = guardian.Hp; float deadline = Time.realtimeSinceStartup + 35f;
            Teleport(new Vector2(guardian.transform.position.x - 3f, 1f)); yield return Wait(.25f);
            while (guardian != null && !guardian.IsDead && Time.realtimeSinceStartup < deadline)
            {
                Collider2D target = guardian.GetComponent<Collider2D>();
                Aim(target.bounds.center, true); Keys(); yield return Wait(.05f);
            }
            Aim(player.transform.position + Vector3.right * 4f);
            Check(guardian == null || guardian.IsDead || guardian.Hp < hp, "Real player projectiles damage the core guardian");
            Check(guardian == null || guardian.IsDead, "Left-mouse fire from the real player defeats the core guardian with the collected starter build");
        }
        yield return null;
        Check(level.CoreDefeated && level.barriers.All(b => b == null || !b.activeSelf), "Defeating all guardians opens the physical core seal");
        Teleport(new Vector2(exit.transform.position.x-.8f, 1f)); yield return Wait(.2f);
        bool reached = false; Action onExit = () => reached = true; level.ExitReached += onExit;
        yield return Press(Key.E);
        Check(reached && level.ExitUsed, "The ending exit accepts E input and fires the story completion event");
        level.ExitReached -= onExit;
    }
    private IEnumerator WalkTo(float x)
    {
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Mathf.Abs(player.transform.position.x - x) > .18f && Time.realtimeSinceStartup < deadline)
        {
            float delta = x - player.transform.position.x;
            // Editor asset imports and screenshots can stretch a frame. Slow the test clock
            // near the target while retaining the player's real speed, gravity and input path.
            Time.timeScale = Mathf.Abs(delta) < 1.5f ? .2f : 1f;
            Aim(player.transform.position + Vector3.right * 4f); Keys(delta > 0f ? Key.D : Key.A);
            yield return null;
        }
        Keys(); Time.timeScale = .2f;
        yield return null; yield return null; yield return new WaitForFixedUpdate();
        Time.timeScale = 1f;
        Check(Mathf.Abs(player.transform.position.x - x) <= .3f, "Walkable main route reaches x=" + x.ToString("0.##"));
    }
    private IEnumerator JumpAcross(float landingX)
    {
        Aim(player.transform.position + Vector3.right * 4f);
        Keys(Key.D, Key.Space); yield return null; Keys(Key.D);
        float deadline = Time.realtimeSinceStartup + 2f;
        while (player.transform.position.x < landingX && Time.realtimeSinceStartup < deadline) yield return Wait(.02f);
        Keys(); yield return Wait(.25f);
    }
    private IEnumerator JumpOnto(float landingX, float top)
    {
        float until = Time.realtimeSinceStartup + 8f;
        Time.timeScale = .65f;
        Keys(Key.Space); yield return null;
        while (Time.realtimeSinceStartup < until)
        {
            float delta = landingX - player.transform.position.x;
            Time.timeScale = Mathf.Abs(delta) < 1f ? .25f : .65f;
            Keys(Mathf.Abs(delta) > .18f ? new[] { delta > 0f ? Key.D : Key.A } : Array.Empty<Key>());
            Aim(player.transform.position + Vector3.right * 4f);
            if (player.IsGrounded() && player.GetComponent<Collider2D>().bounds.min.y >= top - .08f) break;
            yield return null;
        }
        Keys(); yield return null; yield return new WaitForFixedUpdate();
        Time.timeScale = 1f;
        // A landing proves reachability; center the character with ordinary walking if a
        // long editor frame carried it past the middle of that same platform.
        if (player.IsGrounded() && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y - top) < .1f)
            yield return WalkTo(landingX);
        Check(Mathf.Abs(player.transform.position.x - landingX) <= .3f && player.IsGrounded() &&
            Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y - top) < .1f,
            "Real jump reaches optional platform x=" + landingX.ToString("0.##") + " / y=" + top.ToString("0.##"));
    }
    private IEnumerator DropToFloor()
    {
        yield return Press(Key.S, Key.Space); yield return Wait(1.1f);
        Check(player.IsGrounded() && player.GetComponent<Collider2D>().bounds.min.y < .1f,
            "The optional branch lets the player drop back onto the main route");
    }
    private IEnumerator Press(params Key[] values)
    { Keys(values); yield return null; yield return null; Keys(); yield return null; yield return null; }
    private IEnumerator Wait(float seconds)
    {
        // Physics needs simulated frames even when an MCP request stalls the editor thread.
        yield return new WaitForSeconds(seconds);
    }
    private void Teleport(Vector2 position)
    {
        Keys(); player.GetComponent<Rigidbody2D>().position = position;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.transform.position = position; Physics2D.SyncTransforms();
    }
    private void Keys(params Key[] values) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(values));
    private void Aim(Vector3 world, bool fire = false)
    {
        Camera camera = Camera.main; if (camera == null) return;
        Vector3 screen = camera.WorldToScreenPoint(world);
        MouseState state = new MouseState { position = screen };
        if (fire) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
    }
    private static void Check(bool success, string description)
    { Results.Add((success ? "PASS: " : "FAIL: ") + description); if (!success) throw new InvalidOperationException(description); }
    private void Finish()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        previousKeyboard?.MakeCurrent(); previousMouse?.MakeCurrent();
        InputSystem.settings.backgroundBehavior = originalBackground;
        if (player != null) { player.SetInvincible(false); player.SetMenuOpen(false); }
        foreach (Behaviour behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
        validationMode?.Invoke(story, new object[] { false });
        Succeeded = Results.Count > 0 && Results.All(result => result.StartsWith("PASS:"));
        Running = false; Debug.Log("Complete chapter runthrough: " + string.Join("\n", Results));
        Destroy(gameObject);
    }
    private void OnDestroy() { if (Running) Finish(); }
}
#endif
