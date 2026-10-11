using UnityEngine;

public static class DropService
{
    public static void Spawn(GeneData[] pool, int count, Vector2 position, float pickupRange)
    {
        if (pool == null || pool.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            GeneData gene = pool[Random.Range(0, pool.Length)];
            if (gene == null) continue;
            Vector2 location = position + new Vector2((i - (count - 1) * 0.5f) * 0.6f, 0.15f);
            var ground = Physics2D.Raycast(location + Vector2.up * 0.5f, Vector2.down, 5f, LayerMask.GetMask("Default", "Ground", "Wall"));
            if (ground.collider != null) location.y = ground.point.y + 0.25f;
            var obj = new GameObject("Gene Drop " + gene.geneId);
            obj.transform.position = location;
            GenePickup pickup = obj.AddComponent<GenePickup>();
            pickup.gene = gene;
            pickup.interactionRange = pickupRange;
        }
    }
}
