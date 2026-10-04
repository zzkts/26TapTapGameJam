using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected LayerMask groundLM;

    protected float speed;
    protected float damage;
    protected float projectileSizeMultiplier;
    protected float damageMultiplier;
    protected float lifeTime;
    protected Vector2 direction;
    protected LayerMask targetLM;

    public virtual void SetUp(Vector2 pos, Vector2 dir, LayerMask targetLM,
        float speed, float damage, float damageMultiplier, float projectileSizeMultiplier, float lifeTime)
    {
        transform.position = pos;
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.damageMultiplier = damageMultiplier;
        this.projectileSizeMultiplier = projectileSizeMultiplier;
        this.targetLM = targetLM;
        this.lifeTime = lifeTime;
        rb.linearVelocity = direction * this.speed;

        transform.localScale = Vector3.one * projectileSizeMultiplier;
    }

    /// <summary>
    /// 根据规则计算最终伤害
    /// </summary>
    protected virtual float CalculateDamage()
    {
        return damage * damageMultiplier;
    }

    protected abstract void Move();
    protected abstract void HandleCollision(Collider2D collision);

    protected void FixedUpdate()
    {
        Move();
    }

    protected void Update()
    {
        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Explode();
            return;
        }
    }

    protected void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    protected virtual void Explode()
    {
        Destroy(gameObject);
    }
}
