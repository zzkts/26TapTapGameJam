using System.Collections.Generic;
using UnityEngine;

public class ElementField : MonoBehaviour
{
    public static readonly HashSet<ElementField> Active = new();
    public E_ElementType element;
    public float Radius => Mathf.Max(size.x, size.y) * 0.5f;
    private Vector2 size;
    private float expires, duration, nextTick, created, electricUntil;
    private LayerMask targetMask;
    private bool vortex, detonated;
    private SpriteRenderer visual;
    private MaterialPropertyBlock properties;
    public static ElementField Create(E_ElementType element, Vector2 position, Vector2 size,
        float lifetime, LayerMask targets, bool pull = false)
    {
        GameObject obj = new(pull ? "Water Vortex" : element + " Field");
        obj.transform.position = position;
        ElementField field = obj.AddComponent<ElementField>();
        field.element = element; field.size = size; field.duration = lifetime;
        field.created = Time.time; field.expires = Time.time + lifetime;
        field.targetMask = element == E_ElementType.Water ? targets | LayerMask.GetMask("Player","PlayerInvulnerable") : targets; field.vortex = pull;
        field.visual = obj.AddComponent<SpriteRenderer>(); field.visual.sprite = PrototypeVisual.WhiteSprite;
        field.visual.sortingOrder = 3;
        field.visual.color = element == E_ElementType.None ? new Color(1f, 1f, 1f, 0.3f) :
            element == E_ElementType.Poison ? new Color(0.4f, 0.9f, 0.25f, 0.4f) : new Color(0.2f, 0.8f, 1f, 0.4f);
        Material material = Resources.Load<Material>("Material/Combat/ElementSprite");
        if (material != null) field.visual.sharedMaterial = material;
        field.properties = new MaterialPropertyBlock();
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        return field;
    }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    private void Update()
    {
        if (Time.time >= expires) { Destroy(gameObject); return; }
        if (properties != null && visual != null)
        {
            properties.SetFloat("_Field", 1f); properties.SetFloat("_Mode", 1f);
            properties.SetFloat("_Element", (float)element); properties.SetFloat("_Electric", Time.time < electricUntil ? 1f : 0f);
            properties.SetFloat("_Progress", Mathf.Clamp01((Time.time - created) / duration));
            visual.SetPropertyBlock(properties);
        }
        if (Time.time < nextTick || detonated || element == E_ElementType.None) return;
        nextTick = Time.time + 1f;
        HashSet<ICombatable> seen = new();
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(transform.position, size, 0f, targetMask))
        {
            ICombatable actor = collider.GetComponentInParent<ICombatable>();
            if (actor == null || actor.IsDead || !seen.Add(actor)) continue;
            if (element == E_ElementType.Water) actor.AddBuff(element);
            else if (element == E_ElementType.Poison)
            {
                actor.AddBuff(element);
            }
        }
        EmergenceReactions.ApplyAround(transform.position, Radius, element);
    }
    private void FixedUpdate()
    {
        if (vortex) Pull(transform.position, size, targetMask, 2f * Time.fixedDeltaTime);
    }
    public static void Pull(Vector2 origin, Vector2 area, LayerMask targets, float step)
    {
        foreach (EmergenceBody material in new List<EmergenceBody>(EmergenceBody.Active))
        {
            if (material == null || !material.Has(EmergenceMaterial.Movable)) continue;
            Vector2 delta = (Vector2)material.transform.position - origin;
            if (Mathf.Abs(delta.x) > area.x * .5f || Mathf.Abs(delta.y) > area.y * .5f) continue;
            Rigidbody2D movable = material.GetComponent<Rigidbody2D>();
            if (movable != null) movable.MovePosition(Vector2.MoveTowards(movable.position, origin, step));
        }
        HashSet<Rigidbody2D> seen = new();
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(origin, area, 0f, targets))
        {
            EnemyAIController enemy = collider.GetComponentInParent<EnemyAIController>();
            if (enemy == null || enemy.IsDead) continue;
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            if (body != null && seen.Add(body)) body.MovePosition(Vector2.MoveTowards(body.position, origin, step));
        }
    }
    public static void ApplyAround(Vector2 origin, float radius, E_ElementType element, LayerMask targets)
    {
        HashSet<ICombatable> seen = new();
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin, radius, targets))
        {
            ICombatable actor = collider.GetComponentInParent<ICombatable>();
            if (actor != null && !actor.IsDead && seen.Add(actor)) actor.AddBuff(element);
        }
        ReactFields(origin, radius, element);
    }
    public static void ReactFields(Vector2 origin, float radius, E_ElementType incoming)
    {
        EmergenceReactions.ApplyAround(origin, radius, incoming);
        foreach (ElementField field in new List<ElementField>(Active))
        {
            if (field == null || Vector2.Distance(field.transform.position, origin) > radius + field.Radius) continue;
            if (incoming == E_ElementType.Fire && field.element == E_ElementType.Poison) field.Detonate();
            else if (incoming == E_ElementType.Electric && field.element == E_ElementType.Water)
                ActorElementEffects.Conduct(field.transform.position, field.Radius);
        }
    }
    public void Electrify() => electricUntil = Time.time + 3f;
    private void Detonate()
    {
        if (detonated) return;
        detonated = true; HashSet<IHealthActor> seen = new();
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(transform.position, size, 0f, targetMask))
        {
            IHealthActor actor = collider.GetComponentInParent<IHealthActor>();
            if (actor != null && !actor.IsDead && seen.Add(actor))
            {
                // The field explosion consumes poison before the incoming fire is attached.
                // Otherwise the same hit would also trigger a second actor-level explosion.
                (actor as ICombatable)?.RemBuff(E_ElementType.Poison);
                actor.TakeDamage(actor.MaxHp * 0.3f);
            }
        }
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, new Color(1f, 0.5f, 0.15f), Radius, Vector2.up);
        EmergenceReactions.Emit("explosion",transform.position,"毒雾 + 火焰 → 范围爆破",1);
        Destroy(gameObject);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
}
