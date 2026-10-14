using UnityEngine;

public class GenePickup : WorldInteractable
{
    public Modifier gene;
    public override string Title => gene != null ? gene.Name : "基因";
    public override string Detail => gene == null ? "" : (gene.IsAttribute ? "属性 · " : "改造 · ") + gene.Desc;
    private void Start()
    {
        GameplayUITheme theme = Resources.Load<GameplayUITheme>("SO/UI/GameplayUITheme");
        if (gene != null) PrototypeVisual.Add(gameObject, gene.Color, new Vector2(0.45f, 0.45f), theme != null ? theme.element : null);
    }
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || gene == null || player.Genes == null) return false;
        player.Genes.Acquire(gene);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
