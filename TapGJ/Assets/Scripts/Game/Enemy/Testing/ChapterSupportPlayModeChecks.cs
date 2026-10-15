#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Checks the restored chapter squads using current support and combat prefabs.</summary>
public sealed class ChapterSupportPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results = new();
    public static bool Running { get; private set; }
    public static bool Succeeded { get; private set; }
    private GameObject fixture;
    private EnemyAIController support;
    private EnemySupportAura aura;
    private readonly List<EnemyAIController> allies = new();
    private float originalTimeScale;

    public static void Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Running) throw new InvalidOperationException("Chapter support checks are already running.");
        new GameObject("Chapter Support Verification").AddComponent<ChapterSupportPlayModeChecks>().Begin();
    }

    private void Begin()
    {
        Results.Clear(); Running = true; Succeeded = false;
        originalTimeScale = Time.timeScale; Time.timeScale = 1f;
        StartCoroutine(Execute());
    }

    private IEnumerator Execute()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Cases());
        while (stack.Count > 0)
        {
            object wait;
            try
            {
                IEnumerator step = stack.Peek();
                if (!step.MoveNext()) { stack.Pop(); continue; }
                wait = step.Current;
                if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            }
            catch (Exception error) { Results.Add("FAIL: " + error.Message); break; }
            yield return wait;
        }
        Finish();
    }

    private IEnumerator Cases()
    {
        CompleteLevelFlow level = Object.FindFirstObjectByType<CompleteLevelFlow>();
        Check(level != null, "The authored chapter is loaded");
        EnemySupportAura[] squads = level.GetComponentsInChildren<EnemySupportAura>(true);
        Check(squads.Count(healer => healer.transform.position.x < 78f) == 2 && squads.Length >= 3,
            "Laboratory, ecology and spore routes contain real Monster4 healing squads");
        Check(squads.All(healer => healer.GetComponent<EnemyAIController>().IsSupport &&
            healer.HealCooldown >= 5f && healer.HealRadius <= 6f),
            "Authored healers use a readable five-second cadence and local six-metre range");
        Check(level.GetComponentsInChildren<EnemyAIController>(true).Where(enemy => enemy.transform.position.x < 78f && enemy.IsSupport)
            .All(enemy => enemy.MaxHp <= 5f), "The two early healers remain quick priority targets");

        fixture = new GameObject("Support Verification Fixture"); fixture.transform.position = new Vector3(1000f, 30f, 0f);
        GameObject healerPrefab = Resources.Load<GameObject>("Prefab/Enemy/Support/Monster4");
        GameObject allyPrefab = Resources.Load<GameObject>("Prefab/Enemy/Melee/Monster5");
        Check(healerPrefab != null && allyPrefab != null, "Verification loads current Monster4 and Monster5 prefabs");
        support = Object.Instantiate(healerPrefab, fixture.transform).GetComponent<EnemyAIController>();
        support.transform.localPosition = Vector3.zero;
        support.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        Set(support, "patrolRange", 10f); Set(support, "patrolSpeed", .6f);
        Set(support, "patrolTarget", support.transform.position.x + 10f);
        Set(support, "playerTarget", null);
        aura = support.GetComponent<EnemySupportAura>(); aura.enabled = false;
        Check(aura.HealCooldown == 5f && aura.HealAmount == 20f && aura.MaxTargets == 4,
            "The production prefab retains its designed healing defaults");
        // Accelerate only this isolated fixture's cooldown; real encounter timing stays five seconds.
        Set(aura, "healCooldown", .6f); Set(aura, "cooldownTimer", .6f);
        for (int i = 0; i < 5; i++)
        {
            EnemyAIController ally = Object.Instantiate(allyPrefab, fixture.transform).GetComponent<EnemyAIController>();
            ally.transform.localPosition = new Vector2(1f + i, 0f); ally.enabled = false;
            ally.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            ally.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            ally.TakeDamage(3f); allies.Add(ally); Physics2D.SyncTransforms();
            typeof(EnemySupportAura).GetMethod("RefreshCandidates", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(aura, null);
        }
        aura.enabled = true;
        yield return new WaitForSeconds(.2f);
        Check(allies.All(ally => ally.Hp < ally.MaxHp), "Connected allies cannot heal before the scheduled pulse");
        Check(support.GetComponentsInChildren<LineRenderer>().Length == 4,
            "Four visible green connections identify the healing targets before the pulse");
        yield return new WaitForSeconds(.43f);
        Check(allies.Take(4).All(ally => ally.Hp == ally.MaxHp) && allies[4].Hp < allies[4].MaxHp,
            "The healing pulse restores the first four arrivals and leaves the fifth waiting");
        Animator animation = support.GetComponentInChildren<Animator>();
        Check(animation != null && animation.GetCurrentAnimatorStateInfo(0).IsName("Blood"),
            "Actual healing starts the dedicated Blood animation");
        yield return new WaitForSeconds(animation.GetCurrentAnimatorStateInfo(0).length + .12f);
        Check(!animation.GetCurrentAnimatorStateInfo(0).IsName("Blood") &&
            (animation.GetCurrentAnimatorStateInfo(0).IsName("Run") || animation.GetCurrentAnimatorStateInfo(0).IsName("Idle")),
            "A healer resumes its locomotion after Blood while patrolling");
        Check(allies[4].Hp < allies[4].MaxHp, "Healthy early arrivals retain their four priority slots");
        allies[0].transform.localPosition = Vector2.right * 20f; Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.65f);
        Check(allies[4].Hp == allies[4].MaxHp, "A waiting ally receives healing when a priority connection leaves range");
        support.TakeDamage(1f); float woundedSupport = support.Hp;
        yield return new WaitForSeconds(.65f);
        Check(Mathf.Approximately(support.Hp, woundedSupport), "Monster4 cannot heal itself into an endless encounter");

        GameObject playerPrefab = Resources.Load<GameObject>("Prefab/Player/Player");
        PlayerController contactPlayer = Object.Instantiate(playerPrefab, fixture.transform).GetComponent<PlayerController>();
        contactPlayer.enabled = false; contactPlayer.transform.position = support.transform.position;
        contactPlayer.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        contactPlayer.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        support.SetTarget(contactPlayer.transform); float playerHealth = contactPlayer.Hp; Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.7f);
        Check(Mathf.Approximately(contactPlayer.Hp, playerHealth) && support.State != EnemyAIController.EnemyState.Attack,
            "Overlapping the healing monster never causes an unannounced player hit");
        allies[1].TakeDamage(1f); float injuredAlly = allies[1].Hp;
        support.TakeDamage(support.MaxHp + 1f); yield return new WaitForSeconds(.7f);
        Check(support == null || (!aura.enabled && support.GetComponentsInChildren<LineRenderer>().Length == 0),
            "Killing the priority healer immediately removes every healing connection");
        Check(Mathf.Approximately(allies[1].Hp, injuredAlly), "A killed healer cannot complete another healing pulse");
    }

    private static void Set(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new InvalidOperationException(target.GetType().Name + "." + fieldName);
        field.SetValue(target, value);
    }
    private static void Check(bool passed, string description)
    { Results.Add((passed ? "PASS: " : "FAIL: ") + description); if (!passed) throw new InvalidOperationException(description); }
    private void Finish()
    {
        if (fixture != null) Object.Destroy(fixture);
        Time.timeScale = originalTimeScale;
        Succeeded = Results.Count > 0 && Results.All(result => result.StartsWith("PASS:")); Running = false;
        Debug.Log("Chapter support checks: " + string.Join("\n", Results)); Object.Destroy(gameObject);
    }
    private void OnDestroy() { if (Running) Finish(); }
}
#endif
