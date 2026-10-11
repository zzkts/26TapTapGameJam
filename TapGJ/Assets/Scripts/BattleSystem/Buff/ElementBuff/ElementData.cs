using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有元素的统一配置。
/// 集中保存元素说明、附着时间和对应的子弹预制体。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Element Data", fileName = "ElementData")]
public class ElementData : ScriptableObject
{
    [Serializable]
    public struct Info
    {
        public E_ElementType type;
        [TextArea] public string desc;
        [Min(0f)] public float duration;
        public GameObject projectilePrefab;
    }

    [SerializeField] private List<Info> elements = new();

    public Info Get(E_ElementType type)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            if (elements[i].type == type)
                return elements[i];
        }

        return default;
    }
}
