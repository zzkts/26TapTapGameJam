using UnityEngine;

public class PlayerAttackState : PlayerState
{
    private float timer;

    public PlayerAttackState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim)
    {
    }

    public override void Enter()
    {
        base.Enter();
        timer = player.attackDuration;
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f) Trigger();
    }

    public void Trigger()
    {
        if (!player.IsGrounded())
        {
            if (player.YVelocity > 0)
                player.Machine.Switch(player.JumpState);
            else
                player.Machine.Switch(player.FallState);
        }
        else if (Mathf.Abs(player.XInput) > Mathf.Epsilon)
            player.Machine.Switch(player.WalkState);
        else
            player.Machine.Switch(player.IdleState);
    }
}
