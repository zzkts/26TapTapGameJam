using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-380)]
public sealed class CampaignUI:MonoBehaviour
{
    public static CampaignUI Current { get; private set; }
    public Button advanceButton,continueButton;
    public CampaignReactor Pinned { get; private set; }
    public bool MapOpen=>modal!=null&&modal.activeSelf&&map.activeSelf;
    private CampaignFlow flow;
    private PlayerSynthesis ability;
    private Text skills,navigation,toastText,title,summaryText,ruleText;
    private GameObject modal,map,rules,summary;
    private Button[] nodeButtons;
    private Text[] nodeLabels;
    private RectTransform playerDot;
    private Font font;
    private float toastUntil;
    private CanvasGroup hud;
    public static void Ensure(CampaignFlow flow)
    {
        if(Current!=null)return;
        Canvas canvas=StoryUIFactory.FindGameplayCanvas();
        var obj=GameplayUIFactory.Rect("章节与播种界面",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        var ui=obj.AddComponent<CampaignUI>();Current=ui;ui.flow=flow;ui.ability=flow.player.GetComponent<PlayerSynthesis>();
        ui.font=Object.FindFirstObjectByType<PlayerHudPanel>().healthText.font;ui.Build();
    }
    private void Build()
    {
        var h=GameplayUIFactory.Rect("Campaign HUD",transform,Vector2.zero,Vector2.one);
        hud=h.gameObject.AddComponent<CanvasGroup>();hud.blocksRaycasts=false;
        Surface("Abilities",h,.34f,.014f,.77f,.14f,new Color(.10f,.08f,.19f,.9f));
        skills=Label("Skills",h,.355f,.027f,.75f,.125f,16,"",PlayerUIStyle.Cream);
        navigation=Label("Navigation",h,.34f,.865f,.71f,.955f,17,"",PlayerUIStyle.Mint,TextAnchor.MiddleCenter);
        toastText=Label("Discovery Toast",h,.33f,.70f,.72f,.77f,19,"",PlayerUIStyle.Gold,TextAnchor.MiddleCenter);
        modal=GameplayUIFactory.Rect("Chapter Modal",transform,Vector2.zero,Vector2.one).gameObject;
        GameplayUIFactory.Image("Shade",modal.transform,Vector2.zero,Vector2.one,new Color(.04f,.025f,.09f,.95f),true);
        Surface("Panel",modal.transform,.065f,.08f,.935f,.92f,PlayerUIStyle.Ink);
        title=Label("Title",modal.transform,.10f,.80f,.90f,.90f,34,"章节地图",PlayerUIStyle.Cream);
        map=GameplayUIFactory.Rect("Map",modal.transform,new Vector2(.1f,.18f),new Vector2(.9f,.8f)).gameObject;
        Label("Map Goal",map.transform,0,.88f,1,1,20,flow.chapter==3?"最终胜利：接通两座节点 → 击败守卫 → 中央阀门选结局":"任选两座节点接通 → 回中央出口 → 下一章",PlayerUIStyle.Mint);
        nodeButtons=new Button[3];nodeLabels=new Text[3];
        for(int i=0;i<3;i++)
        {
            int index=i;float y=.58f-i*.25f;
            nodeButtons[i]=Button("Node "+i,map.transform,.01f,y,.47f,y+.21f,"",PlayerUIStyle.Violet);
            nodeLabels[i]=nodeButtons[i].GetComponentInChildren<Text>();nodeLabels[i].fontSize=18;nodeLabels[i].alignment=TextAnchor.MiddleLeft;
            nodeButtons[i].onClick.AddListener(()=>{Pinned=flow.reactors[index];Close();});
        }
        Surface("Map Field",map.transform,.51f,.07f,.99f,.82f,new Color(.22f,.18f,.34f));
        Label("Map Legend",map.transform,.52f,.74f,.98f,.80f,15,"中央集合 · 左右支路 · 高处路线",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        Surface("Floor",map.transform,.54f,.15f,.96f,.17f,PlayerUIStyle.Mint);
        foreach(var node in flow.reactors)
        {
            Vector2 point=MapPoint(node.transform.position);
            var b=Button("Map Pin "+node.name,map.transform,point.x-.025f,point.y-.035f,point.x+.025f,point.y+.035f,(System.Array.IndexOf(flow.reactors,node)+1).ToString(),PlayerUIStyle.Violet);
            b.onClick.AddListener(()=>{Pinned=node;Close();});
        }
        Vector2 hubPoint=MapPoint(flow.exit.transform.position);
        Label("Exit Pin",map.transform,hubPoint.x-.10f,hubPoint.y+.045f,hubPoint.x+.10f,hubPoint.y+.10f,13,"中央出口",PlayerUIStyle.Gold,TextAnchor.MiddleCenter);
        Image dot=GameplayUIFactory.Image("You",map.transform,Vector2.zero,Vector2.zero,PlayerUIStyle.Cream);dot.sprite=PlayerUIStyle.Circle;dot.preserveAspect=true;
        playerDot=dot.rectTransform;playerDot.sizeDelta=new Vector2(13,13);
        Label("Map Help",map.transform,.51f,.005f,.99f,.055f,14,"白点是你 · 点击节点设置导航",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        rules=GameplayUIFactory.Rect("Rules",modal.transform,new Vector2(.10f,.22f),new Vector2(.90f,.79f)).gameObject;
        ruleText=Label("Rule Text",rules.transform,0,0,1,1,21,"",PlayerUIStyle.Cream,TextAnchor.UpperLeft);
        summary=GameplayUIFactory.Rect("Chapter Clear",modal.transform,new Vector2(.10f,.25f),new Vector2(.90f,.79f)).gameObject;
        summaryText=Label("Clear Text",summary.transform,.05f,.22f,.95f,.90f,27,"",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        advanceButton=Button("Next Chapter",summary.transform,.18f,.025f,.82f,.16f,"进入下一章",PlayerUIStyle.Mint);
        advanceButton.onClick.AddListener(()=>flow.Advance());
        continueButton=Button("Continue",modal.transform,.38f,.105f,.62f,.165f,"继续探索 · ESC",PlayerUIStyle.Cream);continueButton.onClick.AddListener(Close);
        modal.SetActive(false);
    }
    private Vector2 MapPoint(Vector2 p)=>new Vector2(Mathf.Lerp(.55f,.95f,Mathf.Clamp01(p.x/40f)),Mathf.Lerp(.20f,.68f,Mathf.Clamp01(p.y/10f)));
    private void Update()
    {
        if(flow==null||flow.player==null)return;
        var k=Keyboard.current;
        if(modal.activeSelf&&k!=null&&(k.escapeKey.wasPressedThisFrame||k.mKey.wasPressedThisFrame||k.tabKey.wasPressedThisFrame)){Close();return;}
        if(k!=null&&!flow.player.MenuOpen&&!flow.player.IsDead)
        {
            if(k.mKey.wasPressedThisFrame)OpenMap();
            else if(k.tabKey.wasPressedThisFrame)OpenRules();
        }
        bool hidden=flow.player.MenuOpen;hud.alpha=hidden?0:1;
        skills.text="回流能量 "+ability.Energy.ToString("0.0")+" / 3   ·   当前 "+ability.SelectedName+"   ·   种子 "+ability.Seeds.Count(s=>s!=null)+" / 2\nQ 切换 · R 播种 · T 回收 · F 推 / S+F 吸 · Shift 八向冲刺\n空中空格：泡泡踏跃   ·   "+ability.Notice;
        var target=Pinned!=null&&!Pinned.Complete?Pinned:flow.ChapterSolved?null:flow.reactors.FirstOrDefault(n=>!n.Complete);
        Vector2 at=target!=null?(Vector2)target.transform.position:flow.exit.transform.position;
        Vector2 delta=at-(Vector2)flow.player.transform.position;
        string direction=Mathf.Abs(delta.x)<1.5f?(delta.y>1.5f?"↑ 上方":"● 就在附近"):delta.x>0?(delta.y>2?"↗ 右上方":"→ 右侧"):(delta.y>2?"↖ 左上方":"← 左侧");
        navigation.text=(target!=null?target.displayName:"中央出口 / 回流阀")+" · "+direction+" "+delta.magnitude.ToString("0")+" m\n"+(target!=null?target.hint+"\n"+target.StateText:flow.Goal);
        toastText.text=Time.unscaledTime<toastUntil?toastText.text:"";
        if(MapOpen)
        {
            for(int i=0;i<3;i++)nodeLabels[i].text=flow.reactors[i].displayName+" · "+flow.reactors[i].StateText+"\n"+flow.reactors[i].hint;
            playerDot.anchorMin=playerDot.anchorMax=MapPoint(flow.player.transform.position);
        }
    }
    private void Open(GameObject content,string heading)
    {
        if(flow.player.IsDead||flow.player.MenuOpen)return;
        map.SetActive(content==map);rules.SetActive(content==rules);summary.SetActive(content==summary);
        title.text=heading;modal.SetActive(true);modal.transform.SetAsLastSibling();flow.player.SetMenuOpen(true);
    }
    public void OpenMap()=>Open(map,flow.ChapterName+" · 地图与胜利条件");
    public void OpenRules()
    {
        var progress=EmergenceProgress.Current;
        ruleText.text="你可以播种，再改变种子的位置，让同样的规则出现在不同地方。\n\n"+string.Join("\n\n",new[]{"conduction","steam","lift","growth","explosion","fracture","inertia"}.Select(kind=>(progress!=null&&progress.Discoveries.ContainsKey(kind)?"● 已发现 · ":"○ 可尝试 · ")+EmergenceProgress.Description(kind)));
        Open(rules,"涌现 · 反应手册");
    }
    public void ShowSummary()
    {
        summaryText.text=flow.ChapterName+" 完成\n\n你用 "+flow.Powered+" 座节点重组了这里的回流。\n基因与已发现的反应会带入下一章。";
        advanceButton.GetComponentInChildren<Text>().text=flow.chapter==1?"进入第二章 · 共生菌林":"进入第三章 · 净化核心";
        Open(summary,"本章目标达成");
    }
    public void Close(){modal.SetActive(false);flow.player.SetMenuOpen(false);GameplayUIFactory.ModalInputFrame=Time.frameCount;}
    public void Toast(string text){toastText.text=text;toastUntil=Time.unscaledTime+4;}
    private void OnDestroy(){if(Current==this)Current=null;}
    private Image Surface(string name,Transform parent,float x0,float y0,float x1,float y1,Color color)
    {var img=GameplayUIFactory.Image(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),color);img.sprite=PlayerUIStyle.Rounded;img.type=Image.Type.Sliced;return img;}
    private Text Label(string name,Transform parent,float x0,float y0,float x1,float y1,int size,string text,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {var t=GameplayUIFactory.Label(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,size,text,alignment);t.color=color;return t;}
    private Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string text,Color color)
    {var b=GameplayUIFactory.Button(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,text);var img=b.GetComponent<Image>();img.sprite=PlayerUIStyle.Rounded;img.type=Image.Type.Sliced;img.color=color;b.GetComponentInChildren<Text>().color=color==PlayerUIStyle.Mint||color==PlayerUIStyle.Cream?PlayerUIStyle.Ink:PlayerUIStyle.Cream;return b;}
}
