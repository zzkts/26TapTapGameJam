using System;
using UnityEngine;

/// <summary>
/// 元素附着和元素反应的统一结算入口。
/// 负责匹配蒸汽、毒雾、爆炸等结果，并生成对应的场景效果。
/// </summary>
public static class ElementSystem
{
    private const string ElementDataPath = "SO/Element/ElementData";

    public static event Action<ElementEffectInfo> EffectTriggered;
    private static readonly ElementData data = Resources.Load<ElementData>(ElementDataPath);

    public static E_ElementType GetElementType(float elementId)
    {
        return (E_ElementType)Mathf.RoundToInt(elementId);
    }

    public static string GetDescription(E_ElementType elementType) => data.Get(elementType).desc;

    public static float GetDuration(E_ElementType elementType) => data.Get(elementType).duration;

    public static GameObject GetProjectilePrefab(E_ElementType elementType) =>
        data.Get(elementType).projectilePrefab;

    public static E_ElementEffect Apply(BuffComponent target, E_ElementType elementType,
        Vector2 position, float damage = 0f, LayerMask targetLM = default)
    {
        if (target == null || elementType == E_ElementType.None) return E_ElementEffect.None;
        ElementField.ReactFields(position, 0.1f, elementType);
        if (elementType == E_ElementType.Electric)
            ActorElementEffects.Conduct(position, 0.9f);
        BuffInstance sameElement = null;

        for (int i = 0; i < target.BuffList.Count; i++)
        {
            BuffInstance instance = target.BuffList[i];
            if (instance.ElementType == elementType)
            {
                sameElement = instance;
                continue;
            }

            E_ElementEffect reaction = GetReaction(instance.ElementType, elementType);
            if (reaction == E_ElementEffect.None)
                continue;

            target.Remove(instance);
            Trigger(reaction, position, instance.ElementType, elementType, damage, targetLM);
            if (reaction == E_ElementEffect.Steam)
                target.Add(E_ElementType.Water, GetDuration(E_ElementType.Water));
            return reaction;
        }

        if (sameElement != null)
            target.Remove(sameElement);

        target.Add(elementType, GetDuration(elementType));

        return E_ElementEffect.None;
    }

    public static E_ElementEffect GetReaction(E_ElementType first, E_ElementType second)
    {
        return ElementReactionSystem.GetReaction(first, second);
    }

    private static void Trigger(E_ElementEffect effect, Vector2 position,
        E_ElementType current, E_ElementType incoming, float damage, LayerMask targetLM)
    {
        ElementEffectInfo info = new(effect, position, current, incoming, damage, targetLM);
        ElementReactionSystem.Spawn(info);
        EffectTriggered?.Invoke(info);
    }
}
