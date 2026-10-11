using System;
using UnityEngine;

public enum GeneKind { Modification, Attribute }
[Flags] public enum CombatElement { None = 0, Water = 1, Fire = 2, Poison = 4, Electric = 8, Steam = 16 }
public enum GeneEffectKind { Damage, Speed, Size, Pierce, Bounce, Orbit, Split, Spread, Vortex }
public enum GeneTrigger { OnSpawn, OnHit, OnDespawn }

[Serializable]
public struct GeneEffect
{
    public GeneEffectKind kind;
    public GeneTrigger trigger;
    public float value;
    public float evolvedValue;
}

[CreateAssetMenu(menuName = "Battle/Gene", fileName = "Gene")]
public class GeneData : ScriptableObject
{
    public string geneId;
    public string displayName;
    [TextArea] public string description;
    [TextArea] public string evolvedDescription;
    public GeneKind kind;
    public CombatElement element;
    [Min(0.1f)] public float enchantDuration = 4f;
    public GeneEffect[] effects = Array.Empty<GeneEffect>();
    public Color color = Color.white;
}
