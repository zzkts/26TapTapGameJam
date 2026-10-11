using UnityEngine;

public class GeneEditStation : WorldInteractable
{
    public override string Title => "基因装配台";
    public override string Detail => "调整基因链";
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player)) return false;
        GeneEditPanel panel = UIManager.Instance.GetPanel<GeneEditPanel>();
        if (panel == null) return false;
        panel.Open(player, this);
        return true;
    }
}
