using UnityEngine;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(string stateName, PlayerController player, Animator anim)
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
        else if (player.YVelocity <= 0f)
            player.Machine.Switch(player.FallState);
    }
}
