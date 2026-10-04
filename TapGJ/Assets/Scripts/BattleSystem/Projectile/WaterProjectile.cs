using UnityEngine;

public class WaterProjectile : Projectile
{
    // TODO : 粒子效果
    protected override void HandleCollision(Collider2D collision)
    {
        if((targetLM.value & (1 << collision.gameObject.layer)) != 0)
        {
            collision.GetComponent<IActor>().TakeDamage(CalculateDamage());
            Explode();
        }
        else if ((groundLM.value & (1 << collision.gameObject.layer)) != 0)
        {
            Explode();
        }
    }

    protected override void Move()
    {
        rb.linearVelocity = direction * speed;
    }
}
