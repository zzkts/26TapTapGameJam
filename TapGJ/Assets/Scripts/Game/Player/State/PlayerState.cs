using UnityEngine;

/// <summary>
/// 所有玩家状态的共同基类。
/// 保存玩家和动画引用，并提供常用的地面移动状态切换。
/// </summary>
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
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool(stateName, true);
    }

    public override void FixUpdate() { }
    public override void Update() { }

    public override void Exit()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetBool(stateName, false);
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
