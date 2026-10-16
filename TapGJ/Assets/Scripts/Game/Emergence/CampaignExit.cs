using UnityEngine;
public sealed class CampaignExit : WorldInteractable
{
    public override string Title=>"最终回流阀";
    public override string Detail=>CampaignFlow.Current!=null&&CampaignFlow.Current.level.CoreDefeated?
        "守卫已清除 · 按 E 选择结局":"先击败三名核心守卫 · 无需操作场景水网";
    public override bool Interact(PlayerController player)
    {
        if(!CanUse(player))return false;
        var flow=CampaignFlow.Current;
        if(flow==null)return false;
        if(!flow.level.CoreDefeated){CampaignUI.Current?.Toast("先击败三名核心守卫，再回到这里按 E 选择结局");return true;}
        StorySession.Current.ShowCoreChoice();return true;
    }
}
