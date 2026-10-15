/// <summary>
/// 基因可修改的武器数据类型。
/// ModifierValue 会根据这个类型解释为数值、元素编号或轨迹编号。
/// </summary>
public enum E_ModifierType
{
    Atk = 0,
    DamageMultiplier = 1,
    ProjectileSpeed = 2,
    ProjectileCount = 3,
    ProjectileSize = 4,
    ProjectileLifetime = 5,
    Pierce = 6,
    Rebound = 7,
    ReboundCoefficient = 8,
    ProjectileSpeedMultiplier = 9,
    None = 11,
    Element = 12,
    ProjectileMove = 13,
    Split = 14,
    BodyTrait = 15,
}
