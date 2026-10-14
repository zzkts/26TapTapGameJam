using UnityEngine;
public class PlayerTimedState : PlayerState
{
    private readonly float duration;
    private float remaining;
    public PlayerTimedState(string name, PlayerController player, float duration) : base(name, player, null) { this.duration = duration; }
    public override void Enter() { base.Enter(); remaining = duration; }
    public override void FixUpdate() { if (!player.IsHurt && !player.IsDead) base.FixUpdate(); }
    public override void Update()
    {
        if (player.IsDead) return;
        remaining -= Time.deltaTime;
        if (!player.IsHurt && player.ProcessMovementActions()) return;
        if (remaining <= 0f) SwitchToLocomotionState();
    }
}
