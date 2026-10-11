using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Gene Rules", fileName = "GeneRules")]
public class GeneRules : ScriptableObject
{
    public GeneData[] genes;
    [Range(1, 8)] public int slotCount = 6;
    [Min(2)] public int evolutionCopies = 3;
    [Min(0.1f)] public float reloadDuration = 3f;
    [Min(0.1f)] public float interactionRange = 1.6f;
    [Range(0f, 1f)] public float noDropProbability = 0.4f;
    [Range(0f, 1f)] public float oneDropProbability = 0.4f;
    [Min(1)] public int maxSpawnPerShot = 24;
    [Range(0, 4)] public int maxSplitGeneration = 1;
    [Min(0.1f)] public float statusDuration = 4f;
    [Min(0.1f)] public float poisonDuration = 3f;
    [Min(0.1f)] public float poisonRadius = 2.5f;
    [Min(0f)] public float poisonDamage = 1f;
    [Range(0f, 1f)] public float firePercentPerSecond = 0.05f;
    [Range(0f, 1f)] public float explosionHealthFraction = 0.3f;
    [Min(0f)] public float electricDamage = 1f;
    [Min(0.01f)] public float contactDistance = 0.8f;
    [Min(0.1f)] public float waterDuration = 4f;
    [Min(0.1f)] public float waterRadius = 0.8f;
    [Min(0.1f)] public float vortexDuration = 1.5f;
    [Min(0f)] public float vortexPullSpeed = 2f;
    public GeneData Find(string id)
    {
        if (genes != null) foreach (GeneData gene in genes) if (gene != null && gene.geneId == id) return gene;
        return null;
    }
}
