using System.Collections.Generic;
using UnityEngine;

public class ActorElementEffects : MonoBehaviour
{
    public static readonly HashSet<ActorElementEffects> Active = new();
    public float firePercent = 0.05f, poisonDamage = 1f, electricDamage = 1f;
    private ICombatable actor;
    private SpriteRenderer visual;
    private MaterialPropertyBlock properties;
    private float nextTick, flashUntil;
    private static bool conducting;
    public bool Has(E_ElementType element)
    {
        if (actor == null) return false;
        foreach (BuffInstance buff in actor.BuffList) if (buff.ElementType == element) return true;
        return false;
    }
    private void Awake()
    {
        actor = GetComponent<ICombatable>();
        visual = GetComponentInChildren<SpriteRenderer>();
        Material material = Resources.Load<Material>("Material/Combat/ElementSprite");
        properties = new MaterialPropertyBlock();
        if (visual != null && material != null) visual.sharedMaterial = material;
    }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    public void Flash() => flashUntil = Time.time + 0.12f;
    private void Update()
    {
        if (actor == null || actor.IsDead || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        if (Has(E_ElementType.Fire)) actor.TakeDamage(actor.MaxHp * firePercent);
        if (Has(E_ElementType.Poison))
        {
            PlayerController player = actor as PlayerController;
            float amount = poisonDamage * (1f - (player != null ? player.PoisonResistance : 0f));
            if (amount > 0f) actor.TakeDamage(amount);
            else if (amount < 0f && player != null) player.Heal(-amount);
        }
    }
    private void LateUpdate()
    {
        if (visual == null || properties == null) return;
        visual.GetPropertyBlock(properties);
        properties.SetFloat("_Element", Has(E_ElementType.Fire) ? 2f : Has(E_ElementType.Poison) ? 4f : Has(E_ElementType.Water) ? 1f : 0f);
        properties.SetFloat("_Electric", Has(E_ElementType.Electric) ? 1f : 0f);
        properties.SetFloat("_Flash", Mathf.Clamp01((flashUntil - Time.time) / 0.12f));
        properties.SetFloat("_Mode", 0f);
        visual.SetPropertyBlock(properties);
    }
    public static void Conduct(Vector2 origin, float initialRadius = 1f)
    {
        if (conducting) return;
        conducting = true;
        try
        {
            HashSet<ActorElementEffects> visited = new();
            HashSet<ElementField> fields = new();
            Queue<Vector3> queue = new();
            queue.Enqueue(new Vector3(origin.x, origin.y, initialRadius));
            while (queue.Count > 0)
            {
                Vector3 node = queue.Dequeue();
                foreach (ActorElementEffects target in new List<ActorElementEffects>(Active))
                {
                    if (target == null || target.actor == null || target.actor.IsDead || visited.Contains(target) ||
                        !target.Has(E_ElementType.Water) || Vector2.Distance(node, target.transform.position) > node.z) continue;
                    visited.Add(target);
                    target.actor.AddBuff(E_ElementType.Electric);
                    target.actor.TakeDamage(target.electricDamage);
                    queue.Enqueue(new Vector3(target.transform.position.x, target.transform.position.y, 0.9f));
                }
                foreach (ElementField field in new List<ElementField>(ElementField.Active))
                {
                    if (field == null || field.element != E_ElementType.Water || fields.Contains(field) ||
                        Vector2.Distance(node, field.transform.position) > node.z + field.Radius) continue;
                    fields.Add(field);
                    field.Electrify();
                    queue.Enqueue(new Vector3(field.transform.position.x, field.transform.position.y, field.Radius + 0.9f));
                }
            }
        }
        finally { conducting = false; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { Active.Clear(); conducting = false; }
}
