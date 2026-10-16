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
    private readonly HashSet<ICombatable> tickActors = new();
    private readonly HashSet<IHealthActor> explosionActors = new();
    private Collider2D[] overlaps = new Collider2D[32];
    private static Collider2D[] pullOverlaps = new Collider2D[64];
    private static readonly HashSet<Rigidbody2D> pullBodies = new();
    private bool pooled;
    public static ElementField Create(E_ElementType element, Vector2 position, Vector2 size,
        float lifetime, LayerMask targets, bool pull = false)
    {
        bool fresh = true;
        GameObject obj = Application.isPlaying ? TapGJ.WaterProjectiles.WaterEffectPool.Rent("Element field", out fresh) : new GameObject("Element field");
        obj.transform.position = position;
        ElementField field = fresh ? obj.AddComponent<ElementField>() : obj.GetComponent<ElementField>();
        field.pooled = Application.isPlaying;
        field.nextTick = Time.time; field.detonated = false; field.electricUntil = 0;
        field.element = element; field.size = size; field.duration = lifetime;
        field.created = Time.time; field.expires = Time.time + lifetime;
        field.targetMask = element == E_ElementType.Water ? targets | LayerMask.GetMask("Player","PlayerInvulnerable") : targets; field.vortex = pull;
        if (fresh) field.visual = obj.AddComponent<SpriteRenderer>(); field.visual.sprite = PrototypeVisual.WhiteSprite;
        field.visual.sortingOrder = 3;
        field.visual.color = element == E_ElementType.None ? new Color(1f, 1f, 1f, 0.3f) :
            element == E_ElementType.Poison ? new Color(0.4f, 0.9f, 0.25f, 0.4f) : new Color(0.2f, 0.8f, 1f, 0.4f);
        Material material = Resources.Load<Material>("Material/Combat/ElementSprite");
        if (material != null) field.visual.sharedMaterial = material;
        if (field.properties == null) field.properties = new MaterialPropertyBlock();
        field.properties.Clear(); field.visual.SetPropertyBlock(null);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        return field;
    }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    private void Update()
    {
        if (Time.time >= expires) { Release(); return; }
        if (properties != null && visual != null)
        {
            properties.SetFloat("_Field", 1f); properties.SetFloat("_Mode", 1f);
            properties.SetFloat("_Element", (float)element); properties.SetFloat("_Electric", Time.time < electricUntil ? 1f : 0f);
            properties.SetFloat("_Progress", Mathf.Clamp01((Time.time - created) / duration));
            visual.SetPropertyBlock(properties);
        }
        if (Time.time < nextTick || detonated || element == E_ElementType.None) return;
        nextTick = Time.time + 1f;
        tickActors.Clear();
        int count = QueryBox(transform.position, size, targetMask, ref overlaps);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = overlaps[i];
            ICombatable actor = collider.GetComponentInParent<ICombatable>();
            if (actor == null || actor.IsDead || !tickActors.Add(actor)) continue;
            if (element == E_ElementType.Water) actor.AddBuff(element);
            else if (element == E_ElementType.Poison)
            {
                actor.AddBuff(element);
            }
        }
    }
    private void FixedUpdate()
    {
        if (vortex) Pull(transform.position, size, targetMask, 2f * Time.fixedDeltaTime);
    }
    public static void Pull(Vector2 origin, Vector2 area, LayerMask targets, float step)
    {

        pullBodies.Clear();
        int count = QueryBox(origin, area, targets, ref pullOverlaps);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = pullOverlaps[i];
            EnemyAIController enemy = collider.GetComponentInParent<EnemyAIController>();
            if (enemy == null || enemy.IsDead) continue;
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            if (body != null && pullBodies.Add(body)) body.MovePosition(Vector2.MoveTowards(body.position, origin, step));
        }
    }
    private static int QueryBox(Vector2 origin, Vector2 area, LayerMask targets, ref Collider2D[] buffer)
    {
        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(targets);
        int count = Physics2D.OverlapBox(origin, area, 0, filter, buffer);
        // Grow only on saturation: dense encounters must not silently lose hits.
        while (count == buffer.Length)
        {
            System.Array.Resize(ref buffer, buffer.Length * 2);
            count = Physics2D.OverlapBox(origin, area, 0, filter, buffer);
        }
        return count;
    }
    public void Consume() => Release();
    private void Release()
    {
        if (!gameObject.activeSelf) return;
        if (!pooled) { Destroy(gameObject); return; }
        gameObject.SetActive(false);
        tickActors.Clear(); explosionActors.Clear();
        System.Array.Clear(overlaps, 0, overlaps.Length);
        TapGJ.WaterProjectiles.WaterEffectPool.ReturnAfter(gameObject, 0);
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
        detonated = true; explosionActors.Clear();
        int count = QueryBox(transform.position, size, targetMask, ref overlaps);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = overlaps[i];
            IHealthActor actor = collider.GetComponentInParent<IHealthActor>();
            if (actor != null && !actor.IsDead && explosionActors.Add(actor))
            {
                // The field explosion consumes poison before the incoming fire is attached.
                // Otherwise the same hit would also trigger a second actor-level explosion.
                (actor as ICombatable)?.RemBuff(E_ElementType.Poison);
                actor.TakeDamage(actor.MaxHp * 0.3f);
            }
        }
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, new Color(1f, 0.5f, 0.15f), Radius, Vector2.up);
        EmergenceReactions.Emit("explosion",transform.position,"毒雾 + 火焰 → 范围爆破",1);
        Release();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
}
