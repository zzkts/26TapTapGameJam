using UnityEngine;

/// <summary>A fixed chapter reward; its child artwork is authored by the world builder.</summary>
public sealed class CompleteGeneSample : WorldInteractable
{
    public Modifier gene;
    [Min(1)] public int copies = 1;
    public override string Title => gene != null ? gene.Name + "样本" : "基因样本";
    public override string Detail => "带到基因装配台，为地漏装入新能力";
    public override bool CanUse(PlayerController player) => base.CanUse(player) && CompleteRewardState.IsAvailable(this);
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || gene == null || player.Genes == null) return false;
        for (int i = 0; i < copies; i++) player.Genes.Acquire(gene);
        CompleteRewardState.Claim(this);
        gameObject.SetActive(false); Destroy(gameObject); return true;
    }
}
