using NUnit.Framework;
using System.IO;
using UnityEngine;

public class PlayerHealthTests
{
    [Test]
    public void LocomotionTransitionsAreOwnedByPlayerStates()
    {
        string controller = ReadPlayerSource("PlayerController.cs");
        string idle = ReadPlayerSource("State/PlayerIdleState.cs");
        string walk = ReadPlayerSource("State/PlayerWalkState.cs");
        string jump = ReadPlayerSource("State/PlayerJumpState.cs");
        string fall = ReadPlayerSource("State/PlayerFallState.cs");

        Assert.That(controller, Does.Not.Contain("UpdateLocomotion("));
        Assert.That(idle, Does.Contain("player.Machine.Switch(player.AttackState)"));
        Assert.That(walk, Does.Contain("player.Machine.Switch(player.AttackState)"));
        Assert.That(jump, Does.Contain("player.Machine.Switch(player.AttackState)"));
        Assert.That(fall, Does.Contain("player.Machine.Switch(player.AttackState)"));
    }

    [Test]
    public void PlayerActionsDoNotSwitchFsmInternally()
    {
        string controller = ReadPlayerSource("PlayerController.cs");

        Assert.That(MethodBody(controller, "public bool TryJump()"), Does.Not.Contain("Machine.Switch"));
        Assert.That(MethodBody(controller, "public bool TryDownPlatform()"), Does.Not.Contain("Machine.Switch"));
        Assert.That(MethodBody(controller, "public bool TryAttack()"), Does.Not.Contain("Machine.Switch"));
        Assert.That(MethodBody(controller, "public bool TryRoll()"), Does.Not.Contain("Machine.Switch"));
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
        PlayerController player = CreatePlayer(5f, 10f);

        player.TakeDamage(-2f);

        Assert.That(player.hp, Is.EqualTo(5f));
        Object.DestroyImmediate(player.gameObject);
    }

    [Test]
    public void NonPositiveHealingDoesNotChangeHp()
    {
        PlayerController player = CreatePlayer(5f, 10f);

        player.IncreaseHp(-2f);

        Assert.That(player.hp, Is.EqualTo(5f));
        Object.DestroyImmediate(player.gameObject);
    }

    private static PlayerController CreatePlayer(float hp, float maxHp)
    {
        GameObject gameObject = new("PlayerHealthTest");
        gameObject.SetActive(false);
        PlayerController player = gameObject.AddComponent<PlayerController>();
        player.hp = hp;
        player.maxHp = maxHp;
        return player;
    }

    private static string ReadPlayerSource(string relativePath) =>
        File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/Game/Player", relativePath));

    private static string MethodBody(string source, string signature)
    {
        int start = source.IndexOf(signature, System.StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"Missing method: {signature}");
        int nextMethod = source.IndexOf("\n    public ", start + signature.Length, System.StringComparison.Ordinal);
        return nextMethod < 0 ? source.Substring(start) : source.Substring(start, nextMethod - start);
    }
}
