#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SpritePivotMathTests
{
    private static readonly Rect SpriteRect = new(100f, 200f, 80f, 120f);

    [Test]
    public void ToTexturePositionConvertsNormalizedPivotToPixels()
    {
        Vector2 result = SpritePivotMath.ToTexturePosition(SpriteRect,
            new Vector2(0.5f, 0.25f));

        Assert.That(result, Is.EqualTo(new Vector2(140f, 230f)));
    }

    [Test]
    public void ToNormalizedPivotClampsPositionInsideSprite()
    {
        Vector2 result = SpritePivotMath.ToNormalizedPivot(SpriteRect,
            new Vector2(250f, 150f));

        Assert.That(result, Is.EqualTo(new Vector2(1f, 0f)));
    }

    [Test]
    public void AlignToBaselineCentersXAndConvertsY()
    {
        Vector2 result = SpritePivotMath.AlignToBaseline(SpriteRect, 260f,
            true, new Vector2(0.2f, 0.8f));

        Assert.That(result, Is.EqualTo(new Vector2(0.5f, 0.5f)));
    }

    [Test]
    public void AlignToBaselineCanPreserveX()
    {
        Vector2 result = SpritePivotMath.AlignToBaseline(SpriteRect, 260f,
            false, new Vector2(0.2f, 0.8f));

        Assert.That(result, Is.EqualTo(new Vector2(0.2f, 0.5f)));
    }

    [Test]
    public void LocalPixelPositionUsesSpriteBottomLeftAsOrigin()
    {
        Vector2 result = SpritePivotMath.ToLocalPixelPosition(SpriteRect,
            new Vector2(0.25f, 0.75f));

        Assert.That(result, Is.EqualTo(new Vector2(20f, 90f)));
    }

    [Test]
    public void LocalPixelPositionConvertsBackToNormalizedPivot()
    {
        Vector2 result = SpritePivotMath.FromLocalPixelPosition(SpriteRect,
            new Vector2(20f, 90f));

        Assert.That(result, Is.EqualTo(new Vector2(0.25f, 0.75f)));
    }
}
#endif
