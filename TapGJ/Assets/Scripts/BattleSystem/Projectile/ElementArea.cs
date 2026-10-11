using System.Collections.Generic;
using UnityEngine;

public class ElementArea : MonoBehaviour
{
    public static readonly HashSet<ElementArea> Active = new HashSet<ElementArea>();
    public CombatElement Element { get; private set; }
    public float Radius { get; private set; }
    private BulletSnapshot settings;
    private float expires, created, duration, nextTick;
    private bool vortex, spent;
    private SpriteRenderer visual;
    private readonly Collider2D[] overlaps = new Collider2D[128];
    private readonly HashSet<CombatStatus> actors = new HashSet<CombatStatus>();
    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
    public static ElementArea Create(CombatElement element, Vector2 position, float radius, float lifetime, BulletSnapshot shot, bool isVortex = false)
    {
        var obj = new GameObject(isVortex ? "Vortex" : element + " Area");
        obj.transform.position = position;
        var area = obj.AddComponent<ElementArea>();
        area.Element = element;
        area.Radius = radius;
        area.settings = shot.Copy();
        area.duration = lifetime;
        area.created = Time.time;
        area.expires = Time.time + lifetime;
        area.vortex = isVortex;
        area.visual = obj.AddComponent<SpriteRenderer>();
        area.visual.sprite = PrototypeVisual.WhiteSprite;
        area.visual.color = element == CombatElement.Poison ? new Color(0.35f, 0.8f, 0.2f, 0.25f) :
            element == CombatElement.Steam ? new Color(1f, 1f, 1f, 0.3f) : new Color(0.25f, 0.7f, 1f, 0.3f);
        area.visual.sortingOrder = 2;
        obj.transform.localScale = new Vector3(radius * 2f, element == CombatElement.Water && !isVortex ? 0.12f : radius * 0.7f, 1f);
        return area;
    }
    private void Update()
    {
        if (Time.time >= expires) { Destroy(gameObject); return; }
        if (Element == CombatElement.Poison && visual != null)
        {
            Color color = visual.color;
            color.a = Mathf.Lerp(0.15f, 1f, (Time.time - created) / duration);
            visual.color = color;
        }
        if (settings == null || Time.time < nextTick || spent) return;
        nextTick = Time.time + 1f;
        Collect();
        foreach (CombatStatus actor in actors)
        {
            if (actor == null || actor.Dead) continue;
            if (Element == CombatElement.Poison) actor.PoisonTick(settings.poisonDamage);
            else if (Element == CombatElement.Water) actor.Apply(CombatElement.Water, settings);
        }
    }
    private void FixedUpdate()
    {
        if (!vortex || settings == null) return;
        Collect();
        foreach (CombatStatus actor in actors)
        {
            EnemyAIController enemy = actor.GetComponent<EnemyAIController>();
            if (enemy == null || enemy.IsDead) continue;
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.MovePosition(Vector2.MoveTowards(body.position, transform.position, settings.vortexSpeed * Time.fixedDeltaTime));
        }
    }
    private void Collect()
    {
        actors.Clear();
        var filter = new ContactFilter2D { useTriggers = false };
        int count = vortex ? Physics2D.OverlapBox(transform.position, new Vector2(Radius * 2f, Radius * 0.67f), 0f, filter, overlaps) :
            Physics2D.OverlapCircle(transform.position, Radius, filter, overlaps);
        for (int i = 0; i < count; i++)
        {
            CombatStatus actor = CombatStatus.Get(overlaps[i]);
            if (actor != null) actors.Add(actor);
        }
    }
    public void React(CombatElement incoming, BulletSnapshot shot)
    {
        if (spent) return;
        if (Element == CombatElement.Poison && (incoming & CombatElement.Fire) != 0)
        {
            spent = true;
            Collect();
            foreach (CombatStatus actor in actors)
                if (actor != null && !actor.Dead) actor.GetComponent<IActor>()?.TakeDamage(actor.MaxHealth * shot.explosionFraction);
            TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, new Color(1f, 0.5f, 0.15f), Radius, Vector2.up);
            Destroy(gameObject);
        }
        else if (Element == CombatElement.Water && (incoming & CombatElement.Electric) != 0)
            Conduct(transform.position, shot);
    }
    public static void Conduct(Vector2 origin, BulletSnapshot shot, CombatStatus seed = null, float originRadius = -1f)
    {
        var visitedActors = new HashSet<CombatStatus>();
        var visitedAreas = new HashSet<ElementArea>();
        var queue = new Queue<Vector3>();
        queue.Enqueue(new Vector3(origin.x, origin.y, originRadius >= 0f ? originRadius : shot.contactDistance));
        if (seed != null) { visitedActors.Add(seed); seed.Electrify(shot); }
        // A visited set traverses water connections once without recursive tag application.
        while (queue.Count > 0)
        {
            Vector3 node = queue.Dequeue();
            foreach (CombatStatus actor in CombatStatus.Active)
            {
                if (actor == null || actor.Dead || !actor.Has(CombatElement.Water) || visitedActors.Contains(actor)) continue;
                if (Vector2.Distance(actor.transform.position, node) > node.z) continue;
                visitedActors.Add(actor);
                actor.Electrify(shot);
                queue.Enqueue(new Vector3(actor.transform.position.x, actor.transform.position.y, shot.contactDistance));
            }
            foreach (ElementArea area in Active)
            {
                if (area == null || area.Element != CombatElement.Water || visitedAreas.Contains(area)) continue;
                if (Vector2.Distance(area.transform.position, node) > node.z + area.Radius) continue;
                visitedAreas.Add(area);
                if (area.visual != null) area.visual.color = new Color(0.8f, 0.9f, 1f, 0.8f);
                queue.Enqueue(new Vector3(area.transform.position.x, area.transform.position.y, area.Radius + shot.contactDistance));
            }
        }
    }
}
