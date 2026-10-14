using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BuffSystemTests
{
    [Test]
    public void ElementDataMapsElementsToDescriptionsDurationsAndProjectiles()
    {
        ElementData data = Resources.Load<ElementData>("SO/Element/ElementData");

        Assert.That(data, Is.Not.Null);
        Assert.That(data.Get(E_ElementType.Water).desc, Is.Not.Empty);
        Assert.That(data.Get(E_ElementType.Water).duration, Is.GreaterThan(0f));
        Assert.That(data.Get(E_ElementType.Water).projectilePrefab, Is.Not.Null);
        Assert.That(data.Get(E_ElementType.Fire).projectilePrefab, Is.Not.Null);
        Assert.That(data.Get(E_ElementType.Poison).projectilePrefab, Is.Not.Null);
    }

    [Test]
    public void ReactionDataStoresSymmetricElementPairs()
    {
        ElementReactionData data = Resources.Load<ElementReactionData>(
            "SO/Element/ElementReactionData");

        Assert.That(data, Is.Not.Null);
        Assert.That(data.Get(E_ElementType.Water, E_ElementType.Fire).effect,
            Is.EqualTo(E_ElementEffect.Steam));
        Assert.That(data.Get(E_ElementType.Fire, E_ElementType.Water).effect,
            Is.EqualTo(E_ElementEffect.Steam));
        Assert.That(data.Get(E_ElementType.Water, E_ElementType.Poison).effect,
            Is.EqualTo(E_ElementEffect.PoisonCloud));
        Assert.That(data.Get(E_ElementType.Poison, E_ElementType.Fire).effect,
            Is.EqualTo(E_ElementEffect.Explosion));
    }

    [Test]
    public void BuffInstanceIsRuntimeStateInsteadOfScriptableObjectWrapper()
    {
        Assert.That(typeof(BuffInstance).GetProperty("Data"), Is.Null);
        Assert.That(typeof(BuffInstance).GetProperty("ElementType"), Is.Not.Null);
    }

    [Test]
    public void ElementStatusExpiresAfterConfiguredDuration()
    {
        BuffComponent component = new();

        ElementSystem.Apply(component, E_ElementType.Water, Vector2.zero);
        float duration = component.BuffList[0].RemainingTime;
        component.Update(duration);

        Assert.That(duration, Is.GreaterThan(0f));
        Assert.That(component.BuffList, Is.Empty);
    }

    [Test]
    public void TestGeneAssetsCoverValueMechanicAndElementTypes()
    {
        Modifier[] genes = Resources.LoadAll<Modifier>("SO/Genes");

        Assert.That(genes.Any(x => x.ModifierType == E_ModifierType.Atk), Is.True);
        Assert.That(genes.Any(x => x.ModifierType == E_ModifierType.ProjectileMove), Is.True);
        Assert.That(genes.Any(x => x.ModifierType == E_ModifierType.Element), Is.True);
    }

    [Test]
    public void ElementModifierUsesValueToSelectProjectilePrefab()
    {
        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
        GameObject originalPrefab = new("OriginalProjectile");
        weapon.projectilePrefab = originalPrefab;
        Modifier modifier = ScriptableObject.CreateInstance<Modifier>();
        SerializedObject serialized = new(modifier);
        serialized.FindProperty("modifierType").intValue = 12;
        serialized.FindProperty("modifierValue").floatValue = (float)E_ElementType.Water;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        BulletSnapshot idle = ModifierSystem.Instance.Apply(weapon, new[] { modifier });
        Assert.That(idle.projectilePrefab, Is.SameAs(originalPrefab));
        BulletSnapshot result = ModifierSystem.Instance.Apply(weapon, new[] { modifier }, modifier);

        Assert.That(result.projectilePrefab,
            Is.SameAs(Resources.Load<GameObject>("Prefab/Projectile/WaterProjectile")));
        Object.DestroyImmediate(modifier);
        Object.DestroyImmediate(originalPrefab);
        Object.DestroyImmediate(weapon);
    }

    [Test]
    public void CombatableKeepsExistingHealthContract()
    {
        Assert.That(typeof(IHealthActor).IsAssignableFrom(typeof(ICombatable)), Is.True);
        Assert.That(typeof(ICombatable).GetMethod("TakeDamage", new[] { typeof(DamageInfo) }), Is.Not.Null);
    }

    [Test]
    public void EvolvedFireKeepsTheFirePrefabAndElement()
    {
        Modifier fire = Object.Instantiate(Resources.Load<Modifier>("SO/Genes/ember")); fire.Level = 2;
        WeaponData weapon = Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        BulletSnapshot shot = ModifierSystem.Instance.Apply(weapon, new[] {fire}, fire);
        Assert.That(shot.element, Is.EqualTo(E_ElementType.Fire));
        Assert.That(shot.projectilePrefab, Is.SameAs(ElementSystem.GetProjectilePrefab(E_ElementType.Fire)));
        Object.DestroyImmediate(fire);
    }

}
