using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact route feedback; gene and inventory panels own all build controls.</summary>
[DefaultExecutionOrder(-380)]
public sealed class CampaignUI : MonoBehaviour
{
    public static CampaignUI Current { get; private set; }
    private CampaignFlow flow;
    private Text navigation,toastText;
    private CanvasGroup hud;
    private CanvasGroup toastGroup;
    private float toastUntil,nextRefresh;
    public static void Ensure(CampaignFlow flow)
    {
        if(Current!=null)return;
        Canvas canvas=StoryUIFactory.FindGameplayCanvas();
        var obj=GameplayUIFactory.Rect("章节导航",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        var ui=obj.AddComponent<CampaignUI>();Current=ui;ui.flow=flow;
        ui.hud=obj.AddComponent<CanvasGroup>();ui.hud.blocksRaycasts=false;
        Font font=Object.FindFirstObjectByType<PlayerHudPanel>().healthText.font;
        var route=WaterDrainUI.Dock("Route Card",obj.transform,new Vector2(1,1),new Vector2(-24,-182),new Vector2(402,42));
        var back=route.gameObject.AddComponent<Image>();back.sprite=PlayerUIStyle.Rounded;back.type=Image.Type.Sliced;back.color=WaterDrainUI.Surface;back.raycastTarget=false;
        ui.navigation=GameplayUIFactory.Label("Navigation",route,new Vector2(.04f,.08f),new Vector2(.96f,.92f),font,16,"",TextAnchor.MiddleCenter);
        ui.navigation.color=PlayerUIStyle.Mint;
        var toast=WaterDrainUI.Dock("Route Toast",obj.transform,new Vector2(.5f,1),new Vector2(0,-108),new Vector2(510,56));
        var toastBack=toast.gameObject.AddComponent<Image>();toastBack.sprite=PlayerUIStyle.Rounded;toastBack.type=Image.Type.Sliced;toastBack.color=WaterDrainUI.Surface;toastBack.raycastTarget=false;
        ui.toastGroup=toast.gameObject.AddComponent<CanvasGroup>();ui.toastGroup.alpha=0;ui.toastGroup.blocksRaycasts=false;
        ui.toastText=GameplayUIFactory.Label("Gene Route Toast",toast,new Vector2(.03f,.04f),new Vector2(.97f,.96f),font,18,"",TextAnchor.MiddleCenter);
        ui.toastText.color=PlayerUIStyle.Gold;
        WaterDrainUI.Readable(ui.navigation,15);WaterDrainUI.Readable(ui.toastText,16);
    }
    private void Update()
    {
        if(flow==null||flow.player==null)return;
        hud.alpha=flow.player.MenuOpen?0:1;
        toastGroup.alpha=Time.unscaledTime<toastUntil?1:0;
        if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
        Vector2 delta=flow.NavigationPosition-(Vector2)flow.player.transform.position;
        string direction=Mathf.Abs(delta.x)<1.5f?"就在附近":delta.x>0?"→ 右侧":"← 左侧";
        navigation.text=(flow.chapter<3?"下一区域":flow.NavigatingGuardians?"核心守卫":"终点回流阀")+" · "+direction+" "+delta.magnitude.ToString("0")+" m";
        if(Time.unscaledTime>=toastUntil)toastText.text="";
    }
    public void Toast(string text){toastText.text=text;toastUntil=Time.unscaledTime+4;}
    private void OnDestroy(){if(Current==this)Current=null;}
}
