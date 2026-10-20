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
    private int shownElement=-1,shownElectric=-1;
    private float shownFlash=-1f;
    private static readonly int ElementId=Shader.PropertyToID("_Element"),ElectricId=Shader.PropertyToID("_Electric"),
        FlashId=Shader.PropertyToID("_Flash"),ModeId=Shader.PropertyToID("_Mode");
    private static bool conducting;
    public bool Has(E_ElementType element)
    {
        if (actor == null) return false;
        // BuffList is an IReadOnlyList: foreach boxes its List enumerator on every query.
        var buffs=actor.BuffList;
        for(int i=0;i<buffs.Count;i++)if(buffs[i].ElementType==element)return true;
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
    private void OnEnable(){Active.Add(this);shownElement=shownElectric=-1;shownFlash=-1f;}
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
        int element=Has(E_ElementType.Fire)?2:Has(E_ElementType.Poison)?4:Has(E_ElementType.Water)?1:0;
        int electric=Has(E_ElementType.Electric)?1:0;
        float flash=Mathf.Clamp01((flashUntil-Time.time)/.12f);
        if(element==shownElement&&electric==shownElectric&&flash==shownFlash)return;
        visual.GetPropertyBlock(properties);
        properties.SetFloat(ElementId,element);properties.SetFloat(ElectricId,electric);
        properties.SetFloat(FlashId,flash);properties.SetFloat(ModeId,0f);
        visual.SetPropertyBlock(properties);
        shownElement=element;shownElectric=electric;shownFlash=flash;
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
            if (visited.Count + fields.Count > 0)
                EmergenceReactions.Emit("conduction", origin, "电源 → 水渍 / 湿润敌人 → 传导", visited.Count + fields.Count);
        }
        finally { conducting = false; }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { Active.Clear(); conducting = false; }
}
