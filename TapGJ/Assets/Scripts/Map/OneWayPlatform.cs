using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private BoxCollider2D trigger;
    [SerializeField] private BoxCollider2D boxCollider;
    private readonly HashSet<Collider2D> ignoring = new();
    public void FallDown(Transform target)
    {
        if (boxCollider == null)
            foreach (BoxCollider2D candidate in GetComponents<BoxCollider2D>()) if (!candidate.isTrigger) { boxCollider = candidate; break; }
        Collider2D actor = target != null ? target.GetComponent<Collider2D>() : null;
        if (actor == null || boxCollider == null || !ignoring.Add(actor)) return;
        Physics2D.IgnoreCollision(boxCollider, actor, true);
        StartCoroutine(RestoreWhenClear(actor));
    }
    private IEnumerator RestoreWhenClear(Collider2D actor)
    {
        yield return new WaitForSeconds(0.2f);
        while (actor != null && actor.bounds.max.y >= boxCollider.bounds.min.y &&
            actor.bounds.min.x <= boxCollider.bounds.max.x && actor.bounds.max.x >= boxCollider.bounds.min.x)
            yield return new WaitForFixedUpdate();
        if (actor != null && boxCollider != null) Physics2D.IgnoreCollision(boxCollider, actor, false);
        ignoring.Remove(actor);
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        foreach (Collider2D actor in ignoring) if (actor != null && boxCollider != null) Physics2D.IgnoreCollision(boxCollider, actor, false);
        ignoring.Clear();
    }
}
