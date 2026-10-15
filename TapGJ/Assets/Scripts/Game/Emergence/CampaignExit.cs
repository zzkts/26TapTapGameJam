using UnityEngine;
public sealed class CampaignExit:WorldInteractable
{
    public override string Title=>"最终回流阀";
    public override string Detail=>CampaignFlow.Current!=null&&CampaignFlow.Current.level.CoreDefeated?
        "选择水网的未来 · 按 E 选择结局":
        CampaignFlow.Current!=null&&CampaignFlow.Current.WatershedSolved?
        "击败三名核心守卫后返回这里":
        "水网尚未接通 · 按 E 查看地图与目标";
    public override bool Interact(PlayerController player)
    {
        if(!CanUse(player))return false;
        var flow=CampaignFlow.Current;
        if(!flow.WatershedSolved){CampaignUI.Current?.OpenMap();return true;}
        if(!flow.level.CoreDefeated){CampaignUI.Current?.Toast("先击败三名核心守卫，再到这里选择结局");return true;}
        StorySession.Current.ShowCoreChoice();return true;
    }
}
