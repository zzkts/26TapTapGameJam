using UnityEngine;

[CreateAssetMenu(menuName = "Actor/Player Data", fileName = "PlayerData")]
public class PlayerData : ScriptableObject
{
    public float moveSpeed = 3f;
    public float direction = 1f;
    public float jumpVelocity = 4f;

    public float maxHp = 10f;

    public float rollCooldown = 2f;
    public float rollSpeedMultiplier = 1.5f;
    public float rollDuration = 0.2f;
    public bool dashUnlocked;
    [Min(0f)] public float coyoteTime = 0.1f;
    [Min(0f)] public float jumpBuffer = 0.1f;
    [Min(0f)] public float invulnerabilityTime = 0.8f;
    [Min(0f)] public float hitStopTime = 0.1f;
    [Min(0f)] public float knockbackSpeed = 2.5f;
    [Min(0f)] public float hurtTime = 0.25f;
    [Min(0f)] public float landingTime = 0.12f;
    [Min(0f)] public float poisonResistance;
    public float fallDeathY = -12f;
}
