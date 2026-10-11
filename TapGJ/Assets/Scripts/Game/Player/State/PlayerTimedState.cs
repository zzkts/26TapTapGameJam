using UnityEngine;

public class PlayerTimedState : PlayerState
{
    private readonly float duration;
    private float remaining;
    public PlayerTimedState(string name, PlayerController player, float duration) : base(name, player, null)
    { this.duration = duration; }
    public override void Enter() { base.Enter(); remaining = duration; }
    public override void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining > 0f || player.IsDead) return;
        SwitchToLocomotionState();
    }
}
