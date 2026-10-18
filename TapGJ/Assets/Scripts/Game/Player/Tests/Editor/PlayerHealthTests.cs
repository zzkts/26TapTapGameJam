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
        Assert.That(data.coyoteTime, Is.EqualTo(0.22f));
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
    [TestCase(-1)]
    [TestCase(1)]
    public void HoldingMovementIntoAWallDoesNotPreventJumping(int direction)
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        PlayerController player = null;
        try
        {
            var floor = new GameObject("Floor");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floor, scene);
            floor.transform.position = new Vector2(0, -.5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(10, 1);
            var wall = new GameObject("Wall");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, scene);
            wall.transform.position = new Vector2(direction, 2.5f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1, 5);

            player = CreatePlayer(10, 10);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player.gameObject, scene);
            var collider = player.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1.4f);
            collider.offset = new Vector2(0, -.2f);
            // Exercise initialization on the real controller, without running input or menus.
            typeof(PlayerController).GetMethod("Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(player, null);
            player.enabled = false;
            player.gameObject.SetActive(true);
            var body = player.GetComponent<Rigidbody2D>();
            body.gravityScale = .65f;
            body.position = new Vector2(-direction * .02f, .92f);
            var physics = scene.GetPhysicsScene2D();
            for (int i = 0; i < 30; i++)
            {
                player.Move(direction, player.moveSpeed);
                physics.Simulate(.02f);
            }
            float startY = body.position.y;
            body.linearVelocity = new Vector2(direction * player.moveSpeed, player.jumpVelocity);
            float peak = startY;
            for (int i = 0; i < 100; i++)
            {
                player.Move(direction, player.moveSpeed);
                physics.Simulate(.02f);
                peak = Mathf.Max(peak, body.position.y);
            }
            Assert.That(peak - startY, Is.GreaterThan(1f), "Holding into a wall must not cancel vertical jump speed.");
            Assert.That(body.position.y, Is.EqualTo(startY).Within(.03f), "The player must fall back instead of sticking to the wall.");
        }
        finally
        {
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    private static PlayerController CreatePlayer(float hp,float maxHp)
    {
        GameObject obj = new("PlayerHealthTest"); obj.SetActive(false);
        PlayerController player = obj.AddComponent<PlayerController>();
        player.hp = hp; player.maxHp = maxHp; return player;
    }
}
