using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class CompleteWorldHazard : MonoBehaviour
{
    public float damage = 1f, interval = 1.2f;
    private float nextHit;
    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < nextHit) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead || player.MenuOpen || player.IsInvulnerable) return;
        nextHit = Time.time + interval;
        player.TakeDamageFrom(damage, transform.position);
    }
}
