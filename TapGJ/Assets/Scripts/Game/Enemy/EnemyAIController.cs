using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyAIController : MonoBehaviour, IActor
{
    public enum AttackType { Melee, Ranged }
    public enum EnemyState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    [Header("Behavior")]
    [SerializeField] private AttackType attackType;
    [SerializeField] private bool patrolEnemy = true;
    [SerializeField] private bool chaseEnemy = true;
    [SerializeField] private bool flyingEnemy;
    [SerializeField] private Transform playerTarget;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float patrolRange = 5f;
    [SerializeField, Min(0f)] private float patrolWait = 2f;
    [SerializeField, Min(0f)] private float patrolSpeed = 2f;
    [SerializeField, Min(0f)] private float chaseSpeed = 3f;
    [SerializeField, Min(0f)] private float detectRange = 10f;
    [SerializeField, Min(1f)] private float loseTargetMultiplier = 1.25f;
    [SerializeField] private LayerMask obstacleMask = (1 << 0) | (1 << 3) | (1 << 9);
    [SerializeField, Min(0.01f)] private float obstacleCheckDistance = 0.18f;
    [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.25f;

    [Header("Attack")]
    [Tooltip("Distance between colliders, not between sprite pivots.")]
    [SerializeField, Min(0.01f)] private float attackRange = 0.45f;
    [SerializeField, Min(0f)] private float attackWindup = 0.3f;
    [SerializeField, Min(0.01f)] private float attackDuration = 0.8f;
    [SerializeField, Min(0.01f)] private float attackCooldown = 1.5f;
    [SerializeField, Min(0f)] private float meleeDamage = 1f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1f;

    [Header("Ranged Attack")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private JellyfishProjectile projectilePrefab;
    [SerializeField, Min(0f)] private float bulletDamage = 1f;
    [SerializeField, Min(1)] private int bulletCount = 1;
    [SerializeField, Min(0.01f)] private float bulletSize = 0.06f;
    [SerializeField, Min(0f)] private float bulletSpeed = 5f;
    [SerializeField, Range(0f, 90f)] private float spreadAngle = 15f;

    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHp = 3f;
    [SerializeField, Min(0f)] private float hurtDuration = 0.35f;
    [SerializeField, Min(0f)] private float deathDuration = 0.9f;

    [Header("Animation States")]
    [SerializeField] private string patrolAnimation = "Run";
    [SerializeField] private string chaseAnimation = "Run";

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Collider2D targetCollider;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Color originalColor;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
    private ContactFilter2D environmentFilter;
    private Vector2 desiredVelocity;
    private Vector2 aimPosition;
    private float patrolCenter;
    private float patrolTarget;
    private float waitTimer;
    private float cooldownTimer;
    private float attackElapsed;
    private float hurtTimer;
    private float nextTargetSearch;
    private float facingDirection = 1f;
    private bool attackApplied;
    private bool alerted;

    public AttackType Kind => attackType;
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public float Hp { get; private set; }
    public bool IsDead => State == EnemyState.Dead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        environmentFilter = new ContactFilter2D { useTriggers = false };
        environmentFilter.SetLayerMask(obstacleMask);
        rb.freezeRotation = true;
        if (flyingEnemy)
            rb.gravityScale = 0f;
        Hp = maxHp;
        ResetPatrol();
        SetTarget(playerTarget);
        SetState(EnemyState.Idle, true);
    }

    private void Update()
    {
        if (IsDead)
            return;

        desiredVelocity = Vector2.zero;
        cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);
        if (playerTarget == null && Time.time >= nextTargetSearch)
            FindPlayer();

        if (hurtTimer > 0f)
        {
            hurtTimer -= Time.deltaTime;
            return;
        }
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (State == EnemyState.Attack)
        {
            UpdateAttack();
            return;
        }

        float detectionDistance = detectRange * (alerted ? loseTargetMultiplier : 1f);
        bool inRange = TargetAvailable() &&
            ((Vector2)playerTarget.position - (Vector2)transform.position).sqrMagnitude <= detectionDistance * detectionDistance;
        if (inRange && (alerted || HasLineOfSight()))
        {
            alerted = true;
            Face(TargetPosition().x - bodyCollider.bounds.center.x);
            if (CanAttack())
            {
                if (cooldownTimer <= 0f)
                    BeginAttack();
                else
                    SetState(EnemyState.Idle);
                return;
            }

            Vector2 delta = TargetPosition() - (Vector2)bodyCollider.bounds.center;
            Vector2 direction = flyingEnemy ? delta.normalized : new Vector2(Mathf.Sign(delta.x), 0f);
            if (chaseEnemy && delta.sqrMagnitude > 0.0001f && CanMove(direction))
                desiredVelocity = direction * Mathf.Min(chaseSpeed, delta.magnitude / Time.fixedDeltaTime);
            SetState(desiredVelocity == Vector2.zero ? EnemyState.Idle : EnemyState.Chase);
            return;
        }

        if (alerted)
        {
            alerted = false;
            ResetPatrol();
        }
        if (patrolEnemy)
            Patrol();
        SetState(desiredVelocity == Vector2.zero ? EnemyState.Idle : EnemyState.Patrol);
    }

    private void FixedUpdate()
    {
        if (!IsDead)
            rb.linearVelocity = new Vector2(desiredVelocity.x, flyingEnemy ? desiredVelocity.y : rb.linearVelocity.y);
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
        targetCollider = target != null ? target.GetComponentInChildren<Collider2D>() : null;
    }

    private void FindPlayer()
    {
        nextTargetSearch = Time.time + 1f;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        GameObject target = player != null ? player.gameObject : GameObject.Find("Player");
        SetTarget(target != null ? target.transform : null);
    }

    private bool TargetAvailable()
    {
        if (playerTarget == null || !playerTarget.gameObject.activeInHierarchy)
            return false;
        PlayerController player = playerTarget.GetComponent<PlayerController>();
        return player == null || player.hp > 0f;
    }

    private Vector2 TargetPosition() => targetCollider != null ? targetCollider.bounds.center : playerTarget.position;

    private bool CanAttack()
    {
        if (!TargetAvailable() || (attackType == AttackType.Ranged && projectilePrefab == null))
            return false;
        float distance = targetCollider != null && targetCollider.enabled
            ? bodyCollider.Distance(targetCollider).distance
            : Vector2.Distance(bodyCollider.ClosestPoint(TargetPosition()), TargetPosition());
        if (distance > attackRange || !HasLineOfSight())
            return false;
        if (attackType == AttackType.Melee && targetCollider != null)
        {
            Bounds a = bodyCollider.bounds;
            Bounds b = targetCollider.bounds;
            return a.max.y > b.min.y && a.min.y < b.max.y;
        }
        return true;
    }

    private void BeginAttack()
    {
        aimPosition = TargetPosition();
        attackElapsed = 0f;
        attackApplied = false;
        cooldownTimer = attackCooldown;
        StopMoving();
        SetState(EnemyState.Attack, true);
    }

    private void UpdateAttack()
    {
        attackElapsed += Time.deltaTime;
        if (!attackApplied && attackElapsed >= attackWindup)
        {
            attackApplied = true;
            // Recheck at the hit frame so moving away or taking cover can avoid the attack.
            if (CanAttack() && (TargetPosition().x - bodyCollider.bounds.center.x) * facingDirection >= -0.01f)
            {
                if (attackType == AttackType.Ranged)
                    Shoot();
                else
                    playerTarget.GetComponentInParent<IActor>()?.TakeDamage(meleeDamage * damageMultiplier);
            }
        }
        if (attackElapsed >= Mathf.Max(attackDuration, attackWindup))
            SetState(EnemyState.Idle);
    }

    private void Shoot()
    {
        Vector2 origin = firePoint != null ? firePoint.position : bodyCollider.bounds.center;
        Vector2 direction = (aimPosition - origin).normalized;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right * facingDirection;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = bulletCount == 1 ? 0f : Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(bulletCount - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            JellyfishProjectile bullet = Instantiate(projectilePrefab, origin, Quaternion.identity);
            bullet.Launch(this, playerTarget, origin, shotDirection, obstacleMask,
                bulletSpeed, bulletDamage * damageMultiplier, bulletSize);
        }
    }

    private void Patrol()
    {
        if (patrolRange <= 0f || patrolSpeed <= 0f)
            return;
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }
        float delta = patrolTarget - transform.position.x;
        Vector2 direction = Vector2.right * Mathf.Sign(delta);
        if (Mathf.Abs(delta) <= 0.08f || !CanMove(direction))
        {
            patrolTarget = patrolTarget > patrolCenter ? patrolCenter - patrolRange : patrolCenter + patrolRange;
            waitTimer = patrolWait;
            return;
        }
        Face(delta);
        desiredVelocity = direction * Mathf.Min(patrolSpeed, Mathf.Abs(delta) / Time.fixedDeltaTime);
    }

    private bool CanMove(Vector2 direction)
    {
        Bounds bounds = bodyCollider.bounds;
        int count = Physics2D.BoxCast(bounds.center, (Vector2)bounds.size * 0.85f, 0f,
            direction, environmentFilter, hits, obstacleCheckDistance + chaseSpeed * Time.fixedDeltaTime);
        for (int i = 0; i < count; i++)
            if (IsEnvironment(hits[i].collider))
                return false;
        if (flyingEnemy)
            return true;

        Vector2 origin = new Vector2(bounds.center.x + Mathf.Sign(direction.x) * (bounds.extents.x + 0.08f), bounds.min.y + 0.05f);
        count = Physics2D.Raycast(origin, Vector2.down, environmentFilter, hits, groundCheckDistance);
        for (int i = 0; i < count; i++)
            if (IsEnvironment(hits[i].collider))
                return true;
        return false;
    }

    private bool HasLineOfSight()
    {
        Vector2 origin = bodyCollider.bounds.center;
        Vector2 delta = TargetPosition() - origin;
        int count = Physics2D.Raycast(origin, delta.normalized, environmentFilter, hits, delta.magnitude);
        for (int i = 0; i < count; i++)
            if (IsEnvironment(hits[i].collider))
                return false;
        return true;
    }

    private bool IsEnvironment(Collider2D other)
    {
        // The current map and Player share Default, so filter objects rather than excluding that layer.
        return other != null && !other.isTrigger && other != bodyCollider &&
            !other.transform.IsChildOf(transform) &&
            (playerTarget == null || !other.transform.IsChildOf(playerTarget)) &&
            other.GetComponentInParent<EnemyAIController>() == null;
    }

    private void Face(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;
        facingDirection = Mathf.Sign(direction);
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingDirection < 0f;
        if (firePoint != null && firePoint.IsChildOf(transform))
        {
            Vector3 position = firePoint.localPosition;
            position.x = Mathf.Abs(position.x) * facingDirection;
            firePoint.localPosition = position;
        }
    }

    private void SetState(EnemyState nextState, bool restart = false)
    {
        if (State == nextState && !restart)
            return;
        State = nextState;
        if (animator == null || animator.runtimeAnimatorController == null)
            return;
        string animationName = nextState switch
        {
            EnemyState.Patrol => patrolAnimation,
            EnemyState.Chase => chaseAnimation,
            _ => nextState.ToString()
        };
        int hash = Animator.StringToHash(animationName);
        if (animator.HasState(0, hash))
            animator.Play(hash, 0, 0f);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;
        Hp = Mathf.Max(0f, Hp - damage);
        StopMoving();
        if (Hp <= 0f)
        {
            Die();
            return;
        }
        attackApplied = true;
        cooldownTimer = Mathf.Max(cooldownTimer, hurtDuration + 0.2f);
        hurtTimer = hurtDuration;
        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.45f, 0.45f, originalColor.a);
        SetState(EnemyState.Hurt, true);
    }

    private void ResetPatrol()
    {
        patrolCenter = transform.position.x;
        patrolTarget = patrolCenter + patrolRange;
        waitTimer = 0f;
    }

    private void StopMoving()
    {
        desiredVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(0f, flyingEnemy ? 0f : rb.linearVelocity.y);
    }

    private void Die()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        SetState(EnemyState.Dead);
        float duration = deathDuration;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Update(0f);
            duration = Mathf.Max(duration, animator.GetCurrentAnimatorStateInfo(0).length);
        }
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        Destroy(gameObject, duration);
    }
}
