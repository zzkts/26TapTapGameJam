using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Runtime-only phone layout; authored desktop scenes are never rewritten.</summary>
[DefaultExecutionOrder(-500)]
public sealed class MobileScreenLayout : MonoBehaviour
{
    public RectTransform SafeRoot { get; private set; }
    private Rect lastArea;
    private Vector2Int lastSize;
    private static bool awaitingLandscape;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrepareTouch()
    {
        if (!MobileGameInput.Enabled) return;
        if (Application.isMobilePlatform)
        {
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            // AutoRotation alone can retain the initial portrait orientation until the phone moves.
            // Match the native launch orientation before the first scene or UI is initialized.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            awaitingLandscape = true;
            Application.targetFrameRate = 60;
        }
        // URP's three-finger debug shortcut conflicts with move + aim + jump.
#if UNITY_EDITOR
        UnityEditor.SessionState.SetBool("MobilePreview.DebugCaptured", true);
        UnityEditor.SessionState.SetBool("MobilePreview.PreviousRuntimeDebug", UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI);
#endif
        UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        Attach(SceneManager.GetActiveScene());
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Attach(scene);
    private static void Attach(Scene scene)
    {
        if (!MobileGameInput.Enabled || !scene.IsValid() || !scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace && canvas.GetComponent<MobileScreenLayout>() == null)
                canvas.gameObject.AddComponent<MobileScreenLayout>();
    }
    private void Awake()
    {
        MobileGameInput.Reset();
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        SafeRoot = GameplayUIFactory.Rect("Mobile Safe Area", transform, Vector2.zero, Vector2.one);
        WrapChildren(); ApplySafeArea(); StartCoroutine(Configure());
    }
    private void Update()
    {
        // Wait for the native resize before allowing rotation between the two landscape sides.
        if (Application.isMobilePlatform && awaitingLandscape && Screen.width > Screen.height)
        {
            awaitingLandscape = false;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
        WrapChildren(); ApplySafeArea();
    }
    private void WrapChildren()
    {
        // Runtime chapter notices are created after scene initialization.
        while (transform.childCount > 1)
        {
            Transform child = transform.GetChild(0);
            if (child == SafeRoot) child = transform.GetChild(1);
            child.SetParent(SafeRoot, false);
        }
    }
    private void ApplySafeArea()
    {
        Rect area = Screen.safeArea;
        var size = new Vector2Int(Screen.width, Screen.height);
        if (lastArea == area && lastSize == size || size.x <= 0 || size.y <= 0) return;
        lastArea = area; lastSize = size;
        SafeRoot.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
        SafeRoot.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        SafeRoot.offsetMin = SafeRoot.offsetMax = Vector2.zero;
    }
    private IEnumerator Configure()
    {
        yield return null;
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events != null && Screen.dpi > 0) events.pixelDragThreshold = Mathf.Max(events.pixelDragThreshold, Mathf.RoundToInt(Screen.dpi * .08f));
        var hud = SafeRoot.GetComponentInChildren<PlayerHudPanel>(true);
        if (hud != null)
        {
            if (hud.visualGroup != null) hud.visualGroup.gameObject.SetActive(false);
            var controls = GameplayUIFactory.Rect("Mobile Controls", SafeRoot, Vector2.zero, Vector2.one);
            controls.SetAsFirstSibling(); controls.gameObject.AddComponent<MobileControlsUI>().Build(hud);
            var guide=ControlsGuidePanel.Create(SafeRoot,hud.healthText.font);
            var pauseFrame=hud.runMenu!=null?hud.runMenu.transform.Find("Pause Frame"):null;
            if(pauseFrame!=null)
            {
                var help=GameplayUIFactory.Button("Mobile Guide Button",pauseFrame,Vector2.one,Vector2.one,hud.healthText.font,"操作");
                help.targetGraphic=help.GetComponent<Image>();
                var rect=(RectTransform)help.transform;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-24,-24);rect.sizeDelta=new Vector2(92,48);
                help.image.sprite=PlayerUIStyle.Rounded;help.image.type=Image.Type.Sliced;help.image.color=PlayerUIStyle.Violet;
                help.onClick.AddListener(guide.Show);
            }
        }
        var begin = SafeRoot.GetComponentInChildren<BeginPanel>(true);
        if (begin != null) MobileBeginMenuLayout.Apply(begin,SafeRoot);
        foreach (var setting in SafeRoot.GetComponentsInChildren<SettingPanel>(true))
        {
            setting.togFull.interactable = false;
            TouchSlider(setting.sliderGame, 380); TouchSlider(setting.sliderBk,276);
            var close = (RectTransform)setting.btnClose.transform; close.anchoredPosition = new Vector2(54,56); close.sizeDelta = new Vector2(512,84);
            var credits = (RectTransform)setting.btnCredits.transform; credits.sizeDelta = new Vector2(512,48);
            foreach (var label in setting.GetComponentsInChildren<Text>(true))
            {
                if (label.name == "Fullscreen") label.text = "横屏显示";
                if (label.name == "Display Hint") label.text = "随手机方向切换左右横屏";
                if (label.name == "Settings Hint") label.text = "点击保存并返回 · 音量设置会保留";
            }
        }
        // The workbench already supports tap selection and ScrollRect dragging.
        foreach (var gene in SafeRoot.GetComponentsInChildren<GeneEditPanel>(true))
        {
            var border = gene.transform.Find("Workbench Border") as RectTransform;
            if (border != null)
            { border.anchorMin = new Vector2(.015f, .025f); border.anchorMax = new Vector2(.985f, .975f); }
            foreach (var scrollbar in gene.GetComponentsInChildren<Scrollbar>(true))
            { var rect = (RectTransform)scrollbar.transform; rect.anchorMin = new Vector2(.93f, rect.anchorMin.y); rect.anchorMax = new Vector2(.975f, rect.anchorMax.y); }
            foreach (var filter in gene.filterButtons)
                ((RectTransform)filter.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50);
            foreach (var button in new[]{gene.leftButton,gene.rightButton,gene.removeButton,gene.confirmButton,gene.cancelButton})
            {
                FixedHeight((RectTransform)button.transform,.05f,60);
                var label=button.GetComponentInChildren<Text>();label.fontSize=22;WaterDrainUI.Readable(label,20);
            }
            foreach(var label in gene.GetComponentsInChildren<Text>(true))
            {
                if(label.name=="Workbench Title"){label.fontSize=28;WaterDrainUI.Readable(label,26);}
                if(label.name=="Draft Reminder"){FixedHeight(label.rectTransform,.05f,60);label.fontSize=16;WaterDrainUI.Readable(label,14);}
            }
        }
    }
    private static void FixedHeight(RectTransform rect,float anchorY,float height)
    {
        rect.anchorMin=new Vector2(rect.anchorMin.x,anchorY);rect.anchorMax=new Vector2(rect.anchorMax.x,anchorY);
        rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,0);rect.sizeDelta=new Vector2(rect.sizeDelta.x,height);
    }
    private static void TouchSlider(Slider slider,float y)
    {
        var rect=(RectTransform)slider.transform;rect.anchoredPosition=new Vector2(54,y);rect.sizeDelta=new Vector2(512,70);
        if(slider.handleRect!=null)slider.handleRect.sizeDelta=new Vector2(44,44);
    }
}
