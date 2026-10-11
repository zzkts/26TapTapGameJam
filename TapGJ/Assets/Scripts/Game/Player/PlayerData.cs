using UnityEngine;

/// <summary>
/// 玩家角色的基础配置数据。
/// 统一保存移动、跳跃、生命和翻滚相关数值，方便在编辑器中调整。
/// </summary>
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
