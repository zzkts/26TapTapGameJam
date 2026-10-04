using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAIController : MonoBehaviour, IActor
{
    [Header("类型")]
    [SerializeField] private bool patrolEnemy;
    [SerializeField] private bool chaseEnemy;

    [Header("移动")]
    [SerializeField] private float patrolRange = 5f;
    [SerializeField] private float patrolWait = 2f;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float detectRange = 10f;

    [Header("攻击")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float bulletDamage = 1f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private int bulletCount = 1;
    [SerializeField] private float bulletSize = 1f;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float spreadAngle = 15f;

    [Header("生命")]
    [SerializeField] private float hp = 3f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private PlayerController player;
    private float patrolCenter;
    private float patrolTarget;
    private float waitTimer;
    private float attackTimer;
    private float horizontalSpeed;
    private bool wasInRange;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        player = FindFirstObjectByType<PlayerController>();
        if (projectilePrefab == null)
            projectilePrefab = Resources.Load<Projectile>("Prefab/Projectile/Projectile");

        ResetPatrol(transform.position.x);
    }

    private void Update()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        bool inRange = player != null &&
            (player.transform.position - transform.position).sqrMagnitude <= detectRange * detectRange;

        if (wasInRange && !inRange)
            ResetPatrol(transform.position.x);
        wasInRange = inRange;

        horizontalSpeed = 0f;
        if (inRange && chaseEnemy)
        {
            float deltaX = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.05f)
                horizontalSpeed = Mathf.Sign(deltaX) * chaseSpeed;
        }
        else if (!inRange && (patrolEnemy || chaseEnemy))
        {
            horizontalSpeed = Patrol();
        }

        if (spriteRenderer != null && Mathf.Abs(horizontalSpeed) > 0.01f)
            spriteRenderer.flipX = horizontalSpeed < 0f;

        if (inRange)
            Shoot();
        else
            attackTimer = 0f;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(horizontalSpeed, rb.linearVelocity.y);
    }

    private float Patrol()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return 0f;
        }

        if (Mathf.Abs(patrolTarget - transform.position.x) <= 0.05f)
        {
            patrolTarget = patrolTarget > patrolCenter ? patrolCenter - patrolRange : patrolCenter + patrolRange;
            waitTimer = patrolWait;
            return 0f;
        }

        return Mathf.Sign(patrolTarget - transform.position.x) * patrolSpeed;
    }

    private void ResetPatrol(float center)
    {
        patrolCenter = center;
        patrolTarget = center + patrolRange;
        waitTimer = 0f;
    }

    private void Shoot()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f || projectilePrefab == null)
            return;

        Vector2 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = (Vector2)player.transform.position - origin;
        LayerMask playerLayer = 1 << player.gameObject.layer;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = bulletCount == 1 ? 0f : Mathf.Lerp(-spreadAngle, spreadAngle, i / (float)(bulletCount - 1));
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction.normalized;
            Projectile bullet = Instantiate(projectilePrefab, origin, Quaternion.identity);
            bullet.SetUp(origin, shotDirection, playerLayer, bulletSpeed, bulletDamage,
                damageMultiplier, bulletSize, 5f);

            SpriteRenderer bulletRenderer = bullet.GetComponent<SpriteRenderer>();
            if (bulletRenderer != null)
                bulletRenderer.color = Color.red;
        }

        attackTimer = attackCooldown;
    }

    public void TakeDamage(float damage)
    {
        hp -= damage;
        if (hp <= 0f)
            Destroy(gameObject);
    }
}
