using UnityEngine;

/// <summary>
/// 一种可配置的基因。
/// 保存显示信息，以及它对本次攻击产生的一项修改。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Modifier", fileName = "Modifier")]
public class Modifier : ScriptableObject
{
    [SerializeField, HideInInspector] private uint geneId;
    [SerializeField] private string geneName;
    [SerializeField, TextArea] private string desc;
    [SerializeField] private E_ModifierType modifierType;
    [SerializeField] private float modifierValue;

    public uint GeneId => geneId;
    public string Name => geneName;
    public string Desc => desc;
    public E_ModifierType ModifierType => modifierType;
    public float ModifierValue => modifierValue;
}
