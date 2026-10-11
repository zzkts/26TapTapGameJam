using UnityEngine;

/// <summary>
/// 玩家地面移动状态。
/// 持续处理水平移动，并根据输入切换到其他动作状态。
/// </summary>
public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim) { }

    public override void FixUpdate()
    {
        player.Move(player.XInput, player.moveSpeed);
    }

    public override void Update()
    {
        if (player.AttackPressed && player.TryAttack())
            player.Machine.Switch(player.AttackState);
        else if (!player.IsGrounded())
            player.Machine.Switch(player.FallState);
        else if (player.DownPlatformPressed && player.TryDownPlatform())
            player.Machine.Switch(player.FallState);
        else if (player.JumpPressed && player.TryJump())
            player.Machine.Switch(player.JumpState);
        else if (player.RollPressed && player.TryRoll())
            player.Machine.Switch(player.RollState);
        else if (Mathf.Abs(player.XInput) <= Mathf.Epsilon)
            player.Machine.Switch(player.IdleState);
    }
}
