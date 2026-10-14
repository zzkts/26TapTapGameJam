using UnityEngine;

/// <summary>
/// 水火反应生成的蒸汽区域。
/// 消耗火焰后的视觉反馈，湿润状态保留，不重复结算伤害。
/// </summary>
public class SteamReaction : MonoBehaviour, IElementReaction
{
    [SerializeField] private Vector2 size = new(3f, 2f);
    [SerializeField] private float duration = 2f;
    private float elapsed;
    private SpriteRenderer visual;
    private MaterialPropertyBlock properties;

    public void SetUp(ElementEffectInfo info)
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);
        visual = GetComponent<SpriteRenderer>();
        Material material = Resources.Load<Material>("Material/Combat/ElementSprite");
        if (visual != null && material != null) visual.sharedMaterial = material;
        properties = new MaterialPropertyBlock(); properties.SetFloat("_Field", 1f);
        foreach (Collider2D collider in GetComponents<Collider2D>()) collider.enabled = false;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (visual != null && properties != null)
        {
            visual.color = new Color(.85f, .94f, 1f, .35f * Mathf.Clamp01(1f - elapsed / duration));
            properties.SetFloat("_Progress", elapsed / duration); visual.SetPropertyBlock(properties);
        }
        if (elapsed >= duration) Destroy(gameObject);
    }
}
