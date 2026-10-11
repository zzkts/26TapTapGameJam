using System.Collections.Generic;
using UnityEngine;

public class EnemyGeneDrop : MonoBehaviour
{
    public GeneRules rules;
    public CombatElement dropElement = CombatElement.Water;
    private bool dropped;
    public void Drop()
    {
        if (dropped || rules == null) return;
        dropped = true;
        float roll = Random.value;
        int count = roll < rules.noDropProbability ? 0 : roll < rules.noDropProbability + rules.oneDropProbability ? 1 : 2;
        var pool = new List<GeneData>();
        foreach (GeneData gene in rules.genes)
            if (gene != null && (gene.kind == GeneKind.Modification || gene.element == dropElement)) pool.Add(gene);
        DropService.Spawn(pool.ToArray(), count, transform.position, rules.interactionRange);
    }
}
