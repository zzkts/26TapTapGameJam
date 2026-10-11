using UnityEngine;

public static class PrototypeVisual
{
    private static Sprite white;
    public static Sprite WhiteSprite
    {
        get
        {
            if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            return white;
        }
    }
    public static SpriteRenderer Add(GameObject obj, Color color, Vector2 size)
    {
        var renderer = obj.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = 8;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        return renderer;
    }
}
