using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    private float speed;
    private float damage;
    private float damageMultiplier;
    private float lifeTime;
    private Vector2 direction;
    private LayerMask targetLM;

    private static LayerMask groundLM;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        groundLM = LayerMask.GetMask("Ground");
    }

    public void SetUp(
        Vector2 pos,
        Vector2 dir,
        LayerMask targetLM,
        float spd,
        float dmg,
        float damageMultiplier,
        float projectileSizeMultiplier,
        float lifeTime)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        transform.position = pos;
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        this.damageMultiplier = damageMultiplier;
        this.targetLM = targetLM;
        this.lifeTime = lifeTime;
        transform.localScale *= projectileSizeMultiplier;
        rb.linearVelocity = direction * speed;
    }

    public float CalculateDamage()
    {
        return damage * damageMultiplier;
    }

    private void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    private void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
        }

        // TODO : 碰撞检测(碰撞墙体直接销毁，碰撞目标触发爆炸)
    }

    private void Explode()
    {
        Destroy(gameObject);
    }
}
