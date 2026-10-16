using UnityEngine;

[CreateAssetMenu(menuName = "Actor/Player Data", fileName = "PlayerData")]
public class PlayerData : ScriptableObject
{
    public float moveSpeed = 3f;
    public float direction = 1f;
    public float jumpVelocity = 4f;
    public float maxHp = 10f;
    public bool dashUnlocked;
    public float rollCooldown = 2f;
    public float rollSpeedMultiplier = 1.5f;
    public float rollDuration = 0.2f;
    [Min(0f)] public float coyoteTime = 0.22f;
    [Min(0f)] public float jumpBuffer = 0.22f;
    [Range(.5f, 1f)] public float heldJumpGravity = .85f;
    [Range(1f, 2f)] public float fallingGravity = 1.2f;
    [Min(0f)] public float invulnerabilityTime = 0.8f;
    [Min(0f)] public float hurtTime = 0.22f;
    [Min(0f)] public float hitStopTime = 0.1f;
    [Min(0f)] public float knockbackSpeed = 3f;
    [Min(0f)] public float landingTime = 0.08f;
    [Range(0f, 2f)] public float poisonResistance;
    public float fallDeathY = -12f;
}
