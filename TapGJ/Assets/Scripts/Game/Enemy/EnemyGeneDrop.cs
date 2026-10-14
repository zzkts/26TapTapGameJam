using UnityEngine;

public class EnemyGeneDrop : MonoBehaviour
{
    [SerializeField] private Modifier[] genePool;
    [SerializeField, Range(0f, 1f)] private float noDropProbability = 0.4f;
    [SerializeField, Range(0f, 1f)] private float oneDropProbability = 0.4f;
    [SerializeField] private float pickupRange = 1.6f;
    private bool dropped;
    public void Drop()
    {
        if (dropped || genePool == null || genePool.Length == 0) return;
        dropped = true;
        float roll = Random.value;
        int count = roll < noDropProbability ? 0 : roll < noDropProbability + oneDropProbability ? 1 : 2;
        DropService.Spawn(genePool, count, transform.position, pickupRange);
    }
}
