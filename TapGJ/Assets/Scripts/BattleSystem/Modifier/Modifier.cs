using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Modifier", fileName = "Modifier")]
public class Modifier : ScriptableObject
{
    [SerializeField, HideInInspector] private uint geneId;
    [SerializeField] private string geneName;
    [SerializeField, TextArea] private string desc;
    [SerializeField] private E_ModifierType modifierType;
    [SerializeField] private float modifierValue;
    [SerializeField, Min(0.1f)] private float enchantDuration = 4f;
    [SerializeField] private bool hasEvolution;
    [SerializeField] private float evolvedValue;
    [SerializeField, TextArea] private string evolvedDescription;
    [SerializeField, Min(0f)] private float effectRadius = 3f;
    [SerializeField, Min(0f)] private float evolvedRadius = 5f;
    [SerializeField] private bool vortex;
    [System.NonSerialized] public string InstanceId;
    [System.NonSerialized] public int Copies = 1;
    [System.NonSerialized] public int Level = 1;
    [System.NonSerialized] private Modifier definition;
    public Modifier Definition => definition != null ? definition : this;
    public void SetDefinition(Modifier source) => definition = source.Definition;
    public uint GeneId => geneId;
    public string Name => (Level > 1 ? "高级" : "") + geneName;
    public string Desc => Level > 1 && !string.IsNullOrEmpty(evolvedDescription) ? evolvedDescription : desc;
    public E_ModifierType ModifierType => modifierType;
    public float ModifierValue => Level > 1 && hasEvolution ? evolvedValue : modifierValue;
    public bool IsAttribute => modifierType == E_ModifierType.Element;
    public float EnchantDuration => enchantDuration;
    public float Radius => Level > 1 ? evolvedRadius : effectRadius;
    public bool IsVortex => vortex;
    public E_ElementType Element => IsAttribute ? (E_ElementType)Mathf.RoundToInt(modifierValue) : E_ElementType.None;
    public Color Color => !IsAttribute ? new Color(0.8f, 0.84f, 0.88f) :
        Element == E_ElementType.Fire ? new Color(1f, 0.38f, 0.18f) :
        Element == E_ElementType.Poison ? new Color(0.5f, 0.95f, 0.25f) : new Color(0.2f, 0.85f, 1f);
}
