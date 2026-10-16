using System.Linq;
using NUnit.Framework;
using UnityEngine;

public sealed class EcosystemTests
{
    private GameObject fixture;
    private WaterReservoir2D Tank(){fixture=new GameObject("Reservoir fixture");return fixture.AddComponent<WaterReservoir2D>();}
    [TearDown]public void Cleanup(){if(fixture!=null)Object.DestroyImmediate(fixture);}
    [Test]public void MixingAndTakingConserveTheCarriedPollution()
    {
        var tank=Tank();tank.SetState(4,.8f);tank.Add(2,0);Assert.That(tank.Volume,Is.EqualTo(6));Assert.That(tank.Pollution,Is.EqualTo(3.2f/6).Within(.001f));
        float p;Assert.That(tank.Take(2,out p),Is.EqualTo(2));Assert.That(p,Is.EqualTo(3.2f/6).Within(.001f));Assert.That(tank.Volume*tank.Pollution+2*p,Is.EqualTo(3.2f).Within(.001f));
    }
    [Test]public void FullReservoirRejectsOverflowWithoutChangingItsContents()
    {var t=Tank();t.SetState(10,.7f);Assert.That(t.Add(5,0),Is.Zero);Assert.That(t.Pollution,Is.EqualTo(.7f));}
    [Test]public void FiltrationRemovesPollutionWhileEvaporationConcentratesIt()
    {var t=Tank();t.SetState(4,.5f);t.Purify(1);Assert.That(t.Pollution,Is.EqualTo(.25f));t.Evaporate(2);Assert.That(t.Pollution,Is.EqualTo(.5f));}
    [Test]public void EmptyAndInvalidWaterValuesRemainBounded()
    {var t=Tank();t.SetState(float.NaN,float.PositiveInfinity);Assert.That(t.Volume,Is.Zero);Assert.That(t.Pollution,Is.Zero);float p;t.Take(50,out p);Assert.That(t.Volume,Is.Zero);}
    [Test]public void WaterSurfaceMatchesActualVolume()
    {var t=Tank();t.transform.position=new Vector2(5,-2);t.maxHeight=2;t.capacity=10;t.SetState(5,0);Assert.That(t.SurfaceY,Is.EqualTo(-1));Assert.That(t.Contains(new Vector2(5,-1.5f)),Is.True);Assert.That(t.Contains(new Vector2(5,0)),Is.False);}
    [Test]public void NewBodyGenesHaveDistinctStableDefinitionsAndDoNotAlterWeaponStats()
    {
        var genes=Resources.LoadAll<Modifier>("SO/Genes").Where(g=>g.ModifierType==E_ModifierType.BodyTrait).ToArray();
        Assert.That(genes.Length,Is.EqualTo(3));Assert.That(genes.Select(g=>g.GeneId).Distinct().Count(),Is.EqualTo(3));
        var data=Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");var shot=ModifierSystem.Instance.Apply(data,genes);
        Assert.That(shot.damage,Is.EqualTo(data.damage));Assert.That(shot.speed,Is.EqualTo(data.projectileSpeed));Assert.That(shot.element,Is.EqualTo(E_ElementType.Water));
    }
    [Test]public void SoftVegetationReceivesWaterWithoutStoppingTheProjectile()
    {
        var oldFields=ElementField.Active.ToArray();GameObject plant=null,drop=null;
        try
        {
            plant=new GameObject("Soft plant");plant.layer=LayerMask.NameToLayer("Ground");plant.transform.position=new Vector2(3002,10);
            var collider=plant.AddComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=Vector2.one;
            var material=plant.AddComponent<EmergenceBody>();material.material=EmergenceMaterial.Organic;
            drop=new GameObject("Water through vegetation");drop.AddComponent<CircleCollider2D>().radius=.1f;
            var projectile=drop.AddComponent<Projectile>();projectile.SetUp(new Vector2(3000,10),Vector2.right,LayerMask.GetMask("Ground"),10,1,1,1,2);
            Physics2D.SyncTransforms();projectile.Step(.4f);
            Assert.That(material.Bloomed,Is.True,"The actual cast must still water the plant");
            Assert.That(drop.GetComponent<Rigidbody2D>().position.x,Is.GreaterThan(3003.5f),"A soft trigger must not swallow the shot before the shell behind it");
        }
        finally
        {
            if(drop!=null)Object.DestroyImmediate(drop);if(plant!=null)Object.DestroyImmediate(plant);
            foreach(var field in ElementField.Active.ToArray())if(!oldFields.Contains(field))Object.DestroyImmediate(field.gameObject);
        }
    }
    [Test]public void EcosystemAndCarriedWaterRoundTripThroughTheExistingSave()
    {
        var data=new RunSaveData{carriedWater=4,carriedPollution=.35f,ecosystem=new EcosystemState{growth=.8f,stableSeconds=12,steamSeconds=8,complete=false}};
        data.ecosystem.tanks.Add(new ReservoirState{id="pool",volume=7,pollution=.2f});var copy=JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(data));
        Assert.That(copy.carriedWater,Is.EqualTo(4));Assert.That(copy.ecosystem.tanks[0].pollution,Is.EqualTo(.2f));Assert.That(copy.ecosystem.stableSeconds,Is.EqualTo(12));
    }
}

