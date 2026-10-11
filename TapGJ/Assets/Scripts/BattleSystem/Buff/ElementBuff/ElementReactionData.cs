using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 元素组合与反应预制体的配置表。
/// 通过两个元素查找反应类型以及需要生成的场景对象。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Element Reaction Data", fileName = "ElementReactionData")]
public class ElementReactionData : ScriptableObject
{
    [Serializable]
    public struct Info
    {
        public E_ElementType first;
        public E_ElementType second;
        public E_ElementEffect effect;
        public GameObject reactionPrefab;
    }

    [SerializeField] private List<Info> reactions = new();

    public Info Get(E_ElementType first, E_ElementType second)
    {
        for (int i = 0; i < reactions.Count; i++)
        {
            Info info = reactions[i];
            if (info.first == first && info.second == second ||
                info.first == second && info.second == first)
                return info;
        }

        return default;
    }
}
