using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 毒火反应生成的瞬时爆炸。
/// 创建时对范围内目标造成一次伤害，随后立即结束。
/// </summary>
public class ExplosionReaction : MonoBehaviour, IElementReaction
{
    private const int HitBufferSize = 32;
    private readonly Collider2D[] hitBuffer = new Collider2D[HitBufferSize];
    private readonly HashSet<int> damagedTargets = new();
    [SerializeField] private float radius = 2f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float duration = 0.35f;

    private ContactFilter2D hitFilter;
    private float damage;
    private float elapsed;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock properties;

    public void SetUp(ElementEffectInfo info)
    {
        damage = info.damage * damageMultiplier;
        hitFilter = new ContactFilter2D { useTriggers = true };
        hitFilter.SetLayerMask(info.targetLM);
        transform.localScale = Vector3.one * radius * 2f;
        spriteRenderer = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
    }

    private void Start()
    {
        Explode();
    }

    private void Explode()
    {
        int count = Physics2D.OverlapCircle(transform.position, radius,
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

    private void Update()
    {
        elapsed += Time.deltaTime;
        spriteRenderer.GetPropertyBlock(properties);
        properties.SetFloat("_Progress", Mathf.Clamp01(elapsed / duration));
        spriteRenderer.SetPropertyBlock(properties);

        if (elapsed >= duration)
            Destroy(gameObject);
    }
}
