using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField, Min(0f)] private float verticalOffset = 0.16f;
    [SerializeField, Min(0.1f)] private float width = 1.1f;
    [SerializeField, Min(0.01f)] private float height = 0.08f;
    [SerializeField] private Color backgroundColor = new Color(0.06f, 0.07f, 0.08f, 0.95f);
    [SerializeField] private Color foregroundColor = new Color(0.3f, 1f, 0.35f, 0.95f);
    private EnemyAIController enemy;
    private Collider2D body;
    private SpriteRenderer visual;
    private RectTransform bar;
    private RectTransform fill;
    public float DisplayedRatio { get; private set; }

    private void Awake()
    {
        enemy = GetComponent<EnemyAIController>();
        body = GetComponent<Collider2D>();
        visual = GetComponentInChildren<SpriteRenderer>();
        var root = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        bar = root.GetComponent<RectTransform>();
        bar.sizeDelta = new Vector2(width * 100f, height * 100f);
        MakeImage("Background", bar, backgroundColor);
        fill = MakeImage("Health", bar, foregroundColor);
        LateUpdate();
    }

    private static RectTransform MakeImage(string name, Transform parent, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        obj.GetComponent<Image>().color = color;
        obj.GetComponent<Image>().raycastTarget = false;
        return rect;
    }

    private void LateUpdate()
    {
        if (bar == null || enemy == null) return;
        bar.gameObject.SetActive(!enemy.IsDead);
        if (enemy.IsDead) return;
        float top = body != null ? body.bounds.max.y : transform.position.y;
        if (visual != null) top = Mathf.Max(top, visual.bounds.max.y);
        bar.position = new Vector3(transform.position.x, top + verticalOffset, transform.position.z);
        bar.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        bar.localScale = new Vector3(0.01f / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            0.01f / Mathf.Max(0.001f, Mathf.Abs(scale.y)), 1f);
        DisplayedRatio = Mathf.Clamp01(enemy.HealthNormalized);
        fill.anchorMax = new Vector2(DisplayedRatio, 1f);
    }

    private void OnDisable() { if (bar != null) bar.gameObject.SetActive(false); }
}
