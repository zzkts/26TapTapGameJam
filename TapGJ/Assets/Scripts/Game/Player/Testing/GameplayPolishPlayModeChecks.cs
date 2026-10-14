#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

// Runs against the actual prefabs through MCP; fixtures are never saved to the scene.
public class GameplayPolishPlayModeChecks : MonoBehaviour
{
    public static readonly List<string> Results = new();
    public static bool Running { get; private set; }
    private readonly List<GameObject> fixtures = new();
    private readonly List<Behaviour> suspended = new();
    private Keyboard keyboard;
    private Mouse mouse;
    private Keyboard previousKeyboard;
    private Mouse previousMouse;
    private InputSettings.BackgroundBehavior previousBackground;
    private GameObject rig;
    private PlayerController player;
    private int rigIndex;
    private Vector2 mousePosition;
    private bool mouseHeld;

    public void Begin()
    {
        Results.Clear(); Running = true;
        previousKeyboard = Keyboard.current; previousMouse = Mouse.current;
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
        foreach (Behaviour behaviour in Object.FindObjectsByType<Behaviour>(FindObjectsSortMode.None))
            if (behaviour.enabled && (behaviour is PlayerController || behaviour is PlayerInteraction ||
                behaviour is PlayerHudPanel || behaviour is EnemyAIController || behaviour is EnemySupportAura))
            { suspended.Add(behaviour); behaviour.enabled = false; }
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Cases());
        while (stack.Count > 0)
        {
            object wait = null;
            try
            {
                IEnumerator current = stack.Peek();
                if (!current.MoveNext()) { stack.Pop(); continue; }
                wait = current.Current;
                if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            }
            catch (Exception error) { Results.Add("FAIL: exception " + error); break; }
            yield return wait;
        }
        Cleanup(); Running = false;
        Debug.Log("Gameplay polish checks: " + string.Join("\n", Results));
    }

    private IEnumerator Cases()
    {
        Check(Object.FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None)
            .All(e => e.GetComponent<EnemyHealthBar>() != null), "Every scene enemy owns a health bar");
        PlayerHudPanel hud = Object.FindFirstObjectByType<PlayerHudPanel>(FindObjectsInactive.Include);
        Check(hud.healthFill.sprite != null && hud.stageFill.sprite != null, "Filled HUD images have a sprite so their fill amount renders");
        yield return PlayerCases();
        yield return EnemyCases();
        yield return GeneCases();
        yield return ElementCases();
        yield return ProjectileCases();
    }

    private IEnumerator PlayerCases()
    {
        NewRig(true); yield return new WaitForSeconds(.25f);
        Check(player.IsGrounded() && Mathf.Abs(player.GetComponent<Collider2D>().bounds.min.y + .9f) < .04f,
            "Player feet settle on the floor");
        Check(!player.DashUnlocked && !player.TryRoll(), "Dash is locked before collecting the module");
        float start = player.transform.position.x;
        Aim(player.transform.position + Vector3.right * 5f); Keys(Key.D); MouseButtons(true);
        yield return new WaitForSeconds(.65f);
        Check(player.transform.position.x > start + 1.5f, "Holding fire does not lock horizontal movement");
        Check(rig.GetComponentsInChildren<Projectile>().Length == 0 &&
            Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Count(p => p.Snapshot != null) >= 3,
            "Held mouse produces repeated shots");
        Keys(Key.D, Key.Space); yield return null; yield return new WaitForFixedUpdate();
        Check(player.YVelocity > 2f && !player.IsGrounded(), "Space jumps while moving and shooting");
        Keys(); MouseButtons(false); yield return new WaitForSeconds(.1f);
        player.UnlockDash(); Aim(player.transform.position + Vector3.right * 5f); yield return null;
        Keys(Key.LeftShift); yield return null; yield return new WaitForFixedUpdate();
        float hp = player.Hp;
        player.TakeDamage(1f);
        Check(player.IsDashing && player.IsInvulnerable && player.Hp == hp, "Air dash blocks incoming damage");
        Check(player.gameObject.layer == LayerMask.NameToLayer("PlayerInvulnerable"), "Dash uses the invulnerable collision layer");
        Check(player.GetComponent<Rigidbody2D>().gravityScale == 0f, "Dash temporarily disables gravity");
        Keys(); yield return new WaitForSeconds(.24f);
        Check(!player.IsDashing && player.GetComponent<Rigidbody2D>().gravityScale > 0f && !player.TryRoll(),
            "Dash restores gravity and enforces cooldown");
        player.TakeDamageFrom(1f, (Vector2)player.transform.position + Vector2.right);
        Check(player.Hp == hp - 1f && player.IsHurt && Time.timeScale == 0f, "Damage enters hurt and hit-stop");
        Check(player.gameObject.layer == LayerMask.NameToLayer("PlayerInvulnerable"), "Hurt invulnerability uses the invulnerable collision layer");
        Check(player.GetComponent<Rigidbody2D>().linearVelocityX < 0f, "Knockback points away from the source");
        player.TakeDamage(1f); Check(player.Hp == hp - 1f, "Invulnerability rejects a second hit");
        yield return new WaitForSecondsRealtime(.14f);
        Check(Time.timeScale == 1f, "Hit-stop resumes real time");
        yield return new WaitForSeconds(.8f);
        Check(!player.IsInvulnerable && player.gameObject.layer == LayerMask.NameToLayer("Player"),
            "Invulnerability restores the normal player layer");
        Check(player.Heal(100f) == 1f && player.Hp == player.MaxHp, "Healing is clamped to max HP");

        NewRig(true); yield return new WaitForSeconds(.2f);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.position += Vector2.right * 25f; Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();
        Keys(Key.Space); yield return null; yield return new WaitForFixedUpdate();
        Check(player.YVelocity > 2f, "Coyote time accepts a jump just after leaving the floor");
        Keys();

        NewRig(false);
        BoxCollider2D platform = Floor(new Vector2(0f, -1f), new Vector2(8f, .2f));
        platform.gameObject.AddComponent<OneWayPlatform>();
        PlatformEffector2D effector = platform.gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true; platform.usedByEffector = true;
        yield return new WaitForSeconds(.2f);
        Check(player.TryDownPlatform(), "Down-jump starts ignoring a one-way platform");
        yield return new WaitForSeconds(.65f);
        Check(player.GetComponent<Collider2D>().bounds.max.y < platform.bounds.min.y &&
            !Physics2D.GetIgnoreCollision(player.GetComponent<Collider2D>(), platform),
            "Platform collision is restored after falling clear");

        NewRig(true); player.GetComponent<Rigidbody2D>().position += Vector2.up * .13f;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.down * 2f;
        Keys(Key.Space); yield return null; Keys(); yield return new WaitForSeconds(.08f);
        Check(player.YVelocity > 0f, "Jump buffer fires on landing");
    }

    private IEnumerator EnemyCases()
    {
        foreach (int id in new[] {3, 5})
        {
            NewRig(true); player.enabled = false;
            EnemyAIController enemy = Enemy(id, Vector2.zero, false);
            enemy.SetTarget(player.transform); yield return null;
            Check(player.IsDead, "Monster" + id + " applies the designed 10 contact damage");
            Check(enemy.Kind == EnemyAIController.AttackType.Melee, "Monster" + id + " is melee");
        }
        foreach (int id in new[] {6, 7})
        {
            NewRig(true); player.enabled = false;
            EnemyAIController enemy = Enemy(id, new Vector2(-4f, 0f), false);
            enemy.SetTarget(player.transform);
            yield return new WaitForSeconds(.35f);
            JellyfishProjectile bullet = Object.FindObjectsByType<JellyfishProjectile>(FindObjectsSortMode.None)
                .FirstOrDefault(p => p.name.StartsWith("Monster" + id + "Bullet"));
            var prefab = Resources.Load<GameObject>("Prefab/Enemy/Ranged/Monster" + id + "Bullet");
            Check(bullet != null && bullet.GetComponentInChildren<SpriteRenderer>().sprite == prefab.GetComponentInChildren<SpriteRenderer>().sprite,
                "Monster" + id + " fires its dedicated sprite");
            Check(enemy.Kind == EnemyAIController.AttackType.Ranged, "Monster" + id + " is ranged");
            yield return new WaitForSeconds(.4f);
            Check(enemy.GetComponent<Rigidbody2D>().linearVelocityX < 0f, "Monster" + id + " retreats inside the danger radius");
            yield return new WaitForSeconds(.4f);
            Check(player.Hp < player.MaxHp, "Monster" + id + " projectile actually damages the player");
        }

        foreach (int id in new[] {3, 7})
        {
            NewRig(true); player.enabled = false; player.transform.position += Vector3.right * 50f;
            EnemyAIController enemy = Enemy(id, new Vector2(0f, id == 3 ? -.5f : 0f), true);
            Floor(new Vector2(1.2f, 0f), new Vector2(.1f, 4f));
            bool reversed = false, returned = false;
            float until = Time.time + 1.7f;
            while (Time.time < until)
            {
                reversed |= enemy.GetComponentInChildren<SpriteRenderer>().flipX;
                returned |= enemy.GetComponent<Rigidbody2D>().linearVelocityX < -.5f;
                yield return null;
            }
            Check(reversed && returned, "Monster" + id + " turns and moves away from a patrol wall");
        }
        NewRig(false); player.enabled = false; player.transform.position += Vector3.right * 50f;
        Floor(new Vector2(0f, -1f), new Vector2(3f, .2f));
        EnemyAIController ledge = Enemy(3, new Vector2(0f, -.5f), true);
        yield return new WaitForSeconds(1.4f);
        Check(ledge.transform.localPosition.x < 1f && ledge.transform.localPosition.y > -.6f,
            "Ground patrol reverses at a ledge without falling");

        NewRig(true); player.enabled = false; player.transform.position += Vector3.right * 50f;
        EnemyAIController support = Enemy(4, Vector2.zero, false);
        EnemySupportAura aura = support.GetComponent<EnemySupportAura>();
        Check(aura.HealCooldown == 5f && aura.HealAmount == 20f && aura.MaxTargets == 4, "Support defaults match the design");
        Set(aura, "healCooldown", .6f); Set(aura, "cooldownTimer", .6f);
        var allies = new List<EnemyAIController>();
        for (int i = 0; i < 5; i++)
        {
            EnemyAIController ally = Enemy(5, new Vector2(1f + i * 1.1f, 0f), false);
            ally.enabled = false; ally.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            ally.TakeDamage(3f); allies.Add(ally); yield return new WaitForSeconds(.04f);
        }
        yield return new WaitForSeconds(.45f);
        Check(allies.Take(4).All(e => e.Hp == e.MaxHp) && allies[4].Hp < allies[4].MaxHp,
            "Support heals the first four arrivals, not the fifth");
        Check(support.GetComponentsInChildren<LineRenderer>().Length == 4, "Support renders four healing beams");
        allies[0].transform.position += Vector3.right * 20f;
        yield return new WaitForSeconds(.65f);
        Check(allies[4].Hp == allies[4].MaxHp, "A waiting ally is healed after a priority slot becomes free");
        support.TakeDamage(100f); yield return null;
        Check(!aura.enabled && support.GetComponentsInChildren<LineRenderer>().Length == 0,
            "Dead support stops healing and removes its beams");
    }

    private IEnumerator GeneCases()
    {
        NewRig(true); yield return new WaitForSeconds(.2f);
        GeneInventory inv = player.Genes;
        Modifier waterAsset = Resources.Load<Modifier>("SO/Genes/water");
        Modifier fireAsset = Resources.Load<Modifier>("SO/Genes/ember");
        inv.Acquire(waterAsset); inv.Acquire(waterAsset); inv.Acquire(waterAsset); inv.Acquire(waterAsset); inv.Acquire(fireAsset);
        Modifier water = inv.GenInventory.First(g => g.Definition == waterAsset && g.Level == 2);
        Modifier second = inv.GenInventory.First(g => g.Definition == waterAsset && g.Level == 1);
        Modifier fire = inv.GenInventory.First(g => g.Definition == fireAsset);
        Check(waterAsset.Level == 1 && water != second && water.Copies == 3, "Evolution creates independent instances without mutating assets");
        GeneEditStation station = new GameObject("Test Terminal").AddComponent<GeneEditStation>();
        station.transform.SetParent(rig.transform, false);
        var draft = new Modifier[inv.SlotCount]; draft[0] = water; draft[1] = water;
        Check(!inv.Commit(station, player, draft), "The same instance cannot occupy two slots");
        draft[1] = second; Check(inv.Commit(station, player, draft), "Distinct instances with the same name may be equipped");
        station.transform.localPosition = Vector3.right * 10f;
        Check(!inv.Commit(station, player, new Modifier[inv.SlotCount]), "Equipment changes require a nearby terminal");
        station.transform.localPosition = Vector3.zero;
        GeneEditPanel editor = Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include);
        editor.Open(player, station); editor.SelectSlot(0); editor.Equip(inv.GenInventory.ToList().IndexOf(fire));
        UnityEngine.Canvas.ForceUpdateCanvases();
        Check(editor.inventoryContent.GetComponentsInChildren<UnityEngine.UI.Button>().All(row => row.GetComponent<RectTransform>().rect.width > 100f),
            "Inventory rows have nonzero clickable widths");
        Check(inv.GenChain[0] == water && editor.CopyDraft()[0] == fire && Time.timeScale == 0f, "Editor modifies a paused draft, not the equipped chain");
        editor.Cancel(); Check(inv.GenChain[0] == water && !player.MenuOpen && Time.timeScale == 1f, "Cancel discards the draft and resumes gameplay");
        editor.Open(player, station); editor.SelectSlot(1); editor.Equip(inv.GenInventory.ToList().IndexOf(fire)); editor.Confirm();
        Check(inv.GenChain[1] == fire && !player.MenuOpen, "Confirm commits the selected instance");
        GeneChainController chain = player.GetComponent<GeneChainController>();
        Check(chain.Attributes.Count == 2 && chain.TryActivate() && chain.ActiveAttribute == water, "Right-click activation starts the first equipped attribute");
        player.Weapon.Attack(player.transform.position + Vector3.up * 2f, Vector2.right);
        Projectile firstShot = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).First(p => p.Snapshot != null);
        chain.Tick(water.EnchantDuration + .01f);
        Check(chain.State == GeneChainController.Phase.Ready && chain.CurrentAttribute == fire && chain.TryActivate(), "The second attribute waits for its own activation");
        Check(firstShot.Snapshot.element == E_ElementType.Water, "Already-fired bullets keep their own element snapshot");
        chain.Tick(fire.EnchantDuration + .01f);
        Check(chain.State == GeneChainController.Phase.Reloading && !chain.TryActivate(), "The last attribute enters the shared reload");
        chain.Tick(chain.reloadDuration + .01f);
        Check(chain.State == GeneChainController.Phase.Ready && chain.CurrentAttribute == water, "Reload returns to the first attribute");
        inv.Commit(station, player, new Modifier[inv.SlotCount]);
        Check(!chain.TryActivate() && chain.State == GeneChainController.Phase.Ready, "No attributes means no reload cycle");

        RunMenuPanel menu = Object.FindFirstObjectByType<RunMenuPanel>(FindObjectsInactive.Include);
        menu.Open(player, false); yield return new WaitForSecondsRealtime(.15f);
        Check(player.MenuOpen && Time.timeScale == 0f && menu.GetComponent<CanvasGroup>().alpha == 1f, "Pause menu animates while time is frozen");
        menu.settingsButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(.15f);
        SettingPanel settings = Object.FindFirstObjectByType<SettingPanel>();
        Check(settings != null && settings.gameObject.activeSelf, "Settings opens from the pause menu");
        Keys(Key.Escape); yield return null; Keys(); yield return new WaitForSecondsRealtime(.16f);
        Check(!settings.gameObject.activeSelf && player.MenuOpen, "Escape closes only settings and preserves pause");
        menu.resumeButton.onClick.Invoke(); Check(!player.MenuOpen && Time.timeScale == 1f, "Resume restores gameplay");
    }

    private IEnumerator ElementCases()
    {
        NewRig(true); player.enabled = false;
        EnemyAIController a = Enemy(5, new Vector2(3f, 0f), false); a.enabled = false;
        EnemyAIController b = Enemy(5, new Vector2(3.7f, 0f), false); b.enabled = false;
        EnemyAIController dry = Enemy(5, new Vector2(3.3f, 0f), false); dry.enabled = false;
        a.AddBuff(E_ElementType.Water); b.AddBuff(E_ElementType.Water);
        float hp = a.Hp, hpB = b.Hp, dryHp = dry.Hp;
        ActorElementEffects.Conduct(a.transform.position, .8f);
        Check(a.Hp == hp - 1f && b.Hp == hpB - 1f && dry.Hp == dryHp, "Electricity propagates once per wet target, skipping dry targets");
        a.RemBuff(E_ElementType.Electric); a.AddBuff(E_ElementType.Fire);
        Check(a.GetComponent<ActorElementEffects>().Has(E_ElementType.Water) && !a.GetComponent<ActorElementEffects>().Has(E_ElementType.Fire),
            "Steam consumes fire while retaining wetness");
        b.RemBuff(E_ElementType.Water); b.RemBuff(E_ElementType.Electric); b.AddBuff(E_ElementType.Poison);
        ElementField.Create(E_ElementType.Poison, b.transform.position, Vector2.one * 5f, 3f, LayerMask.GetMask("Enemy"));
        hpB = b.Hp; b.AddBuff(E_ElementType.Fire);
        Check(Mathf.Abs(b.Hp - (hpB - b.MaxHp * .3f)) < .001f && Object.FindObjectsByType<ExplosionReaction>(FindObjectsSortMode.None).Length == 0,
            "A burning poison field causes one 30-percent explosion, not two");
        b.RemBuff(E_ElementType.Fire); b.AddBuff(E_ElementType.Fire);
        Set(b.GetComponent<ActorElementEffects>(), "nextTick", Time.time);
        hpB = b.Hp; yield return null;
        Check(Mathf.Abs(b.Hp - (hpB - b.MaxHp * .05f)) < .001f, "Fire damage ticks at five percent of max HP");

        NewRig(true); player.enabled = false; player.hp = 5f;
        PlayerData original = (PlayerData)Get(player, "playerData");
        PlayerData resistance = Object.Instantiate(original); Set(player, "playerData", resistance);
        resistance.poisonResistance = 1f; player.AddBuff(E_ElementType.Poison);
        Set(player.GetComponent<ActorElementEffects>(), "nextTick", Time.time); yield return null;
        Check(player.Hp == 5f, "One-hundred-percent poison resistance prevents poison damage");
        resistance.poisonResistance = 1.5f; Set(player.GetComponent<ActorElementEffects>(), "nextTick", Time.time); yield return null;
        Check(player.Hp == 5.5f, "Resistance above one-hundred percent turns poison into healing");
        Set(player, "playerData", original); Object.Destroy(resistance);

        NewRig(true); player.enabled = false;
        EnemyAIController pulled = Enemy(5, new Vector2(2f, -.4f), false);
        float start = pulled.transform.position.x;
        ElementField.Create(E_ElementType.Water, (Vector2)rig.transform.position + Vector2.down * .4f,
            new Vector2(6f, 1f), 1f, LayerMask.GetMask("Enemy"), true);
        yield return new WaitForSeconds(.3f);
        Check(pulled.transform.position.x < start - .2f, "Vortex pulls an enemy while its AI is still running");
    }

    private IEnumerator ProjectileCases()
    {
        NewRig(true); player.enabled = false;
        EnemyAIController target = Enemy(5, new Vector2(3f, 2f), false); target.enabled = false;
        target.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var snapshot = new BulletSnapshot(player.Weapon.BaseData) { speed = 60f, splitCount = 3, element = E_ElementType.None, lifeTime = 3f, size = .1f };
        var budget = new ShotSpawnBudget(24); budget.Take();
        Projectile shot = Object.Instantiate(snapshot.projectilePrefab).GetComponent<Projectile>();
        shot.LaunchSnapshot((Vector2)rig.transform.position + Vector2.up * 2f, Vector2.right, LayerMask.GetMask("Enemy"), player.transform, snapshot, budget);
        float hp = target.Hp; Physics2D.SyncTransforms(); shot.Step(.1f); yield return null;
        Projectile[] children = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Where(p => p.Generation == 1).ToArray();
        Check(target.Hp == hp - snapshot.damage * snapshot.damageMultiplier && children.Length == 3, "Swept high-speed hit deals one hit and produces three split children");
        var block = new MaterialPropertyBlock();
        children[0].GetComponentInChildren<SpriteRenderer>().GetPropertyBlock(block);
        Check(block.GetFloat("_Mode") == 1f, "Projectile shader is in projectile mode");
        foreach (Projectile child in children) child.Step(.1f); yield return null;
        Check(target.Hp == hp - snapshot.damage * snapshot.damageMultiplier, "Split children cannot immediately re-hit the parent target");
        Check(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).All(p => p.Generation <= 1), "Splitting is capped at one generation");
        var limited = new ShotSpawnBudget(2);
        Check(limited.Take() && limited.Take() && !limited.Take(), "The shared shot budget cannot be exceeded");
        yield return ShaderCases();
    }

    private IEnumerator ShaderCases()
    {
        NewRig(false); player.enabled = false;
        var cameraObject = new GameObject("Shader Test Camera"); cameraObject.transform.SetParent(rig.transform, false);
        cameraObject.transform.localPosition = new Vector3(2f, 0f, -10f);
        Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1 << 10;
        var shots = new List<Projectile>();
        for (int i = 0; i < 3; i++)
        {
            var snap = new BulletSnapshot(player.Weapon.BaseData) {element = new[] {E_ElementType.Water,E_ElementType.Fire,E_ElementType.Poison}[i], speed = 0f, size = 1f};
            Projectile projectile = Object.Instantiate(snap.projectilePrefab).GetComponent<Projectile>();
            projectile.LaunchSnapshot((Vector2)rig.transform.position + Vector2.right * (2f*i), Vector2.right, 0, player.transform, snap, new ShotSpawnBudget(24));
            shots.Add(projectile);
        }
        yield return null;
        var render = new RenderTexture(600,300,24); var pixels = new Texture2D(600,300,TextureFormat.RGB24,false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
            pixels.ReadPixels(new Rect(0,0,600,300),0,0); pixels.Apply();
            Color water = pixels.GetPixel(100,150), fire = pixels.GetPixel(300,150), poison = pixels.GetPixel(500,150);
            Check(water.b > water.r && fire.r > fire.b && poison.g > poison.r,
                "Rendered water, fire and poison pixels keep independent colors");
            System.IO.File.WriteAllBytes("Temp/PolishChecks/element-shader.png",pixels.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; camera.targetTexture = null; render.Release(); Object.Destroy(render); Object.Destroy(pixels); }
    }

    private void NewRig(bool floor)
    {
        Keys(); MouseButtons(false); Time.timeScale = 1f;
        if (rig != null) { rig.SetActive(false); Object.Destroy(rig); }
        foreach (Projectile p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
        foreach (ElementField f in new List<ElementField>(ElementField.Active)) if (f != null) Object.Destroy(f.gameObject);
        rig = new GameObject("Polish Fixture " + rigIndex);
        rig.transform.position = new Vector3(1000f + 100f * rigIndex++, 0f, 0f); fixtures.Add(rig);
        if (floor) Floor(new Vector2(0f, -1f), new Vector2(40f, .2f));
        player = Object.Instantiate(Resources.Load<GameObject>("Prefab/Player/Player"), rig.transform).GetComponent<PlayerController>();
        player.transform.localPosition = Vector3.zero;
        Physics2D.SyncTransforms(); Aim(player.transform.position + Vector3.right * 5f);
    }
    private BoxCollider2D Floor(Vector2 position, Vector2 size)
    {
        var obj = new GameObject("Test Terrain"); obj.transform.SetParent(rig.transform, false); obj.transform.localPosition = position;
        obj.layer = LayerMask.NameToLayer("Ground"); var body = obj.AddComponent<BoxCollider2D>(); body.size = size; return body;
    }
    private EnemyAIController Enemy(int id, Vector2 position, bool patrol)
    {
        string group = id == 4 ? "Support" : id >= 6 ? "Ranged" : "Melee";
        var holder = new GameObject("Enemy Holder"); holder.transform.SetParent(rig.transform, false); holder.SetActive(false);
        EnemyAIController enemy = Object.Instantiate(Resources.Load<GameObject>("Prefab/Enemy/" + group + "/Monster" + id), holder.transform).GetComponent<EnemyAIController>();
        enemy.transform.localPosition = position;
        Set(enemy, "patrolEnemy", patrol); Set(enemy, "chaseEnemy", false);
        Set(enemy, "playerTarget", player.transform); Set(enemy, "patrolWait", .1f);
        holder.SetActive(true); Physics2D.SyncTransforms(); return enemy;
    }
    private void Keys(params Key[] keys) { if (keyboard != null) InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    private void Aim(Vector3 point) { if (mouse != null && Camera.main != null) { mousePosition = Camera.main.WorldToScreenPoint(point); QueueMouse(); } }
    private void MouseButtons(bool held) { mouseHeld = held; QueueMouse(); }
    private void QueueMouse() { if (mouse != null) InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePosition }.WithButton(MouseButton.Left, mouseHeld)); }
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static object Get(object target, string field) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    private static void Check(bool result, string name) => Results.Add((result ? "PASS: " : "FAIL: ") + name);
    private void Cleanup()
    {
        UIManager.Instance.HidePanel<GeneEditPanel>(false); UIManager.Instance.HidePanel<RunMenuPanel>(false); UIManager.Instance.HidePanel<SettingPanel>(false);
        foreach (GameObject obj in fixtures) if (obj != null) Object.Destroy(obj);
        foreach (Projectile p in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Object.Destroy(p.gameObject);
        foreach (ElementField f in new List<ElementField>(ElementField.Active)) if (f != null) Object.Destroy(f.gameObject);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        previousKeyboard?.MakeCurrent(); previousMouse?.MakeCurrent();
        InputSystem.settings.backgroundBehavior = previousBackground;
        foreach (Behaviour behaviour in suspended) if (behaviour != null) behaviour.enabled = true;
        suspended.Clear(); Time.timeScale = 1f;
    }
    private void OnDestroy() { if (Running) { Cleanup(); Running = false; } }
}
#endif
