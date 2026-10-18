using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class SplitGeneLaunchTests
{
    [TestCase(1,1,0.5f)]
    [TestCase(2,1,0.75f)]
    [TestCase(1,2,0.25f)]
    [TestCase(2,2,0.5625f)]
    public void EachSplitCompoundsItsPerBulletDamagePenalty(int level,int count,float expected)
    {
        var split=Object.Instantiate(Resources.Load<Modifier>("SO/Genes/split"));
        var weapon=ScriptableObject.CreateInstance<WeaponData>();weapon.damage=8;weapon.damageMultiplier=1;
        try
        {
            split.Level=level;var genes=new Modifier[count];for(int i=0;i<count;i++)genes[i]=split;
            var shot=ModifierSystem.Instance.Apply(weapon,genes);
            Assert.That(shot.damage*shot.damageMultiplier,Is.EqualTo(8*expected).Within(.0001f));
            Assert.That(shot.splitCount,Is.Zero);
            Assert.That(weapon.damageMultiplier,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(split);Object.DestroyImmediate(weapon);}
    }

    [Test]
    public void NormalAndAdvancedSplitPenaltiesAreIndependentOfSlotOrder()
    {
        var normal=Object.Instantiate(Resources.Load<Modifier>("SO/Genes/split"));
        var advanced=Object.Instantiate(normal);advanced.Level=2;
        var weapon=ScriptableObject.CreateInstance<WeaponData>();weapon.damage=8;weapon.damageMultiplier=2;
        try
        {
            var before=ModifierSystem.Instance.Apply(weapon,new[]{normal,advanced});
            var after=ModifierSystem.Instance.Apply(weapon,new[]{advanced,null,normal});
            Assert.That(before.damage*before.damageMultiplier,Is.EqualTo(6).Within(.0001f));
            Assert.That(after.damageMultiplier,Is.EqualTo(before.damageMultiplier));
        }
        finally{Object.DestroyImmediate(normal);Object.DestroyImmediate(advanced);Object.DestroyImmediate(weapon);}
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SplitScalesFinalDamageAfterAdditiveModifiers(bool splitFirst)
    {
        var split=Resources.Load<Modifier>("SO/Genes/split");
        var attack=ScriptableObject.CreateInstance<Modifier>();
        var boost=ScriptableObject.CreateInstance<Modifier>();
        var weapon=ScriptableObject.CreateInstance<WeaponData>();weapon.damage=8;weapon.damageMultiplier=1;
        try
        {
            var data=new UnityEditor.SerializedObject(attack);
            data.FindProperty("modifierType").intValue=(int)E_ModifierType.Atk;
            data.FindProperty("modifierValue").floatValue=4;data.ApplyModifiedPropertiesWithoutUndo();
            data=new UnityEditor.SerializedObject(boost);
            data.FindProperty("modifierType").intValue=(int)E_ModifierType.DamageMultiplier;
            data.FindProperty("modifierValue").floatValue=.5f;data.ApplyModifiedPropertiesWithoutUndo();
            var genes=splitFirst?new[]{split,attack,boost}:new[]{boost,attack,split};
            var shot=ModifierSystem.Instance.Apply(weapon,genes);
            Assert.That(shot.damage*shot.damageMultiplier,Is.EqualTo((8+4)*1.5f*.5f).Within(.0001f));
        }
        finally{Object.DestroyImmediate(attack);Object.DestroyImmediate(boost);Object.DestroyImmediate(weapon);}
    }

    [TestCase(1,3)]
    [TestCase(2,4)]
    public void RestoringSplitKeepsItsCopyCountAndEquippedSlot(int level,int copies)
    {
        var root=new GameObject("Split save regression");root.SetActive(false);
        var inventory=root.AddComponent<GeneInventory>();
        var player=root.AddComponent<PlayerController>();player.hp=10;
        typeof(PlayerController).GetField("geneInventory",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(player,inventory);
        var split=Resources.Load<Modifier>("SO/Genes/split");
        var data=new RunSaveData();
        data.genes.Add(new SavedGene{geneId=split.GeneId,definitionName=split.name,instanceId="split-save",level=level,copies=copies});
        data.equipped=new[]{-1,0,-1,-1,-1,-1};
        try
        {
            RunProgress.RestorePlayer(player,data);
            Assert.That(inventory.GenInventory[0].Copies,Is.EqualTo(copies));
            Assert.That(inventory.GenInventory[0].Level,Is.EqualTo(level));
            Assert.That(inventory.GenChain[1],Is.SameAs(inventory.GenInventory[0]));
            Assert.That(inventory.GenInventory[0].InstanceId,Is.EqualTo("split-save"));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [Test]
    public void SplitEvolvesOnFourthCopyWithoutChangingOtherGenes()
    {
        var root=new GameObject("Split evolution regression");
        var inventory=root.AddComponent<GeneInventory>();
        var split=Resources.Load<Modifier>("SO/Genes/split");
        var water=Resources.Load<Modifier>("SO/Genes/water");
        try
        {
            for(int i=0;i<3;i++)inventory.Acquire(split);
            Assert.That(inventory.GenInventory[0].Level,Is.EqualTo(1),"Split needs four copies, not the default three");
            inventory.Acquire(split);
            Assert.That(inventory.GenInventory[0].Level,Is.EqualTo(2));
            Assert.That(inventory.GenInventory[0].Copies,Is.EqualTo(4));
            inventory.Acquire(split);
            Assert.That(inventory.GenInventory.Count,Is.EqualTo(2));
            Assert.That(inventory.GenInventory[1].Copies,Is.EqualTo(1));
            for(int i=0;i<3;i++)inventory.Acquire(water);
            Assert.That(inventory.GenInventory[2].Level,Is.EqualTo(2));
            Assert.That(split.Level,Is.EqualTo(1));
        }
        finally{Object.DestroyImmediate(root);}
    }

    [TestCase(1,0,3)]
    [TestCase(2,0,4)]
    [TestCase(1,2,5)]
    [TestCase(2,2,8)]
    public void SplitAddsLaunchBulletsScaledByEquippedModifications(int level,int otherMods,int expected)
    {
        var split=Object.Instantiate(Resources.Load<Modifier>("SO/Genes/split"));
        var weapon=ScriptableObject.CreateInstance<WeaponData>();weapon.projectileCount=1;
        try
        {
            split.Level=level;
            var genes=new Modifier[6];genes[0]=split;
            genes[1]=Resources.Load<Modifier>("SO/Genes/water");
            if(otherMods>0)genes[2]=Resources.Load<Modifier>("SO/Genes/speed");
            if(otherMods>1)genes[3]=Resources.Load<Modifier>("SO/Genes/bounce");
            var snapshot=ModifierSystem.Instance.Apply(weapon,genes);
            Assert.That(snapshot.projectileCount,Is.EqualTo(expected));
            Assert.That(snapshot.splitCount,Is.Zero,"The equipped gene must not enable collision or expiry splitting");
            Assert.That(split.Desc,Does.Contain("发射时"));
        }
        finally{Object.DestroyImmediate(split);Object.DestroyImmediate(weapon);}
    }

    [Test]
    public void SplitCountIsIndependentOfSlotOrderAndAttributeStage()
    {
        var split=Resources.Load<Modifier>("SO/Genes/split");
        var water=Resources.Load<Modifier>("SO/Genes/water");
        var speed=Resources.Load<Modifier>("SO/Genes/speed");
        var weapon=Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        var normal=ModifierSystem.Instance.Apply(weapon,new[]{split,water,speed,null});
        var enchanted=ModifierSystem.Instance.Apply(weapon,new[]{speed,null,water,split},water);
        Assert.That(enchanted.projectileCount,Is.EqualTo(normal.projectileCount));
        Assert.That(enchanted.projectileCount,Is.EqualTo(4));
        Assert.That(enchanted.element,Is.EqualTo(E_ElementType.Water));
        Assert.That(enchanted.speed,Is.EqualTo(normal.speed));
        Assert.That(enchanted.splitCount,Is.Zero);
    }

    [Test]
    public void MultipleSplitInstancesStackLaunchCountsWithinShotBudget()
    {
        var split=Resources.Load<Modifier>("SO/Genes/split");
        var weapon=Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        var result=ModifierSystem.Instance.Apply(weapon,new[]{split,split});
        Assert.That(result.projectileCount,Is.EqualTo(7));
        Assert.That(result.splitCount,Is.Zero);
        result=ModifierSystem.Instance.Apply(weapon,new[]{split,split,split,split,split,split});
        Assert.That(result.projectileCount,Is.EqualTo(result.maxSpawn));
        Assert.That(split.ModifierValue,Is.EqualTo(1));
        Assert.That(split.Level,Is.EqualTo(1),"Applying a build cannot mutate the configuration asset");
    }
}
