using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private BoxCollider2D trigger;
    [SerializeField] private BoxCollider2D boxCollider;
    private Collider2D targetCollider;

    public void FallDown(Transform target)
    {
        targetCollider = target.GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(boxCollider, targetCollider, true);
        trigger.enabled = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != targetCollider) return;

        Physics2D.IgnoreCollision(boxCollider, targetCollider, false);
        trigger.enabled = false;
    }
}
