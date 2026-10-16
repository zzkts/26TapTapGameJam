using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class SplitGeneLaunchTests
{
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
