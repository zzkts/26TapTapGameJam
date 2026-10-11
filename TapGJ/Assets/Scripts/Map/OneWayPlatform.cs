using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatform : MonoBehaviour
{
    [SerializeField] private BoxCollider2D trigger;
    [SerializeField] private BoxCollider2D boxCollider;
    private readonly HashSet<Collider2D> dropping = new HashSet<Collider2D>();
    private void Awake() { if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>(); }
    public void FallDown(Transform target)
    {
        Collider2D actor = target.GetComponent<Collider2D>();
        if (boxCollider == null || actor == null || !dropping.Add(actor)) return;
        StartCoroutine(Drop(actor));
    }
    private IEnumerator Drop(Collider2D actor)
    {
        Physics2D.IgnoreCollision(boxCollider, actor, true);
        float until = Time.time + 0.6f;
        yield return new WaitForFixedUpdate();
        while (actor != null && actor.bounds.max.y > boxCollider.bounds.min.y && Time.time < until)
            yield return new WaitForFixedUpdate();
        if (actor != null && boxCollider != null) Physics2D.IgnoreCollision(boxCollider, actor, false);
        dropping.Remove(actor);
    }
    private void OnDisable()
    {
        foreach (Collider2D actor in dropping)
            if (actor != null && boxCollider != null) Physics2D.IgnoreCollision(boxCollider, actor, false);
        dropping.Clear();
    }
}
