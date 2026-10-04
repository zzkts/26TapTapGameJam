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
    public float rollDuration = 0.5f;
}
