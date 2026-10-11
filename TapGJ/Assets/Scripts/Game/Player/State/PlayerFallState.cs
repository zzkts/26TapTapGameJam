using UnityEngine;

/// <summary>
/// 玩家处于空中下落时的状态。
/// 保留空中移动，并在落地后回到待机或移动状态。
/// </summary>
public class PlayerFallState : PlayerState
{
    public PlayerFallState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim) { }

    public override void FixUpdate()
    {
        player.Move(player.XInput, player.moveSpeed);
    }

    public override void Update()
    {
        if (player.AttackPressed && player.TryAttack())
            player.Machine.Switch(player.AttackState);
        else if (player.JumpPressed && player.TryJump())
            player.Machine.Switch(player.JumpState);
        else if (player.IsGrounded())
            SwitchToLocomotionState();
    }
}
