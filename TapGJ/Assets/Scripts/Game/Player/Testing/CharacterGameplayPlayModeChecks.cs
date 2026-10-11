#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public class CharacterGameplayPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results = new List<string>();
    public static bool Running { get; private set; }
    private PlayerController player;
    private Rigidbody2D body;
    private Vector3 start;
    private Keyboard keyboard, previousKeyboard;
    private bool previousKeyboardEnabled;
    private InputSettings.BackgroundBehavior previousBackground;
    private GeneInventory inventory;
    private GeneChainController chain;
    private GeneEditStation station;
    private GeneEditPanel panel;
    private GameObject fixture;
    private EnemyAIController mouseSource;
    private readonly List<Behaviour> paused = new List<Behaviour>();

    public void Begin()
    {
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
            try { if (!tests.MoveNext()) break; wait = tests.Current; }
            catch (Exception error) { Results.Add("FAIL: " + error); break; }
            yield return wait;
        }
        Cleanup();
        Running = false;
        Debug.Log("Character gameplay checks:\n" + string.Join("\n", Results));
    }
    private IEnumerator Cases()
    {
        player = FindFirstObjectByType<PlayerController>();
        body = player.GetComponent<Rigidbody2D>();
        start = player.transform.position;
        inventory = player.GetComponent<GeneInventory>();
        chain = player.GetComponent<GeneChainController>();
        station = GameObject.Find("Gene Edit Station").GetComponent<GeneEditStation>();
        panel = UIManager.Instance.GetPanel<GeneEditPanel>();
        mouseSource = GameObject.Find("MouseMonster").GetComponent<EnemyAIController>();
        foreach (EnemyAIController enemy in FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None))
        {
            Check(enemy.GetComponent<EnemyHealthBar>() != null, enemy.name + " has UGUI health bar");
            if (enemy.enabled) { paused.Add(enemy); enemy.enabled = false; }
            var aura = enemy.GetComponent<EnemySupportAura>();
            if (aura != null && aura.enabled) { paused.Add(aura); aura.enabled = false; }
        }
        previousKeyboard = Keyboard.current;
        previousKeyboardEnabled = previousKeyboard != null && previousKeyboard.enabled;
        if (previousKeyboardEnabled) InputSystem.DisableDevice(previousKeyboard);
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>();
        fixture = new GameObject("Character Test Fixture");
        yield return new WaitForSeconds(0.1f);
        Check(player.IsGrounded(), "Player starts grounded");
        Check(!player.DashUnlocked && !player.TryRoll(), "Dash is locked initially");
        Vector2 initial = body.position;
        Press(Key.D);
        yield return new WaitForSeconds(0.4f);
        Press();
        yield return null;
        Check(body.position.x > initial.x + 0.2f, "D input moves the real player (delta=" + (body.position.x - initial.x) + ")");
        Press(Key.W);
        yield return new WaitForFixedUpdate();
        yield return null;
        Check(player.YVelocity > 0f && !player.IsGrounded(), "W input enters jump");
        Press();
        yield return new WaitForSeconds(1.5f);

        // Leaving support immediately before W validates coyote time through the input system.
        var floor = new GameObject("Coyote Floor", typeof(BoxCollider2D));
        floor.transform.SetParent(fixture.transform);
        floor.transform.position = new Vector3(1000f, 0f, 0f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(4f, 0.5f);
        Teleport(new Vector2(1000f, 1f));
        yield return new WaitForSeconds(0.15f);
        Check(player.IsGrounded(), "Coyote fixture is grounded");
        Teleport(new Vector2(1004f, body.position.y));
        yield return null;
        Press(Key.W);
        yield return new WaitForFixedUpdate();
        yield return null;
        Check(player.YVelocity > 0f, "Jump works within coyote time after leaving ground");
        Press();
        Teleport(start);
        yield return new WaitForSeconds(0.2f);

        var platform = GameObject.Find("Practice One Way Platform").GetComponent<BoxCollider2D>();
        Teleport(new Vector2(platform.bounds.center.x, platform.bounds.max.y + 0.74f));
        yield return new WaitForSeconds(0.2f);
        Check(player.IsGrounded(), "Player can stand on one-way platform (height=" + (body.position.y - platform.bounds.max.y) + ")");
        Press(Key.S, Key.Space);
        yield return new WaitForSeconds(0.25f);
        Press();
        Check(body.position.y < platform.bounds.max.y + 0.65f && player.YVelocity < 0f,
            "S + jump passes down through platform (height=" + (body.position.y - platform.bounds.max.y) + ", vy=" + player.YVelocity + ")");
        yield return new WaitForSeconds(0.8f);
        Check(!Physics2D.GetIgnoreCollision(player.GetComponent<Collider2D>(), platform), "Platform collision is restored after dropping");

        var dash = GameObject.Find("Dash Module").GetComponent<PlayerSupply>();
        Teleport(dash.transform.position + Vector3.up * 0.6f);
        Check(dash.Interact(player) && player.DashUnlocked, "Dash pickup unlocks ability");
        Teleport(start + Vector3.up * 2f);
        Check(player.TryRoll() && player.IsDashing && body.gravityScale == 0f, "Dash works in midair");
        float hp = player.hp;
        player.TakeDamage(1f);
        Check(player.hp == hp && !player.TryJump(), "Dash blocks damage and jump input");
        yield return new WaitForSeconds(0.3f);
        Check(!player.IsDashing && body.gravityScale > 0f && !player.TryRoll(), "Dash ends and cooldown blocks reuse");
        Teleport(start);
        yield return new WaitForSeconds(0.2f);
        player.TakeDamageFrom(2f, player.transform.position + Vector3.right);
        player.TakeDamage(2f);
        Check(Mathf.Approximately(player.hp, hp - 2f) && player.IsInvulnerable, "Hurt immunity prevents duplicate damage");
        Check(Time.timeScale == 0f && player.gameObject.layer == LayerMask.NameToLayer("PlayerInvulnerable"), "Hit-stop and invulnerable layer activate");
        yield return new WaitForSecondsRealtime(0.15f);
        Check(Time.timeScale == 1f, "Hit-stop releases on unscaled time");
        yield return new WaitForSeconds(0.85f);
        Check(!player.IsInvulnerable && player.gameObject.layer == LayerMask.NameToLayer("Player"), "Hurt immunity restores normal layer");
        player.Heal(999f);
        Check(player.hp == player.maxHp, "Healing clamps at maximum health");
        Teleport(start);
        yield return new WaitForSeconds(0.15f);

        foreach (GenePickup pickup in FindObjectsByType<GenePickup>(FindObjectsSortMode.None)) pickup.Interact(player);
        Check(inventory.Records.Count >= 3, "Nearby gene pickups populate inventory");
        GeneRecord speed = inventory.Records.First(r => r.geneId == "speed");
        GeneRecord water = inventory.Records.First(r => r.geneId == "diffusion");
        GeneRecord fire = inventory.Records.First(r => r.geneId == "ember");
        string[] empty = chain.CopySlots();
        Teleport(start + Vector3.right * 10f);
        Check(!station.Interact(player) && !chain.Commit(station, empty), "Gene editing is denied outside a station");
        Teleport(start);
        Check(station.Interact(player) && player.MenuOpen && Time.timeScale == 0f, "Station opens UGUI and suspends combat");
        panel.SelectSlot(0);
        panel.Equip(speed.instanceId);
        panel.Cancel();
        Check(chain.CopySlots().All(string.IsNullOrEmpty) && !player.MenuOpen && Time.timeScale == 1f, "Cancel discards the draft and resumes");
        station.Interact(player);
        panel.SelectSlot(0); panel.Equip(speed.instanceId);
        panel.SelectSlot(1); panel.Equip(water.instanceId);
        panel.SelectSlot(3); panel.Equip(fire.instanceId);
        panel.confirmButton.onClick.Invoke();
        Check(chain.AttributeCount == 2 && chain.CurrentAttribute.geneId == "diffusion", "Confirm builds ordered attributes and skips modifications");
        BulletSnapshot beforeEvolution = chain.Capture(player.Weapon.BaseData);
        Check(Mathf.Approximately(beforeEvolution.speed, player.Weapon.BaseData.projectileSpeed * 1.25f), "Modification applies to ordinary shots");
        Check(chain.TryActivate() && chain.Capture(player.Weapon.BaseData).element == CombatElement.Water, "Right-click activation starts first attribute");
        chain.Tick(chain.CurrentAttribute.enchantDuration + 0.01f);
        Check(chain.State == GeneChainController.Phase.Ready && chain.CurrentAttribute.geneId == "ember", "Next attribute waits for a separate activation");
        chain.TryActivate();
        BulletSnapshot fireSnapshot = chain.Capture(player.Weapon.BaseData);
        Check(fireSnapshot.element == CombatElement.Fire, "Active attribute is included in the shot snapshot");
        chain.Tick(chain.CurrentAttribute.enchantDuration + 0.01f);
        Check(chain.State == GeneChainController.Phase.Reloading && !chain.TryActivate(), "Last attribute starts whole-chain reload");
        Check(Mathf.Approximately(chain.Capture(player.Weapon.BaseData).speed, beforeEvolution.speed), "Modifications stay active while reloading");
        chain.Tick(inventory.Rules.reloadDuration + 0.01f);
        Check(chain.State == GeneChainController.Phase.Ready && chain.AttributeIndex == 0, "Reload returns to first attribute");
        inventory.Acquire(inventory.Rules.Find("speed"));
        inventory.Acquire(inventory.Rules.Find("speed"));
        Check(speed.level == 2 && Mathf.Approximately(beforeEvolution.speed, 6.25f) &&
            Mathf.Approximately(chain.Capture(player.Weapon.BaseData).speed, 7.5f), "Evolution changes future shots and preserves old snapshots");
        GeneRecord nextSpeed = inventory.Acquire(inventory.Rules.Find("speed"));
        Check(nextSpeed.instanceId != speed.instanceId && nextSpeed.level == 1, "Copies after evolution create a distinct instance");
        string[] draft = new string[empty.Length];
        draft[0] = speed.instanceId; draft[1] = speed.instanceId;
        Check(!chain.Commit(station, draft), "One instance cannot occupy two slots");
        draft[1] = nextSpeed.instanceId;
        Check(chain.Commit(station, draft) && !chain.TryActivate(), "Distinct same-gene instances stack without empty reload loops");
        chain.Tick(100f);
        Check(chain.State == GeneChainController.Phase.Ready, "Modification-only chain remains ready");
        draft = new string[empty.Length]; draft[2] = water.instanceId;
        chain.Commit(station, draft); chain.TryActivate(); chain.Tick(10f);
        Check(chain.State == GeneChainController.Phase.Reloading, "Single attribute reloads after its phase");
        Check(fireSnapshot.element == CombatElement.Fire, "Editing does not mutate a previous elemental snapshot");
        chain.Commit(station, empty);

        var chest = GameObject.Find("Gene Cold Storage").GetComponent<GeneChest>();
        Teleport(chest.transform.position + Vector3.up * 0.7f);
        int oldDrops = FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Length;
        Check(chest.Interact(player) && !chest.Interact(player), "Cold storage opens once");
        Check(FindObjectsByType<GenePickup>(FindObjectsSortMode.None).Length == oldDrops + 2, "Cold storage uses shared drop service for two genes");
        Teleport(start);

        EnemyAIController victim = Target(new Vector2(1010f, 10f));
        float initialHp = victim.Hp;
        BulletSnapshot shot = new BulletSnapshot(player.Weapon.BaseData, inventory.Rules);
        shot.speed = 40f; shot.element = CombatElement.Fire;
        Fire(shot, new Vector2(1008f, 10f), Vector2.right);
        yield return new WaitForSeconds(0.12f);
        Check(victim.Hp < initialHp, "Swept projectile hits a target at high speed");
        Check(victim.GetComponent<EnemyHealthBar>().DisplayedRatio < 1f, "Enemy UGUI health bar follows damage");
        CombatStatus status = CombatStatus.Get(victim);
        status.Apply(CombatElement.Water, shot);
        Check(!status.Has(CombatElement.Fire) && status.Has(CombatElement.Water) &&
            ElementArea.Active.Any(a => a != null && a.Element == CombatElement.Steam), "Water extinguishes fire and creates steam");

        EnemyAIController first = Target(new Vector2(1020f, 10f));
        EnemyAIController second = Target(new Vector2(1022f, 10f));
        shot = new BulletSnapshot(player.Weapon.BaseData, inventory.Rules) { speed = 20f, pierce = 1 };
        Fire(shot, new Vector2(1018f, 10f), Vector2.right);
        yield return new WaitForSeconds(0.3f);
        Check(Mathf.Approximately(first.Hp, first.MaxHp - shot.damage) && Mathf.Approximately(second.Hp, second.MaxHp - shot.damage),
            "Piercing hits two actors once each");

        var wall = new GameObject("Projectile Wall", typeof(BoxCollider2D));
        wall.transform.SetParent(fixture.transform); wall.transform.position = new Vector3(1031f, 10f, 0f);
        wall.GetComponent<BoxCollider2D>().size = new Vector2(0.02f, 5f);
        shot = new BulletSnapshot(player.Weapon.BaseData, inventory.Rules) { speed = 10f, bounces = 1 };
        WaterProjectile bounce = Fire(shot, new Vector2(1030f, 10f), Vector2.right);
        yield return new WaitForSeconds(0.2f);
        Check(bounce != null && !bounce.Spent && bounce.transform.position.x < 1030.8f, "Projectile reflects from a thin wall");

        Target(new Vector2(1041f, 10f));
        shot = new BulletSnapshot(player.Weapon.BaseData, inventory.Rules) { speed = 10f, element = CombatElement.Fire, maxGeneration = 1, maxSpawn = 3 };
        shot.effects = new[] { new ResolvedGeneEffect(GeneEffectKind.Split, GeneTrigger.OnHit, 2f, false) };
        var budget = new ShotSpawnBudget(3); budget.Take();
        Fire(shot, new Vector2(1040f, 10f), Vector2.right, budget);
        yield return new WaitForSeconds(0.2f);
        var children = FindObjectsByType<WaterProjectile>(FindObjectsSortMode.None).Where(p => p.Generation == 1).ToArray();
        Check(children.Length == 2 && budget.Remaining == 0 && children.All(p => p.Snapshot.element == CombatElement.Fire),
            "Split children inherit attributes and share a bounded spawn budget");

        EnemyAIController poisonVictim = Target(new Vector2(1050f, 10f));
        shot = new BulletSnapshot(player.Weapon.BaseData, inventory.Rules);
        var poison = ElementArea.Create(CombatElement.Poison, poisonVictim.transform.position, 1f, 3f, shot);
        float beforeExplosion = poisonVictim.Hp;
        poison.React(CombatElement.Fire, shot);
        Check(Mathf.Approximately(poisonVictim.Hp, beforeExplosion - poisonVictim.MaxHp * shot.explosionFraction),
            "Fire detonates poison for configured percentage damage");
        EnemyAIController electricVictim = Target(new Vector2(1050.5f, 10f));
        CombatStatus.Get(poisonVictim).Apply(CombatElement.Water, shot);
        CombatStatus.Get(electricVictim).Apply(CombatElement.Water, shot);
        float beforeElectric = electricVictim.Hp;
        CombatStatus.Get(poisonVictim).Apply(CombatElement.Electric, shot);
        Check(electricVictim.Hp < beforeElectric, "Electricity propagates between nearby wet actors");

        Teleport(start);
        player.Heal(999f);
        player.TakeDamage(player.maxHp);
        Check(player.IsDead && !player.TryAttack() && !player.TryRoll() && player.Heal(5f) == 0f, "Death blocks combat and healing");
        yield return new WaitForSecondsRealtime(0.85f);
        Check(UIManager.Instance.GetPanel<RunMenuPanel>().gameObject.activeSelf, "Death shows the UGUI restart screen");
    }
    private EnemyAIController Target(Vector2 position)
    {
        EnemyAIController target = Instantiate(mouseSource, position, Quaternion.identity, fixture.transform);
        target.name = "Character Check Enemy"; target.enabled = false;
        var rb = target.GetComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.constraints = RigidbodyConstraints2D.FreezeAll;
        EnemyGeneDrop drop = target.GetComponent<EnemyGeneDrop>(); if (drop != null) drop.rules = null;
        Physics2D.SyncTransforms();
        return target;
    }
    private WaterProjectile Fire(BulletSnapshot shot, Vector2 position, Vector2 heading, ShotSpawnBudget budget = null)
    {
        GameObject prefab = player.Weapon.BaseData.projectilePrefab;
        var bullet = Instantiate(prefab, position, Quaternion.identity).GetComponent<WaterProjectile>();
        bullet.Launch(shot, position, heading, LayerMask.GetMask("Enemy"), player.transform, prefab, budget ?? new ShotSpawnBudget(24));
        return bullet;
    }
    private void Teleport(Vector2 position)
    {
        player.transform.position = position;
        body.position = position;
        body.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
    }
    private void Press(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private void Check(bool ok, string message) => Results.Add((ok ? "PASS: " : "FAIL: ") + message);
    private void Cleanup()
    {
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        InputSystem.settings.backgroundBehavior = previousBackground;
        if (previousKeyboard != null)
        {
            if (previousKeyboardEnabled) InputSystem.EnableDevice(previousKeyboard);
            previousKeyboard.MakeCurrent();
        }
        foreach (Behaviour behaviour in paused) if (behaviour != null) behaviour.enabled = true;
        if (fixture != null) Destroy(fixture);
        foreach (WaterProjectile projectile in FindObjectsByType<WaterProjectile>(FindObjectsSortMode.None))
            if (projectile.transform.position.x > 900f) Destroy(projectile.gameObject);
        foreach (ElementArea area in new List<ElementArea>(ElementArea.Active))
            if (area != null && area.transform.position.x > 900f) Destroy(area.gameObject);
    }
    private void OnDisable()
    {
        if (keyboard == null) return;
        InputSystem.RemoveDevice(keyboard);
        keyboard = null;
        InputSystem.settings.backgroundBehavior = previousBackground;
        if (previousKeyboardEnabled && previousKeyboard != null) InputSystem.EnableDevice(previousKeyboard);
    }
}
#endif
