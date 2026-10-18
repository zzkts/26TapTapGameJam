using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GeneChainController))]
public class GeneInventory : MonoBehaviour
{
    [SerializeField, Range(1, 8)] private int slotCount = 6;
    [SerializeField, Min(2)] private int evolutionCopies = 3;
    [SerializeField] private List<Modifier> genInventory = new();
    [SerializeField] private List<Modifier> genChain = new();
    [SerializeField] private Weapon weapon;
    private readonly List<Modifier> owned = new();
    public IReadOnlyList<Modifier> GenInventory => genInventory;
    public IReadOnlyList<Modifier> GenChain => genChain;
    public int SlotCount => slotCount;
    public int EvolutionCopies => evolutionCopies;
    public int GetEvolutionCopies(Modifier gene) => gene != null && gene.EvolutionRequiredCopies > 0 ?
        Mathf.Max(2, gene.EvolutionRequiredCopies) : evolutionCopies;
    public event Action Changed;
    public event Action<Modifier> Acquired;
    private void Awake()
    {
        weapon = weapon != null ? weapon : GetComponent<Weapon>();
        // Inventory entries are per-run instances; configuration assets are never evolved.
        Dictionary<Modifier, Modifier> migrated = new();
        for (int i = 0; i < genInventory.Count; i++)
        {
            Modifier original = genInventory[i];
            if (original == null) continue;
            Modifier instance = Clone(original); genInventory[i] = instance; migrated[original] = instance;
        }
        ResizeChain();
        for (int i = 0; i < genChain.Count; i++)
        {
            Modifier original = genChain[i];
            if (original == null) continue;
            if (!migrated.TryGetValue(original, out Modifier instance)) { instance = Clone(original); genInventory.Add(instance); migrated[original] = instance; }
            genChain[i] = instance;
        }
    }
    private Modifier Clone(Modifier source)
    {
        Modifier instance = Instantiate(source);
        instance.hideFlags = HideFlags.DontSave; instance.InstanceId = Guid.NewGuid().ToString("N");
        instance.Level = 1; instance.Copies = 1; instance.SetDefinition(source); owned.Add(instance); return instance;
    }
    public void Acquire(Modifier gene)
    {
        if (gene == null || (gene.IsAttribute && gene.Element == E_ElementType.Electric)) return;
        Modifier instance = genInventory.Find(x => x != null && x.Level == 1 &&
            (gene.GeneId != 0 ? x.GeneId == gene.GeneId : x.Definition == gene.Definition));
        if (instance == null) { instance = Clone(gene); genInventory.Add(instance); }
        else { instance.Copies++; if (instance.Copies >= GetEvolutionCopies(instance)) instance.Level = 2; }
        Apply(); Acquired?.Invoke(instance); Changed?.Invoke();
    }
    public Modifier[] CopyChain() { ResizeChain(); return genChain.ToArray(); }
    public bool Commit(GeneEditStation station, PlayerController player, Modifier[] draft)
    {
        if (station == null || !station.CanUse(player) || draft == null || draft.Length != slotCount) return false;
        HashSet<Modifier> used = new();
        foreach (Modifier gene in draft)
            if (gene != null && (!genInventory.Contains(gene) || !used.Add(gene))) return false;
        genChain.Clear(); genChain.AddRange(draft);
        GetComponent<GeneChainController>().ResetChain();
        Apply(); Changed?.Invoke(); return true;
    }
    public void Apply() { ResizeChain(); if (weapon != null) weapon.Apply(genChain); }
    private void ResizeChain()
    {
        slotCount = Mathf.Clamp(slotCount, 1, 8);
        while (genChain.Count < slotCount) genChain.Add(null);
        if (genChain.Count > slotCount) genChain.RemoveRange(slotCount, genChain.Count - slotCount);
    }
    private void OnDestroy()
    {
        foreach (Modifier instance in owned) if (instance != null)
        {
            if (Application.isPlaying) Destroy(instance);
            else DestroyImmediate(instance);
        }
    }
}
