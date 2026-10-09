#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Invoked through Unity MCP in Play Mode; fixtures never modify the saved scene.
public class EnemyAIPlayModeChecks : MonoBehaviour, IActor
{
    public static readonly List<string> Results = new List<string>();
    public static bool Running { get; private set; }
    public int HitCount { get; private set; }
    public float DamageTaken { get; private set; }

    private GameObject fixture;
    private EnemyAIController enemy;
    private EnemyAIPlayModeChecks target;
    private EnemyAIController mouseSource;
    private EnemyAIController jellySource;
    private int fixtureIndex;

    public void TakeDamage(float damage)
    {
        HitCount++;
        DamageTaken += damage;
    }

    public void Begin(EnemyAIController mouse, EnemyAIController jelly)
    {
        mouseSource = mouse;
        jellySource = jelly;
        Results.Clear();
        Running = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        IEnumerator tests = Cases();
        while (true)
        {
            object wait;
            try
            {
                if (!tests.MoveNext())
                    break;
                wait = tests.Current;
            }
            catch (System.Exception exception)
            {
                Results.Add("FAIL: exception: " + exception);
                break;
            }
            yield return wait;
        }
        Cleanup();
        Running = false;
        Debug.Log("Enemy AI checks: " + string.Join("\n", Results));
    }

    private IEnumerator Cases()
    {
        CreateFixture(mouseSource, 1f);
        yield return new WaitForSeconds(0.15f);
        Check(enemy.State == EnemyAIController.EnemyState.Attack && target.HitCount == 0, "Melee has a windup");
        yield return new WaitForSeconds(0.3f);
        Check(target.HitCount == 1 && Mathf.Approximately(target.DamageTaken, 1f), "Melee damages once at the hit frame");
        Check(CountBullets() == 0, "Melee never spawns projectiles");
        yield return new WaitForSeconds(0.55f);
        Check(target.HitCount == 1, "Melee cooldown prevents continuous contact damage");
        yield return new WaitForSeconds(0.95f);
        Check(target.HitCount == 2, "Melee repeats after cooldown");

        CreateFixture(mouseSource, 1f);
        yield return new WaitForSeconds(0.12f);
        target.transform.localPosition = new Vector3(5f, 0f, 0f);
        yield return new WaitForSeconds(0.45f);
        Check(target.HitCount == 0, "Leaving melee reach during windup avoids damage");

        CreateFixture(mouseSource, 1f);
        yield return new WaitForSeconds(0.12f);
        enemy.TakeDamage(0.5f);
        Check(enemy.State == EnemyAIController.EnemyState.Hurt && Mathf.Approximately(enemy.Hp, 2.5f), "Hurt state and health update");
        yield return new WaitForSeconds(0.5f);
        Check(target.HitCount == 0, "Hurt cancels pending melee damage");
        enemy.TakeDamage(100f);
        Check(enemy.IsDead && !enemy.GetComponent<Collider2D>().enabled && !enemy.GetComponent<Rigidbody2D>().simulated, "Death disables combat and physics");
        Check(enemy.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Dead"), "Death animation plays");
        yield return new WaitForSeconds(1f);
        Check(enemy == null, "Dead enemy is removed after animation");

        CreateFixture(jellySource, 3f);
        yield return new WaitForSeconds(0.18f);
        Check(enemy.State == EnemyAIController.EnemyState.Attack && CountBullets() == 0, "Ranged has a windup");
        yield return new WaitForSeconds(0.32f);
        Check(CountBullets() == 1, "Jellyfish emits its own projectile");
        JellyfishProjectile shot = FindBullet();
        Check(shot != null && shot.GetComponent<SpriteRenderer>().sprite.name == "Bullet", "Jellyfish uses the dedicated Bullet sprite");
        yield return new WaitForSeconds(0.6f);
        Check(target.HitCount == 1 && Mathf.Approximately(target.DamageTaken, 1f), "Ranged projectile damages target once");
        Check(CountBullets() == 0, "Projectile disappears on impact");

        CreateFixture(jellySource, 3f);
        yield return new WaitForSeconds(0.15f);
        enemy.TakeDamage(0.5f);
        yield return new WaitForSeconds(0.6f);
        Check(CountBullets() == 0 && target.HitCount == 0, "Hurt cancels ranged windup");

        CreateFixture(jellySource, 3f);
        AddWall(1.5f);
        yield return new WaitForSeconds(1.1f);
        Check(CountBullets() == 0 && target.HitCount == 0, "Walls block detection and shooting");

        CreateFixture(jellySource, 3f);
        enemy.enabled = false;
        AddWall(1.5f);
        shot = SpawnBullet(150f);
        yield return new WaitForSeconds(0.15f);
        Check(shot == null && target.HitCount == 0, "Fast projectile cannot tunnel through a thin wall");

        CreateFixture(jellySource, 3f);
        enemy.enabled = false;
        GameObject noActor = new GameObject("TargetWithoutIActor");
        noActor.transform.SetParent(fixture.transform, false);
        noActor.transform.localPosition = new Vector3(1.5f, 0f, 0f);
        noActor.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.8f);
        shot = Instantiate(GetProjectile());
        shot.Launch(enemy, noActor.transform, enemy.transform.position, Vector2.right, 1, 20f, 1f, 0.06f);
        yield return new WaitForSeconds(0.25f);
        Check(shot == null, "Target without IActor consumes projectile without an exception");

        CreateFixture(jellySource, 3f);
        enemy.enabled = false;
        shot = SpawnBullet(0f, 0.1f);
        yield return new WaitForSeconds(0.25f);
        Check(shot == null, "Projectile expires at its lifetime");

        CreateFixture(mouseSource, 6f);
        float startX = enemy.transform.position.x;
        yield return new WaitForSeconds(0.7f);
        Check(enemy.transform.position.x > startX + 0.5f, "Ground enemy chases on Default-layer terrain");
        yield return new WaitForSeconds(1.5f);
        Check(enemy.State == EnemyAIController.EnemyState.Attack || target.HitCount > 0, "Chase transitions into melee attack");

        CreateFixture(jellySource, 8f);
        startX = enemy.transform.position.x;
        float startY = enemy.transform.position.y;
        yield return new WaitForSeconds(0.7f);
        Check(enemy.transform.position.x > startX + 0.5f && Mathf.Abs(enemy.transform.position.y - startY) < 0.05f, "Jellyfish chases while floating");

        CreateFixture(mouseSource, 20f);
        target.gameObject.SetActive(false);
        fixture.transform.Find("Ground").GetComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
        yield return new WaitForSeconds(2.5f);
        Check(enemy.transform.localPosition.x < 1.65f && enemy.transform.localPosition.y > -0.5f, "Patrol stops at ledges");
        yield return new WaitForSeconds(2f);
        Check(enemy.transform.localPosition.x < 0.8f, "Patrol turns back after waiting");

        CreateFixture(mouseSource, 6f);
        fixture.transform.Find("Ground").GetComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
        yield return new WaitForSeconds(1.5f);
        Check(enemy.transform.localPosition.x < 1.65f && enemy.transform.localPosition.y > -0.5f, "Chase also stops at ledges");

        CreateFixture(mouseSource, 1f);
        target.transform.localPosition = new Vector3(1f, 3f, 0f);
        yield return new WaitForSeconds(0.5f);
        Check(target.HitCount == 0, "Melee cannot hit a target on another height");

        CreateFixture(jellySource, 3f);
        yield return new WaitForSeconds(0.1f);
        Destroy(target.gameObject);
        yield return new WaitForSeconds(0.6f);
        Check(enemy != null && !enemy.IsDead, "Losing target during windup is safe");
    }

