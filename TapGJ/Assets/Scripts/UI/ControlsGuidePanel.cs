using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Matching guide cards with platform-specific instructions.</summary>
public sealed class ControlsGuidePanel : MonoBehaviour
{
    public const string MenuHint = "操作步骤见「操作指南」";
    public static ControlsGuidePanel Current { get; private set; }
    public Button CloseButton { get; private set; }
    public bool IsOpen => gameObject.activeInHierarchy;
    private GameObject previousSelection;
    private int openedFrame;

    public static ControlsGuidePanel Create(Transform parent, Font font, bool touch = true)
    {
        var existing = parent.GetComponentInChildren<ControlsGuidePanel>(true);
        if (existing != null) return existing;
        var root = GameplayUIFactory.Rect(touch?"Mobile Guide":"Desktop Guide", parent, Vector2.zero, Vector2.one);
        var guide = root.gameObject.AddComponent<ControlsGuidePanel>();
        guide.Build(font,touch); root.gameObject.SetActive(false); return guide;
    }
    private void Build(Font font,bool touch)
    {
        Current = this;
        var shade = GameplayUIFactory.Image("Guide Shade", transform, Vector2.zero, Vector2.one,
            new Color(.04f,.025f,.09f,.88f),true);
        var dismiss = shade.gameObject.AddComponent<Button>(); dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(Hide);
        var frame = WaterDrainUI.Dock("Guide Card", transform, Vector2.one*.5f,Vector2.zero,new Vector2(980,600));
        var face = frame.gameObject.AddComponent<Image>(); face.sprite=PlayerUIStyle.Rounded;
        face.type=Image.Type.Sliced; face.color=WaterDrainUI.Surface;
        var border=frame.gameObject.AddComponent<Outline>();border.effectColor=WaterDrainUI.Border;border.effectDistance=new Vector2(1,-1);
        TextAt(frame,"Guide Title",font,new Vector2(36,-24),new Vector2(760,56),34,"操作指南",PlayerUIStyle.Cream);
        TextAt(frame,"Guide Subtitle",font,new Vector2(36,-88),new Vector2(860,34),18,touch?"移动探索，点击射击，在终端重组基因。":"键鼠操作 · 收集基因，在终端重组能力。",WaterDrainUI.Muted);
        string[] titles={"移动探索",touch?"点击射击":"鼠标射击","跳跃与下落","冲刺","基因附魔","交互与装配"};
        string[] bodies=touch?new[]{"拖动左侧摇杆移动；\n松开摇杆后停下。","点击场景瞄准射击；\n按住连射，拖动换目标。",
            "点击跳跃越过障碍；\n点击下落穿过单向平台。","取得模块后点击冲刺；\n留意按钮上的冷却时间。",
            "装入属性基因后点击附魔；\n按基因链顺序启动属性。","靠近目标后点击交互；\n终端选槽装入基因，再确认。"}:
            new[]{"A / D 左右移动；\n移动时也可以瞄准和射击。","鼠标指向目标，左键射击；\n按住左键持续发射水弹。",
            "空格 / W 跳跃；\nS + 跳跃穿过单向平台。","取得模块后按 Shift 冲刺；\n向面朝方向快速移动。",
            "装入属性基因后点击右键；\n按基因链顺序启动属性。","靠近目标按 E 交互；\n终端点选基因，空格确认。"};
        for(int i=0;i<titles.Length;i++)
        {
            var row=Top("Guide Step "+(i+1),frame,new Vector2(36+(i%2)*466,-140-(i/2)*126),new Vector2(442,112));
            var well=row.gameObject.AddComponent<Image>();well.sprite=PlayerUIStyle.Rounded;well.type=Image.Type.Sliced;
            well.color=new Color(.22f,.175f,.33f);
            var badge=Top("Step Number",row,new Vector2(18,-18),new Vector2(36,36));
            var disc=badge.gameObject.AddComponent<Image>();disc.sprite=PlayerUIStyle.Circle;disc.color=PlayerUIStyle.Violet;disc.raycastTarget=false;
            var number=GameplayUIFactory.Label("Number",badge,Vector2.zero,Vector2.one,font,16,(i+1).ToString("00"),TextAnchor.MiddleCenter);number.color=PlayerUIStyle.Mint;
            TextAt(row,"Step Title",font,new Vector2(72,-12),new Vector2(350,34),21,titles[i],PlayerUIStyle.Cream);
            TextAt(row,"Step Detail",font,new Vector2(72,-48),new Vector2(350,58),17,bodies[i],WaterDrainUI.Muted);
        }
        CloseButton=GameplayUIFactory.Button("Guide Close",frame,Vector2.zero,Vector2.zero,font,"知道了");
        var rect=(RectTransform)CloseButton.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,0);
        rect.anchoredPosition=new Vector2(0,24);rect.sizeDelta=new Vector2(248,58);
        CloseButton.image.sprite=PlayerUIStyle.Rounded;CloseButton.image.type=Image.Type.Sliced;CloseButton.image.color=PlayerUIStyle.Mint;
        var label=CloseButton.GetComponentInChildren<Text>();label.color=PlayerUIStyle.Ink;label.fontSize=22;WaterDrainUI.Readable(label,20);
        CloseButton.onClick.AddListener(Hide);
    }
    private static RectTransform Top(string name,Transform parent,Vector2 at,Vector2 size)
        =>WaterDrainUI.Dock(name,parent,new Vector2(0,1),at,size);
    private static Text TextAt(Transform parent,string name,Font font,Vector2 at,Vector2 size,int fontSize,string value,Color color)
    {
        var rect=Top(name,parent,at,size);var label=rect.gameObject.AddComponent<Text>();
        label.font=font;label.fontSize=fontSize;label.text=value;label.alignment=TextAnchor.MiddleLeft;
        label.color=color;label.lineSpacing=1.12f;WaterDrainUI.Readable(label,fontSize-1);return label;
    }
    public void Show()
    {
        previousSelection=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        openedFrame=Time.frameCount;transform.SetAsLastSibling();gameObject.SetActive(true);
        if(!MobileGameInput.Enabled&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(CloseButton.gameObject);
    }
    public void Hide()
    {
        gameObject.SetActive(false);GameplayUIFactory.ModalInputFrame=Time.frameCount;
        if(EventSystem.current!=null&&previousSelection!=null&&previousSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(previousSelection);
    }
    private void Update()
    {
        if(Time.frameCount>openedFrame&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Hide();
    }
    private void OnDestroy(){if(Current==this)Current=null;}
}
