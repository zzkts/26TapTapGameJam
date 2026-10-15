using NUnit.Framework;
using UnityEngine;

public class PlayerHealthTests
{
    [Test]
    public void PlayerDefaultsMatchControlDesign()
    {
        PlayerData data = ScriptableObject.CreateInstance<PlayerData>();
        Assert.That(data.dashUnlocked, Is.False);
        Assert.That(data.rollDuration, Is.EqualTo(0.2f));
        Assert.That(data.invulnerabilityTime, Is.EqualTo(0.8f));
        Assert.That(data.coyoteTime, Is.EqualTo(0.14f));
        Object.DestroyImmediate(data);
    }
    [Test]
    public void HealingClampsAndPublishesTheActualChange()
    {
        PlayerController player = CreatePlayer(8f,10f);
        int events = 0;
        player.HealthChanged += () => events++;
        Assert.That(player.IncreaseHp(5f), Is.EqualTo(2f));
        Assert.That(player.hp, Is.EqualTo(10f));
        Assert.That(player.Heal(5f), Is.Zero);
        Assert.That(events, Is.EqualTo(1));
        Object.DestroyImmediate(player.gameObject);
    }
    [Test]
    public void DeathRejectsHealing()
    {
        PlayerController player = CreatePlayer(0f,10f);
        Assert.That(player.IsDead, Is.True);
        Assert.That(player.Heal(10f), Is.Zero);
        Object.DestroyImmediate(player.gameObject);
    }
    [Test]
    public void HealthActorExposesCurrentHpAndDeadState()
    {
        Assert.That(typeof(IHealthActor).GetProperty("Hp"), Is.Not.Null);
        Assert.That(typeof(IHealthActor).GetProperty("IsDead"), Is.Not.Null);
    }
    [Test]
    public void NonPositiveDamageDoesNotChangeHp()
    {
        PlayerController player = CreatePlayer(5f,10f); player.TakeDamage(-2f);
        Assert.That(player.hp, Is.EqualTo(5f)); Object.DestroyImmediate(player.gameObject);
    }
    [Test]
    public void NonPositiveHealingDoesNotChangeHp()
    {
        PlayerController player = CreatePlayer(5f,10f); player.IncreaseHp(-2f);
        Assert.That(player.hp, Is.EqualTo(5f)); Object.DestroyImmediate(player.gameObject);
    }
    private static PlayerController CreatePlayer(float hp,float maxHp)
    {
        GameObject obj = new("PlayerHealthTest"); obj.SetActive(false);
        PlayerController player = obj.AddComponent<PlayerController>();
        player.hp = hp; player.maxHp = maxHp; return player;
    }
}
