using UnityEngine;

public interface IElementReaction
{
    void SetUp(ElementEffectInfo info);
}

/// <summary>
/// 元素反应结果的生成入口。
/// 根据反应类型创建蒸汽、毒雾或爆炸对象，并传入各自需要的数据。
/// </summary>
public static class ElementReactionSystem
{
    private const string DataPath = "SO/Element/ElementReactionData";
    private static readonly ElementReactionData data =
        Resources.Load<ElementReactionData>(DataPath);

    public static E_ElementEffect GetReaction(E_ElementType first, E_ElementType second)
    {
        return data.Get(first, second).effect;
    }

    public static GameObject Spawn(ElementEffectInfo info)
    {
        ElementReactionData.Info reaction = data.Get(info.current, info.incoming);
        GameObject effect = Object.Instantiate(reaction.reactionPrefab,
            info.position, Quaternion.identity);
        effect.name = $"{reaction.effect}Reaction";
        effect.GetComponent<IElementReaction>().SetUp(info);
        return effect;
    }
}