    private void CreateFixture(EnemyAIController source, float targetX)
    {
        Cleanup();
        fixture = new GameObject("Enemy AI Test Fixture");
        fixture.SetActive(false);
        fixture.transform.position = new Vector3(1000f + fixtureIndex++ * 40f, 30f, 0f);
        var floor = new GameObject("Ground");
        floor.transform.SetParent(fixture.transform, false);
        floor.transform.localPosition = new Vector3(0f, -1f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 1f);
        var victim = new GameObject("Damage Test Target");
        victim.transform.SetParent(fixture.transform, false);
        victim.transform.localPosition = new Vector3(targetX, 0f, 0f);
        victim.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.8f);
        target = victim.AddComponent<EnemyAIPlayModeChecks>();
        enemy = Instantiate(source, fixture.transform);
        enemy.transform.localPosition = Vector3.zero;
        enemy.transform.localRotation = Quaternion.identity;
        enemy.SetTarget(victim.transform);
        fixture.SetActive(true);
        Physics2D.SyncTransforms();
    }

    private void AddWall(float x)
    {
        var wall = new GameObject("Test Wall");
        wall.transform.SetParent(fixture.transform, false);
        wall.transform.localPosition = new Vector3(x, 0.5f, 0f);
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 3f);
        Physics2D.SyncTransforms();
    }

    private JellyfishProjectile GetProjectile() =>
        Resources.Load<JellyfishProjectile>("Prefab/Enemy/JellyfishProjectile");

    private JellyfishProjectile SpawnBullet(float speed, float lifetime = 5f)
    {
        var shot = Instantiate(GetProjectile());
        shot.Launch(enemy, target.transform, enemy.transform.position, Vector2.right, 1, speed, 1f, 0.06f, lifetime);
        return shot;
    }

    private JellyfishProjectile FindBullet()
    {
        foreach (var bullet in FindObjectsByType<JellyfishProjectile>(FindObjectsSortMode.None))
            if (bullet.transform.position.x > 900f)
                return bullet;
        return null;
    }

    private int CountBullets()
    {
        int count = 0;
        foreach (var bullet in FindObjectsByType<JellyfishProjectile>(FindObjectsSortMode.None))
            if (bullet.transform.position.x > 900f)
                count++;
        return count;
    }

    private void Check(bool condition, string message) => Results.Add((condition ? "PASS: " : "FAIL: ") + message);

    private void Cleanup()
    {
        if (fixture != null)
        {
            fixture.SetActive(false);
            Destroy(fixture);
        }
        foreach (var bullet in FindObjectsByType<JellyfishProjectile>(FindObjectsSortMode.None))
            if (bullet.transform.position.x > 900f)
            {
                bullet.gameObject.SetActive(false);
                Destroy(bullet.gameObject);
            }
    }
}
#endif
