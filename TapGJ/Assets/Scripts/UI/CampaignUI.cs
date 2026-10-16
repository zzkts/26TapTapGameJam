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
    private float toastUntil,nextRefresh;
    public static void Ensure(CampaignFlow flow)
    {
        if(Current!=null)return;
        Canvas canvas=StoryUIFactory.FindGameplayCanvas();
        var obj=GameplayUIFactory.Rect("章节导航",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        var ui=obj.AddComponent<CampaignUI>();Current=ui;ui.flow=flow;
        ui.hud=obj.AddComponent<CanvasGroup>();ui.hud.blocksRaycasts=false;
        Font font=Object.FindFirstObjectByType<PlayerHudPanel>().healthText.font;
        ui.navigation=GameplayUIFactory.Label("Navigation",obj.transform,new Vector2(.36f,.88f),new Vector2(.68f,.955f),font,16,"",TextAnchor.MiddleCenter);
        ui.navigation.color=PlayerUIStyle.Mint;
        ui.toastText=GameplayUIFactory.Label("Gene Route Toast",obj.transform,new Vector2(.28f,.71f),new Vector2(.74f,.79f),font,18,"",TextAnchor.MiddleCenter);
        ui.toastText.color=PlayerUIStyle.Gold;
    }
    private void Update()
    {
        if(flow==null||flow.player==null)return;
        hud.alpha=flow.player.MenuOpen?0:1;
        if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.1f;
        Vector2 delta=flow.NavigationPosition-(Vector2)flow.player.transform.position;
        string direction=Mathf.Abs(delta.x)<1.5f?"就在附近":delta.x>0?"→ 右侧":"← 左侧";
        navigation.text=(flow.chapter<3?"下一区域":flow.NavigatingGuardians?"核心守卫":"终点回流阀")+" · "+direction+" "+delta.magnitude.ToString("0")+" m";
        if(Time.unscaledTime>=toastUntil)toastText.text="";
    }
    public void Toast(string text){toastText.text=text;toastUntil=Time.unscaledTime+4;}
    private void OnDestroy(){if(Current==this)Current=null;}
}
