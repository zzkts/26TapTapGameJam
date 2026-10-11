using UnityEngine;

public class GenePickup : WorldInteractable
{
    public Modifier gene;
    public override string Title => gene != null ? gene.Name : "基因";
    public override string Detail => gene == null ? "" : gene.Desc;
    private void Start()
    {
        if (gene != null) PrototypeVisual.Add(gameObject, Color.white, new Vector2(0.28f, 0.28f));
    }
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || gene == null) return false;
        player.Genes.Acquire(gene);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
