using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BattleSystemProjectileTests
{
    private readonly List<GameObject> objects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < objects.Count; i++)
            Object.DestroyImmediate(objects[i]);

        objects.Clear();
    }

    [Test]
    public void PlayerExposesAssignedGeneInventory()
    {
        PropertyInfo property = typeof(PlayerController).GetProperty("Genes");
        FieldInfo field = typeof(PlayerController).GetField("geneInventory",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(property, Is.Not.Null);
        Assert.That(property.PropertyType, Is.EqualTo(typeof(GeneInventory)));
        Assert.That(field, Is.Not.Null);
        Assert.That(field.FieldType, Is.EqualTo(typeof(GeneInventory)));
    }

    [Test]
    public void GeneInventoryAppliesCurrentChainToWeapon()
    {
        GameObject player = CreateObject("Player", Vector2.zero);
        player.SetActive(false);
        Weapon weapon = player.AddComponent<Weapon>();
        GeneInventory inventory = player.AddComponent<GeneInventory>();
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.damage = 1f;
        data.projectilePrefab = CreateObject("ProjectilePrefab", Vector2.zero);
        Modifier modifier = CreateModifier(E_ModifierType.Atk, 2f);

        SerializedObject weaponObject = new(weapon);
        weaponObject.FindProperty("baseData").objectReferenceValue = data;
        weaponObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject inventoryObject = new(inventory);
        inventoryObject.FindProperty("weapon").objectReferenceValue = weapon;
        SerializedProperty chain = inventoryObject.FindProperty("genChain");
        chain.arraySize = 1;
        chain.GetArrayElementAtIndex(0).objectReferenceValue = modifier;
        inventoryObject.ApplyModifiedPropertiesWithoutUndo();

        player.SetActive(true);
        MethodInfo apply = typeof(GeneInventory).GetMethod("Apply");
        PropertyInfo currentShot = typeof(Weapon).GetProperty("CurrentShot");

        Assert.That(apply, Is.Not.Null);
        Assert.That(currentShot, Is.Not.Null);
        apply.Invoke(inventory, null);
        BulletSnapshot result = (BulletSnapshot)currentShot.GetValue(weapon);
        Assert.That(result.damage, Is.EqualTo(3f));

        Object.DestroyImmediate(data);
        Object.DestroyImmediate(modifier);
    }

    [Test]
    public void ModifierIsAConfigAssetWithMetadata()
    {
        Modifier modifier = CreateModifier(E_ModifierType.Atk, 2f, "攻击强化", "攻击力增加 2");

        Assert.That(modifier, Is.InstanceOf<ScriptableObject>());
        Assert.That(modifier.Name, Is.EqualTo("攻击强化"));
        Assert.That(modifier.Desc, Is.EqualTo("攻击力增加 2"));
        Assert.That(modifier.ModifierType, Is.EqualTo(E_ModifierType.Atk));
        Assert.That(modifier.ModifierValue, Is.EqualTo(2f));
    }

    [Test]
    public void GeneInventoryStoresOwnedGenesAndEquippedChain()
    {
        GameObject player = CreateObject("Player", Vector2.zero);
        GeneInventory inventory = player.AddComponent<GeneInventory>();
        Modifier atk = CreateModifier(E_ModifierType.Atk, 1f);
        Modifier count = CreateModifier(E_ModifierType.ProjectileCount, 1f);

        inventory.Acquire(atk);
        inventory.Acquire(count);
        inventory.Equip(0, 0);
        inventory.Equip(1, 1);

        Assert.That(inventory.GenInventory, Is.EqualTo(new[] { atk, count }));
        Assert.That(inventory.GenChain.Count, Is.EqualTo(inventory.SlotCount));
        Assert.That(inventory.GenChain[0], Is.SameAs(atk));
        Assert.That(inventory.GenChain[1], Is.SameAs(count));

        inventory.Move(1, 0);
        Assert.That(inventory.GenChain[0], Is.SameAs(count));
        Assert.That(inventory.GenChain[1], Is.SameAs(atk));

        inventory.Unequip(0);
        Assert.That(inventory.GenChain[0], Is.Null);
    }

    [Test]
    public void ModifierSystemAppliesProjectileMechanics()
    {
        WeaponData source = ScriptableObject.CreateInstance<WeaponData>();
        source.projectileLifetime = 3f;
        source.pierceCount = 0;
        source.reboundCount = 0;
        source.reboundCoefficient = 1f;

        List<Modifier> modifiers = new()
        {
            CreateModifier(E_ModifierType.ProjectileLifetime, 2f),
            CreateModifier(E_ModifierType.Pierce, 2f),
            CreateModifier(E_ModifierType.Rebound, 3f),
            CreateModifier(E_ModifierType.ReboundCoefficient, -0.25f),
        };

        BulletSnapshot result = ModifierSystem.Instance.Apply(source, modifiers);

        Assert.That(result.lifeTime, Is.EqualTo(5f));
        Assert.That(result.pierceCount, Is.EqualTo(2));
        Assert.That(result.reboundCount, Is.EqualTo(3));
        Assert.That(result.reboundCoefficient, Is.EqualTo(0.75f));

        Object.DestroyImmediate(source);
    }

    [Test]
    public void ModifierSystemAppliesSpeedMultiplier()
    {
        WeaponData source = ScriptableObject.CreateInstance<WeaponData>();
        source.projectileSpeed = 8f;

        BulletSnapshot result = ModifierSystem.Instance.Apply(source, new[]
        {
            CreateModifier(E_ModifierType.ProjectileSpeedMultiplier, 1.25f),
        });

        Assert.That(result.speed, Is.EqualTo(10f));

        Object.DestroyImmediate(source);
    }

    [Test]
    public void ModifierOnlyKeepsGenericGeneData()
    {
        Assert.That(System.Enum.IsDefined(typeof(E_ModifierType), "ProjectilePrefab"), Is.False);
        Assert.That(typeof(Modifier).GetProperty("ProjectilePrefab"), Is.Null);
        Assert.That(typeof(Modifier).GetField("projectilePrefab",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
        Assert.That(typeof(Modifier).GetProperty("HitBuff"), Is.Null);
        Assert.That(typeof(Modifier).GetProperty("MoveType"), Is.Null);
        Assert.That(typeof(Modifier).GetProperty("Color"), Is.Null);
        Assert.That(typeof(Modifier).GetField("hitBuff",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
        Assert.That(typeof(Modifier).GetField("moveType",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
        Assert.That(typeof(Modifier).GetField("color",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic), Is.Null);
    }

    [Test]
    public void PoisonCloudReactionDamagesTargetsByTick()
    {
        GameObject cloudObject = CreateObject("PoisonCloud", Vector2.zero);
        PoisonCloudReaction cloud = cloudObject.AddComponent<PoisonCloudReaction>();
        GameObject target = CreateObject("Target", Vector2.zero);
        target.layer = 0;
        target.AddComponent<CircleCollider2D>().isTrigger = true;
        TestDamageActor actor = target.AddComponent<TestDamageActor>();

        cloud.SetUp(new ElementEffectInfo(E_ElementEffect.PoisonCloud,
            Vector2.zero, E_ElementType.Water, E_ElementType.Poison,
            2f, 1 << target.layer));
        Physics2D.SyncTransforms();
        typeof(PoisonCloudReaction).GetMethod("DamageTargets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(cloud, null);

        Assert.That(actor.TotalDamage, Is.EqualTo(0.4f));
    }

    [Test]
    public void VortexPullsTargetsTowardItsCenter()
    {
        System.Type type = typeof(Projectile).Assembly.GetType("VortexArea");
        Assert.That(type, Is.Not.Null);

        GameObject vortexObject = CreateObject("Vortex", Vector2.zero);
        Component vortex = vortexObject.AddComponent(type);
        GameObject target = CreateObject("Target", new Vector2(1f, 0f));
        target.layer = 0;
        Rigidbody2D targetBody = target.AddComponent<Rigidbody2D>();
        targetBody.gravityScale = 0f;
        target.AddComponent<CircleCollider2D>().isTrigger = true;
        target.AddComponent<TestDamageActor>();

        type.GetMethod("SetUp").Invoke(vortex, new object[]
        {
            new Vector2(3f, 1f), (LayerMask)(1 << target.layer), 2f, 1f,
        });
        Physics2D.SyncTransforms();
        type.GetMethod("Step").Invoke(vortex, new object[] { 0.25f });

        Assert.That(targetBody.position.x, Is.LessThan(1f));
    }

    [Test]
    public void ProjectileReflectsOnlyFromEnvironmentLayer()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        Assert.That(groundLayer, Is.GreaterThanOrEqualTo(0));

        TestProjectile projectile = CreateProjectile(Vector2.zero);
        GameObject wall = CreateObject("Wall", new Vector2(1f, 0f));
        wall.layer = groundLayer;
        BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(0.2f, 2f);

        projectile.ConfigureGround(1 << groundLayer);
        projectile.SetUp(Vector2.zero, Vector2.right, 0, 10f, 1f, 1f, 1f, 3f, 0, 1, 1f);
        Physics2D.SyncTransforms();

        projectile.Step(0.2f);

        Assert.That(projectile.CurrentDirection.x, Is.LessThan(0f));
        Assert.That(projectile.HitCount, Is.Zero);
    }

    [Test]
    public void ProjectileDamagesSameTargetOnlyOnce()
    {
        TestProjectile projectile = CreateProjectile(Vector2.zero);
        GameObject target = CreateObject("Target", new Vector2(0.5f, 0f));
        target.layer = 0;
        CircleCollider2D targetCollider = target.AddComponent<CircleCollider2D>();
        targetCollider.isTrigger = true;

        projectile.SetUp(Vector2.zero, Vector2.right, 1 << target.layer,
            10f, 1f, 1f, 1f, 3f, 2, 0, 1f);
        Physics2D.SyncTransforms();

        projectile.Step(0.1f);
        projectile.SetPosition(Vector2.zero);
        Physics2D.SyncTransforms();
        projectile.Step(0.1f);

        Assert.That(projectile.HitCount, Is.EqualTo(1));
    }

    private TestProjectile CreateProjectile(Vector2 position)
    {
        GameObject gameObject = CreateObject("Projectile", position);
        Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.1f;
        collider.isTrigger = true;
        return gameObject.AddComponent<TestProjectile>();
    }

    private GameObject CreateObject(string name, Vector2 position)
    {
        GameObject gameObject = new(name);
        gameObject.transform.position = position;
        objects.Add(gameObject);
        return gameObject;
    }

    private static Modifier CreateModifier(E_ModifierType type, float value,
        string geneName = "测试基因", string desc = "测试描述")
    {
        Modifier modifier = ScriptableObject.CreateInstance<Modifier>();
        SerializedObject serialized = new(modifier);
        serialized.FindProperty("geneName").stringValue = geneName;
        serialized.FindProperty("desc").stringValue = desc;
        serialized.FindProperty("modifierType").intValue = (int)type;
        serialized.FindProperty("modifierValue").floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return modifier;
    }
}

public class TestProjectile : Projectile
{
    public int HitCount { get; private set; }
    public Vector2 CurrentDirection => direction;

    public void ConfigureGround(LayerMask layerMask)
    {
        groundLM = layerMask;
    }

    public void SetPosition(Vector2 position)
    {
        rb.position = position;
    }

    protected override void Move()
    {
    }

    protected override void HandleCollision(Collider2D collision)
    {
        HitCount++;
    }
}

public class TestDamageActor : MonoBehaviour, IActor
{
    public float TotalDamage { get; private set; }

    public void TakeDamage(float damage)
    {
        TotalDamage += damage;
    }
}
