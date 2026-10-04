using UnityEngine;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim)
    {
    }

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
        else if(player.JumpPressed && player.TryJump())
            player.Machine.Switch(player.JumpState);
        else if(player.RollPressed && player.TryRoll())
            player.Machine.Switch(player.RollState);
        else if(Mathf.Abs(player.XInput) <= Mathf.Epsilon)
            player.Machine.Switch(player.IdleState);
    }
}
