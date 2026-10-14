using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class JellyfishProjectile : Projectile
{
    private EnemyAIController enemyOwner;
    private Transform target;
    private CircleCollider2D hitCollider;
    private ContactFilter2D enemyHitFilter;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private bool spent;
    private bool launched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<CircleCollider2D>();
        hitCollider.isTrigger = true;
        rb.gravityScale = 0f;
    }

    public void Launch(EnemyAIController source, Transform victim, Vector2 position, Vector2 heading,
        LayerMask environment, float velocity, float hitDamage, float size, float lifetime = 5f)
    {
        enemyOwner = source;
        target = victim;
        groundLM = environment;
        enemyHitFilter = new ContactFilter2D { useTriggers = true };
        int targetLayer = victim != null ? 1 << victim.gameObject.layer : 0;
        enemyHitFilter.SetLayerMask(environment | targetLayer | LayerMask.GetMask("Player"));
        SetUp(position, heading, 0, velocity, hitDamage, 1f, size, lifetime);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
        launched = true;
    }

    protected override void Move()
    {
        if (!launched || spent)
            return;
        // Sweep the next physics step to catch thin walls and fast projectiles.
        Vector2 origin = transform.TransformPoint(hitCollider.offset);
        float radius = hitCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        int count = Physics2D.CircleCast(origin, radius, direction, enemyHitFilter, hits, speed * Time.fixedDeltaTime);
        for (int i = 0; i < count && !spent; i++)
            HandleCollision(hits[i].collider);
        if (!spent)
            rb.linearVelocity = direction * speed;
    }

    protected override void HandleCollision(Collider2D other)
    {
        if (!launched || spent || other == null || other == hitCollider ||
            (enemyOwner != null && other.transform.IsChildOf(enemyOwner.transform)) ||
            other.GetComponentInParent<EnemyAIController>() != null)
            return;

        if (target != null && other.transform.IsChildOf(target))
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsInvulnerable) return;
            IActor actor = other.GetComponentInParent<IActor>();
            Explode();
            if (player != null) player.TakeDamage(new DamageInfo(CalculateDamage(), elementType, transform.position, LayerMask.GetMask("Player")));
            else actor?.TakeDamage(CalculateDamage());
        }
        else if (!other.isTrigger && (groundLM.value & (1 << other.gameObject.layer)) != 0)
        {
            Explode();
        }
    }

    protected override void Explode()
    {
        if (spent)
            return;
        spent = true;
        rb.linearVelocity = Vector2.zero;
        hitCollider.enabled = false;
        base.Explode();
    }
}
