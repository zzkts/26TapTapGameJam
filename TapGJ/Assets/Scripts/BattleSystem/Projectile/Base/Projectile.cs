using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家子弹的通用运行时实体。
/// 统一处理轨迹、手动物理检测、伤害、元素附着、穿透、反弹和生命周期。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected LayerMask groundLM;
    [SerializeField] protected E_ElementType elementType;

    private const int HitBufferSize = 16;
    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[HitBufferSize];
    private readonly HashSet<int> damagedTargets = new();

    private Collider2D projectileCollider;
    private ContactFilter2D hitFilter;

    protected float speed;
    protected float damage;
    protected float projectileSizeMultiplier;
    protected float damageMultiplier;
    protected float lifeTime;
    protected int pierceCount;
    protected int reboundCount;
    protected float reboundCoefficient;
    protected Vector2 direction;
    protected LayerMask targetLM;
    protected bool isExpired;
    protected Transform owner;
    private ProjectileMove projectileMove;
    private BulletSnapshot snapshot;
    private ShotSpawnBudget spawnBudget;
    private int generation;
    private bool hasSplit;
    private MaterialPropertyBlock visualProperties;
    private SpriteRenderer visual;
    public BulletSnapshot Snapshot => snapshot;
    public int Generation => generation;
    public E_ElementType ElementType => elementType;

    public void LaunchSnapshot(Vector2 position, Vector2 heading, LayerMask targets, Transform source,
        BulletSnapshot shot, ShotSpawnBudget budget, int splitGeneration = 0)
    {
        snapshot = shot.Copy(); spawnBudget = budget; generation = splitGeneration;
        SetOwner(source);
        elementType = snapshot.element;
        SetUp(position, heading, targets, shot.speed, shot.damage, shot.damageMultiplier, shot.size,
            shot.lifeTime, shot.pierceCount, shot.reboundCount, shot.reboundCoefficient,
            shot.moveType, shot.orbitRadius, shot.orbitAngularSpeed);
        visual = GetComponentInChildren<SpriteRenderer>();
        if (Application.isPlaying && visual != null && GetComponentInChildren<TrailRenderer>() == null)
            TapGJ.WaterProjectiles.WaterVfx.AddRibbon(transform,
                elementType == E_ElementType.Fire ? new Color(1f,.5f,.1f) :
                elementType == E_ElementType.Poison ? new Color(.5f,.9f,.2f) : new Color(.2f,.8f,1f), .1f, visual.sortingOrder - 1);
        Material material = Resources.Load<Material>("Material/Combat/ElementSprite");
        if (visual != null && material != null)
        {
            visual.sharedMaterial = material; visualProperties = new MaterialPropertyBlock();
            visualProperties.SetFloat("_Mode", 1f);
            visualProperties.SetFloat("_Element", (float)elementType);
            visual.SetPropertyBlock(visualProperties);
        }
    }

    internal Vector2 Position => rb.position;
    internal Vector2 Direction => direction;
    internal float Speed => speed;
    internal Transform Owner => owner;

    public void SetOwner(Transform owner)
    {
        this.owner = owner;
    }

    public virtual void SetUp(Vector2 pos, Vector2 dir, LayerMask targetLM,
        float speed, float damage, float damageMultiplier, float projectileSizeMultiplier, float lifeTime)
    {
        SetUp(pos, dir, targetLM, speed, damage, damageMultiplier,
            projectileSizeMultiplier, lifeTime, 0, 0, 1f);
    }

    public virtual void SetUp(Vector2 pos, Vector2 dir, LayerMask targetLM,
        float speed, float damage, float damageMultiplier, float projectileSizeMultiplier,
        float lifeTime, int pierceCount, int reboundCount, float reboundCoefficient)
    {
        SetUp(pos, dir, targetLM, speed, damage, damageMultiplier,
            projectileSizeMultiplier, lifeTime, pierceCount, reboundCount,
            reboundCoefficient, E_ProjectileMoveType.Straight, 2f, 180f);
    }

    public virtual void SetUp(Vector2 pos, Vector2 dir, LayerMask targetLM,
        float speed, float damage, float damageMultiplier, float projectileSizeMultiplier,
        float lifeTime, int pierceCount, int reboundCount, float reboundCoefficient,
        E_ProjectileMoveType moveType, float orbitRadius, float orbitAngularSpeed)
    {
        transform.position = pos;
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.damageMultiplier = damageMultiplier;
        this.projectileSizeMultiplier = projectileSizeMultiplier;
        this.targetLM = targetLM;
        this.lifeTime = lifeTime;
        this.pierceCount = pierceCount;
        this.reboundCount = reboundCount;
        this.reboundCoefficient = reboundCoefficient;
        isExpired = false;
        hasSplit = false;

        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        projectileCollider.isTrigger = true;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.position = pos;
        rb.linearVelocity = Vector2.zero;
        damagedTargets.Clear();

        hitFilter = new ContactFilter2D
        {
            useTriggers = true,
        };
        hitFilter.SetLayerMask(targetLM | groundLM);

        transform.localScale = Vector3.one * projectileSizeMultiplier;
        projectileMove = moveType == E_ProjectileMoveType.Orbit
            ? new OrbitMove(orbitRadius, orbitAngularSpeed)
            : new StraightMove();
        projectileMove.SetUp(this);
    }

    /// <summary>
    /// 根据规则计算最终伤害
    /// </summary>
    protected virtual float CalculateDamage()
    {
        return damage * damageMultiplier;
    }

    protected void DealDamage(Collider2D collision, float amount)
    {
        IActor actor = collision.GetComponentInParent<IActor>();
        if (actor is ICombatable combatable)
            combatable.TakeDamage(new DamageInfo(amount, elementType,
                collision.bounds.center, targetLM));
        else
            actor?.TakeDamage(amount);
    }

    protected virtual void Move()
    {
        projectileMove.Move(this, Time.fixedDeltaTime);
    }

    protected virtual void HandleCollision(Collider2D collision)
    {
        DealDamage(collision, CalculateDamage());
        if (snapshot != null)
        {
            if (snapshot.effectRadius > 0f && elementType != E_ElementType.None)
                ElementField.ApplyAround(collision.bounds.center, snapshot.effectRadius, elementType, targetLM);
            if (snapshot.vortex)
                ElementField.Create(E_ElementType.Water, collision.bounds.center, new Vector2(3f, 1f), 2f, targetLM, true);
            Split(rb.position);
        }
    }

    public void Step(float deltaTime)
    {
        if (!isExpired && projectileMove != null) projectileMove.Move(this, deltaTime);
    }

    internal void MoveForward(float deltaTime)
    {
        MoveWithPhysics(deltaTime);
    }

    internal void MoveBy(Vector2 movement)
    {
        MoveWithPhysics(movement);
    }

    protected void MoveWithPhysics(float deltaTime)
    {
        MoveWithPhysics(direction * speed * deltaTime);
    }

    protected void MoveWithPhysics(Vector2 movement)
    {
        float moveDistance = movement.magnitude;
        if (moveDistance <= 0f)
            return;

        Vector2 moveDirection = movement / moveDistance;
        Vector2 start = rb.position;
        direction = moveDirection;
        int hitCount = projectileCollider.Cast(moveDirection, hitFilter, hitBuffer, moveDistance);
        SortHitsByDistance(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hitBuffer[i];
            if (hit.collider == null || hit.collider == projectileCollider ||
                (owner != null && hit.collider.transform.IsChildOf(owner))) continue;
            int layer = 1 << hit.collider.gameObject.layer;
            IActor actor = hit.collider.GetComponentInParent<IActor>();

            if ((groundLM.value & layer) != 0 && actor == null && !hit.collider.isTrigger)
            {
                rb.position = start + moveDirection * hit.distance;

                if (reboundCount > 0)
                {
                    direction = Vector2.Reflect(moveDirection, hit.normal).normalized;
                    speed *= reboundCoefficient;
                    reboundCount--;
                    return;
                }

                Explode();
                return;
            }

            if ((targetLM.value & layer) == 0)
                continue;
            if (actor == null) continue;

            int targetId = actor is Object actorObject
                ? actorObject.GetInstanceID()
                : hit.collider.GetInstanceID();
            if (!damagedTargets.Add(targetId))
                continue;

            rb.position = start + moveDirection * hit.distance;
            HandleCollision(hit.collider);
            if (isExpired)
                return;

            if (pierceCount > 0)
            {
                pierceCount--;
                continue;
            }

            Explode();
            return;
        }

        rb.position = start + moveDirection * moveDistance;
    }

    private void SortHitsByDistance(int hitCount)
    {
        for (int i = 1; i < hitCount; i++)
        {
            RaycastHit2D current = hitBuffer[i];
            int index = i - 1;

            while (index >= 0 && hitBuffer[index].distance > current.distance)
            {
                hitBuffer[index + 1] = hitBuffer[index];
                index--;
            }

            hitBuffer[index + 1] = current;
        }
    }

    protected void FixedUpdate()
    {
        if (!isExpired && projectileMove != null) Move();
        if (snapshot != null && snapshot.movingVortex)
            ElementField.Pull(transform.position, new Vector2(3f, 1f), targetLM, 2f * Time.fixedDeltaTime);
    }

    protected void Update()
    {
        if (isExpired)
            return;

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Explode();
            return;
        }
    }

    protected virtual void Explode()
    {
        if (isExpired) return;
        if (Application.isPlaying)
            TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(rb.position,
                elementType == E_ElementType.Fire ? new Color(1f, 0.45f, 0.1f) :
                elementType == E_ElementType.Poison ? new Color(0.5f, 0.9f, 0.2f) : Color.cyan, 0.3f, -direction);
        if (snapshot != null)
        {
            if (elementType == E_ElementType.Poison)
                ElementField.Create(E_ElementType.Poison, rb.position, Vector2.one * 5f, 3f, targetLM);
            else if (elementType == E_ElementType.Water)
                ElementField.Create(E_ElementType.Water, rb.position, new Vector2(1.5f, 0.2f), 4f, targetLM);
            Split(rb.position);
        }
        isExpired = true;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void Split(Vector2 position)
    {
        if (hasSplit || snapshot == null || snapshot.splitCount <= 0 || generation >= snapshot.maxGeneration || spawnBudget == null) return;
        hasSplit = true;
        for (int i = 0; i < snapshot.splitCount && spawnBudget.Take(); i++)
        {
            float angle = snapshot.splitCount == 1 ? 0f : Mathf.Lerp(-35f, 35f, i / (float)(snapshot.splitCount - 1));
            Vector2 heading = Quaternion.Euler(0f, 0f, angle) * direction;
            Vector2 origin = position + heading * 0.03f;
            Projectile child = Instantiate(snapshot.projectilePrefab, origin, Quaternion.identity).GetComponent<Projectile>();
            if (child != null)
            {
                child.LaunchSnapshot(origin, heading, targetLM, owner, snapshot, spawnBudget, generation + 1);
                // A split belongs to the same shot and cannot re-hit the parent impact target.
                child.damagedTargets.UnionWith(damagedTargets);
            }
        }
    }
}
