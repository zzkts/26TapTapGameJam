using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 水火反应生成的蒸汽区域。
/// 在持续时间内按间隔对范围内目标造成伤害。
/// </summary>
public class SteamReaction : MonoBehaviour, IElementReaction
{
    private const int HitBufferSize = 32;
    private readonly Collider2D[] hitBuffer = new Collider2D[HitBufferSize];
    private readonly HashSet<int> damagedTargets = new();

    [SerializeField] private Vector2 size = new(3f, 2f);
    [SerializeField] private float duration = 2f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float damageMultiplier = 0.25f;

    private ContactFilter2D hitFilter;
    private float damage;
    private float remainingTime;
    private float tickTimer;

    public void SetUp(ElementEffectInfo info)
    {
        damage = info.damage * damageMultiplier;
        remainingTime = duration;
        tickTimer = tickInterval;
        hitFilter = new ContactFilter2D { useTriggers = true };
        hitFilter.SetLayerMask(info.targetLM);
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void Update()
    {
        remainingTime -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            tickTimer += tickInterval;
            DamageTargets();
        }

        if (remainingTime <= 0f)
            Destroy(gameObject);
    }

    private void DamageTargets()
    {
        damagedTargets.Clear();
        int count = Physics2D.OverlapBox(transform.position, size, 0f,
            hitFilter, hitBuffer);

        for (int i = 0; i < count; i++)
        {
            IActor actor = hitBuffer[i].GetComponentInParent<IActor>();
            Object actorObject = actor as Object;
            if (!damagedTargets.Add(actorObject.GetInstanceID()))
                continue;

            actor.TakeDamage(damage);
        }
    }
}
