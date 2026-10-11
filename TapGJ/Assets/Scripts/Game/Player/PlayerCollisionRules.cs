using UnityEngine;

public class PlayerCollisionRules : MonoBehaviour
{
    private int player, invulnerable, enemy, bullets;
    private bool previousPlayer, previousInvulnerable, previousBullets;
    private void OnEnable()
    {
        player = LayerMask.NameToLayer("Player");
        invulnerable = LayerMask.NameToLayer("PlayerInvulnerable");
        enemy = LayerMask.NameToLayer("Enemy");
        bullets = LayerMask.NameToLayer("EnemyBullet");
        if (player < 0 || invulnerable < 0 || enemy < 0 || bullets < 0) return;
        previousPlayer = Physics2D.GetIgnoreLayerCollision(player, enemy);
        previousInvulnerable = Physics2D.GetIgnoreLayerCollision(invulnerable, enemy);
        previousBullets = Physics2D.GetIgnoreLayerCollision(invulnerable, bullets);
        Physics2D.IgnoreLayerCollision(player, enemy, true);
        Physics2D.IgnoreLayerCollision(invulnerable, enemy, true);
        Physics2D.IgnoreLayerCollision(invulnerable, bullets, true);
    }
    private void OnDisable()
    {
        if (player < 0 || invulnerable < 0 || enemy < 0 || bullets < 0) return;
        Physics2D.IgnoreLayerCollision(player, enemy, previousPlayer);
        Physics2D.IgnoreLayerCollision(invulnerable, enemy, previousInvulnerable);
        Physics2D.IgnoreLayerCollision(invulnerable, bullets, previousBullets);
    }
}
