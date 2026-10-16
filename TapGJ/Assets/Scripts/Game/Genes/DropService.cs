using UnityEngine;

public static class DropService
{
    public static void Spawn(Modifier[] pool, int count, Vector2 position, float pickupRange)
    {
        if (pool == null || pool.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            Modifier gene = pool[Random.Range(0, pool.Length)];
            if (gene == null) continue;
            Vector2 location = position + new Vector2((i - (count - 1) * 0.5f) * 0.6f, 0.15f);
            // Upper encounters remain part of the scenery after air-step removal. Their
            // rewards must reach the main floor rather than stay suspended out of reach.
            var ground = Physics2D.Raycast(location + Vector2.up * 0.5f, Vector2.down, 32f, LayerMask.GetMask("Default", "Ground", "Wall"));
            if (ground.collider != null) location.y = ground.point.y + 0.25f;
            var obj = new GameObject("Gene Drop " + gene.GeneId);
            obj.transform.position = location;
            GenePickup pickup = obj.AddComponent<GenePickup>();
            pickup.gene = gene;
            pickup.interactionRange = pickupRange;
        }
    }
}
