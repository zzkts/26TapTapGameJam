using System.Collections.Generic;
using UnityEngine;

public class CombatStatus : MonoBehaviour
{
    public static readonly HashSet<CombatStatus> Active = new HashSet<CombatStatus>();
    public CombatElement Elements { get; private set; }
    private readonly Dictionary<CombatElement, float> expiry = new Dictionary<CombatElement, float>();
    private BulletSnapshot settings;
    private IActor actor;
    private EnemyAIController enemy;
    private PlayerController player;
    private SpriteRenderer visual;
    private SpriteRenderer tint;
    private float nextTick;
    public float MaxHealth => enemy != null ? enemy.MaxHp : player != null ? player.maxHp : 0f;
    public bool Dead => enemy != null ? enemy.IsDead : player == null || player.IsDead;
    public static CombatStatus Get(Component target)
    {
        if (target == null) return null;
        var enemy = target.GetComponentInParent<EnemyAIController>();
        var player = target.GetComponentInParent<PlayerController>();
        GameObject obj = enemy != null ? enemy.gameObject : player != null ? player.gameObject : null;
        if (obj == null) return null;
        CombatStatus status = obj.GetComponent<CombatStatus>();
        return status != null ? status : obj.AddComponent<CombatStatus>();
    }
    private void Awake()
    {
        actor = GetComponent<IActor>();
        enemy = GetComponent<EnemyAIController>();
        player = GetComponent<PlayerController>();
        visual = GetComponentInChildren<SpriteRenderer>();
    }
    private void OnEnable() => Active.Add(this);
    private void OnDisable() { Active.Remove(this); if (tint != null) tint.enabled = false; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => Active.Clear();
    public bool Has(CombatElement element) => (Elements & element) != 0;

    public void Apply(CombatElement incoming, BulletSnapshot shot)
    {
        if (Dead || incoming == CombatElement.None || shot == null) return;
        settings = shot.Copy();
        if (((incoming & CombatElement.Water) != 0 && Has(CombatElement.Fire)) ||
            ((incoming & CombatElement.Fire) != 0 && Has(CombatElement.Water)))
        {
            Remove(CombatElement.Fire);
            incoming &= ~CombatElement.Fire;
            ElementArea.Create(CombatElement.Steam, transform.position, 0.6f, 4f, shot);
        }
        foreach (CombatElement flag in new[] { CombatElement.Water, CombatElement.Fire, CombatElement.Poison, CombatElement.Electric })
            if ((incoming & flag) != 0)
            {
                Elements |= flag;
                expiry[flag] = Time.time + shot.statusDuration;
            }
        if ((incoming & CombatElement.Electric) != 0 && Has(CombatElement.Water))
            ElementArea.Conduct(transform.position, shot, this);
    }
    public void Electrify(BulletSnapshot shot)
    {
        if (Dead) return;
        settings = shot.Copy();
        Elements |= CombatElement.Electric;
        expiry[CombatElement.Electric] = Time.time + shot.statusDuration;
        actor?.TakeDamage(shot.electricDamage);
    }
    private void Remove(CombatElement element) { Elements &= ~element; expiry.Remove(element); }
    private void Update()
    {
        if (Dead) { if (tint != null) tint.enabled = false; return; }
        foreach (CombatElement element in new[] { CombatElement.Water, CombatElement.Fire, CombatElement.Poison, CombatElement.Electric })
            if (expiry.TryGetValue(element, out float until) && Time.time >= until) Remove(element);
        if (settings == null || Time.time < nextTick) return;
        nextTick = Time.time + 1f;
        if (Has(CombatElement.Fire)) actor?.TakeDamage(MaxHealth * settings.firePercent);
        if (Has(CombatElement.Poison)) PoisonTick(settings.poisonDamage);
    }
    public void PoisonTick(float damage)
    {
        float resistance = player != null ? player.PoisonResistance : 0f;
        float delta = damage * (resistance - 1f);
        if (delta > 0f) { if (player != null) player.Heal(delta); else enemy?.Heal(delta); }
        else if (delta < 0f) actor?.TakeDamage(-delta);
    }
    private void LateUpdate()
    {
        bool visible = !Dead && Elements != CombatElement.None && visual != null;
        if (!visible) { if (tint != null) tint.enabled = false; return; }
        if (tint == null)
        {
            var obj = new GameObject("Element Tint");
            obj.transform.SetParent(visual.transform, false);
            tint = obj.AddComponent<SpriteRenderer>();
            tint.sharedMaterial = visual.sharedMaterial;
        }
        tint.enabled = true;
        tint.sprite = visual.sprite;
        tint.flipX = visual.flipX;
        tint.sortingLayerID = visual.sortingLayerID;
        tint.sortingOrder = visual.sortingOrder + 1;
        Color color = Has(CombatElement.Fire) ? new Color(1f, 0.3f, 0.2f) :
            Has(CombatElement.Poison) ? new Color(0.4f, 1f, 0.25f) : new Color(0.3f, 0.85f, 1f);
        color.a = Has(CombatElement.Electric) ? Mathf.PingPong(Time.time * 8f, 0.6f) : 0.3f;
        tint.color = color;
    }
}
