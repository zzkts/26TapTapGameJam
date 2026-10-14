using System.Collections.Generic;
using UnityEngine;

public class GeneChainController : MonoBehaviour
{
    public enum Phase { Ready, Active, Reloading }
    [Min(0.1f)] public float reloadDuration = 3f;
    private GeneInventory inventory;
    private readonly List<Modifier> attributes = new();
    public Phase State { get; private set; }
    public int AttributeIndex { get; private set; }
    public float Remaining { get; private set; }
    public IReadOnlyList<Modifier> Attributes => attributes;
    public Modifier CurrentAttribute => attributes.Count > 0 && AttributeIndex < attributes.Count ? attributes[AttributeIndex] : null;
    public Modifier ActiveAttribute => State == Phase.Active ? CurrentAttribute : null;
    public float Progress => State == Phase.Ready ? 1f : Mathf.Clamp01(Remaining /
        (State == Phase.Reloading ? reloadDuration : CurrentAttribute != null ? CurrentAttribute.EnchantDuration : 1f));
    private void Awake() => inventory = GetComponent<GeneInventory>();
    private void Start() => ResetChain();
    private void Update() => Tick(Time.deltaTime);
    public void ResetChain()
    {
        if (inventory == null) inventory = GetComponent<GeneInventory>();
        attributes.Clear();
        foreach (Modifier gene in inventory.GenChain) if (gene != null && gene.IsAttribute) attributes.Add(gene);
        State = Phase.Ready; Remaining = 0f; AttributeIndex = 0;
    }
    public bool TryActivate()
    {
        PlayerController player = GetComponent<PlayerController>();
        if (State != Phase.Ready || CurrentAttribute == null ||
            (player != null && (player.MenuOpen || player.IsDead || player.IsDashing || player.IsHurt))) return false;
        State = Phase.Active; Remaining = CurrentAttribute.EnchantDuration; return true;
    }
    public void Tick(float delta)
    {
        if (delta <= 0f || State == Phase.Ready) return;
        Remaining -= delta;
        if (Remaining > 0f) return;
        if (State == Phase.Reloading) { State = Phase.Ready; AttributeIndex = 0; Remaining = 0f; return; }
        AttributeIndex++;
        if (AttributeIndex >= attributes.Count) { AttributeIndex = 0; State = Phase.Reloading; Remaining = reloadDuration; }
        else { State = Phase.Ready; Remaining = 0f; }
    }
}
