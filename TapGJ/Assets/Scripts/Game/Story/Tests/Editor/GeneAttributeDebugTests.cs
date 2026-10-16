using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class GeneAttributeDebugTests
{
    [Test]
    public void AttributeDebugListContainsAllCurrentElementGenesButNoEnvironmentElectricity()
    {
        var genes=GeneDebugWindow.AttributeDefinitions();
        CollectionAssert.AreEquivalent(new[]{"water","vortex","ember","sewage"},genes.Select(g=>g.name));
        Assert.That(genes.All(g=>g.IsAttribute&&g.Element!=E_ElementType.Electric),Is.True);
        Assert.That(genes.Select(g=>g.Element).Distinct().Count(),Is.EqualTo(3));
    }
    [Test]
    public void DebugSamplesCannotSpawnOrAcquireProgressOutsidePlayMode()
    {
        int before=Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Length;
        Assert.That(GeneDebugWindow.SpawnAttributeSamples(null,Resources.Load<Modifier>("SO/Genes/water")),Is.Zero);
        Assert.That(Object.FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Length,Is.EqualTo(before));
    }
}
