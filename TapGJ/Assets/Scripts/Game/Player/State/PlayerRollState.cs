using UnityEngine;
public class PlayerRollState : PlayerState
{
    private float remaining;
    public PlayerRollState(string name, PlayerController player, Animator anim) : base(name, player, anim) { }
    public override void Enter() { base.Enter(); remaining = player.rollDuration; player.BeginDash(); }
    public override void Exit() => player.EndDash();
    public override void FixUpdate() { }
    public override void Update() { remaining -= Time.deltaTime; if (remaining <= 0f) SwitchToLocomotionState(); }
}
