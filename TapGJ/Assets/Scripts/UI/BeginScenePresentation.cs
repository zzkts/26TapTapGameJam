using UnityEngine;
using UnityEngine.UI;

public sealed class BeginScenePresentation : MonoBehaviour
{
    public Image hero;
    public Sprite[] idleFrames;
    public RectTransform[] bubbles;
    private Vector2[] bubbleOrigins;
    private void Start()
    {
        bubbleOrigins = new Vector2[bubbles.Length];
        for (int i = 0; i < bubbles.Length; i++) bubbleOrigins[i] = bubbles[i].anchoredPosition;
    }
    private void Update()
    {
        float t = Time.unscaledTime;
        if (hero != null)
        {
            float breath = Mathf.Sin(t * 2.1f) * .012f;
            hero.transform.localScale = new Vector3(1f - breath * .5f, 1f + breath, 1f);
            if (idleFrames != null && idleFrames.Length > 0) hero.sprite = idleFrames[(int)(t * 5f) % idleFrames.Length];
        }
        if (bubbleOrigins == null) return;
        for (int i = 0; i < bubbles.Length; i++)
            if (bubbles[i] != null) bubbles[i].anchoredPosition = bubbleOrigins[i] +
                new Vector2(Mathf.Sin(t * .7f + i) * 8f, Mathf.Sin(t * .9f + i * 1.7f) * 16f);
    }
}
