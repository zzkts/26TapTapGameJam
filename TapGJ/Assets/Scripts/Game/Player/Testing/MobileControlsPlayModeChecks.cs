#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>Real multi-touch events through InputSystemUIInputModule; isolated save, no player build.</summary>
public sealed class MobileControlsPlayModeChecks : MonoBehaviour
{
    private const string Folder = "Temp/MobileControls/";
    [Serializable] private sealed class Report
    {
        public bool success, saveUnchanged, endingsUnchanged;
        public string stage, error;
        public string[] checks;
    }
    private readonly Report report = new();
    private readonly List<string> checks = new();
    private Touchscreen screen, previousTouch;
    private InputSettings.BackgroundBehavior previousBackground;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private bool previousRunInBackground;
    private readonly Dictionary<int, Vector2> touches = new();
    private PlayerController player;
    private MobileControlsUI ui;
    private GameObject terminalFixture;
    private float music, effects;
    private bool cleaned, audioCaptured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Isolate()
    { if (SessionState.GetBool("MobileQA.Run", false)) RunProgress.BeginIsolatedValidation(); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        if (!SessionState.GetBool("MobileQA.Run", false)) return;
        SessionState.SetBool("MobileQA.Run", false);
        var host = new GameObject("Mobile touch verification"); DontDestroyOnLoad(host);
        host.AddComponent<MobileControlsPlayModeChecks>().StartCoroutine("Run");
    }
    private IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);
        previousRunInBackground = Application.runInBackground; Application.runInBackground = true;
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        previousTouch = Touchscreen.current; screen = InputSystem.AddDevice<Touchscreen>();
        var stack = new Stack<IEnumerator>(); stack.Push(Cases());
        while (stack.Count > 0)
        {
            object wait;
            try { var step = stack.Peek(); if (!step.MoveNext()) { stack.Pop(); continue; } wait = step.Current; }
            catch (Exception e) { report.error = e.ToString(); break; }
            if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            yield return wait;
        }
        report.saveUnchanged = PlayerPrefs.GetString(RunProgress.SaveKey, "") == SessionState.GetString("MobileQA.Save", "missing");
        report.endingsUnchanged = PlayerPrefs.GetInt(RunProgress.EndingKey, 0) == SessionState.GetInt("MobileQA.Endings", -1);
        report.success = string.IsNullOrEmpty(report.error) && report.saveUnchanged && report.endingsUnchanged;
        report.checks = checks.ToArray(); File.WriteAllText(Folder + "result.json", JsonUtility.ToJson(report, true));
        Cleanup(); SessionState.SetBool("MobileQA.Finished", true);
    }
    private IEnumerator Cases()
    {
        yield return null; yield return null; yield return null;
        Check(RunProgress.IsolatedValidation && MobileGameInput.Enabled, "Phone controls and isolated progress are enabled");
        music = GlobalManager.Instance.BackgroundVolume; effects = GlobalManager.Instance.GameVolume; audioCaptured = true;
        yield return Resolution(1280, 720);
        var begin = FindFirstObjectByType<BeginPanel>();
        Check(begin != null && begin.GetComponentInParent<MobileScreenLayout>() != null, "BeginScene uses a phone safe-area layout");
        yield return MenuCases(begin);
        yield return Capture("01-mobile-menu");
        yield return Tap(begin.btnBegin.transform, 1);
        float deadline = Time.realtimeSinceStartup + 25;
        while (StorySession.Current == null || !StorySession.Current.Ready || MobileControlsUI.Current == null)
        { if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Touch Start button did not open GameScene"); yield return null; }
        StorySession.Current.SetValidationMode(true); player = StorySession.Current.player; ui = MobileControlsUI.Current;
        player.SetInvincible(true);
        foreach (var enemy in FindObjectsByType<EnemyAIController>(FindObjectsInactive.Include, FindObjectsSortMode.None)) enemy.enabled = false;
        yield return new WaitForSecondsRealtime(.3f);
        Check(Mathf.Approximately(player.jumpVelocity, 4) && Mathf.Approximately(player.moveSpeed, 3), "Phone uses the original movement and jump values");
        Canvas.ForceUpdateCanvases();
        Check(ui.transform.Find("Mobile Health/Health").GetComponent<Text>().cachedTextGenerator.vertexCount>4 &&
            ui.transform.Find("Mobile Gene Ribbon/Attribute").GetComponent<Text>().cachedTextGenerator.vertexCount>4,
            "Compact health numbers and current projectile name actually render");
        LayoutCheck(); yield return Capture("02-mobile-game-16x9");
        report.stage = "Simultaneous movement, aiming, firing and jump";
        float start = player.transform.position.x; int created = ProjectilePool.CreatedCount, reused = ProjectilePool.ReusedCount;
        Down(11, Point(ui.MoveStick.transform)); Drag(11, Point(ui.MoveStick.transform, new Vector2(90, 0)));
        yield return null;
        Check(!MobileGameInput.Firing, "Movement stick does not trigger world shooting");
        Down(22, WorldPoint());
        yield return new WaitForSecondsRealtime(.8f);
        Check(player.transform.position.x > start + 1.2f && MobileGameInput.Move.x > .7f, "Left finger moves the real player");
        Check(MobileGameInput.Firing && MobileGameInput.Aim.y > .25f && ProjectilePool.CreatedCount + ProjectilePool.ReusedCount > created + reused, "Second finger aims and fires actual pooled water projectiles");
        Down(33, Point(ui.JumpButton.transform)); yield return new WaitForSecondsRealtime(.15f);
        Check(player.YVelocity > .2f && player.JumpHeld, "Third finger jumps while moving and shooting");
        Up(33); Up(11); Up(22); yield return new WaitForSecondsRealtime(.3f);
        Check(!MobileGameInput.JumpHeld && !MobileGameInput.Firing && MobileGameInput.Move == Vector2.zero, "Releasing all fingers clears held actions");
        Check(!UnityEngine.Rendering.DebugManager.instance.displayRuntimeUI && !UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI, "Three-finger gameplay cannot open URP debugging over the controls");
        yield return Tap(ui.DashButton.transform, 44, .06f);
        Check(player.IsDashing || player.DashCooldownRemaining > 0, "Touch dash uses the real cooldown");
        yield return new WaitForSecondsRealtime(.4f);
        float platformFeet = player.GetComponent<Collider2D>().bounds.min.y;
        Check(player.IsGrounded() && platformFeet > .8f, "Touch movement landed on the first real one-way platform");
        yield return Tap(ui.DropButton.transform, 45); yield return new WaitForSecondsRealtime(.3f);
        Check(player.GetComponent<Collider2D>().bounds.min.y < platformFeet - .4f, "The down button drops through the actual one-way platform");
        yield return TouchAimCases();

        report.stage = "Pause cancels owned fingers";
        Down(11, Point(ui.MoveStick.transform)); Drag(11, Point(ui.MoveStick.transform, new Vector2(90, 0)));
        Down(22, WorldPoint()); yield return new WaitForSecondsRealtime(.12f);
        yield return Tap(ui.PauseButton.transform, 55);
        Check(player.MenuOpen && Time.timeScale == 0 && !MobileGameInput.Firing && MobileGameInput.Move == Vector2.zero && !ui.MoveStick.Held && !ui.ScreenAim.Held, "Pause releases movement and firing even while fingers remain on screen");
        var hud = FindFirstObjectByType<PlayerHudPanel>();
        Canvas.ForceUpdateCanvases();
        Check(hud.runMenu.title.cachedTextGenerator.vertexCount>4,"Pause title actually renders inside its text bounds");
        yield return Capture("03-mobile-pause");
        var pauseGuide=hud.runMenu.GetComponentsInChildren<Button>(true).First(b=>b.name=="Mobile Guide Button");
        yield return Tap(pauseGuide.transform,85);
        Check(ControlsGuidePanel.Current.IsOpen && player.MenuOpen && Time.timeScale==0,"Pause menu opens the guide without resuming gameplay");
        yield return Tap(ControlsGuidePanel.Current.CloseButton.transform,86);
        Check(!ControlsGuidePanel.Current.IsOpen && player.MenuOpen && !MobileGameInput.Firing,"Closing the guide returns to pause without firing");
        yield return Tap(hud.runMenu.resumeButton.transform, 56);
        Check(!player.MenuOpen && !MobileGameInput.Firing && MobileGameInput.Move == Vector2.zero, "Resume does not replay fingers held before pause");
        Up(11); Up(22); yield return new WaitForSecondsRealtime(.2f);
        Down(23, WorldPoint()); yield return new WaitForSecondsRealtime(.08f);
        Send(23, touches[23], TouchPhase.Canceled); touches.Remove(23); yield return new WaitForSecondsRealtime(.12f);
        Check(!MobileGameInput.Firing && !ui.ScreenAim.Held,"Canceled touches release shooting ownership");
        Down(24,WorldPoint());yield return new WaitForSecondsRealtime(.08f);
        ui.SendMessage("OnApplicationPause",true);yield return new WaitForSecondsRealtime(.12f);
        Check(player.MenuOpen&&!MobileGameInput.Firing&&!ui.ScreenAim.Held,"App suspension clears held firing and opens pause");
        Up(24);yield return Tap(hud.runMenu.resumeButton.transform,57);

        yield return TouchWorkbench();
        report.stage = "Wide phone and tablet layouts";
        yield return Resolution(2340, 1080); yield return new WaitForSecondsRealtime(.25f); LayoutCheck(); yield return Capture("05-mobile-wide");
        yield return Resolution(1440, 1080); yield return new WaitForSecondsRealtime(.25f); LayoutCheck(); yield return Capture("06-mobile-tablet");
        yield return Resolution(1280, 720);
        report.stage = "Audio sliders through touch";
        yield return Tap(ui.PauseButton.transform, 60); yield return Tap(hud.runMenu.settingsButton.transform, 61);
        var settings = UIManager.Instance.GetPanel<SettingPanel>();
        yield return new WaitForSecondsRealtime(.2f);
        float before = settings.sliderBk.value;
        var slider = (RectTransform)settings.sliderBk.transform;
        Down(62, Point(slider, new Vector2(-slider.rect.width * .35f, 0)));
        yield return new WaitForSecondsRealtime(.06f);
        Drag(62, Point(slider, new Vector2(slider.rect.width * .3f, 0))); yield return new WaitForSecondsRealtime(.15f); Up(62);
        Check(Mathf.Abs(settings.sliderBk.value - before) > .05f || settings.sliderBk.value > .7f, "Music slider responds to a touch drag");
        settings.sliderBk.value = music; settings.sliderGame.value = effects;
        yield return Capture("07-mobile-settings");
        yield return Tap(settings.btnClose.transform, 63);
        // Keep the exact existing preference keys, including their absence.
        RestoreAudioPrefs();
        yield return Tap(hud.runMenu.resumeButton.transform, 64);
        Check(!player.MenuOpen && !MobileGameInput.Firing, "Settings closes back to pause, then resumes safely");
        report.stage = "Completed";
    }
    private IEnumerator MenuCases(BeginPanel begin)
    {
        report.stage="Phone menu composition and on-demand guide";
        Check(!begin.transform.Find("Control Hint").gameObject.activeSelf,"Old bottom-left instruction text is hidden on phones");
        var help=begin.GetComponentsInChildren<Button>(true).First(b=>b.name=="Mobile Guide Button");
        var controls=new[]{begin.btnBegin,begin.btnContinue,begin.btnSetting,help};
        int[,] sizes={{1280,720},{2340,1080},{1440,1080}};
        for(int s=0;s<3;s++)
        {
            yield return Resolution(sizes[s,0],sizes[s,1]);yield return null;Canvas.ForceUpdateCanvases();
            Rect frame=Bounds(begin.transform.Find("Menu Surface") as RectTransform);
            foreach(var control in controls)
            {
                Rect r=Bounds((RectTransform)control.transform);
                Check(frame.Contains(r.min)&&frame.Contains(r.max),Screen.width+"x"+Screen.height+": menu "+control.name+" fits inside the card");
            }
            for(int a=0;a<controls.Length;a++)for(int b=a+1;b<controls.Length;b++)
                if(Bounds((RectTransform)controls[a].transform).Overlaps(Bounds((RectTransform)controls[b].transform)))throw new Exception("Overlapping main menu buttons");
            yield return Capture("menu-"+sizes[s,0]+"x"+sizes[s,1]);
        }
        yield return Resolution(1280,720);
        yield return Tap(help.transform,91);
        Check(ControlsGuidePanel.Current.IsOpen,"Main menu opens the touch guide by tapping its button");
        yield return Capture("01a-mobile-guide");
        yield return Tap(begin.btnBegin.transform,92);
        Check(ControlsGuidePanel.Current.IsOpen && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="BeginScene","The guide blocks taps from starting a run behind it");
        yield return Tap(ControlsGuidePanel.Current.CloseButton.transform,93);
        Check(!ControlsGuidePanel.Current.IsOpen,"Guide closes back to the main menu");
    }
    private static Vector2 WorldPoint(float x = .62f, float y = .62f) => new(Screen.width * x, Screen.height * y);
    private IEnumerator TouchAimCases()
    {
        report.stage = "Screen-position aiming without an attack joystick";
        Check(ui.GetComponentsInChildren<MobileStick>().Length == 1, "Only the movement joystick exists");
        yield return new WaitForSecondsRealtime(.5f);
        var target = WorldPoint();
        Down(81, target); yield return new WaitForSecondsRealtime(.12f);
        var camera = Camera.main;
        Vector2 expected = camera.ScreenToWorldPoint(new Vector3(target.x,target.y,player.transform.position.z-camera.transform.position.z));
        Check(MobileGameInput.Firing && Vector2.Distance(player.AimPosition,expected)<.08f,
            "World touch aims at the exact tapped world position rather than a stick direction; firing="+MobileGameInput.Firing+" menu="+player.MenuOpen+" aimDistance="+Vector2.Distance(player.AimPosition,expected));
        target = WorldPoint(.08f);
        Drag(81,target); yield return new WaitForSecondsRealtime(.12f);
        expected = camera.ScreenToWorldPoint(new Vector3(target.x,target.y,player.transform.position.z-camera.transform.position.z));
        Check(player.FaceDir<0 && Vector2.Distance(player.AimPosition,expected)<.08f, "Dragging the aiming finger can shoot behind the player");
        Down(82,WorldPoint(.75f)); yield return new WaitForSecondsRealtime(.1f);
        Check(MobileGameInput.AimScreenPosition==target, "A second world finger cannot steal the current aim");
        Drag(81,Point(ui.JumpButton.transform)); yield return new WaitForSecondsRealtime(.1f);
        Check(!MobileGameInput.Firing, "Dragging an aiming finger over a UI button stops firing behind it");
        Drag(81,WorldPoint()); yield return new WaitForSecondsRealtime(.1f);
        Check(MobileGameInput.Firing, "Dragging back into the scene resumes held fire");
        Up(81); Up(82); yield return new WaitForSecondsRealtime(.12f);
        Check(!MobileGameInput.Firing && !ui.ScreenAim.Held, "Releasing the owning world finger stops shooting");
        int shots=player.Presentation.ShotCount;
        Down(83,Point(ui.DropButton.transform)); yield return new WaitForSecondsRealtime(.12f);
        Check(!MobileGameInput.Firing && player.Presentation.ShotCount==shots, "Touching an action button does not shoot into the scene");
        Up(83);
        float deadline=Time.realtimeSinceStartup+2;
        while(!player.Weapon.CanAttack() && Time.realtimeSinceStartup<deadline)yield return null;
        shots=player.Presentation.ShotCount;
        Down(84,WorldPoint()); yield return new WaitForSecondsRealtime(.025f); Up(84);
        yield return new WaitForSecondsRealtime(.15f);
        Check(player.Presentation.ShotCount==shots+1 && !MobileGameInput.Firing, "A quick tap fires one shot without leaving continuous fire held");
    }
    private IEnumerator TouchWorkbench()
    {
        report.stage = "Touch interaction and gene workbench";
        foreach (var definition in Resources.LoadAll<Modifier>("SO/Genes").Where(g => !g.IsAttribute || g.Element != E_ElementType.Electric)) player.Genes.Acquire(definition);
        // Temporary station only. The actual player is never teleported or reconfigured.
        terminalFixture = new GameObject("Mobile QA terminal"); terminalFixture.transform.position = player.transform.position;
        terminalFixture.AddComponent<GeneEditStation>().interactionRange = 3;
        yield return new WaitForSecondsRealtime(.2f);
        Check(ui.InteractButton.Available, "Nearby actions enable the touch interaction button");
        yield return Tap(ui.InteractButton.transform, 70); yield return new WaitForSecondsRealtime(.2f);
        var panel = UIManager.Instance.GetPanel<GeneEditPanel>();
        Check(player.MenuOpen && panel.gameObject.activeSelf && !MobileGameInput.Firing, "Touch interaction opens the real gene workbench");
        var scroll = panel.inventoryContent.GetComponentInParent<ScrollRect>();
        var viewport = scroll.viewport;
        yield return Capture("04a-workbench-before-scroll");
        Down(71, Point(viewport, new Vector2(0, -viewport.rect.height * .3f)));
        yield return new WaitForSecondsRealtime(.06f);
        for(int i=1;i<=8;i++)
        {Drag(71,Point(viewport,new Vector2(0,Mathf.Lerp(-.3f,.3f,i/8f)*viewport.rect.height)));yield return new WaitForSecondsRealtime(.03f);}
        Up(71);
        yield return new WaitForSecondsRealtime(.15f);
        Check(scroll.verticalNormalizedPosition < .95f, "Gene inventory scrolls with a finger instead of a mouse wheel; normalized="+scroll.verticalNormalizedPosition.ToString("F2")+" rows="+panel.inventoryContent.childCount);
        scroll.verticalNormalizedPosition = 1; yield return null;
        yield return Tap(panel.slotButtons[1].transform, 72);
        Check(panel.SelectedSlot == 1, "A slot can be selected by touch");
        var first = panel.inventoryContent.GetComponentsInChildren<Button>().First();
        yield return Tap(first.transform, 73);
        Check(panel.CopyDraft()[1] != null && player.Genes.GenChain[1] == null, "Touch equips a preview without changing the active build");
        yield return Capture("04-mobile-workbench");
        yield return Tap(panel.confirmButton.transform, 74); yield return new WaitForSecondsRealtime(.15f);
        Check(!player.MenuOpen && player.Genes.GenChain[1] != null, "Touch confirms and applies the gene build");
        // Equip a real attribute through the same station for the enchant button check.
        yield return Tap(ui.InteractButton.transform, 75);
        int attribute = player.Genes.GenInventory.ToList().FindIndex(g => g.IsAttribute);
        panel.SelectSlot(0); panel.Equip(attribute); panel.Confirm(); yield return new WaitForSecondsRealtime(.2f);
        yield return Tap(ui.EnchantButton.transform, 76);
        Check(player.GetComponent<GeneChainController>().State == GeneChainController.Phase.Active, "Touch enchant activates the real attribute chain");
        Destroy(terminalFixture); yield return null;
    }
    private void LayoutCheck()
    {
        var controls = new RectTransform[] { (RectTransform)ui.MoveStick.transform,
            (RectTransform)ui.JumpButton.transform, (RectTransform)ui.DashButton.transform, (RectTransform)ui.EnchantButton.transform,
            (RectTransform)ui.InteractButton.transform, (RectTransform)ui.DropButton.transform, (RectTransform)ui.PauseButton.transform };
        Rect safe = Screen.safeArea;
        foreach (var rect in controls)
        {
            Rect bounds = Bounds(rect);
            Check(bounds.xMin >= safe.xMin - 1 && bounds.xMax <= safe.xMax + 1 && bounds.yMin >= safe.yMin - 1 && bounds.yMax <= safe.yMax + 1,
                Screen.width + "x" + Screen.height + ": " + rect.name + " stays in the safe area");
        }
        for (int i = 0; i < controls.Length; i++) for (int j = i + 1; j < controls.Length; j++)
            if (Bounds(controls[i]).Overlaps(Bounds(controls[j]))) throw new Exception("Overlapping touch targets: " + controls[i].name + "/" + controls[j].name);
        checks.Add("PASS: Touch targets do not overlap at " + Screen.width + "x" + Screen.height);
    }
    private static Rect Bounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]); var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private static Vector2 Point(Transform target, Vector2 offset = default)
    {
        var rect = target as RectTransform;
        Vector3 center = rect != null ? (Vector3)rect.rect.center : Vector3.zero;
        return RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(center + (Vector3)offset));
    }
    private void Down(int id, Vector2 at) { touches[id] = at; Send(id, at, TouchPhase.Began); }
    private void Drag(int id, Vector2 at) { touches[id] = at; Send(id, at, TouchPhase.Moved); }
    private void Up(int id) { if (touches.TryGetValue(id, out var at)) { Send(id, at, TouchPhase.Ended); touches.Remove(id); } }
    private void Send(int id, Vector2 at, TouchPhase phase) => InputSystem.QueueStateEvent(screen, new TouchState { touchId = id, position = at, phase = phase, pressure = phase == TouchPhase.Ended ? 0 : 1 });
    private IEnumerator Tap(Transform target, int id, float hold = .08f)
    { Down(id, Point(target)); yield return new WaitForSecondsRealtime(hold); Up(id); yield return new WaitForSecondsRealtime(.12f); }
    private IEnumerator Resolution(int width, int height)
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var views = Resources.FindObjectsOfTypeAll(type); var view = views.FirstOrDefault() as EditorWindow;
        if (view == null) throw new Exception("Open the Game view before running phone checks.");
        var method = type.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method.Invoke(view, new object[] { new Vector2(width, height), "Water Drain Mobile QA" }); view.Repaint();
        yield return null; yield return null; yield return null;
        float deadline = Time.realtimeSinceStartup + 3;
        while (Screen.width != width || Screen.height != height)
        { if (Time.realtimeSinceStartup > deadline) throw new Exception("Game view did not resize to " + width + "x" + height); yield return null; }
    }
    private IEnumerator Capture(string name)
    { Canvas.ForceUpdateCanvases(); yield return null; ScreenCapture.CaptureScreenshot(Folder + name + ".png"); yield return null; yield return null; }
    private void Check(bool value, string message)
    {
        if(!value&&player!=null)message+=" [focused="+Application.isFocused+", paused="+player.MenuOpen+", timeScale="+Time.timeScale+", move="+MobileGameInput.Move+"]";
        checks.Add((value ? "PASS: " : "FAIL: ") + message);
        report.checks = checks.ToArray(); File.WriteAllText(Folder + "progress.json", JsonUtility.ToJson(report, true));
        if (!value) throw new Exception(message);
    }
    private static void RestoreAudioPrefs()
    {
        foreach (var key in new[] { "Audio.BackgroundVolume", "Audio.GameVolume" })
            if (SessionState.GetBool("MobileQA.Has." + key, false)) PlayerPrefs.SetFloat(key, SessionState.GetFloat("MobileQA.Value." + key, 1));
            else PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
    private void Cleanup()
    {
        if (cleaned) return; cleaned = true;
        if (screen != null) { InputSystem.RemoveDevice(screen); screen = null; }
        previousTouch?.MakeCurrent(); MobileGameInput.Reset();
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        Application.runInBackground = previousRunInBackground;
        var audio = FindFirstObjectByType<GlobalManager>();
        if (audioCaptured && audio != null) { audio.SetBackgroundVolume(music); audio.SetGameVolume(effects); }
        RestoreAudioPrefs(); if (terminalFixture != null) Destroy(terminalFixture);
    }
    private void OnDestroy() => Cleanup();
}
#endif
