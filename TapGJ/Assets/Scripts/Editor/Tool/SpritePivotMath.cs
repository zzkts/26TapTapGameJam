#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Sprite Pivot 编辑器使用的坐标换算。
/// 负责在纹理像素位置和切片内归一化 Pivot 之间转换。
/// </summary>
internal static class SpritePivotMath
{
    public static Vector2 ToTexturePosition(Rect rect, Vector2 pivot)
    {
        return new Vector2(
            rect.xMin + rect.width * pivot.x,
            rect.yMin + rect.height * pivot.y);
    }

    public static Vector2 ToNormalizedPivot(Rect rect, Vector2 texturePosition)
    {
        return new Vector2(
            Mathf.Clamp01((texturePosition.x - rect.xMin) / rect.width),
            Mathf.Clamp01((texturePosition.y - rect.yMin) / rect.height));
    }

    public static Vector2 ToLocalPixelPosition(Rect rect, Vector2 pivot)
    {
        return new Vector2(rect.width * pivot.x, rect.height * pivot.y);
    }

    public static Vector2 FromLocalPixelPosition(Rect rect, Vector2 position)
    {
        return new Vector2(
            Mathf.Clamp01(position.x / rect.width),
            Mathf.Clamp01(position.y / rect.height));
    }

    public static Vector2 AlignToBaseline(Rect rect, float baselineY,
        bool centerX, Vector2 currentPivot)
    {
        Vector2 pivot = ToNormalizedPivot(rect,
            new Vector2(rect.xMin + rect.width * currentPivot.x, baselineY));
        if (centerX)
            pivot.x = 0.5f;
        return pivot;
    }
}
#endif
