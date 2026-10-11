using UnityEngine;

public class PlayerSupply : WorldInteractable
{
    public enum SupplyKind { Health, Dash }
    public SupplyKind kind;
    [Min(0f)] public float healAmount = 4f;
    public override string Title => kind == SupplyKind.Health ? "恢复药剂" : "冲刺模块";
    public override string Detail => kind == SupplyKind.Health ? "恢复 " + healAmount + " 点生命" : "解锁地面与空中冲刺";
    public override bool CanUse(PlayerController player) => base.CanUse(player) &&
        (kind == SupplyKind.Health ? player.hp < player.maxHp : !player.DashUnlocked);
    private void Awake()
    {
        if (kind != SupplyKind.Health) return;
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        int layer = LayerMask.NameToLayer("Pickup");
        if (layer >= 0) gameObject.layer = layer;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (kind != SupplyKind.Health) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null) Interact(player);
    }
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player)) return false;
        if (kind == SupplyKind.Health) player.Heal(healAmount);
        else player.UnlockDash();
        gameObject.SetActive(false);
        Destroy(gameObject);
        return true;
    }
}
