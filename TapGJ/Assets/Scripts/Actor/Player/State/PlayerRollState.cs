using UnityEngine;

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
        timer = player.rollDuration;
        rollDirection = player.FaceDir;
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
