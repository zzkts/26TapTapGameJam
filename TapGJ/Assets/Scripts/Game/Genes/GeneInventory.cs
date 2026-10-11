using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家持有的基因库存和装备栏。
/// 负责获取、装备、移除和调整基因，并生成本次攻击快照。
/// </summary>
public class GeneInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int slotCount = 6;
    [SerializeField] private List<Modifier> genInventory = new();
    [SerializeField] private List<Modifier> genChain = new();
    [SerializeField] private Weapon weapon;

    public IReadOnlyList<Modifier> GenInventory => genInventory;
    public IReadOnlyList<Modifier> GenChain => genChain;
    public int SlotCount => slotCount;

    public event Action Changed;
    public event Action<Modifier> Acquired;

    private void Awake()
    {
        ResizeChain();
    }

    public void Acquire(Modifier gene)
    {
        genInventory.Add(gene);
        Acquired?.Invoke(gene);
        Changed?.Invoke();
    }

    public void Equip(int inventoryIndex, int slotIndex)
    {
        ResizeChain();
        genChain[slotIndex] = genInventory[inventoryIndex];
        Changed?.Invoke();
    }

    public void Unequip(int slotIndex)
    {
        ResizeChain();
        genChain[slotIndex] = null;
        Changed?.Invoke();
    }

    public void Move(int from, int to)
    {
        ResizeChain();
        (genChain[from], genChain[to]) = (genChain[to], genChain[from]);
        Changed?.Invoke();
    }

    public Modifier[] CopyChain()
    {
        ResizeChain();
        return genChain.ToArray();
    }

    public bool Commit(GeneEditStation station, PlayerController player, Modifier[] draft)
    {
        if (!station.CanUse(player) || draft.Length != slotCount)
            return false;

        genChain.Clear();
        genChain.AddRange(draft);
        Changed?.Invoke();
        return true;
    }

    public void Apply()
    {
        ResizeChain();
        weapon.Apply(genChain);
    }

    private void ResizeChain()
    {
        while (genChain.Count < slotCount)
            genChain.Add(null);

        if (genChain.Count > slotCount)
            genChain.RemoveRange(slotCount, genChain.Count - slotCount);
    }
}
