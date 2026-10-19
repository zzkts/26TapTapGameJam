using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class WaterFootprintTests
{
    private readonly List<GameObject> objects = new();
    private ElementField Water(Vector2? contact, Vector2 normal)
    {
        var field = ElementField.Create(E_ElementType.Water, new Vector2(300, 301), new Vector2(1.5f, .2f),
            4f, 0, surfacePoint: contact, surfaceNormal: normal);
        // EditMode does not drive MonoBehaviour activation; mirror its normal runtime registration.
        ElementField.Active.Add(field);
        objects.Add(field.gameObject); return field;
    }
    [TearDown]
    public void Cleanup()
    {
        foreach (var obj in objects)
        {
            ElementField.Active.Remove(obj.GetComponent<ElementField>());
            Object.DestroyImmediate(obj);
        }
        objects.Clear();
    }

    [TestCase(0f)]
    [TestCase(25f)]
    [TestCase(-25f)]
    public void WetMarkFollowsContactAndSlopeWithoutMovingTheGameplayVolume(float angle)
    {
        Vector2 normal = Quaternion.Euler(0, 0, angle) * Vector2.up;
        Vector2 contact = new Vector2(300, 300);
        var field = Water(contact, normal);
        var visual = field.GetComponentInChildren<SpriteRenderer>();
        Assert.That(visual.enabled, Is.True);
        Assert.That(Vector2.Distance(visual.transform.position, contact + normal * .025f), Is.LessThan(.001f));
        Assert.That(Vector2.Dot(visual.transform.up, normal), Is.GreaterThan(.999f));
        Assert.That((Vector2)field.transform.position, Is.EqualTo(new Vector2(300, 301)));
        Assert.That(field.Radius, Is.EqualTo(.75f));
        Assert.That(visual.transform.localScale.y, Is.LessThan(.2f));
    }

    [TestCase(1f, 0f)]
    [TestCase(-1f, 0f)]
    [TestCase(0f, -1f)]
    public void WallsAndCeilingsDoNotProduceHorizontalFloatingPuddles(float x, float y)
    {
        Assert.That(Water(new Vector2(300, 300), new Vector2(x, y)).GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
    }

    [Test]
    public void AirExpiryDoesNotCreateASuspendedWetMark()
    {
        Assert.That(Water(null, Vector2.up).GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
    }

    [Test]
    public void RepeatedHitsRefreshTheVisibleMarkWithoutStackingOpaqueBars()
    {
        var first = Water(new Vector2(300, 300), Vector2.up);
        var second = Water(new Vector2(300.1f, 300), Vector2.up);
        Assert.That(first.GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
        Assert.That(second.GetComponentInChildren<SpriteRenderer>().enabled, Is.True);
        Assert.That(ElementField.Active.Contains(first) && ElementField.Active.Contains(second), Is.True,
            "Visual consolidation must retain both existing gameplay fields.");
        var uv = second.GetComponentInChildren<SpriteRenderer>().sprite.uv;
        Assert.That(uv.Max(p => p.x) - uv.Min(p => p.x), Is.EqualTo(1f).Within(.001f));
        Assert.That(uv.Max(p => p.y) - uv.Min(p => p.y), Is.EqualTo(1f).Within(.001f));
    }
}
