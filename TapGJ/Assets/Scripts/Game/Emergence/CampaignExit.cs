using UnityEngine;
public sealed class CampaignExit:WorldInteractable
{
    public override string Title=>CampaignFlow.Current!=null&&CampaignFlow.Current.level.CoreDefeated?"最终回流阀 · E 选择结局":"最终回流阀 · E 查看胜利目标";
    public override string Detail=>"泵站输出清水 → 菌林与核心各接通两节点 → 击败守卫 → E 选择结局";
    public override bool Interact(PlayerController player)
    {
        if(!CanUse(player))return false;
        var flow=CampaignFlow.Current;
        if(!flow.WatershedSolved){CampaignUI.Current?.OpenMap();return true;}
        if(!flow.level.CoreDefeated){CampaignUI.Current?.Toast("先击败三名核心守卫，再到这里选择结局");return true;}
        StorySession.Current.ShowCoreChoice();return true;
    }
}
