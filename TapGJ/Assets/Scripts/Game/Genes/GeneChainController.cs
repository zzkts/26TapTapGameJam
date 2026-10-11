using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GeneInventory))]
public class GeneChainController : MonoBehaviour
{
    public enum Phase { Ready, Active, Reloading }
    private GeneInventory inventory;
    private PlayerController player;
    private string[] slots;
    private readonly List<string> attributes = new List<string>();
    public Phase State { get; private set; }
    public int AttributeIndex { get; private set; }
    public float Remaining { get; private set; }
    public int AttributeCount => attributes.Count;
    public GeneInventory Inventory => inventory;
    public GeneData CurrentAttribute => attributes.Count == 0 ? null : inventory.Definition(inventory.Find(attributes[AttributeIndex]));
    public event Action Changed;
    public string[] CopySlots() => slots != null ? (string[])slots.Clone() : Array.Empty<string>();

    private void Awake()
    {
        inventory = GetComponent<GeneInventory>();
        player = GetComponent<PlayerController>();
        GeneRules rules = inventory.Rules != null ? inventory.Rules : Resources.Load<GeneRules>("SO/Genes/GeneRules");
        slots = new string[rules != null ? rules.slotCount : 6];
    }
    private void Update()
    {
        if (player != null && (player.IsDead || player.MenuOpen)) return;
        Tick(Time.deltaTime);
    }
    public void Tick(float seconds)
    {
        if (State == Phase.Ready || attributes.Count == 0) return;
        Remaining = Mathf.Max(0f, Remaining - seconds);
        if (Remaining > 0f) return;
        if (State == Phase.Reloading)
        {
            AttributeIndex = 0;
            State = Phase.Ready;
        }
        else if (AttributeIndex + 1 < attributes.Count)
        {
            AttributeIndex++;
            State = Phase.Ready;
        }
        else
        {
            State = Phase.Reloading;
            Remaining = inventory.Rules.reloadDuration;
        }
        Changed?.Invoke();
    }
    public bool TryActivate()
    {
        if (attributes.Count == 0 || State != Phase.Ready || (player != null && (player.IsDead || player.MenuOpen))) return false;
        State = Phase.Active;
        Remaining = CurrentAttribute.enchantDuration;
        Changed?.Invoke();
        return true;
    }
    public bool Commit(GeneEditStation station, string[] draft)
    {
        if (station == null || player == null || !station.CanUse(player) || draft == null || draft.Length != slots.Length) return false;
        var used = new HashSet<string>();
        foreach (string id in draft)
            if (!string.IsNullOrEmpty(id) && (inventory.Find(id) == null || !used.Add(id))) return false;
        slots = (string[])draft.Clone();
        attributes.Clear();
        foreach (string id in slots)
        {
            GeneData gene = inventory.Definition(inventory.Find(id));
            if (gene != null && gene.kind == GeneKind.Attribute) attributes.Add(id);
        }
        AttributeIndex = 0;
        Remaining = 0f;
        State = Phase.Ready;
        Changed?.Invoke();
        return true;
    }

    public BulletSnapshot Capture(WeaponData weapon)
    {
        var shot = new BulletSnapshot(weapon, inventory.Rules);
        var effects = new List<ResolvedGeneEffect>();
        foreach (string id in slots)
        {
            GeneRecord record = inventory.Find(id);
            GeneData gene = inventory.Definition(record);
            if (gene == null || gene.kind != GeneKind.Modification) continue;
            ApplyGene(shot, effects, gene, record.level);
        }
        if (State == Phase.Active && attributes.Count > 0)
        {
            GeneRecord record = inventory.Find(attributes[AttributeIndex]);
            GeneData gene = inventory.Definition(record);
            shot.element = gene.element;
            shot.color = gene.color;
            ApplyGene(shot, effects, gene, record.level);
        }
        shot.effects = effects.ToArray();
        shot.Clamp();
        return shot;
    }

    private static void ApplyGene(BulletSnapshot shot, List<ResolvedGeneEffect> effects, GeneData gene, int level)
    {
        foreach (GeneEffect effect in gene.effects)
        {
            float value = level > 1 ? effect.evolvedValue : effect.value;
            switch (effect.kind)
            {
                case GeneEffectKind.Damage: shot.damage *= value; break;
                case GeneEffectKind.Speed: shot.speed *= value; break;
                case GeneEffectKind.Size: shot.size *= value; break;
                case GeneEffectKind.Pierce: shot.pierce += Mathf.RoundToInt(value); break;
                case GeneEffectKind.Bounce: shot.bounces += Mathf.RoundToInt(value); break;
                case GeneEffectKind.Orbit: shot.orbitRadius = value; break;
                default: effects.Add(new ResolvedGeneEffect(effect.kind, effect.trigger, value, level > 1)); break;
            }
        }
    }
}
