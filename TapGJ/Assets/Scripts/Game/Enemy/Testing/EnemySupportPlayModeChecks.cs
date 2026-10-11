#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EnemySupportPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results = new List<string>();
    public static bool Running { get; private set; }

    private GameObject fixture;
    private EnemyAIController support;
    private readonly List<EnemyAIController> targets = new List<EnemyAIController>();

    public void Begin()
    {
        Results.Clear();
        Running = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        CreateFixture();
        yield return new WaitForSeconds(0.1f);
        Check(targets[0].Hp >= targets[0].MaxHp && targets[3].Hp >= targets[3].MaxHp,
            "Support heals the first four damaged enemies");
        Check(targets[4].Hp < targets[4].MaxHp, "Support respects the four target limit");
        Check(fixture.GetComponentsInChildren<LineRenderer>(true).Length > 0, "Support shows heal connection lines");
        Check(Mathf.Approximately(support.Hp, support.MaxHp), "Support does not heal itself");
        Check(support.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Blood"),
            "Healing plays the Blood animation");
        yield return new WaitForSeconds(support.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length + 0.1f);
        Check(targets[4].Hp < targets[4].MaxHp, "Full-health early arrivals keep their priority slots");
        Check(support.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Idle"),
            "Blood animation returns to Idle");
        targets[0].transform.localPosition = new Vector3(20f, 0f, 0f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.55f);
        Check(targets[4].Hp >= targets[4].MaxHp, "Waiting enemy is healed after an early arrival leaves");
        Check(targets[0].Hp >= targets[0].MaxHp && targets[4].Hp >= targets[4].MaxHp,
            "Support does not over-heal targets");
        support.TakeDamage(support.MaxHp);
        yield return new WaitForSeconds(0.05f);
        Check(!support.GetComponent<EnemySupportAura>().enabled &&
            fixture.GetComponentsInChildren<LineRenderer>(true).Length == 0,
            "Support death stops healing and removes connection lines");

        Cleanup();
        Running = false;
        Debug.Log("Enemy support checks: " + string.Join("\n", Results));
    }

    private void CreateFixture()
    {
        fixture = new GameObject("Enemy Support Test Fixture");
        fixture.transform.position = new Vector3(1000f, 30f, 0f);

        EnemyAIController supportPrefab = Resources.Load<EnemyAIController>("Prefab/Enemy/RestoreHealthMonster");
        EnemyAIController mousePrefab = GameObject.Find("MouseMonster").GetComponent<EnemyAIController>();
        support = Instantiate(supportPrefab, fixture.transform);
        support.name = "Support Test Monster";
        support.transform.localPosition = Vector3.zero;
        support.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        support.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;

        EnemySupportAura aura = support.GetComponent<EnemySupportAura>();
        SetField(aura, "healRadius", 8f);
        SetField(aura, "healAmount", 2f);
        SetField(aura, "healCooldown", 0.5f);
        SetField(aura, "cooldownTimer", 0f);
        SetField(aura, "maxTargets", 4);
        SetField(aura, "targetMask", (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));
        aura.enabled = false;

        for (int i = 0; i < 5; i++)
        {
            EnemyAIController target = Instantiate(mousePrefab, fixture.transform);
            target.name = "Support Test Target " + i;
            target.transform.localPosition = new Vector3(1f + i, 0f, 0f);
            target.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            target.enabled = false;
            target.TakeDamage(2f);
            targets.Add(target);
            // Stage each arrival to make the priority order deterministic.
            Physics2D.SyncTransforms();
            typeof(EnemySupportAura).GetMethod("RefreshCandidates", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(aura, null);
        }
        aura.enabled = true;
        Physics2D.SyncTransforms();
    }

    private void Check(bool condition, string message)
    {
        Results.Add((condition ? "PASS: " : "FAIL: ") + message);
    }

    private void Cleanup()
    {
        if (fixture != null)
            Destroy(fixture);
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }
}
#endif
