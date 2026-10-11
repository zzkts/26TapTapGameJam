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

        rb = GetComponent<Rigidbody2D>();
        projectileCollider = GetComponent<Collider2D>();
        projectileCollider.isTrigger = true;
        rb.gravityScale = 0f;
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
            actor.TakeDamage(amount);
    }

    protected virtual void Move()
    {
        projectileMove.Move(this, Time.fixedDeltaTime);
    }

    protected virtual void HandleCollision(Collider2D collision)
    {
        DealDamage(collision, CalculateDamage());
    }

    public void Step(float deltaTime)
    {
        projectileMove.Move(this, deltaTime);
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
        direction = moveDirection;
        int hitCount = projectileCollider.Cast(moveDirection, hitFilter, hitBuffer, moveDistance);
        SortHitsByDistance(hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hitBuffer[i];
            int layer = 1 << hit.collider.gameObject.layer;

            if ((groundLM.value & layer) != 0)
            {
                rb.position += moveDirection * hit.distance;

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

            IActor actor = hit.collider.GetComponentInParent<IActor>();
            int targetId = actor is Object actorObject
                ? actorObject.GetInstanceID()
                : hit.collider.GetInstanceID();
            if (!damagedTargets.Add(targetId))
                continue;

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

        rb.position += moveDirection * moveDistance;
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
        Move();
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
        isExpired = true;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject);
    }
}
