using UnityEngine;

/// <summary>
/// 玩家向上跳跃阶段的状态。
/// 保留空中移动，达到最高点后切换到下落状态。
/// </summary>
public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim) { }

    public override void FixUpdate()
    {
        player.Move(player.XInput, player.moveSpeed);
    }

    public override void Update()
    {
        if (player.AttackPressed && player.TryAttack())
            player.Machine.Switch(player.AttackState);
        else if (player.YVelocity <= 0f)
            player.Machine.Switch(player.FallState);
    }
}
