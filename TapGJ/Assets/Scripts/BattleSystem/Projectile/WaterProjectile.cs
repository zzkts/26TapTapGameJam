using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class WaterProjectile : Projectile
{
    private CircleCollider2D hitbox;
    private SpriteRenderer visual;
    private ContactFilter2D filter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[64];
    private readonly HashSet<IActor> damaged = new HashSet<IActor>();
    private BulletSnapshot snapshot;
    private ShotSpawnBudget budget;
    private GameObject template;
    private Transform owner;
    private int generation, remainingPierce, remainingBounces;
    private bool spent, split, launched;
    private float age, travelSpeed, lastWallUntil, nextVortexTick;
    private int lastWall;
    private float orbitAngle;
    public BulletSnapshot Snapshot => snapshot?.Copy();
    public int Generation => generation;
    public bool Spent => spent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        hitbox.isTrigger = true;
    }
    public void Launch(BulletSnapshot shot, Vector2 position, Vector2 heading, LayerMask targets,
        Transform source, GameObject prefab, ShotSpawnBudget sharedBudget, int splitGeneration = 0, IActor previousHit = null)
    {
        snapshot = shot.Copy();
        snapshot.Clamp();
        owner = source;
        template = prefab;
        budget = sharedBudget;
        generation = splitGeneration;
        remainingPierce = snapshot.pierce;
        remainingBounces = snapshot.bounces;
        travelSpeed = snapshot.speed;
        groundLM = LayerMask.GetMask("Default", "Ground", "Wall");
        SetUp(position, heading, targets, snapshot.speed, snapshot.damage, 1f, snapshot.size, snapshot.lifetime);
        rb.linearVelocity = Vector2.zero;
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLM | targets);
        if (previousHit != null) damaged.Add(previousHit);
        if (visual != null) { visual.color = snapshot.color; visual.sortingOrder = 20; }
        orbitAngle = Mathf.Atan2(direction.y, direction.x);
        launched = true;
        TapGJ.WaterProjectiles.WaterVfx.AddRibbon(transform, snapshot.color, 0.12f * snapshot.size, 19);
        ProcessEffects(GeneTrigger.OnSpawn, null, position, Vector2.up);
    }
    protected override void Move()
    {
        if (!launched || spent) return;
        age += Time.fixedDeltaTime;
        Vector2 delta = direction * travelSpeed * Time.fixedDeltaTime;
        if (snapshot.orbitRadius > 0f && owner != null)
        {
            float radius = snapshot.orbitRadius * Mathf.Clamp01(age / 0.3f);
            orbitAngle += travelSpeed / Mathf.Max(0.1f, snapshot.orbitRadius) * Time.fixedDeltaTime;
            Vector2 goal = (Vector2)owner.position + new Vector2(Mathf.Cos(orbitAngle), Mathf.Sin(orbitAngle)) * radius;
            delta = goal - rb.position;
            if (delta.sqrMagnitude > 0.00001f) direction = delta.normalized;
        }
        float radiusWorld = hitbox.radius * Mathf.Abs(transform.lossyScale.x);
        int count = Physics2D.CircleCast(rb.position, radiusWorld, delta.normalized, filter, hits, delta.magnitude);
        for (int i = 0; i < count && !spent; i++)
            if (Resolve(hits[i].collider, hits[i].point, hits[i].normal))
            {
                if (!spent) rb.position = hits[i].centroid + hits[i].normal * 0.015f;
                return;
            }
        if (!spent) rb.MovePosition(rb.position + delta);
        if (Time.time >= nextVortexTick)
        {
            nextVortexTick = Time.time + 0.15f;
            foreach (ResolvedGeneEffect effect in snapshot.effects)
                if (effect.Kind == GeneEffectKind.Vortex && effect.Evolved)
                    PullEnemies(rb.position, effect.Value, snapshot.vortexSpeed * 0.15f);
        }
        foreach (ElementArea area in new List<ElementArea>(ElementArea.Active))
            if (area != null && Vector2.Distance(area.transform.position, rb.position) <= area.Radius)
                area.React(snapshot.element, snapshot);
    }
    protected override void HandleCollision(Collider2D other)
    {
        if (other == null) return;
        Vector2 normal = ((Vector2)transform.position - (Vector2)other.bounds.center).normalized;
        Resolve(other, other.ClosestPoint(transform.position), normal);
    }
    private bool Resolve(Collider2D other, Vector2 point, Vector2 normal)
    {
        if (!launched || spent || other == null || other == hitbox || other.isTrigger ||
            (owner != null && other.transform.IsChildOf(owner))) return false;
        IActor actor = other.GetComponentInParent<IActor>();
        if (actor != null)
        {
            if ((targetLM.value & (1 << other.gameObject.layer)) == 0 || !damaged.Add(actor)) return false;
            actor.TakeDamage(CalculateDamage());
            CombatStatus.Get(other)?.Apply(snapshot.element, snapshot);
            ProcessEffects(GeneTrigger.OnHit, actor, point, normal);
            if ((snapshot.element & CombatElement.Steam) != 0 || remainingPierce-- > 0) return false;
            Explode();
            return true;
        }
        if ((groundLM.value & (1 << other.gameObject.layer)) == 0) return false;
        if (other.GetInstanceID() == lastWall && Time.time < lastWallUntil) return false;
        if ((snapshot.element & CombatElement.Steam) != 0) return false;
        ProcessEffects(GeneTrigger.OnHit, null, point, normal);
        if (remainingBounces > 0)
        {
            remainingBounces--;
            direction = Vector2.Reflect(direction, normal).normalized;
            travelSpeed *= snapshot.reboundRetention;
            if (travelSpeed < 0.1f) { Explode(); return true; }
            lastWall = other.GetInstanceID();
            lastWallUntil = Time.time + 0.06f;
            return true;
        }
        Explode();
        return true;
    }
    private void ProcessEffects(GeneTrigger trigger, IActor actor, Vector2 point, Vector2 normal)
    {
        if (snapshot == null) return;
        if (trigger == GeneTrigger.OnHit)
        {
            if ((snapshot.element & CombatElement.Poison) != 0)
                ElementArea.Create(CombatElement.Poison, point, snapshot.poisonRadius, snapshot.poisonDuration, snapshot);
            else if ((snapshot.element & CombatElement.Water) != 0)
                ElementArea.Create(CombatElement.Water, point, snapshot.waterRadius, snapshot.waterDuration, snapshot);
        }
        foreach (ResolvedGeneEffect effect in snapshot.effects)
        {
            if (effect.Trigger != trigger) continue;
            if (effect.Kind == GeneEffectKind.Split) Split(effect, actor, point, actor == null ? normal : direction);
            if (effect.Kind == GeneEffectKind.Spread && actor != null)
            {
                var processed = new HashSet<CombatStatus>();
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(point, effect.Value, targetLM))
                {
                    CombatStatus status = CombatStatus.Get(collider);
                    if (status != null && processed.Add(status)) status.Apply(snapshot.element, snapshot);
                }
            }
            if (effect.Kind == GeneEffectKind.Vortex && actor != null && !effect.Evolved)
                ElementArea.Create(CombatElement.Water, point, effect.Value, snapshot.vortexDuration, snapshot, true);
        }
    }
    private void Split(ResolvedGeneEffect effect, IActor previous, Vector2 point, Vector2 heading)
    {
        if (split || generation >= snapshot.maxGeneration || template == null || budget == null) return;
        split = true;
        int count = Mathf.Clamp(Mathf.RoundToInt(effect.Value), 2, 12);
        for (int i = 0; i < count && budget.Take(); i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, Mathf.Lerp(-35f, 35f, i / (float)(count - 1))) * heading;
            WaterProjectile child = Instantiate(template, point + dir * 0.15f, Quaternion.identity).GetComponent<WaterProjectile>();
            child.Launch(snapshot, point + dir * 0.15f, dir, targetLM, owner, template, budget, generation + 1, previous);
        }
    }
    private void PullEnemies(Vector2 center, float radius, float distance)
    {
        var used = new HashSet<EnemyAIController>();
        foreach (Collider2D collider in Physics2D.OverlapBoxAll(center, new Vector2(radius * 2f, radius * 0.67f), 0f, targetLM))
        {
            EnemyAIController enemy = collider.GetComponentInParent<EnemyAIController>();
            if (enemy == null || enemy.IsDead || !used.Add(enemy)) continue;
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.MovePosition(Vector2.MoveTowards(body.position, center, distance));
        }
    }
    protected override void Explode()
    {
        if (spent) return;
        spent = true;
        if (snapshot != null)
        {
            ProcessEffects(GeneTrigger.OnDespawn, null, transform.position, -direction);
            TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, snapshot.color, snapshot.size * 0.4f, -direction);
        }
        rb.linearVelocity = Vector2.zero;
        hitbox.enabled = false;
        TapGJ.WaterProjectiles.WaterVfx.ReleaseTrail(transform);
        base.Explode();
    }
}
