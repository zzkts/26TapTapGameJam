using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GeneRecord
{
    public string instanceId;
    public string geneId;
    public int copies = 1;
    public int level = 1;
}

public class GeneInventory : MonoBehaviour
{
    [SerializeField] private GeneRules rules;
    [SerializeField] private List<GeneRecord> records = new List<GeneRecord>();
    public GeneRules Rules => rules;
    public IReadOnlyList<GeneRecord> Records => records;
    public event Action Changed;
    public event Action<GeneRecord, bool> Acquired;
    private void Awake() { if (rules == null) rules = Resources.Load<GeneRules>("SO/Genes/GeneRules"); }
    public GeneRecord Find(string instanceId) => records.Find(r => r.instanceId == instanceId);
    public GeneData Definition(GeneRecord record) => record != null && rules != null ? rules.Find(record.geneId) : null;

    public GeneRecord Acquire(GeneData gene)
    {
        if (gene == null || rules == null || rules.Find(gene.geneId) == null) return null;
        GeneRecord record = records.Find(r => r.geneId == gene.geneId && r.level == 1);
        bool evolved = false;
        if (record == null)
        {
            record = new GeneRecord { instanceId = Guid.NewGuid().ToString("N"), geneId = gene.geneId };
            records.Add(record);
        }
        else
        {
            record.copies++;
            if (record.copies >= rules.evolutionCopies) { record.level = 2; evolved = true; }
        }
        Changed?.Invoke();
        Acquired?.Invoke(record, evolved);
        return record;
    }
}
