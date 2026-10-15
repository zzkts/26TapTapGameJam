using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyAIController))]
public class EnemySupportAura : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField, Min(0.1f)] private float healRadius = 8f;
    [SerializeField, Min(0f)] private float healAmount = 20f;
    [SerializeField, Min(0.01f)] private float healCooldown = 5f;
    [SerializeField, Min(1)] private int maxTargets = 4;
    [SerializeField] private LayerMask targetMask = 1 << 8;

    [Header("Feedback")]
    [SerializeField, Min(0.001f)] private float beamWidth = 0.035f;
    [SerializeField] private Color beamColor = new Color(0.35f, 1f, 0.45f, 0.9f);

    private EnemyAIController owner;
    private readonly Collider2D[] overlapResults = new Collider2D[64];
    private readonly Dictionary<EnemyAIController, long> enteredOrder = new Dictionary<EnemyAIController, long>();
    private readonly Dictionary<EnemyAIController, LineRenderer> beams = new Dictionary<EnemyAIController, LineRenderer>();
    private readonly List<EnemyAIController> priorityTargets = new List<EnemyAIController>();
    private Material beamMaterial;
    private float cooldownTimer;
    private long sequence;

    public float HealRadius => healRadius;
    public float HealAmount => healAmount;
    public float HealCooldown => healCooldown;
    public int MaxTargets => maxTargets;

    private void Awake()
    {
        owner = GetComponent<EnemyAIController>();
        cooldownTimer = healCooldown;
    }

    private void Update()
    {
        if (owner == null || owner.IsDead)
        {
            enabled = false;
            return;
        }

        RefreshCandidates();
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
            HealOldestTargets();
    }

    private void RefreshCandidates()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetMask);
        int count = Physics2D.OverlapCircle(transform.position, healRadius, filter, overlapResults);
        var inRange = new HashSet<EnemyAIController>();
        for (int i = 0; i < count; i++)
        {
            EnemyAIController candidate = overlapResults[i] != null
                ? overlapResults[i].GetComponentInParent<EnemyAIController>()
                : null;
            if (candidate == null || candidate == owner || candidate.IsDead)
                continue;

            inRange.Add(candidate);
            if (!enteredOrder.ContainsKey(candidate))
                enteredOrder.Add(candidate, sequence++);
        }

        var stale = new List<EnemyAIController>();
        foreach (EnemyAIController candidate in enteredOrder.Keys)
            if (!inRange.Contains(candidate) || candidate == null || candidate.IsDead)
                stale.Add(candidate);
        foreach (EnemyAIController candidate in stale)
        {
            enteredOrder.Remove(candidate);
            RemoveBeam(candidate);
        }

        priorityTargets.Clear();
        priorityTargets.AddRange(enteredOrder.Keys);
        priorityTargets.Sort((left, right) => enteredOrder[left].CompareTo(enteredOrder[right]));
        if (priorityTargets.Count > maxTargets)
            priorityTargets.RemoveRange(maxTargets, priorityTargets.Count - maxTargets);

        foreach (EnemyAIController candidate in priorityTargets)
            ShowBeam(candidate);
        foreach (EnemyAIController candidate in new List<EnemyAIController>(beams.Keys))
            if (!priorityTargets.Contains(candidate))
                RemoveBeam(candidate);
    }

    private void HealOldestTargets()
    {
        int healedTargets = 0;
        foreach (EnemyAIController candidate in priorityTargets)
        {
            if (candidate == null || candidate.IsDead || candidate.Hp >= candidate.MaxHp)
                continue;

            if (candidate.Heal(healAmount) <= 0f)
                continue;

            healedTargets++;
        }

        cooldownTimer = healCooldown;
        foreach(EcoFungus fungus in new List<EcoFungus>(EcoFungus.Active))
            if(fungus!=null&&Vector2.Distance(fungus.transform.position,transform.position)<=healRadius)
            {fungus.Nourish(.10f);healedTargets++;}
        if (healedTargets > 0)
            owner.PlaySupportAnimation();
    }

    private void ShowBeam(EnemyAIController target)
    {
        if (!beams.TryGetValue(target, out LineRenderer beam) || beam == null)
        {
            GameObject beamObject = new GameObject("HealBeam");
            beamObject.transform.SetParent(transform, false);
            beam = beamObject.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.startWidth = beamWidth;
            beam.endWidth = beamWidth;
            beam.startColor = beamColor;
            beam.endColor = beamColor;
            if (beamMaterial == null)
                beamMaterial = CreateBeamMaterial();
            beam.sharedMaterial = beamMaterial;
            beam.sortingOrder = 4;
            beams[target] = beam;
        }

        beam.enabled = true;
        beam.SetPosition(0, transform.position);
        beam.SetPosition(1, target.transform.position);
    }

    private void RemoveBeam(EnemyAIController target)
    {
        if (beams.TryGetValue(target, out LineRenderer beam) && beam != null)
            Destroy(beam.gameObject);
        beams.Remove(target);
    }

    private Material CreateBeamMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        return shader == null ? null : new Material(shader) { hideFlags = HideFlags.DontSave };
    }

    private void OnDisable()
    {
        foreach (LineRenderer beam in beams.Values)
            if (beam != null)
                Destroy(beam.gameObject);
        beams.Clear();
        priorityTargets.Clear();
        enteredOrder.Clear();
        if (beamMaterial != null)
            Destroy(beamMaterial);
        beamMaterial = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 1f, 0.45f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}
