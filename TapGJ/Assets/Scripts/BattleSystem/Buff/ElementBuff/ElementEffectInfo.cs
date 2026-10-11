using UnityEngine;

/// <summary>
/// 一次元素效果的运行时信息。
/// 记录效果类型、发生位置和参与反应的两个元素。
/// </summary>
public readonly struct ElementEffectInfo
{
    public readonly E_ElementEffect effect;
    public readonly Vector2 position;
    public readonly E_ElementType current;
    public readonly E_ElementType incoming;
    public readonly float damage;
    public readonly LayerMask targetLM;

    public ElementEffectInfo(E_ElementEffect effect, Vector2 position,
        E_ElementType current, E_ElementType incoming, float damage = 0f,
        LayerMask targetLM = default)
    {
        this.effect = effect;
        this.position = position;
        this.current = current;
        this.incoming = incoming;
        this.damage = damage;
        this.targetLM = targetLM;
    }
}
