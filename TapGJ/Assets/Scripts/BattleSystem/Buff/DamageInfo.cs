using UnityEngine;

/// <summary>
/// 一次命中的伤害信息。
/// 同时携带伤害、投射物元素、命中位置和反应作用层。
/// </summary>
public readonly struct DamageInfo
{
    public readonly float damage;
    public readonly E_ElementType elementType;
    public readonly Vector2 hitPoint;
    public readonly LayerMask targetLM;

    public DamageInfo(float damage, E_ElementType elementType = E_ElementType.None,
        Vector2 hitPoint = default, LayerMask targetLM = default)
    {
        this.damage = damage;
        this.elementType = elementType;
        this.hitPoint = hitPoint;
        this.targetLM = targetLM;
    }
}
