#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Replaces the prototype map with one authored, completable chapter.</summary>
public static class CompleteGameWorldBuilder
{
    public const string ArtFolder = "Assets/ArtRes/WorldPolished";
    public const string WorldRootName = "Complete Chapter - Water Drain";
    private static readonly string[] TileNames = { "rock_fill", "rock_top_moss", "metal_deck", "pipe_horizontal" };
    private static readonly string[] PropNames = { "crystal", "mushroom", "pipe_elbow", "gene_terminal", "supply_crate", "core_gate" };
    private static readonly Dictionary<string, Sprite> art = new();
    private static Transform terrain, props, actors, interactions;
    private static Font font;
    private static CompleteLevelFlow flow;
    public static readonly List<string> LastValidation = new();

    [MenuItem("Tools/TapGJ/Build Complete Game World")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rebuilding the world.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "GameScene") throw new InvalidOperationException("Open GameScene first.");
        LoadArtwork();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        if (font == null) throw new InvalidOperationException("Player UI font is missing.");
        PlayerController player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefab/Player/Player.prefab");
            player = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<PlayerController>();
        }
        Camera view = Camera.main;
        if (view == null) { var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera"; view = cameraObject.AddComponent<Camera>(); }
        // The prototype grouped stations and scenery alongside the player. Retain only the character.
        player.transform.SetParent(null, true);
        view.transform.SetParent(null, true);
        foreach (EventSystem events in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            events.transform.SetParent(null, true);
        // Everything except the playable character, cameras, interface and story session belonged to the old map.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            bool keep = root == player.transform.root.gameObject || root.GetComponentsInChildren<Canvas>(true).Any(canvas => canvas.renderMode != RenderMode.WorldSpace) ||
                root.GetComponentInChildren<Camera>(true) != null || root.GetComponentInChildren<EventSystem>(true) != null ||
                root.name.IndexOf("Story", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!keep) Object.DestroyImmediate(root);
        }
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == WorldRootName) Object.DestroyImmediate(root);
        var world = new GameObject(WorldRootName);
        flow = world.AddComponent<CompleteLevelFlow>(); flow.player = player;
        terrain = Group("01 - Ground and Traversal", world.transform);
        props = Group("02 - Environment Artwork", world.transform);
        actors = Group("03 - Encounters", world.transform);
        interactions = Group("04 - Rewards and Stations", world.transform);
        BuildBackdrop(world.transform);
        BuildTerrain(); BuildScenery(); BuildRewards(); BuildEncounters(); BuildStory(scene);
        EmergenceWorldBuilder.Expand(flow,view);
        player.transform.position = new Vector3(3f, 1f, 0f);
        if (player.gameObject.layer != LayerMask.NameToLayer("Player")) player.gameObject.layer = LayerMask.NameToLayer("Player");
        player.gameObject.SetActive(true);
        SerializedObject playerFields = new(player);
        playerFields.FindProperty("checkGroundLM").intValue = LayerMask.GetMask("Ground", "Wall", "OneWayPlatform");
        playerFields.ApplyModifiedPropertiesWithoutUndo();
        GeneInventory inventory = player.GetComponent<GeneInventory>();
        if (inventory == null) throw new InvalidOperationException("The chapter player is missing GeneInventory.");
        SerializedObject inventoryFields = new(inventory);
        inventoryFields.FindProperty("genInventory").arraySize = 0;
        inventoryFields.FindProperty("genChain").arraySize = 0;
        inventoryFields.ApplyModifiedPropertiesWithoutUndo();
        view.transform.position = new Vector3(9.6f, 3.2f, -10f); view.orthographic = true; view.orthographicSize = 5.4f;
        view.backgroundColor = new Color(.16f, .13f, .25f);
        PlayerCameraFollow cameraFollow = view.GetComponent<PlayerCameraFollow>();
        if (cameraFollow == null) cameraFollow = view.gameObject.AddComponent<PlayerCameraFollow>();
        cameraFollow.target = player.transform; cameraFollow.horizontalLimits = new Vector2(9.6f, 253f); cameraFollow.smoothing = .16f;
        foreach (string actorLayer in new[] { "Player", "PlayerInvulnerable", "Enemy" })
        foreach (string groundLayer in new[] { "Ground", "Wall", "OneWayPlatform" })
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(actorLayer), LayerMask.NameToLayer(groundLayer), false);
        Physics2D.SyncTransforms();
        ValidateScene();
        EditorUtility.SetDirty(flow); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Complete chapter built: " + string.Join("; ", LastValidation));
    }

    private static void LoadArtwork()
    {
        art.Clear();
        ImportSingle("CavernBackdrop", "CavernBackdrop.png", 24f);
        ImportSheet("PlatformTiles.png", 2, 2, TileNames);
        ImportSheet("WorldProps.png", 3, 2, PropNames);
        foreach (string name in TileNames.Concat(PropNames))
            if (!art.ContainsKey(name)) throw new InvalidOperationException("Missing finished world artwork: " + name);
    }
    private static void ImportSingle(string key, string filename, float worldWidth)
    {
        string path = ArtFolder + "/" + filename;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (importer == null) throw new InvalidOperationException("Missing world artwork: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.GetSourceTextureWidthAndHeight(out int width, out _);
        importer.spritePixelsPerUnit = width / worldWidth;
        ConfigureImporter(importer); importer.SaveAndReimport();
        art[key] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void ImportSheet(string filename, int columns, int rows, string[] names)
    {
        bool separate = names.All(name => System.IO.File.Exists(ArtFolder + "/" + name + ".png"));
        if (separate) { foreach (string name in names) ImportSingle(name, name + ".png", name.Contains("rock") || name == "metal_deck" ? 4f : 2f); return; }
        string path = ArtFolder + "/" + filename;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (importer == null) throw new InvalidOperationException("Missing world artwork sheet: " + path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        int cellWidth = width / columns, cellHeight = height / rows;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = cellWidth / (filename == "PlatformTiles.png" ? 4f : 2f);
        var slices = new SpriteMetaData[names.Length];
        for (int i = 0; i < names.Length; i++) slices[i] = new SpriteMetaData
        {
            name = names[i], rect = new Rect(i % columns * cellWidth, height - (i / columns + 1) * cellHeight, cellWidth, cellHeight),
            alignment = (int)SpriteAlignment.Center, pivot = Vector2.one * .5f
        };
#pragma warning disable CS0618
        importer.spritesheet = slices;
#pragma warning restore CS0618
        ConfigureImporter(importer); importer.SaveAndReimport();
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()) art[sprite.name] = sprite;
    }
    private static void ConfigureImporter(TextureImporter importer)
    {
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
        TextureImporterSettings settings = new(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
    }
    private static Transform Group(string name, Transform parent)
    { var group = new GameObject(name); group.transform.SetParent(parent, false); return group.transform; }
    private static SpriteRenderer Visual(string name, Transform parent, string key, Vector2 position, Vector2 size, int order, Color? color = null)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent, false); obj.transform.position = position;
        SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = art[key]; renderer.sortingOrder = order;
        renderer.color = color ?? Color.white;
        Vector2 original = renderer.sprite.bounds.size;
        obj.transform.localScale = new Vector3(size.x / original.x, size.y / original.y, 1f);
        return renderer;
    }
    private static void BuildBackdrop(Transform root)
    {
        Transform backdrop = Group("00 - Distant Cavern", root);
        backdrop.gameObject.AddComponent<CompleteWorldParallax>().followFraction = .08f;
        for (int i = -1; i < 6; i++) Visual("Cavern Panorama " + i, backdrop, "CavernBackdrop", new Vector2(i * 24f + 12f, 3.2f), new Vector2(24.1f, 13.5f), -40);
    }
    private static void Ground(string name, float left, float right, float top, float depth = 4f)
    {
        float width = right - left;
        var obj = new GameObject(name); obj.transform.SetParent(terrain, false); obj.layer = LayerMask.NameToLayer("Ground");
        obj.transform.position = new Vector2((left + right) * .5f, top - depth * .5f);
        BoxCollider2D body = obj.AddComponent<BoxCollider2D>(); body.size = new Vector2(width, depth);
        SpriteRenderer fill = Visual("Cavern Rock", obj.transform, "rock_fill", obj.transform.position, Vector2.one * 4f, -4);
        fill.transform.localScale = Vector3.one; fill.drawMode = SpriteDrawMode.Tiled; fill.size = new Vector2(width, depth);
        SpriteRenderer cap = Visual("Living Moss Edge", obj.transform, "rock_top_moss", new Vector2((left + right) * .5f, top - .3f), new Vector2(width, .6f), 2);
        cap.transform.localScale = new Vector3(1f, .6f / cap.sprite.bounds.size.y, 1f);
        cap.drawMode = SpriteDrawMode.Tiled; cap.size = new Vector2(width, cap.sprite.bounds.size.y);
        // The thin top edge matches the solid collider; the hanging moss sits below it.
    }
    private static void Platform(string name, float x, float top, float width)
    {
        var obj = new GameObject(name); obj.transform.SetParent(terrain, false); obj.layer = LayerMask.NameToLayer("OneWayPlatform");
        obj.transform.position = new Vector2(x, top - .12f);
        BoxCollider2D body = obj.AddComponent<BoxCollider2D>(); body.size = new Vector2(width, .24f); body.usedByEffector = true;
        PlatformEffector2D effector = obj.AddComponent<PlatformEffector2D>(); effector.useOneWay = true; effector.surfaceArc = 170f;
        OneWayPlatform drop = obj.AddComponent<OneWayPlatform>(); Set(drop, "boxCollider", body);
        Visual("Suspended Walkway", obj.transform, "metal_deck", new Vector2(x, top - .2f), new Vector2(width, .4f), 3);
    }
    private static void BuildTerrain()
    {
        Ground("Main Floor - Drain Laboratory", -3f, 38f, 0f);
        Ground("Main Floor - Ecology West", 38f, 55f, 0f);
        Ground("Main Floor - Ecology Middle", 57.4f, 66f, 0f);
        Ground("Main Floor - Ecology East", 68.4f, 78f, 0f);
        Ground("Main Floor - Core Chamber", 78f, 115f, 0f);
        Ground("Left Safety Wall", -4f, -3f, 8f, 12f); Ground("Right Safety Wall", 115f, 116f, 8f, 12f);
        Platform("Teaching Jump Step", 8f, .70f, 3f);
        Platform("Laboratory Upper Route", 22f, .75f, 4f);
        Platform("Laboratory Reward Shelf", 25.7f, 1.5f, 3.5f);
        Platform("Ecology Branch Step 1", 46f, .75f, 3f);
        Platform("Ecology Branch Step 2", 49.5f, 1.5f, 3f);
        Platform("Ecology Branch Reward", 53f, 2.25f, 3.5f);
        Platform("Ecology Mid Cover", 61.5f, .7f, 3f);
        Platform("Ecology East Step", 71f, .75f, 3.5f);
        Platform("Ecology East Reward", 74.5f, 1.5f, 3f);
        Platform("Core Arena Left Dodge Deck", 88.5f, 1f, 4f);
        Platform("Core Arena Right Dodge Deck", 99.5f, 1f, 4f);
    }
    private static void BuildScenery()
    {
        for (int i = 0; i < 23; i++)
        {
            float x = i * 5f + 1.5f;
            if (x > 54f && x < 59f || x > 65f && x < 70f) continue;
            string key = i % 3 == 0 ? "mushroom" : "crystal";
            float height = key == "mushroom" ? 2.6f : 1.6f;
            Visual("Cavern Flora " + i, props, key, new Vector2(x, height * .5f - .05f), new Vector2(key == "mushroom" ? 1.6f : 1.1f, height), -8,
                new Color(.73f, .77f, .89f, 1f));
        }
        for (int i = 0; i < 8; i++)
        {
            Visual("Ceiling Pipe " + i, props, "pipe_horizontal", new Vector2(i * 16f + 4f, 6.7f), new Vector2(16.2f, .65f), -6);
            Visual("Pipe Elbow " + i, props, "pipe_elbow", new Vector2(i * 16f + 9f, 5.7f), new Vector2(2f, 2.2f), -5);
        }
        Sign("01 / 废弃实验区", new Vector2(4f, 4.8f));
        Sign("02 / 荧光生态区", new Vector2(42f, 4.8f));
        Sign("06 / 排水核心", new Vector2(82f, 4.8f));
        Sign("空格跳跃  ·  S + 空格穿下平台", new Vector2(8f, 2.25f));
        Sign("冷库与终端  ·  E 交互", new Vector2(16f, 3.3f));
        Sign("裂缝可跳过  ·  失足返回安全区", new Vector2(56f, 4.2f));
        Sign("SHIFT 冲刺躲避  ·  高处可绕行", new Vector2(70f, 3.7f));
        Hazard("Polluted Runoff West", 50f); Hazard("Polluted Runoff East", 72f);
    }
    private static void Sign(string text, Vector2 position)
    {
        var obj = new GameObject("Wayfinding - " + text, typeof(RectTransform), typeof(Canvas));
        obj.transform.SetParent(props, false); obj.transform.position = position; obj.transform.localScale = Vector3.one * .01f;
        Canvas canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 5;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(640f, 72f);
        Text label = GameplayUIFactory.Label("Label", obj.transform, Vector2.zero, Vector2.one, font, 27, text, TextAnchor.MiddleCenter);
        label.color = new Color(.75f, .9f, .89f, .8f);
    }
    private static void Hazard(string name, float x)
    {
        var obj = new GameObject(name); obj.transform.SetParent(interactions, false); obj.transform.position = new Vector2(x, .17f);
        BoxCollider2D trigger = obj.AddComponent<BoxCollider2D>(); trigger.isTrigger = true; trigger.size = new Vector2(1.8f, .34f);
        obj.AddComponent<CompleteWorldHazard>();
        Visual("Bioluminescent Sludge", obj.transform, "rock_top_moss", new Vector2(x, .09f), new Vector2(2f, .2f), 4, new Color(.55f, 1f, .55f));
    }
    private static GameObject Item(string name, string key, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name); obj.transform.SetParent(interactions, false); obj.transform.position = position;
        Visual("Artwork", obj.transform, key, position + Vector2.up * size.y * .5f, size, 4); return obj;
    }
    private static void BuildRewards()
    {
        foreach (float x in new[] { 16f, 41f, 78f })
        {
            GameObject terminal = Item("Gene Assembly Terminal " + x, "gene_terminal", new Vector2(x, 0f), new Vector2(1.4f, 1.8f));
            terminal.AddComponent<GeneEditStation>().interactionRange = 2f;
        }
        foreach (float x in new[] { 18.5f, 39f, 64f, 80f, 100f })
        {
            GameObject supply = Item("Health Cache " + x, "supply_crate", new Vector2(x, 0f), new Vector2(.9f, 1f));
            PlayerSupply health = supply.AddComponent<PlayerSupply>(); health.kind = PlayerSupply.SupplyKind.Health; health.healAmount = 5f;
            supply.AddComponent<CompleteRewardState>().rewardId = "health-" + x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            BoxCollider2D collider = supply.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(1f, 1.2f);
            collider.offset = new Vector2(0f, .4f); supply.layer = LayerMask.NameToLayer("Pickup");
        }
        GameObject dash = Item("Dash Module", "supply_crate", new Vector2(31f, 0f), new Vector2(1f, 1.15f));
        dash.AddComponent<PlayerSupply>().kind = PlayerSupply.SupplyKind.Dash;
        dash.AddComponent<CompleteRewardState>().rewardId = "dash-module";
        dash.GetComponentInChildren<SpriteRenderer>().color = new Color(.7f, 1f, 1f);
        Sign("冲刺模块  ·  E 获取", new Vector2(31f, 2.3f));
        Sample("Water Sample", "water", new Vector2(13.5f, .15f));
        Sample("Attack Upgrade", "attack", new Vector2(21f, .9f));
        Sample("Fire Sample", "ember", new Vector2(35.5f, .15f));
        Sample("Poison Sample", "sewage", new Vector2(60f, .15f));
        Chest("Laboratory Branch Cold Storage", new Vector2(25.7f, 1.5f), "water", "attack", "size");
        Chest("Ecology Upper Branch Cold Storage", new Vector2(53f, 2.25f), "split", "sewage", "vortex");
        Chest("Ecology East Cold Storage", new Vector2(74.5f, 1.5f), "ember", "pierce", "attack");
        GameObject exit = Item("Chapter Exit - Drain Core", "core_gate", new Vector2(108f, 0f), new Vector2(3f, 4f));
        CompleteWorldExit gate = exit.AddComponent<CompleteWorldExit>(); gate.level = flow; gate.interactionRange = 2.1f;
    }
    private static void Sample(string name, string geneName, Vector2 position)
    {
        GameObject obj = Item(name, "supply_crate", position, new Vector2(.65f, .8f));
        CompleteGeneSample sample = obj.AddComponent<CompleteGeneSample>(); sample.gene = Resources.Load<Modifier>("SO/Genes/" + geneName);
        obj.AddComponent<CompleteRewardState>().rewardId = "sample-" + geneName;
        obj.GetComponentInChildren<SpriteRenderer>().color = sample.gene != null ? sample.gene.Color : Color.white;
    }
    private static void Chest(string name, Vector2 position, params string[] geneNames)
    {
        GameObject obj = Item(name, "supply_crate", position, new Vector2(1.2f, 1.1f));
        GeneChest chest = obj.AddComponent<GeneChest>();
        obj.AddComponent<CompleteRewardState>().rewardId = "chest-" + position.x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        Set(chest, "genePool", geneNames.Select(name => Resources.Load<Modifier>("SO/Genes/" + name)).Where(gene => gene != null).ToArray());
    }
    private static EnemyAIController Enemy(int kind, float x, float y, float hp, float range = 2f)
    {
        string path = kind >= 6 ? "Assets/Resources/Prefab/Enemy/Ranged/Monster"+kind+".prefab" : kind==4 ? "Assets/Resources/Prefab/Enemy/Support/Monster4.prefab" : "Assets/Resources/Prefab/Enemy/Melee/Monster" + kind + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new InvalidOperationException("Missing enemy prefab " + path);
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, actors);
        obj.name = "Encounter - Monster" + kind + " at " + x; obj.transform.position = new Vector2(x, y);
        EnemyAIController enemy = obj.GetComponent<EnemyAIController>();
        Type polish = FindType("EnemyCombatPolishSetup");
        var configure = polish != null ? polish.GetMethod("ConfigureSceneEnemy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) : null;
        if (configure != null && configure.GetParameters().Length == 1) configure.Invoke(null, new object[] { enemy });
        // Keep the chapter's slower introductory encounters after configuring their attack animation.
        Set(enemy, "maxHp", hp); Set(enemy, "meleeDamage", 2f); Set(enemy, "bulletDamage", 1f);
        Set(enemy, "patrolRange", range); Set(enemy, "detectRange", 7f); Set(enemy, "chaseSpeed", kind == 5 ? 2f : 1.7f);
        Set(enemy, "attackCooldown", kind == 6 ? 2.2f : 1.7f); Set(enemy, "bulletSpeed", 3.5f);
        Set(enemy, "obstacleMask", LayerMask.GetMask("Ground", "Wall", "OneWayPlatform"));
        if (kind == 4)
        {
            // Healers stay with their squad. They never become a second contact attacker.
            Set(enemy, "patrolSpeed", .6f); Set(enemy, "chaseEnemy", false);
            Set(enemy, "contactDamage", false); Set(enemy, "meleeDamage", 0f);
            EnemySupportAura aura = enemy.GetComponent<EnemySupportAura>();
            if (aura == null) throw new InvalidOperationException("Monster4 is missing its healing aura.");
            Set(aura, "healRadius", 6f); Set(aura, "healAmount", 20f);
            Set(aura, "healCooldown", 5f); Set(aura, "maxTargets", 4);
            Set(aura, "targetMask", LayerMask.GetMask("Enemy"));
        }
        EnemyGeneDrop drop = obj.GetComponent<EnemyGeneDrop>();
        if (drop != null) Set(drop, "genePool", Resources.LoadAll<Modifier>("SO/Genes").Where(g => g.Element != E_ElementType.Electric).ToArray());
        return enemy;
    }
    private static void BuildEncounters()
    {
        Enemy(3, 24f, .5f, 4f, 1.5f); Enemy(3, 34f, .5f, 5f, 2f);
        Enemy(6, 36f, 2.5f, 4f, 1f);
        Enemy(4, 36.5f, 1.2f, 4f, .65f);
        Sign("绿色连线正在治疗  ·  先打断治疗怪", new Vector2(34.5f, 4.1f));
        Enemy(5, 44f, .55f, 6f, 1.5f); Enemy(3, 51.5f, .5f, 5f, 1.5f);
        Enemy(4, 48.8f, 2.7f, 5f, .65f);
        Enemy(6, 53f, 3.7f, 5f, 1f); Enemy(5, 62f, .55f, 6f, 1.5f);
        Enemy(3, 70.5f, .5f, 5f, 1.5f); Enemy(6, 75f, 3f, 5f, 1f);
        Enemy(5, 81.5f, .55f, 6f, 1.2f);
        EnemyAIController heavy = Enemy(5, 94f, .7f, 32f, 3.5f);
        heavy.name = "Core Guardian - Spore Sentinel"; heavy.transform.localScale = Vector3.one * 1.25f;
        Set(heavy, "detectRange", 15f); Set(heavy, "patrolSpeed", 1.3f);
        EnemyAIController ranged = Enemy(6, 98f, 3f, 18f, 2f);
        ranged.name = "Core Guardian - Drain Jelly"; Set(ranged, "bulletCount", 3); Set(ranged, "spreadAngle", 20f);
        Set(ranged, "detectRange", 15f); Set(ranged, "attackRange", 13f);
        EnemyAIController flank = Enemy(3, 91f, .5f, 18f, 2f);
        flank.name = "Core Guardian - Ember Mite"; Set(flank, "detectRange", 15f);
        flow.guardians = new[] { heavy, ranged, flank };
        foreach (EnemyAIController guardian in flow.guardians) guardian.gameObject.SetActive(false);
        GameObject barrier = new GameObject("Core Seal - Opens After Guardians"); barrier.transform.SetParent(terrain, false);
        barrier.transform.position = new Vector2(104.5f, 2.3f); barrier.layer = LayerMask.NameToLayer("Wall");
        barrier.AddComponent<BoxCollider2D>().size = new Vector2(.6f, 4.6f);
        Visual("Sealed Core Pipe", barrier.transform, "pipe_horizontal", barrier.transform.position, new Vector2(.8f, 4.6f), 4);
        flow.barriers = new[] { barrier };
    }
    private static void BuildStory(Scene scene)
    {
        Type sessionType = FindType("StorySession");
        if (sessionType == null) throw new InvalidOperationException("StorySession has not compiled yet.");
        Component existing = Object.FindFirstObjectByType(sessionType, FindObjectsInactive.Include) as Component;
        if (existing == null) new GameObject("Story Session").AddComponent(sessionType);
        StoryPoint("StoryTrigger", "Entrance", new Vector2(3f, 1f), new Vector2(5f, 3f));
        foreach (float x in new[] { 16f, 41f, 78f }) StoryPoint("StoryTrigger", "Checkpoint", new Vector2(x, 1f), new Vector2(3f, 3f));
        StoryPoint("StoryTrigger", "GuardianGate", new Vector2(84f, 1f), new Vector2(2f, 3f));
        StoryPoint("StoryInteractable", "LabRecord", new Vector2(10f, .6f), Vector2.zero);
        StoryPoint("StoryInteractable", "LabRecord", new Vector2(46f, .6f), Vector2.zero);
    }
    private static Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).FirstOrDefault(type => type != null);
    private static void StoryPoint(string typeName, string beat, Vector2 position, Vector2 triggerSize)
    {
        Type type = FindType(typeName); if (type == null) throw new InvalidOperationException(typeName + " has not compiled yet.");
        var obj = new GameObject(typeName + " - " + beat + " - " + position.x); obj.transform.SetParent(interactions, false); obj.transform.position = position;
        Component component = obj.AddComponent(type); var field = type.GetField("beat");
        if (field == null) throw new InvalidOperationException(typeName + ".beat is missing.");
        field.SetValue(component, Enum.Parse(field.FieldType, beat));
        if (triggerSize != Vector2.zero)
        {
            BoxCollider2D trigger = obj.GetComponent<BoxCollider2D>(); if (trigger == null) trigger = obj.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true; trigger.size = triggerSize;
        }
        else Visual("Record Terminal", obj.transform, "gene_terminal", new Vector2(position.x, .5f), new Vector2(.7f, 1f), 4);
        EditorUtility.SetDirty(component);
    }
    private static void Set(Object target, string field, object value)
    {
        var serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field);
        if (value is Object reference) property.objectReferenceValue = reference;
        else if (value is bool flag) property.boolValue = flag;
        else if (value is float scalar) property.floatValue = scalar;
        else if (value is int integer) property.intValue = integer;
        else if (value is Modifier[] genes) { property.arraySize = genes.Length; for (int i = 0; i < genes.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = genes[i]; }
        else throw new InvalidOperationException("Unsupported serialized world field " + field);
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
    [MenuItem("Tools/TapGJ/Validate Complete Game World")]
    public static void ValidateScene()
    {
        LastValidation.Clear();
        CompleteLevelFlow level = Object.FindFirstObjectByType<CompleteLevelFlow>();
        if (level == null) throw new InvalidOperationException("Complete chapter is missing.");
        Require(level.guardians.Length == 3 && level.guardians.All(g => g != null), "Three core guardians are assigned");
        EnemySupportAura[] supportSquads = level.GetComponentsInChildren<EnemySupportAura>(true);
        Require(supportSquads.Length >= 3 && supportSquads.Count(aura => aura.transform.position.x < 78f) == 2,
            "Laboratory, ecology and spore districts retain their healing squads");
        foreach (EnemySupportAura healer in supportSquads)
        {
            Require(healer.GetComponent<EnemyAIController>().IsSupport && healer.HealCooldown >= 5f && healer.HealRadius <= 6f,
                "Each healer has a readable five-second support cadence and local squad radius");
            Require(level.GetComponentsInChildren<EnemyAIController>(true).Any(ally => ally.gameObject.activeSelf &&
                ally.gameObject != healer.gameObject && !ally.IsSupport && Vector2.Distance(ally.transform.position, healer.transform.position) < healer.HealRadius),
                "Each healer begins within range of a living combat squad");
        }
        Require(Object.FindObjectsByType<GeneEditStation>(FindObjectsSortMode.None).Length == 6, "Six gene terminals support repeated build decisions");
        Require(Object.FindObjectsByType<CompleteWorldExit>(FindObjectsSortMode.None).Length == 1, "The chapter has a guarded ending exit");
        Require(Object.FindObjectsByType<CompleteGeneSample>(FindObjectsSortMode.None).All(sample => sample.gene != null), "Fixed rewards reference actual gene assets");
        CompleteRewardState[] rewards = level.GetComponentsInChildren<CompleteRewardState>(true);
        Require(rewards.Length >= 20 && rewards.All(reward => !string.IsNullOrEmpty(reward.rewardId)) &&
            rewards.Select(reward => reward.rewardId).Distinct().Count() == rewards.Length,
            "All authored rewards have unique persistent identities");
        Require(Object.FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, "All prototype Tilemaps were removed");
        PlayerController player = level.player;
        SerializedObject fields = new(player); PlayerData data = fields.FindProperty("playerData").objectReferenceValue as PlayerData;
        float gravity = Mathf.Abs(Physics2D.gravity.y * player.GetComponent<Rigidbody2D>().gravityScale);
        float jumpHeight = data.jumpVelocity * data.jumpVelocity / (2f * gravity);
        float jumpRange = data.moveSpeed * 2f * data.jumpVelocity / gravity;
        Require(jumpHeight >= .85f && jumpRange >= 2.8f, "Jump reaches all .75m steps and 2.4m gaps");
        Require(level.checkpoints.Length == 6 && level.recoveryY > data.fallDeathY, "Six checkpoints recover falls before the player death plane");
        foreach (SpriteRenderer renderer in level.GetComponentsInChildren<SpriteRenderer>(true))
            if (!renderer.name.Contains("Enemy") && renderer.sprite == null) throw new InvalidOperationException("Unassigned artwork: " + renderer.name);
        LastValidation.Add("Chapter extent 268m; three new systemic districts and alternate solutions");
    }
    internal static void AddGround(string name,float left,float right,float top,float depth=4f)=>Ground(name,left,right,top,depth);
    internal static void AddPlatform(string name,float x,float top,float width)=>Platform(name,x,top,width);
    internal static void AddSign(string text,Vector2 position)=>Sign(text,position);
    internal static GameObject AddItem(string name,string key,Vector2 position,Vector2 size)=>Item(name,key,position,size);
    internal static SpriteRenderer AddVisual(string name,Transform parent,string key,Vector2 position,Vector2 size,int order)=>Visual(name,parent,key,position,size,order);
    internal static void AddChest(string name,Vector2 position,params string[] genes)=>Chest(name,position,genes);
    internal static EnemyAIController AddEnemy(int kind,float x,float y,float hp,float range=2f)=>Enemy(kind,x,y,hp,range);
    private static void Require(bool success, string description)
    { if (!success) throw new InvalidOperationException(description); LastValidation.Add("PASS: " + description); }
}
#endif
