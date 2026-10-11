using UnityEngine;

/// <summary>
/// 玩家在地面翻滚时的状态。
/// 按进入时的方向移动，并在翻滚期间开启无敌。
/// </summary>
public class PlayerRollState : PlayerState
{
    private float timer;
    private float rollDirection;

    public PlayerRollState(string stateName, PlayerController player, Animator anim)
        : base(stateName, player, anim)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.SetInvincible(true);
        timer = player.rollDuration;
        rollDirection = player.FaceDir;
    }

    public override void Exit()
    {
        player.SetInvincible(false);
        base.Exit();
    }

    public override void FixUpdate()
    {
        player.Move(rollDirection, player.moveSpeed * player.rollSpeedMultiplier);
    }

    public override void Update()
    {
        timer -= Time.deltaTime;
        if(timer < 0)
            player.Machine.Switch(player.IdleState);
    }
}
