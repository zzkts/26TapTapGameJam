#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class CharacterSceneSetup
{
    private const string GeneFolder = "Assets/Resources/SO/Genes";
    private static Font font;
    private static Sprite white;
    [MenuItem("Tools/TapGJ/Set Up Character Gameplay")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        if (GameObject.Find("Gameplay Canvas") != null) throw new InvalidOperationException("Character Gameplay already exists.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene")
            throw new InvalidOperationException("Open GameScene first.");
        EnsureFolder(GeneFolder);
        EnsureFolder("Assets/Resources/Prototype");
        EnsureFolder("Assets/Resources/Prefab/Player");
        EnsureLayer("PlayerInvulnerable");
        EnsureLayer("EnemyBullet");
        EnsureLayer("Pickup");
        white = CreateWhiteSprite();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Front/SIMKAI.TTF");
        GeneRules rules = BuildGenes();
        PlayerData data = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Resources/SO/Player/DefaultPlayerData.asset");
        data.moveSpeed = 3f; data.jumpVelocity = 4f; data.maxHp = 10f;
        data.rollDuration = 0.2f; data.dashUnlocked = false;
        EditorUtility.SetDirty(data);
        WeaponData weaponData = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Resources/SO/Weapon/DefaultWaterBallWeaponData.asset");
        weaponData.projectileSpeed = 5f; weaponData.projectileLifetime = 3f; weaponData.projectileSizeMultiplier = 0.35f;
        EditorUtility.SetDirty(weaponData);

        var root = GameObject.Find("Character Gameplay");
        if (root == null) root = new GameObject("Character Gameplay");
        Ensure<PlayerCollisionRules>(root);
        var playerObject = GameObject.Find("Player");
        playerObject.layer = LayerMask.NameToLayer("Player");
        Rigidbody2D rb = Ensure<Rigidbody2D>(playerObject);
        rb.gravityScale = 0.65f; rb.freezeRotation = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        BoxCollider2D body = playerObject.GetComponent<BoxCollider2D>();
        body.size = new Vector2(0.65f / playerObject.transform.localScale.x, 1.3f / playerObject.transform.localScale.y);
        body.offset = new Vector2(0f, -0.05f / playerObject.transform.localScale.y);
        playerObject.transform.position = new Vector3(-13.2f, GroundY(-13.2f) + 0.74f, 0f);
        MoveVisualToChild(playerObject);
        SpriteRenderer sr = playerObject.GetComponentInChildren<SpriteRenderer>();
        sr.sortingOrder = 5;
        Transform muzzle = new GameObject("Water Muzzle").transform;
        muzzle.SetParent(playerObject.transform, false);
        muzzle.localPosition = new Vector3(0f, 0.35f / playerObject.transform.localScale.y, 0f);
        GeneInventory inventory = playerObject.AddComponent<GeneInventory>();
        Set(inventory, "rules", rules);
        playerObject.AddComponent<GeneChainController>();
        Weapon weapon = Ensure<Weapon>(playerObject);
        Set(weapon, "baseData", weaponData);
        SetInt(weapon, "targetLM", LayerMask.GetMask("Enemy"));
        PlayerController player = Ensure<PlayerController>(playerObject);
        Set(player, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/Input/InputSystem_Actions.inputactions"));
        Set(player, "playerData", data); Set(player, "rb", rb); Set(player, "bodyCollider", body);
        Set(player, "animator", playerObject.GetComponentInChildren<Animator>()); Set(player, "sr", sr);
        Set(player, "weapon", weapon); Set(player, "attackTransform", muzzle);
        SetInt(player, "checkGroundLM", LayerMask.GetMask("Default", "Ground", "Wall"));
        playerObject.AddComponent<PlayerInteraction>();
        playerObject.AddComponent<CombatStatus>();
        AnimatorController controller = playerObject.GetComponentInChildren<Animator>().runtimeAnimatorController as AnimatorController;
        AddState(controller, "Hurt", "Assets/ArtRes/Animation/Player/Hurt/Hurt.anim");
        AddState(controller, "Dead", "Assets/ArtRes/Animation/Player/Dead/Dead.anim");
        AddState(controller, "Land", "Assets/ArtRes/Animation/Player/Idle/Idle.anim");
        AddState(controller, "Fall", "Assets/ArtRes/Animation/Player/Jump/Jump.anim");
        AddState(controller, "Roll", "Assets/ArtRes/Animation/Player/Jump/Jump.anim");
        Camera camera = Camera.main;
        if (camera != null)
        {
            var follow = Ensure<PlayerCameraFollow>(camera.gameObject);
            follow.target = playerObject.transform;
            camera.transform.position = new Vector3(-7f, camera.transform.position.y, camera.transform.position.z);
        }
        foreach (EnemyAIController enemy in UnityEngine.Object.FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None))
        {
            if (enemy.GetComponent<EnemyHealthBar>() == null) enemy.gameObject.AddComponent<EnemyHealthBar>();
            EnemyGeneDrop drop = Ensure<EnemyGeneDrop>(enemy.gameObject);
            drop.rules = rules;
            drop.dropElement = enemy.Kind == EnemyAIController.AttackType.Melee ? CombatElement.Poison : CombatElement.Water;
        }
        UpdateEnemyPrefabs(rules);
        PlaceWorld(root.transform, rules);
        BuildUI(root.transform, rules.slotCount);
        PrefabUtility.SaveAsPrefabAssetAndConnect(playerObject, "Assets/Resources/Prefab/Player/Player.prefab", InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static GeneRules BuildGenes()
    {
        var definitions = new List<GeneData>();
        definitions.Add(Gene("vortex", "涡流", GeneKind.Attribute, CombatElement.Water,
            "命中后产生涡流，聚集附近敌人。", "飞行中的子弹持续牵引附近敌人。", GeneEffectKind.Vortex, 1.5f, 1.5f));
        definitions.Add(Gene("diffusion", "扩散", GeneKind.Attribute, CombatElement.Water,
            "命中后为半径 3 米内敌人附加水状态。", "水状态扩散半径提高至 5 米。", GeneEffectKind.Spread, 3f, 5f));
        definitions.Add(Gene("ember", "火种", GeneKind.Attribute, CombatElement.Fire,
            "命中后引燃半径 3 米内的敌人，可引爆毒雾。", "引燃半径提高至 5 米。", GeneEffectKind.Spread, 3f, 5f));
        definitions.Add(Gene("sewage", "污水", GeneKind.Attribute, CombatElement.Poison,
            "命中后生成持续 3 秒的毒雾。", "毒属性向命中点周围扩散。", GeneEffectKind.Spread, 0f, 2.5f));
        definitions.Add(Gene("speed", "加速", GeneKind.Modification, CombatElement.None,
            "子弹飞行速度乘以 1.25。", "子弹飞行速度乘以 1.5。", GeneEffectKind.Speed, 1.25f, 1.5f));
        definitions.Add(Gene("bounce", "反弹", GeneKind.Modification, CombatElement.None,
            "子弹可在墙面反弹 3 次。", "子弹可在墙面反弹 5 次。", GeneEffectKind.Bounce, 3f, 5f));
        definitions.Add(Gene("orbit", "环流", GeneKind.Modification, CombatElement.None,
            "子弹沿半径 2 米的环绕路径飞行。", "环绕半径增加至 3 米。", GeneEffectKind.Orbit, 2f, 3f));
        definitions.Add(Gene("split", "分裂", GeneKind.Modification, CombatElement.None,
            "命中时分裂为两颗子弹，保留本次射击属性。", "命中时分裂为三颗子弹。", GeneEffectKind.Split, 2f, 3f));
        GeneRules rules = AssetDatabase.LoadAssetAtPath<GeneRules>(GeneFolder + "/GeneRules.asset");
        if (rules == null) { rules = ScriptableObject.CreateInstance<GeneRules>(); AssetDatabase.CreateAsset(rules, GeneFolder + "/GeneRules.asset"); }
        rules.genes = definitions.ToArray();
        EditorUtility.SetDirty(rules);
        return rules;
    }
    private static GeneData Gene(string id, string name, GeneKind kind, CombatElement element, string description,
        string advanced, GeneEffectKind effect, float basic, float evolved)
    {
        string path = GeneFolder + "/" + id + ".asset";
        GeneData gene = AssetDatabase.LoadAssetAtPath<GeneData>(path);
        if (gene == null) { gene = ScriptableObject.CreateInstance<GeneData>(); AssetDatabase.CreateAsset(gene, path); }
        gene.geneId = id; gene.displayName = name; gene.kind = kind; gene.element = element;
        gene.description = description; gene.evolvedDescription = advanced; gene.enchantDuration = 4f;
        gene.color = element == CombatElement.Fire ? new Color(1f, 0.55f, 0.35f) :
            element == CombatElement.Poison ? new Color(0.65f, 0.92f, 0.35f) :
            element == CombatElement.Water ? new Color(0.4f, 0.85f, 1f) : new Color(0.95f, 0.86f, 0.5f);
        gene.effects = new[] { new GeneEffect { kind = effect, trigger = GeneTrigger.OnHit, value = basic, evolvedValue = evolved } };
        EditorUtility.SetDirty(gene);
        return gene;
    }
    private static void PlaceWorld(Transform root, GeneRules rules)
    {
        GameObject station = WhiteObject("Gene Edit Station", root, -14.3f, new Vector2(0.6f, 0.9f), Color.white);
        station.AddComponent<GeneEditStation>().interactionRange = rules.interactionRange;
        GameObject chest = WhiteObject("Gene Cold Storage", root, -11.5f, new Vector2(0.8f, 0.55f), new Color(0.85f, 0.94f, 1f));
        chest.AddComponent<GeneChest>().rules = rules;
        GameObject dash = WhiteObject("Dash Module", root, -9.8f, new Vector2(0.3f, 0.5f), Color.white);
        dash.AddComponent<PlayerSupply>().kind = PlayerSupply.SupplyKind.Dash;
        GameObject health = WhiteObject("Health Supply", root, -8.2f, new Vector2(0.3f, 0.45f), new Color(0.75f, 1f, 0.8f));
        health.AddComponent<PlayerSupply>().kind = PlayerSupply.SupplyKind.Health;
        string[] starter = { "diffusion", "speed", "ember" };
        for (int i = 0; i < starter.Length; i++)
        {
            GameObject pickup = WhiteObject("Starter " + starter[i], root, -13.2f + i * 0.55f, Vector2.one * 0.28f, Color.white);
            pickup.AddComponent<GenePickup>().gene = rules.Find(starter[i]);
        }
        GameObject nextStation = WhiteObject("Gene Edit Station East", root, 14f, new Vector2(0.6f, 0.9f), Color.white);
        nextStation.AddComponent<GeneEditStation>().interactionRange = rules.interactionRange;
        GameObject nextChest = WhiteObject("Gene Cold Storage East", root, 16f, new Vector2(0.8f, 0.55f), new Color(0.85f, 0.94f, 1f));
        nextChest.AddComponent<GeneChest>().rules = rules;
        GameObject electric = WhiteObject("Environment Electric Source", root, 10.5f, new Vector2(0.6f, 0.3f), new Color(0.7f, 0.9f, 1f));
        var source = electric.AddComponent<EnvironmentElementSource>();
        source.rules = rules;
        source.sourceTemplate = Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        GameObject platform = WhiteObject("Practice One Way Platform", root, -6.8f, new Vector2(1.7f, 0.12f), Color.white);
        platform.layer = LayerMask.NameToLayer("Ground");
        platform.transform.position += Vector3.up * 0.85f;
        BoxCollider2D box = platform.AddComponent<BoxCollider2D>();
        box.usedByEffector = true;
        var effector = platform.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 170f;
        var oneWay = platform.AddComponent<OneWayPlatform>();
        Set(oneWay, "boxCollider", box);
    }
    private static GameObject WhiteObject(string name, Transform parent, float x, Vector2 size, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(x, GroundY(x) + size.y * 0.5f + 0.03f, 0f);
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = white; renderer.color = color; renderer.sortingOrder = 4;
        return obj;
    }
    private static float GroundY(float x)
    {
        var hit = Physics2D.Raycast(new Vector2(x, 6f), Vector2.down, 20f, LayerMask.GetMask("Default", "Ground", "Wall"));
        return hit.collider != null ? hit.point.y : -1.8f;
    }
    private static void BuildUI(Transform root, int slotCount)
    {
        var canvasObj = new GameObject("Gameplay Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(root, false); canvasObj.layer = 5;
        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Gameplay EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root, false);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        RectTransform hudRoot = GameplayUIFactory.Rect("Player HUD", canvasObj.transform, Vector2.zero, Vector2.one);
        PlayerHudPanel hud = hudRoot.gameObject.AddComponent<PlayerHudPanel>();
        GameplayUIFactory.Image("Health Background", hudRoot, new Vector2(0.025f, 0.902f), new Vector2(0.225f, 0.924f), new Color(0.05f, 0.07f, 0.07f, 0.9f));
        hud.healthFill = GameplayUIFactory.Image("Health", hudRoot, new Vector2(0.025f, 0.902f), new Vector2(0.225f, 0.924f), new Color(0.4f, 0.9f, 0.65f));
        Filled(hud.healthFill);
        hud.healthText = GameplayUIFactory.Label("Health Text", hudRoot, new Vector2(0.025f, 0.927f), new Vector2(0.24f, 0.98f), font, 24, "生命  10 / 10");
        hud.dashText = GameplayUIFactory.Label("Dash", hudRoot, new Vector2(0.025f, 0.847f), new Vector2(0.25f, 0.895f), font, 20, "冲刺 · 未解锁");
        GameplayUIFactory.Image("Gene Background", hudRoot, new Vector2(0.025f, 0.03f), new Vector2(0.435f, 0.135f), new Color(0.04f, 0.06f, 0.06f, 0.85f));
        hud.slotTexts = new Text[slotCount];
        for (int i = 0; i < slotCount; i++)
            hud.slotTexts[i] = GameplayUIFactory.Label("Gene Slot " + i, hudRoot, new Vector2(0.027f + i * 0.405f / slotCount, 0.035f),
                new Vector2(0.025f + (i + 1) * 0.405f / slotCount, 0.13f), font, 18, (i + 1) + "\n-", TextAnchor.MiddleCenter);
        hud.attributeText = GameplayUIFactory.Label("Attribute", hudRoot, new Vector2(0.025f, 0.157f), new Vector2(0.43f, 0.205f), font, 21, "无属性 · 就绪");
        hud.stageFill = GameplayUIFactory.Image("Attribute Progress", hudRoot, new Vector2(0.025f, 0.142f), new Vector2(0.435f, 0.147f), new Color(0.45f, 0.82f, 1f));
        Filled(hud.stageFill);
        hud.interactionText = GameplayUIFactory.Label("Interaction", hudRoot, new Vector2(0.46f, 0.035f), new Vector2(0.96f, 0.15f), font, 21, "", TextAnchor.MiddleRight);
        hud.toastText = GameplayUIFactory.Label("Acquired Gene", hudRoot, new Vector2(0.28f, 0.79f), new Vector2(0.76f, 0.87f), font, 24, "", TextAnchor.MiddleCenter);
        BuildGenePanel(canvasObj.transform, slotCount);
        hud.runMenu = BuildMenu(canvasObj.transform);
    }
    private static void BuildGenePanel(Transform canvas, int count)
    {
        Image overlay = GameplayUIFactory.Image("Gene Edit Panel", canvas, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.78f), true);
        GeneEditPanel panel = overlay.gameObject.AddComponent<GeneEditPanel>(); panel.font = font;
        Image surface = GameplayUIFactory.Image("Surface", overlay.transform, new Vector2(0.06f, 0.07f), new Vector2(0.94f, 0.93f), new Color(0.07f, 0.085f, 0.08f), true);
        Transform p = surface.transform;
        GameplayUIFactory.Label("Title", p, new Vector2(0.025f, 0.905f), new Vector2(0.6f, 0.985f), font, 30, "基因装配");
        panel.slotButtons = new Button[count];
        for (int i = 0; i < count; i++)
            panel.slotButtons[i] = GameplayUIFactory.Button("Slot " + i, p, new Vector2(0.025f + i * 0.955f / count, 0.76f),
                new Vector2(0.025f + (i + 1) * 0.955f / count - 0.01f, 0.895f), font, (i + 1) + "\n空槽");
        panel.inventoryCount = GameplayUIFactory.Label("Inventory Count", p, new Vector2(0.025f, 0.655f), new Vector2(0.42f, 0.73f), font, 22, "基因库存");
        Image viewport = GameplayUIFactory.Image("Inventory", p, new Vector2(0.025f, 0.16f), new Vector2(0.43f, 0.65f), new Color(0.11f, 0.13f, 0.12f), true);
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24f;
        RectTransform content = GameplayUIFactory.Rect("Content", viewport.transform, new Vector2(0, 1), Vector2.one);
        content.pivot = new Vector2(0.5f, 1f);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childControlHeight = true; layout.childForceExpandHeight = false;
        layout.childControlWidth = true; layout.childForceExpandWidth = true; layout.spacing = 6f;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content; scroll.viewport = viewport.rectTransform; panel.inventoryContent = content;
        panel.detail = GameplayUIFactory.Label("Gene Detail", p, new Vector2(0.47f, 0.34f), new Vector2(0.97f, 0.70f), font, 24, "", TextAnchor.UpperLeft);
        panel.rotation = GameplayUIFactory.Label("Attribute Sequence", p, new Vector2(0.47f, 0.16f), new Vector2(0.97f, 0.32f), font, 20, "");
        panel.leftButton = GameplayUIFactory.Button("Move Left", p, new Vector2(0.025f, 0.045f), new Vector2(0.085f, 0.115f), font, "<");
        panel.rightButton = GameplayUIFactory.Button("Move Right", p, new Vector2(0.095f, 0.045f), new Vector2(0.155f, 0.115f), font, ">");
        panel.removeButton = GameplayUIFactory.Button("Remove", p, new Vector2(0.18f, 0.045f), new Vector2(0.3f, 0.115f), font, "移除");
        panel.cancelButton = GameplayUIFactory.Button("Cancel", p, new Vector2(0.69f, 0.045f), new Vector2(0.81f, 0.115f), font, "取消");
        panel.confirmButton = GameplayUIFactory.Button("Confirm", p, new Vector2(0.835f, 0.045f), new Vector2(0.975f, 0.115f), font, "确认装配");
        panel.confirmButton.GetComponent<Image>().color = new Color(0.18f, 0.5f, 0.39f);
        overlay.gameObject.SetActive(false);
    }
    private static RunMenuPanel BuildMenu(Transform canvas)
    {
        Image overlay = GameplayUIFactory.Image("Run Menu", canvas, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.82f), true);
        RunMenuPanel menu = overlay.gameObject.AddComponent<RunMenuPanel>();
        menu.title = GameplayUIFactory.Label("Title", overlay.transform, new Vector2(0.25f, 0.57f), new Vector2(0.75f, 0.73f), font, 38, "已暂停", TextAnchor.MiddleCenter);
        menu.resumeButton = GameplayUIFactory.Button("Resume", overlay.transform, new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.48f), font, "继续探索");
        menu.retryButton = GameplayUIFactory.Button("Retry", overlay.transform, new Vector2(0.4f, 0.28f), new Vector2(0.6f, 0.36f), font, "重新开始");
        overlay.gameObject.SetActive(false);
        return menu;
    }
    private static void Filled(Image image) { image.sprite = white; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0; }
    private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(UnityEngine.Object target, string property, int value)
    {
        var so = new SerializedObject(target); so.FindProperty(property).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void AddState(AnimatorController controller, string name, string clipPath)
    {
        if (controller == null) return;
        foreach (var state in controller.layers[0].stateMachine.states) if (state.state.name == name) return;
        controller.layers[0].stateMachine.AddState(name).motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        EditorUtility.SetDirty(controller);
    }
    private static void UpdateEnemyPrefabs(GeneRules rules)
    {
        string path = "Assets/Resources/Prefab/Enemy/RestoreHealthMonster.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var drop = Ensure<EnemyGeneDrop>(prefab);
            drop.rules = rules;
            if (prefab.GetComponent<EnemyHealthBar>() == null) prefab.AddComponent<EnemyHealthBar>();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        path = "Assets/Resources/Prefab/Enemy/JellyfishProjectile.prefab";
        prefab = PrefabUtility.LoadPrefabContents(path);
        try { prefab.layer = LayerMask.NameToLayer("EnemyBullet"); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
    private static Sprite CreateWhiteSprite()
    {
        string path = "Assets/Resources/Prototype/White.asset";
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Sprite sprite) return sprite;
        var texture = UnityEngine.Object.Instantiate(Texture2D.whiteTexture);
        texture.name = "White"; texture.filterMode = FilterMode.Point;
        AssetDatabase.CreateAsset(texture, path);
        var whiteSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f, texture.width);
        whiteSprite.name = "White"; AssetDatabase.AddObjectToAsset(whiteSprite, texture);
        return whiteSprite;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }
    private static T Ensure<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }
    private static void MoveVisualToChild(GameObject player)
    {
        var renderer = player.GetComponent<SpriteRenderer>();
        var animator = player.GetComponent<Animator>();
        if (renderer == null || animator == null) return;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        EditorUtility.CopySerialized(renderer, visual.AddComponent<SpriteRenderer>());
        EditorUtility.CopySerialized(animator, visual.AddComponent<Animator>());
        UnityEngine.Object.DestroyImmediate(animator);
        UnityEngine.Object.DestroyImmediate(renderer);
    }
    private static void EnsureLayer(string name)
    {
        if (LayerMask.NameToLayer(name) >= 0) return;
        var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = so.FindProperty("layers");
        for (int i = 8; i < 32; i++)
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            { layers.GetArrayElementAtIndex(i).stringValue = name; so.ApplyModifiedProperties(); return; }
        throw new InvalidOperationException("No free layer for " + name);
    }
}
#endif
