using UnityEngine;

public abstract class PlayerState : State
{
    protected Animator anim;
    protected PlayerController player;
    public PlayerState(string name, PlayerController player, Animator anim) : base(name) { this.player = player; this.anim = anim; }
    public override void Enter() => player.PlayAnimation(stateName);
    public override void Exit() { }
    public override void FixUpdate() => player.Move(player.XInput, player.moveSpeed);
    public override void Update()
    {
        if (!player.ProcessMovementActions()) SwitchToLocomotionState();
    }
    protected void SwitchToLocomotionState()
    {
        if (!player.IsGrounded()) player.Machine.Switch(player.YVelocity > 0f ? player.JumpState : player.FallState);
        else player.Machine.Switch(Mathf.Abs(player.XInput) > 0.01f ? player.WalkState : player.IdleState);
    }
}
