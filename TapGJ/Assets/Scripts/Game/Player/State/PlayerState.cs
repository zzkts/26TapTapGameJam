using UnityEngine;

public abstract class PlayerState : State
{
    protected Animator anim;
    protected PlayerController player;

    public PlayerState(string stateName, PlayerController player, Animator anim) : base(stateName)
    {
        this.anim = anim;
        this.player = player;
    }

    public override void Enter()
    {
        player.PlayAnimation(stateName);
    }

    public override void FixUpdate() { }
    public override void Update() { }

    public override void Exit()
    {
    }

    protected void SwitchToLocomotionState()
    {
        if (!player.IsGrounded())
            player.Machine.Switch(player.FallState);
        else if (Mathf.Abs(player.XInput) > Mathf.Epsilon)
            player.Machine.Switch(player.WalkState);
        else
            player.Machine.Switch(player.IdleState);
    }
}
