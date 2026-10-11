using System.Collections.Generic;
using UnityEngine;

public class EnvironmentElementSource : MonoBehaviour
{
    public GeneRules rules;
    public WeaponData sourceTemplate;
    public CombatElement element = CombatElement.Electric;
    [Min(0.1f)] public float radius = 1f;
    [Min(0.1f)] public float interval = 1f;
    private float nextPulse;
    private BulletSnapshot snapshot;
    private void Awake()
    {
        if (sourceTemplate != null) snapshot = new BulletSnapshot(sourceTemplate, rules);
    }
    private void Update()
    {
        if (snapshot == null || Time.time < nextPulse) return;
        nextPulse = Time.time + interval;
        if (element == CombatElement.Electric)
        {
            // One environmental pulse traverses a connected water group once.
            ElementArea.Conduct(transform.position, snapshot, null, radius);
            return;
        }
        var seen = new HashSet<CombatStatus>();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            CombatStatus status = CombatStatus.Get(collider);
            if (status != null && seen.Add(status)) status.Apply(element, snapshot);
        }
        foreach (ElementArea area in new List<ElementArea>(ElementArea.Active))
            if (area != null && Vector2.Distance(area.transform.position, transform.position) <= radius + area.Radius)
                area.React(element, snapshot);
    }
}
