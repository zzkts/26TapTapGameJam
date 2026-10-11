using UnityEngine;

public class GenePickup : WorldInteractable
{
    public GeneData gene;
    public override string Title => gene != null ? gene.displayName : "基因";
    public override string Detail => gene == null ? "" : (gene.kind == GeneKind.Modification ? "改造" : "属性") + " · " + gene.description;
    private void Start()
    {
        if (gene != null) PrototypeVisual.Add(gameObject, Color.Lerp(Color.white, gene.color, 0.45f), new Vector2(0.28f, 0.28f));
    }
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || gene == null || !player.TryGetComponent(out GeneInventory inventory)) return false;
        if (inventory.Acquire(gene) == null) return false;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
