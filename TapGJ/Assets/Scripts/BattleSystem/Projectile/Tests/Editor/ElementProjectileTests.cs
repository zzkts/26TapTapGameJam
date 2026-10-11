using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 元素反应和子弹轨迹的编辑器测试。
/// 验证元素效果集中结算，并确保轨迹能独立于子弹外观切换。
/// </summary>
public class ElementProjectileTests
{
    private readonly List<Object> objects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < objects.Count; i++)
            Object.DestroyImmediate(objects[i]);

        objects.Clear();
    }

    [TestCase(E_ElementType.Water, E_ElementType.Fire, E_ElementEffect.Steam)]
    [TestCase(E_ElementType.Fire, E_ElementType.Poison, E_ElementEffect.Explosion)]
    [TestCase(E_ElementType.Water, E_ElementType.Poison, E_ElementEffect.PoisonCloud)]
    public void ElementSystemResolvesConfiguredReactions(
        E_ElementType current, E_ElementType incoming, E_ElementEffect expected)
    {
        Assert.That(ElementSystem.GetReaction(current, incoming), Is.EqualTo(expected));
        Assert.That(ElementSystem.GetReaction(incoming, current), Is.EqualTo(expected));
    }

    [Test]
    public void PoisonWithoutReactionOnlyAddsBuff()
    {
        BuffComponent buffs = new();
        ElementEffectInfo received = default;
        System.Action<ElementEffectInfo> handler = info => received = info;
        ElementSystem.EffectTriggered += handler;

        E_ElementEffect effect;
        try
        {
            effect = ElementSystem.Apply(buffs, E_ElementType.Poison, Vector2.one);
        }
        finally
        {
            ElementSystem.EffectTriggered -= handler;
        }

        Assert.That(effect, Is.EqualTo(E_ElementEffect.None));
        Assert.That(received.effect, Is.EqualTo(E_ElementEffect.None));
        Assert.That(buffs.BuffList.Count, Is.EqualTo(1));
        Assert.That(buffs.BuffList[0].ElementType, Is.EqualTo(E_ElementType.Poison));
        Assert.That(buffs.BuffList[0].RemainingTime, Is.GreaterThan(0f));
    }

    [Test]
    public void ProjectileMoveModifierChangesAttackSnapshot()
    {
        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
        Modifier modifier = ScriptableObject.CreateInstance<Modifier>();
        objects.Add(weapon);
        objects.Add(modifier);

        SerializedObject serialized = new(modifier);
        serialized.FindProperty("modifierType").intValue = (int)E_ModifierType.ProjectileMove;
        serialized.FindProperty("modifierValue").floatValue = (float)E_ProjectileMoveType.Orbit;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        BulletSnapshot result = ModifierSystem.Instance.Apply(weapon, new[] { modifier });

        Assert.That(result.moveType, Is.EqualTo(E_ProjectileMoveType.Orbit));
    }

    [Test]
    public void MoveStrategyDataProvidesOrbitParameters()
    {
        ScriptableObject moveData = Resources.Load<ScriptableObject>("SO/Weapon/MoveStrategyData");

        Assert.That(moveData, Is.Not.Null);
        SerializedObject serialized = new(moveData);
        SerializedProperty orbit = serialized.FindProperty("orbit");
        Assert.That(orbit, Is.Not.Null);
        Assert.That(orbit.FindPropertyRelative("radius").floatValue, Is.EqualTo(2f));
        Assert.That(orbit.FindPropertyRelative("angularSpeed").floatValue, Is.EqualTo(180f));
    }

    [Test]
    public void MoveModifierUsesStrategyDataInsteadOfWeaponData()
    {
        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();
        Modifier modifier = ScriptableObject.CreateInstance<Modifier>();
        objects.Add(weapon);
        objects.Add(modifier);

        SerializedObject weaponSerialized = new(weapon);
        SerializedProperty oldRadius = weaponSerialized.FindProperty("orbitRadius");
        SerializedProperty oldSpeed = weaponSerialized.FindProperty("orbitAngularSpeed");
        if (oldRadius != null) oldRadius.floatValue = 9f;
        if (oldSpeed != null) oldSpeed.floatValue = 33f;
        weaponSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject modifierSerialized = new(modifier);
        modifierSerialized.FindProperty("modifierType").intValue =
            (int)E_ModifierType.ProjectileMove;
        modifierSerialized.FindProperty("modifierValue").floatValue =
            (float)E_ProjectileMoveType.Orbit;
        modifierSerialized.ApplyModifiedPropertiesWithoutUndo();

        BulletSnapshot result = ModifierSystem.Instance.Apply(weapon, new[] { modifier });

        Assert.That(result.orbitRadius, Is.EqualTo(2f));
        Assert.That(result.orbitAngularSpeed, Is.EqualTo(180f));
    }

    [Test]
    public void WeaponDataDoesNotStoreStrategySpecificParameters()
    {
        Assert.That(typeof(WeaponData).GetField("orbitRadius"), Is.Null);
        Assert.That(typeof(WeaponData).GetField("orbitAngularSpeed"), Is.Null);
    }

    [Test]
    public void ModifierSystemSeparatesLargeModifierCategories()
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        Assert.That(typeof(ModifierSystem).GetMethod("ApplyElement", flags), Is.Not.Null);
        Assert.That(typeof(ModifierSystem).GetMethod("ApplyMove", flags), Is.Not.Null);
    }

    [Test]
    public void GenericProjectileUsesOrbitMove()
    {
        GameObject owner = CreateObject("Owner", Vector2.zero);
        GameObject projectileObject = CreateObject("Projectile", Vector2.right);
        Rigidbody2D body = projectileObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        projectileObject.AddComponent<CircleCollider2D>().isTrigger = true;
        Projectile projectile = projectileObject.AddComponent<Projectile>();

        projectile.SetOwner(owner.transform);
        projectile.SetUp(Vector2.right, Vector2.right, 0, 5f,
            1f, 1f, 1f, 3f, 0, 0, 1f,
            E_ProjectileMoveType.Orbit, 2f, 180f);
        projectile.Step(0.25f);

        Assert.That(body.position.y, Is.Not.EqualTo(0f));
    }

    [Test]
    public void ElementSystemUsesElementEnumNumberAsId()
    {
        System.Reflection.MethodInfo method = typeof(ElementSystem).GetMethod("GetElementType");

        Assert.That(method, Is.Not.Null);
        Assert.That(method.Invoke(null, new object[] { (float)E_ElementType.Fire }),
            Is.EqualTo(E_ElementType.Fire));
    }

    [Test]
    public void ProjectileAndDamageInfoUseElementTypeInsteadOfHitBuff()
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public;

        Assert.That(typeof(Projectile).GetField("hitBuff", flags), Is.Null);
        Assert.That(typeof(Projectile).GetField("elementType", flags)?.FieldType,
            Is.EqualTo(typeof(E_ElementType)));
        Assert.That(typeof(DamageInfo).GetField("hitBuff", flags), Is.Null);
        Assert.That(typeof(DamageInfo).GetField("elementType", flags)?.FieldType,
            Is.EqualTo(typeof(E_ElementType)));
    }

    [TestCase("WaterProjectile", E_ElementType.Water)]
    [TestCase("FireProjectile", E_ElementType.Fire)]
    [TestCase("PoisonProjectile", E_ElementType.Poison)]
    public void ElementProjectilePrefabCarriesItsConfiguredElement(
        string prefabName, E_ElementType expected)
    {
        GameObject prefab = Resources.Load<GameObject>($"Prefab/Projectile/{prefabName}");
        SerializedObject projectile = new(prefab.GetComponent<Projectile>());

        Assert.That(projectile.FindProperty("elementType").intValue,
            Is.EqualTo((int)expected));
    }

    [TestCase("Steam", typeof(SteamReaction))]
    [TestCase("PoisonCloud", typeof(PoisonCloudReaction))]
    [TestCase("Explosion", typeof(ExplosionReaction))]
    public void ReactionPrefabCarriesItsMatchingReactionScript(
        string prefabName, System.Type expected)
    {
        GameObject prefab = Resources.Load<GameObject>(
            $"Prefab/Projectile/Reaction/{prefabName}");

        Assert.That(prefab.GetComponent(expected), Is.Not.Null);
    }

    private GameObject CreateObject(string name, Vector2 position)
    {
        GameObject gameObject = new(name);
        gameObject.transform.position = position;
        objects.Add(gameObject);
        return gameObject;
    }
}
