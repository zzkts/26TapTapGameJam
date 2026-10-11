using UnityEngine;

public class PlayerFallState : PlayerState
{
    public PlayerFallState(string name, PlayerController player, Animator anim) : base(name, player, anim) { }
    public override void FixUpdate() => player.Move(player.XInput, player.moveSpeed);
    public override void Update() => player.UpdateLocomotion();
}
