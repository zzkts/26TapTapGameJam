using UnityEngine;

public sealed class CompleteWorldExit : WorldInteractable
{
    public CompleteLevelFlow level;
    public override string Title => level != null && level.CoreDefeated ? "排水核心 · 已接通" : "排水核心 · 被封锁";
    public override string Detail => level != null && level.CoreDefeated ? "决定地底世界的未来" : "清除核心室的三名守卫";
    public override bool CanUse(PlayerController player) => base.CanUse(player) && level != null && !level.ExitUsed;
    public override bool Interact(PlayerController player) => CanUse(player) && level.ReachExit(player);
}
