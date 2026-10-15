using UnityEngine;
public sealed class CampaignExit:WorldInteractable
{
    public override string Title=>CampaignFlow.Current==null?"中央出口":CampaignFlow.Current.chapter==3?
        CampaignFlow.Current.level.CoreDefeated?"最终阀门 · E 选择结局":CampaignFlow.Current.level.BattleStarted?"最终阀门 · 先击败守卫":"最终阀门 · 先接通两座核心":
        CampaignFlow.Current.ChapterSolved?"中央出口 · 本章目标达成":"中央出口 · 目标尚未完成";
    public override string Detail=>CampaignFlow.Current==null?"":CampaignFlow.Current.chapter==3?"接通节点 → 击败守卫 → E 选择结局":"任意两座节点接通后，E 进入下一章 · M 查看地图";
    public override bool Interact(PlayerController player)
    {
        if(!CanUse(player))return false;
        var flow=CampaignFlow.Current;
        if(!flow.ChapterSolved){CampaignUI.Current?.OpenMap();return true;}
        if(flow.chapter==3)
        {
            if(!flow.level.CoreDefeated){CampaignUI.Current?.Toast("先击败剩余守卫，再回到中央阀门");return true;}
            StorySession.Current.ShowCoreChoice();return true;
        }
        CampaignUI.Current?.ShowSummary();return true;
    }
}
