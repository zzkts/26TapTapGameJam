#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Checks real scene/button/input transitions. Combat and platform traversal have separate checks.</summary>
public sealed class CompleteGameLifecycleChecks : MonoBehaviour
{
    public static readonly List<string> Results = new List<string>();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    public static string Stage { get; private set; }
    private Keyboard keyboard, previousKeyboard;
    private InputSettings.BackgroundBehavior originalBackground;
    private InputSettings.EditorInputBehaviorInPlayMode originalEditorInput;
    private readonly List<Behaviour> suspended = new List<Behaviour>();
    private PlayerController player;
    private StorySession story;
    private CompleteLevelFlow level;
    private bool preservePrefs, hadSave, hadEndings, cleaned;
    private string previousSave;
    private int previousEndings;
    private string expectedGenes, expectedChain;

    public static void Run(bool preservePlayerPrefs = true)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Running) throw new InvalidOperationException("Lifecycle checks are already running.");
        var runner = new GameObject("Complete Game Lifecycle Verification").AddComponent<CompleteGameLifecycleChecks>();
        Object.DontDestroyOnLoad(runner.gameObject); runner.Begin(preservePlayerPrefs);
    }
    private void Begin(bool preserve)
    {
        Results.Clear(); Running = true; Succeeded = false; preservePrefs = preserve;
        hadSave = PlayerPrefs.HasKey(RunProgress.SaveKey); previousSave = PlayerPrefs.GetString(RunProgress.SaveKey);
        hadEndings = PlayerPrefs.HasKey(RunProgress.EndingKey); previousEndings = PlayerPrefs.GetInt(RunProgress.EndingKey);
        PlayerPrefs.DeleteKey(RunProgress.SaveKey); PlayerPrefs.DeleteKey(RunProgress.EndingKey); PlayerPrefs.Save();
        previousKeyboard = Keyboard.current; originalBackground = InputSystem.settings.backgroundBehavior;
        originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(Execute());
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
            catch (Exception error) { Results.Add("FAIL: " + Stage + ": " + error); break; }
            yield return wait;
        }
        Finish(); Destroy(gameObject);
    }
    private IEnumerator Cases()
    {
        Stage = "fresh main menu";
        // Reload even when already at the menu so its OnEnable sees the deliberately cleared save.
        {
            int handle = SceneManager.GetActiveScene().handle;
            SceneManager.LoadSceneAsync("BeginScene"); yield return SceneReady("BeginScene", handle);
        }
        yield return Frames(3);
        BeginPanel begin = Object.FindFirstObjectByType<BeginPanel>();
        Check(begin != null && !begin.btnContinue.interactable && !RunProgress.CanContinue,
            "A fresh main menu disables Continue without an incomplete checkpoint");
        yield return StartFromMenu();
        Check(story.dialogue.IsDialogue && player.MenuOpen && Time.timeScale == 0f &&
            story.dialogue.GetComponentInParent<Canvas>().rootCanvas.renderMode != RenderMode.WorldSpace,
            "Start opens the chapter and its screen-space opening dialogue pauses gameplay");
        Check(story.dialogue.body.font != null && story.dialogue.body.font.HasCharacter('\u56de'),
            "Opening dialogue has a font that renders Chinese narrative text");
        yield return DrainDialogue();

        Stage = "checkpoint with evolved genes and equipped chain";
        Teleport(new Vector2(13.5f, 1f)); yield return Wait(.25f); yield return Press(Key.E);
        Check(player.Genes.GenInventory.Any(g => g.Definition.name == "water"),
            "Real E input collects the first fixed water sample");
        Modifier water = Resources.Load<Modifier>("SO/Genes/water");
        player.Genes.Acquire(water); player.Genes.Acquire(water);
        player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/ember"));
        player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/sewage"));
        player.UnlockDash(); yield return DrainDialogue();
        Teleport(new Vector2(41f, 1f)); yield return Wait(.25f); yield return DrainDialogue();
        yield return Press(Key.E);
        GeneEditPanel editor = UIManager.Instance.GetPanel<GeneEditPanel>();
        Check(editor != null && editor.gameObject.activeInHierarchy && player.MenuOpen,
            "E at checkpoint 41 opens the actual gene editor");
        for (int i = 0; i < 3; i++) { editor.slotButtons[i].onClick.Invoke(); editor.Equip(i); }
        editor.confirmButton.onClick.Invoke(); yield return Frames(3);
        story.SaveCheckpoint(new Vector2(41f, 1f));
        expectedGenes = Genes(player); expectedChain = Chain(player);
        RunSaveData save = RunProgress.Read();
        Check(save != null && Mathf.Abs(save.checkpointX - 41f) < .01f && save.dashUnlocked &&
            save.genes.Count == 3 && save.genes.Any(g => g.level == 2 && g.copies == 3) &&
            save.equipped.Take(3).SequenceEqual(new[] { 0, 1, 2 }) &&
            save.completedTriggers.Contains("reward:sample-water"),
            "Checkpoint persists evolved copies, exact equipped instances, dash and claimed sample");

        Stage = "fatal damage and death-menu retry";
        Teleport(new Vector2(44f, 1f)); player.TakeDamage(100f);
        yield return Until(() => UIManager.Instance.GetPanel<RunMenuPanel>() is RunMenuPanel menu &&
            menu.gameObject.activeInHierarchy && menu.retryButton.gameObject.activeSelf, 4f, "Death menu did not appear");
        RunMenuPanel death = UIManager.Instance.GetPanel<RunMenuPanel>();
        Check(player.IsDead && player.MenuOpen && Time.timeScale == 0f && !death.resumeButton.gameObject.activeSelf,
            "Actual fatal damage shows the death menu after the character death animation");
        int previousScene = SceneManager.GetActiveScene().handle; death.retryButton.onClick.Invoke();
        yield return SceneReady("GameScene", previousScene); yield return BindChapter(); yield return DrainDialogue();
        AssertRestored("Death Retry");

        Stage = "pause return and Continue";
        yield return Press(Key.Escape);
        RunMenuPanel pause = UIManager.Instance.GetPanel<RunMenuPanel>();
        Check(pause.gameObject.activeInHierarchy && pause.resumeButton.gameObject.activeSelf && player.MenuOpen,
            "Escape opens the live pause menu after a restored checkpoint");
        previousScene = SceneManager.GetActiveScene().handle; pause.menuButton.onClick.Invoke();
        yield return SceneReady("BeginScene", previousScene); yield return Frames(3);
        begin = Object.FindFirstObjectByType<BeginPanel>();
        Check(begin.btnContinue.interactable && RunProgress.CanContinue,
            "Return to the main menu offers Continue for the incomplete saved run");
        previousScene = SceneManager.GetActiveScene().handle; begin.btnContinue.onClick.Invoke();
        yield return SceneReady("GameScene", previousScene); yield return BindChapter(); yield return DrainDialogue();
        AssertRestored("Main-menu Continue");
        yield return FinishEnding(true);

        Stage = "second fresh run and sealed ending";
        yield return StartFromMenu(); yield return DrainDialogue();
        Check(player.Genes.GenInventory.Count == 0 && !player.DashUnlocked && !level.CoreDefeated,
            "Starting again resets inventory, dash and core encounter while retaining discovered endings");
        yield return FinishEnding(false);
        Check(RunProgress.DiscoveredEndings == 3,
            "Both real choice buttons record their distinct endings across two fresh runs");
        Stage = "complete";
    }
    private IEnumerator StartFromMenu()
    {
        BeginPanel begin = Object.FindFirstObjectByType<BeginPanel>();
        Check(begin != null && begin.btnBegin.interactable, "The main-menu Start button is usable");
        int handle = SceneManager.GetActiveScene().handle; begin.btnBegin.onClick.Invoke();
        yield return SceneReady("GameScene", handle); yield return BindChapter();
    }
    private IEnumerator BindChapter()
    {
        yield return Until(() => StorySession.Current != null && StorySession.Current.Ready, 10f, "StorySession did not initialize");
        story = StorySession.Current; player = story.player; level = story.level;
        Check(player != null && level != null && !story.ValidationMode,
            "The new gameplay scene binds player, world and a live narrative session");
        foreach (Behaviour item in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (item.enabled && (item is EnemyAIController || item is EnemySupportAura))
            { suspended.Add(item); item.enabled = false; }
        yield return Frames(2);
    }
    private void AssertRestored(string source)
    {
        Check(Mathf.Abs(player.transform.position.x - 41f) < .15f && player.Hp == player.MaxHp && player.DashUnlocked &&
            Genes(player) == expectedGenes && Chain(player) == expectedChain &&
            story.WasTriggerCompleted("reward:sample-water") &&
            !Object.FindObjectsByType<CompleteGeneSample>(FindObjectsSortMode.None).Any(s => s.name == "Water Sample"),
            source + " restores checkpoint 41, full life, evolved gene instances, equipped order, dash and claimed rewards");
    }
    private IEnumerator FinishEnding(bool coexistence)
    {
        Stage = coexistence ? "coexistence ending" : "sealed ending";
        Teleport(new Vector2(level.guardianEntryX, 1f)); yield return Wait(.25f); yield return DrainDialogue();
        Check(level.BattleStarted && level.guardians.Length == 3 && level.guardians.All(g => g != null && g.gameObject.activeInHierarchy),
            "The guardian story trigger activates all three core guardians");
        foreach (EnemyAIController guardian in level.guardians) guardian.TakeDamage(guardian.MaxHp + 1f);
        yield return Frames(3); yield return DrainDialogue();
        Check(level.CoreDefeated && story.GuardianCleared && level.barriers.All(b => b == null || !b.activeSelf),
            "Defeating living guardians updates story progress and opens the physical seal");
        Teleport(new Vector2(Object.FindFirstObjectByType<CompleteWorldExit>().transform.position.x-.8f, 1f)); yield return Wait(.2f); yield return DrainDialogue(); yield return Press(Key.E);
        yield return DrainDialogue();
        Check(level.ExitUsed && story.dialogue.IsChoosing && player.MenuOpen,
            "Real E input at the core exit opens the final decision UI");
        story.dialogue.skip.onClick.Invoke(); yield return Frames(2);
        Check(story.dialogue.IsChoosing && RunProgress.CanContinue,
            "Skipping narration cannot bypass the required final choice");
        (coexistence ? story.dialogue.coexist : story.dialogue.seal).onClick.Invoke(); yield return DrainDialogue();
        yield return Until(() => story.dialogue.IsResult, 5f, "Ending result did not appear");
        string ending = coexistence ? "\u6c34\u7684\u5f52\u5904" : "\u6700\u540e\u7684\u56de\u58f0";
        RunSaveData data = RunProgress.Read();
        Check(data != null && data.completed && data.ending == ending && !RunProgress.CanContinue &&
            story.dialogue.endingTitle.text == ending && player.MenuOpen,
            "The " + (coexistence ? "coexistence" : "sealed") + " choice shows its own ending result and completes the checkpoint");
        int handle = SceneManager.GetActiveScene().handle; story.dialogue.mainMenu.onClick.Invoke();
        yield return SceneReady("BeginScene", handle); yield return Frames(3);
        BeginPanel begin = Object.FindFirstObjectByType<BeginPanel>();
        Check(begin != null && !begin.btnContinue.interactable && !RunProgress.CanContinue && Time.timeScale == 1f,
            "The ending's main-menu button returns safely and disables Continue for the completed run");
    }
    private IEnumerator DrainDialogue()
    {
        float deadline = Time.realtimeSinceStartup + 10f; int quiet = 0;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (story.dialogue.IsDialogue) { quiet = 0; story.dialogue.skip.onClick.Invoke(); }
            else if (story.dialogue.IsChoosing || story.dialogue.IsResult) yield break;
            else if (!story.IsModal && ++quiet >= 4) yield break;
            yield return null;
        }
        throw new InvalidOperationException("Dialogue callbacks did not resume gameplay or open the next modal.");
    }
    private static string Genes(PlayerController owner) => string.Join("|", owner.Genes.GenInventory.Select(g =>
        g.Definition.name + ":" + g.InstanceId + ":" + g.Level + ":" + g.Copies));
    private static string Chain(PlayerController owner) => string.Join("|", owner.Genes.GenChain.Select(g => g != null ? g.InstanceId : "empty"));
    private IEnumerator SceneReady(string name, int priorHandle)
    {
        yield return Until(() => SceneManager.GetActiveScene().name == name &&
            SceneManager.GetActiveScene().handle != priorHandle, 15f, "Scene transition to " + name + " timed out");
        yield return Frames(3);
    }
    private IEnumerator Until(Func<bool> predicate, float seconds, string failure)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!predicate()) { if (Time.realtimeSinceStartup >= deadline) throw new InvalidOperationException(failure); yield return null; }
    }
    private static IEnumerator Frames(int count) { for (int i = 0; i < count; i++) yield return null; }
    private static IEnumerator Wait(float seconds)
    { float until = Time.realtimeSinceStartup + seconds; while (Time.realtimeSinceStartup < until) yield return null; }
    private IEnumerator Press(params Key[] values)
    { Keys(values); yield return Frames(2); Keys(); yield return Frames(2); }
    private void Keys(params Key[] values) { if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, new KeyboardState(values)); }
    private void Teleport(Vector2 point)
    {
        Keys(); Rigidbody2D body = player.GetComponent<Rigidbody2D>(); body.position = point; body.linearVelocity = Vector2.zero;
        player.transform.position = point; Physics2D.SyncTransforms();
    }
    private static void Check(bool condition, string message)
    { Results.Add((condition ? "PASS: " : "FAIL: ") + message); if (!condition) throw new InvalidOperationException(message); }
    private void Finish()
    {
        if (cleaned) return; cleaned = true;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard); previousKeyboard?.MakeCurrent();
        InputSystem.settings.backgroundBehavior = originalBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
        foreach (Behaviour item in suspended) if (item != null) item.enabled = true;
        if (preservePrefs)
        {
            if (hadSave) PlayerPrefs.SetString(RunProgress.SaveKey, previousSave); else PlayerPrefs.DeleteKey(RunProgress.SaveKey);
            if (hadEndings) PlayerPrefs.SetInt(RunProgress.EndingKey, previousEndings); else PlayerPrefs.DeleteKey(RunProgress.EndingKey);
            PlayerPrefs.Save();
            BeginPanel begin = Object.FindFirstObjectByType<BeginPanel>();
            if (begin != null) begin.btnContinue.interactable = RunProgress.CanContinue;
        }
        Succeeded = Results.Count > 0 && Results.All(r => r.StartsWith("PASS:")); Running = false;
        Debug.Log("Complete game lifecycle checks: " + string.Join("\n", Results));
    }
    private void OnDestroy() { if (!cleaned) Finish(); }
}
#endif
