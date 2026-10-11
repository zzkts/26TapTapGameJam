using System.Collections.Generic;

/// <summary>
/// 可受伤对象的基础接口。
/// 子弹和近战统一通过 TakeDamage 造成伤害。
/// </summary>
public interface IActor
{
    void TakeDamage(float damage);
}

/// <summary>
/// 带有生命值的受伤对象接口。
/// 对外提供当前生命、最大生命和死亡状态。
/// </summary>
public interface IHealthActor : IActor
{
    float Hp { get; }
    float MaxHp { get; }
    bool IsDead { get; }
}

/// <summary>
/// 同时具备生命和元素状态能力的战斗单位接口。
/// 在旧受伤接口基础上补充统一伤害信息和临时状态入口。
/// </summary>
public interface ICombatable : IHealthActor
{
    IReadOnlyList<BuffInstance> BuffList { get; }
    void TakeDamage(DamageInfo damageInfo);
    void AddBuff(E_ElementType elementType);
    void RemBuff(E_ElementType elementType);
}
