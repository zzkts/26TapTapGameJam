using UnityEngine;

/// <summary>
/// 支持向下穿越的单向平台。
/// 下跳时暂时忽略玩家碰撞，玩家离开触发范围后恢复阻挡。
/// </summary>
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
