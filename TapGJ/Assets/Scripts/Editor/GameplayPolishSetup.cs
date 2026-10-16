using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameplayPolishSetup
{
    private const string Generated = "Assets/ArtRes/Animation/Gameplay";
    private static GameplayUITheme theme;
    private static Font font;
    private static Sprite whiteUI;
    private static readonly Color Ink = new(0.1f, 0.15f, 0.2f);
    [MenuItem("Tools/TapGJ/Apply Gameplay Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before setup.");
        EnsureFolder(Generated);
        ConfigureLayers(); BuildMaterial(); ConfigureGenes(); BuildTheme(); ConfigurePlayer();
        foreach (int id in new[] {3,4,5,6,7}) BuildMonster(id);
        BuildUI(); ConfigureWorld();
        // Rebuilding gameplay should keep the approved player artwork and UI once they exist.
        if (Directory.Exists(PlayerAnimationPolish.Artwork + "/Idle"))
        {
            PlayerAnimationPolish.Apply();
            PlayerController scenePlayer = Object.FindFirstObjectByType<PlayerController>();
            if (scenePlayer != null) PlayerAnimationPolish.ConfigureVisual(scenePlayer);
        }
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene" &&
            File.Exists("Assets/ArtRes/UI/Player/StatusPanel.png")) PlayerUIPolishSetup.Apply();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
        Debug.Log("Gameplay polish setup complete.");
    }
    [MenuItem("Tools/TapGJ/Configure Main Menu")]
    public static void ConfigureMainMenu()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before setup.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "BeginScene") throw new InvalidOperationException("Open BeginScene first.");
        BeginPanel menu = Object.FindFirstObjectByType<BeginPanel>(FindObjectsInactive.Include);
        if (menu == null) throw new InvalidOperationException("Main menu missing.");
        Canvas canvas = menu.GetComponentInParent<Canvas>();
        BuildTheme();
        foreach (var keyboard in canvas.GetComponents<DialogScripts.DialogKeyboard>()) keyboard.enabled = false;
        Transform dialogue = canvas.transform.Find("BasePanel");
        if (dialogue != null) dialogue.gameObject.SetActive(false);
        Transform wave = canvas.transform.Find("Deco_Wave");
        if (wave != null) wave.gameObject.SetActive(false);
        menu.gameObject.SetActive(true); menu.isShow = true;
        CanvasGroup group = Component<CanvasGroup>(menu.gameObject);
        group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true;
        menu.btnContinue.interactable = false;
        if (menu.transform.Find("Game Title") == null)
        {
            Text title = Text("Game Title", menu.transform, .08f, .62f, .52f, .79f, 72, "地漏");
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2f,-3f);
            Text("Game Subtitle", menu.transform, .085f, .57f, .5f, .64f, 25, "Waterdrain");
        }
        foreach (SettingPanel settings in Object.FindObjectsByType<SettingPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(settings.gameObject);
        BuildSettings(canvas.transform);
        CanvasScaler scaler = Component<CanvasScaler>(canvas.gameObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f); scaler.matchWidthOrHeight = .5f;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    private static void ConfigureLayers()
    {
        SerializedObject tags = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tags.FindProperty("layers");
        foreach (string name in new[] {"EnemyBullet", "PlayerInvulnerable", "Pickup", "OneWayPlatform"})
        {
            if (LayerMask.NameToLayer(name) >= 0) continue;
            int index = Enumerable.Range(8,24).First(i => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue));
            layers.GetArrayElementAtIndex(index).stringValue = name;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (string actor in new[] {"Player", "PlayerInvulnerable", "Enemy"})
        foreach (string terrain in new[] {"Default", "Ground", "Wall", "OneWayPlatform"})
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer(actor), LayerMask.NameToLayer(terrain), false);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"),LayerMask.NameToLayer("Enemy"),true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("PlayerInvulnerable"),LayerMask.NameToLayer("Enemy"),true);
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("PlayerInvulnerable"),LayerMask.NameToLayer("EnemyBullet"),true);
        EditorUtility.SetDirty(tags.targetObject);
        EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset")[0]);
    }
    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\','/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    private static T Component<T>(GameObject obj) where T : Component
    {
        T result = obj.GetComponent<T>(); return result != null ? result : obj.AddComponent<T>();
    }
    private static void Set(Object target, string field, object value)
    {
        SerializedObject serialized = new(target); SerializedProperty property = serialized.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field);
        if (value is Object asset) property.objectReferenceValue = asset;
        else if (value is bool flag) property.boolValue = flag;
        else if (value is int integer) property.intValue = integer;
        else if (value is float number) property.floatValue = number;
        else if (value is string text) property.stringValue = text;
        else if (value is Vector2 vector) property.vector2Value = vector;
        else if (value is Modifier[] pool)
        {
            property.arraySize = pool.Length;
            for (int i = 0; i < pool.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
    private static string Art(string original, string organized) => AssetDatabase.IsValidFolder(original) ? original : organized;
    private static Sprite Sprite(string path, float fixedWorldWidth = 0f)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null && (fixedWorldWidth <= 0f || Mathf.Abs(sprite.bounds.size.x - fixedWorldWidth) < .001f)) return sprite;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing sprite: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        if (fixedWorldWidth > 0f && sprite != null) importer.spritePixelsPerUnit = sprite.rect.width / fixedWorldWidth;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (fixedWorldWidth > 0f && Mathf.Abs(sprite.bounds.size.x - fixedWorldWidth) >= .001f)
        {
            importer.spritePixelsPerUnit = sprite.rect.width / fixedWorldWidth; importer.SaveAndReimport();
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprite;
    }
    private static void BuildMaterial()
    {
        EnsureFolder("Assets/Resources/Material/Combat");
        Shader shader = Shader.Find("TapGJ/ElementSprite");
        if (shader == null) throw new InvalidOperationException("Element shader not imported.");
        const string path = "Assets/Resources/Material/Combat/ElementSprite.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        else material.shader = shader;
        const string whitePath = "Assets/Resources/Material/Combat/WhiteUI.asset";
        whiteUI = AssetDatabase.LoadAllAssetsAtPath(whitePath).OfType<Sprite>().FirstOrDefault();
        if (whiteUI == null)
        {
            Texture2D texture = new(1,1); texture.name = "WhiteUI"; texture.SetPixel(0,0,Color.white); texture.Apply();
            AssetDatabase.CreateAsset(texture,whitePath);
            whiteUI = UnityEngine.Sprite.Create(texture,new Rect(0,0,1,1),Vector2.one*.5f,1f);
            whiteUI.name = "WhiteUI"; AssetDatabase.AddObjectToAsset(whiteUI,texture);
        }
    }
    private static void ConfigureGenes()
    {
        foreach (Modifier gene in Resources.LoadAll<Modifier>("SO/Genes"))
        {
            bool attribute = gene.IsAttribute;
            Set(gene,"hasEvolution",true);
            float upgraded = attribute ? gene.ModifierValue : gene.ModifierType == E_ModifierType.ProjectileSpeedMultiplier ? 1.5f :
                gene.ModifierType == E_ModifierType.ProjectileMove ? gene.ModifierValue : gene.ModifierValue * 2f;
            Set(gene,"evolvedValue",upgraded);
            Set(gene,"effectRadius",3f); Set(gene,"evolvedRadius",5f); Set(gene,"enchantDuration",4f);
            if (gene.name == "water") { Set(gene,"geneName","扩散"); Set(gene,"desc","命中时使半径 3 米内的敌人湿润。"); Set(gene,"evolvedDescription","命中时使半径 5 米内的敌人湿润。"); }
            if (gene.name == "ember") { Set(gene,"geneName","火种"); Set(gene,"desc","命中时引燃半径 3 米内的敌人。"); Set(gene,"evolvedDescription","命中时引燃半径 5 米内的敌人。"); }
            if (gene.name == "sewage") { Set(gene,"geneName","污水"); Set(gene,"desc","毒弹消失后生成毒雾，遇火爆炸。"); }
            if (gene.name == "split") { Set(gene,"modifierType",(int)E_ModifierType.Split); Set(gene,"modifierValue",1f); Set(gene,"evolvedValue",2f); Set(gene,"desc","发射时同时射出基础 2 发；每装配一个改造基因（含自身）额外 +1 发。命中或消失时不再分裂。"); Set(gene,"evolvedDescription","发射时同时射出基础 2 发；每装配一个改造基因（含自身）额外 +2 发。命中或消失时不再分裂。"); }
        }
        const string vortexPath = "Assets/Resources/SO/Genes/vortex.asset";
        Modifier vortex = AssetDatabase.LoadAssetAtPath<Modifier>(vortexPath);
        if (vortex == null) { vortex = ScriptableObject.CreateInstance<Modifier>(); AssetDatabase.CreateAsset(vortex,vortexPath); }
        Set(vortex,"geneId",14); Set(vortex,"geneName","涡流"); Set(vortex,"desc","命中后将 3 × 1 米范围内的敌人聚拢。");
        Set(vortex,"modifierType",(int)E_ModifierType.Element); Set(vortex,"modifierValue",(float)E_ElementType.Water);
        Set(vortex,"vortex",true); Set(vortex,"effectRadius",0f); Set(vortex,"evolvedRadius",0f);
        Set(vortex,"hasEvolution",true); Set(vortex,"evolvedValue",(float)E_ElementType.Water);
        Set(vortex,"evolvedDescription","飞行中的水弹持续聚拢周围敌人。");
    }
    private static void BuildTheme()
    {
        EnsureFolder("Assets/Resources/SO/UI");
        const string path = "Assets/Resources/SO/UI/GameplayUITheme.asset";
        theme = AssetDatabase.LoadAssetAtPath<GameplayUITheme>(path);
        if (theme == null) { theme = ScriptableObject.CreateInstance<GameplayUITheme>(); AssetDatabase.CreateAsset(theme,path); }
        string folder = Art("Assets/ArtRes/GameUi/Image/UI/GenUi","Assets/ArtRes/UI/Genes");
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{folder}))
        {
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
        }
        theme.book=Sprite(folder+"/GenBook.png"); theme.slot=Sprite(folder+"/GenSlot.png");
        theme.chainBackground=Sprite(folder+"/GenChainBg.png"); theme.chain=Sprite(folder+"/GenChain.png");
        theme.iconBorder=Sprite(folder+"/GenIconBord.png"); theme.description=Sprite(folder+"/GenDesc.png");
        theme.element=Sprite(folder+"/Element.png"); theme.star=Sprite(folder+"/Star.png");
        string fontPath = AssetDatabase.FindAssets("t:Font").Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>p.EndsWith(".ttf",StringComparison.OrdinalIgnoreCase) && p.ToLowerInvariant().Contains("simkai"));
        font = fontPath != null ? AssetDatabase.LoadAssetAtPath<Font>(fontPath) : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EditorUtility.SetDirty(theme);
    }
    private static AnimatorController Controller(string prefix, Dictionary<string, string> states)
    {
        string folder=Generated+"/"+prefix; EnsureFolder(folder);
        string controllerPath=folder+"/"+prefix+".controller";
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null) controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine machine=controller.layers[0].stateMachine;
        string idlePath=Directory.GetFiles(states["Idle"],"*.png").OrderBy(NaturalFrame).First().Replace('\\','/');
        float frameWidth=Sprite(idlePath).bounds.size.x;
        foreach(var child in machine.states) machine.RemoveState(child.state);
        foreach(var entry in states)
        {
            string[] files=Directory.GetFiles(entry.Value,"*.png",SearchOption.TopDirectoryOnly).OrderBy(NaturalFrame).ToArray();
            if(files.Length==0)continue;
            Sprite[] sprites=files.Select(p=>Sprite(p.Replace('\\','/'),frameWidth)).Where(s=>s!=null).ToArray();
            if(sprites.Length==0)continue;
            string clipPath=folder+"/"+entry.Key+".anim";
            AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(clip==null){clip=new AnimationClip(); AssetDatabase.CreateAsset(clip,clipPath);}
            clip.frameRate=12f;
            ObjectReferenceKeyframe[] keys=new ObjectReferenceKeyframe[sprites.Length+1];
            for(int i=0;i<sprites.Length;i++)keys[i]=new ObjectReferenceKeyframe{time=i/12f,value=sprites[i]};
            keys[sprites.Length]=new ObjectReferenceKeyframe{time=sprites.Length/12f,value=sprites[sprites.Length-1]};
            AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
            var settings=AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime=entry.Key=="Idle"||entry.Key=="Run"||entry.Key=="move"||entry.Key=="Chase"||entry.Key=="Fall";
            AnimationUtility.SetAnimationClipSettings(clip,settings);
            AnimatorState state=machine.AddState(entry.Key); state.motion=clip;
            if(entry.Key=="Idle")machine.defaultState=state;
            EditorUtility.SetDirty(clip);
        }
        EditorUtility.SetDirty(controller); return controller;
    }
    private static string NaturalFrame(string path)
    {
        string name=Path.GetFileNameWithoutExtension(path);
        var match=System.Text.RegularExpressions.Regex.Match(name,@"[-_](\d+)$");
        return match.Success ? int.Parse(match.Groups[1].Value).ToString("D4") : name;
    }
    private static void ConfigurePlayer()
    {
        PlayerController player=Object.FindFirstObjectByType<PlayerController>();
        if(player==null)throw new InvalidOperationException("Player missing.");
        GameObject obj=player.gameObject;
        Rigidbody2D body=Component<Rigidbody2D>(obj); body.gravityScale=0.65f; body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        BoxCollider2D collider=Component<BoxCollider2D>(obj); collider.size=new Vector2(1f,1.4f); collider.offset=new Vector2(0f,-0.2f); collider.isTrigger=false;
        foreach(Collider2D child in obj.GetComponentsInChildren<Collider2D>(true))if(child!=collider)Object.DestroyImmediate(child);
        obj.layer=LayerMask.NameToLayer("Player");
        Set(player,"bodyCollider",collider); Set(player,"checkGroundLM",LayerMask.GetMask("Default","Ground","Wall","OneWayPlatform"));
        Set(player,"checkGroundDis",0.08f);
        Component<PlayerInteraction>(obj); Component<PlayerCollisionRules>(obj); Component<ActorElementEffects>(obj);
        GeneInventory inventory=Component<GeneInventory>(obj); Set(player,"geneInventory",inventory); Set(inventory,"weapon",obj.GetComponent<Weapon>());
        Set(obj.GetComponent<Weapon>(),"targetLM",LayerMask.GetMask("Enemy"));
        PlayerData data=Resources.Load<PlayerData>("SO/Player/DefaultPlayerData");
        data.jumpVelocity=4f; data.rollDuration=0.2f; data.dashUnlocked=false; EditorUtility.SetDirty(data);
        Set(player,"playerData",data);
        string folder=Art("Assets/ArtRes/999/Animation/Player","Assets/ArtRes/Animation/Player");
        var states=new Dictionary<string,string>{{"Idle",folder+"/Idle"},{"move",folder+"/Run"},{"Jump",folder+"/Jump"},{"Fall",folder+"/Jump"},{"Land",folder+"/Idle"},{"Attack",folder+"/Attack"},{"Hurt",folder+"/Hurt"},{"Dead",folder+"/Dead"},{"Roll",folder+"/Jump"}};
        Animator animator=obj.GetComponentInChildren<Animator>();
        if(animator==null)throw new InvalidOperationException("Player visual animator missing.");
        animator.runtimeAnimatorController=Controller("Player",states); animator.applyRootMotion=false;
        Set(player,"animator",animator); Set(player,"sr",animator.GetComponent<SpriteRenderer>());
        Transform muzzle=obj.transform.Find("AttackPos");
        if(muzzle==null){muzzle=new GameObject("AttackPos").transform;muzzle.SetParent(obj.transform,false);}
        muzzle.localPosition=new Vector3(.65f,.1f,0f); Set(player,"attackTransform",muzzle);
        player.transform.position=new Vector3(-13.67f,-0.7f,0f);
        PlayerCameraFollow follow=Component<PlayerCameraFollow>(Camera.main.gameObject); follow.target=player.transform;
        follow.horizontalLimits=new Vector2(-8f,176f);
        EnsureFolder("Assets/Resources/Prefab/Player");
        if(PrefabUtility.IsPartOfPrefabInstance(obj)) PrefabUtility.ApplyPrefabInstance(obj,InteractionMode.AutomatedAction);
        else PrefabUtility.SaveAsPrefabAssetAndConnect(obj,"Assets/Resources/Prefab/Player/Player.prefab",InteractionMode.AutomatedAction);
    }
    private static void BuildMonster(int id)
    {
        GameObject obj=GameObject.Find("Monster"+id);
        if(obj==null)obj=new GameObject("Monster"+id);
        string raw=id>=6?"Assets/ArtRes/Monster/RemoteMonster/"+id:"Assets/ArtRes/Monster/"+id;
        string organized="Assets/ArtRes/Monster/"+(id==4?"Support":id>=6?"Ranged":"Melee")+"/Monster"+id;
        string folder=Art(raw,organized);
        string statePrefix=id==3?"M":"";
        var states=new Dictionary<string,string>{{"Idle",folder+"/"+statePrefix+"Idle"},{"Run",folder+"/"+statePrefix+"Move"},{"Chase",folder+"/"+statePrefix+"Move"},{"Hurt",folder+"/"+statePrefix+"Hurt"},{"Dead",folder+"/"+statePrefix+"Dead"}};
        states.Add(id==4?"Blood":"Attack",folder+"/"+(id==4?"Recovery":statePrefix+"Attack"));
        if(PrefabUtility.IsPartOfPrefabInstance(obj))PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        Animator oldAnimator=obj.GetComponent<Animator>(); SpriteRenderer oldRenderer=obj.GetComponent<SpriteRenderer>();
        if(oldAnimator!=null)Object.DestroyImmediate(oldAnimator); if(oldRenderer!=null)Object.DestroyImmediate(oldRenderer);
        Transform child=obj.transform.Find("Visual");
        GameObject visual=child!=null?child.gameObject:new GameObject("Visual");
        visual.transform.SetParent(obj.transform,false);
        obj.transform.localScale=Vector3.one;
        SpriteRenderer renderer=Component<SpriteRenderer>(visual);
        string[] idle=Directory.GetFiles(states["Idle"],"*.png").OrderBy(NaturalFrame).ToArray();
        renderer.sprite=Sprite(idle[0].Replace('\\','/')); renderer.sortingOrder=6;
        float width=id==4?1.45f:id==5?1.45f:1.3f;
        float visualScale=width/renderer.sprite.bounds.size.x;
        visual.transform.localScale=Vector3.one*visualScale; visual.transform.localPosition=new Vector3(0f,0.12f,0f);
        Component<Animator>(visual).runtimeAnimatorController=Controller("Monster"+id,states);
        obj.layer=LayerMask.NameToLayer("Enemy");
        Rigidbody2D rb=Component<Rigidbody2D>(obj); rb.gravityScale=id>=4&&id!=5?0f:1f;
        rb.freezeRotation=true; rb.interpolation=RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D body=Component<BoxCollider2D>(obj); body.size=new Vector2(id==5?0.85f:0.9f,id==5?1f:0.8f); body.isTrigger=false;
        EnemyAIController ai=Component<EnemyAIController>(obj);
        Set(ai,"attackType",id==4?2:id>=6?1:0);
        Set(ai,"flyingEnemy",id>=4&&id!=5); Set(ai,"chaseEnemy",id==3||id==5);
        Set(ai,"patrolEnemy",true); Set(ai,"contactDamage",id==3||id==5);
        Set(ai,"retreatFromPlayer",id>=6); Set(ai,"patrolRange",3f); Set(ai,"patrolWait",1f);
        Set(ai,"obstacleMask",LayerMask.GetMask("Default","Ground","Wall","OneWayPlatform"));
        Set(ai,"attackRange",id>=6?10f:0.12f); Set(ai,"attackCooldown",2f);
        Set(ai,"attackWindup",0.25f); Set(ai,"attackDuration",0.65f); Set(ai,"meleeDamage",10f);
        Set(ai,"maxHp",id==4?6f:id==5?10f:8f); Set(ai,"detectRange",10f);
        Set(ai,"patrolAnimation","Run"); Set(ai,"chaseAnimation","Chase");
        Component<EnemyHealthBar>(obj); Component<ActorElementEffects>(obj);
        EnemyGeneDrop drop=Component<EnemyGeneDrop>(obj);
        Modifier[] genes=Resources.LoadAll<Modifier>("SO/Genes");
        E_ElementType group=id==3?E_ElementType.Fire:id==5||id==7?E_ElementType.Poison:E_ElementType.Water;
        Set(drop,"genePool",genes.Where(g=>!g.IsAttribute||g.Element==group).ToArray());
        if(id==4)Component<EnemySupportAura>(obj);
        if(id>=6)
        {
            JellyfishProjectile bullet=BuildEnemyBullet(id,folder);
            Set(ai,"projectilePrefab",bullet); Set(ai,"bulletSize",1f); Set(ai,"bulletSpeed",5f);
            Transform point=obj.transform.Find("FirePoint");
            if(point==null){point=new GameObject("FirePoint").transform;point.SetParent(obj.transform,false);}
            point.localPosition=new Vector3(0.55f,0.05f,0f); Set(ai,"firePoint",point);
        }
        // Separate overlapping preview actors and keep the entrance safe for first-time play.
        float x=id==3?-1f:id==4?7f:id==5?18f:id==6?12f:24f;
        obj.transform.position=new Vector3(x,id>=4&&id!=5?0.1f:-0.8f,0f);
        EnsureFolder("Assets/Resources/Prefab/Enemy/"+(id==4?"Support":id>=6?"Ranged":"Melee"));
        string path="Assets/Resources/Prefab/Enemy/"+(id==4?"Support":id>=6?"Ranged":"Melee")+"/Monster"+id+".prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(obj,path,InteractionMode.AutomatedAction);
    }
    private static JellyfishProjectile BuildEnemyBullet(int id,string folder)
    {
        string path="Assets/Resources/Prefab/Enemy/Ranged/Monster"+id+"Bullet.prefab";
        EnsureFolder("Assets/Resources/Prefab/Enemy/Ranged");
        GameObject obj=new("Monster"+id+"Bullet");
        obj.layer=LayerMask.NameToLayer("EnemyBullet");
        Rigidbody2D body=obj.AddComponent<Rigidbody2D>(); body.gravityScale=0f; body.bodyType=RigidbodyType2D.Kinematic;
        CircleCollider2D collider=obj.AddComponent<CircleCollider2D>(); collider.radius=0.14f; collider.isTrigger=true;
        SpriteRenderer renderer=obj.AddComponent<SpriteRenderer>();
        renderer.sprite=Sprite(Directory.GetFiles(folder+"/Bullet","*.png")[0].Replace('\\','/'));
        GameObject image=new("Visual"); image.transform.SetParent(obj.transform,false);
        SpriteRenderer sprite=image.AddComponent<SpriteRenderer>(); sprite.sprite=renderer.sprite; sprite.sortingOrder=7;
        image.transform.localScale=Vector3.one*(0.34f/renderer.sprite.bounds.size.x); Object.DestroyImmediate(renderer);
        JellyfishProjectile projectile=obj.AddComponent<JellyfishProjectile>();
        Set(projectile,"elementType",(int)(id==6?E_ElementType.Water:E_ElementType.Poison));
        Set(projectile,"groundLM",LayerMask.GetMask("Default","Ground","Wall"));
        GameObject asset=PrefabUtility.SaveAsPrefabAsset(obj,path); Object.DestroyImmediate(obj);
        return asset.GetComponent<JellyfishProjectile>();
    }
    private static Image Image(string name,Transform parent,float x0,float y0,float x1,float y1,Color color,Sprite sprite=null,bool raycast=false)
    {
        Image image=GameplayUIFactory.Image(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),color,raycast);
        image.sprite=sprite; return image;
    }
    private static Text Text(string name,Transform parent,float x0,float y0,float x1,float y1,int size,string value,bool dark=false)
    {
        Text text=GameplayUIFactory.Label(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,size,value);
        if(dark)text.color=Ink; return text;
    }
    private static Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string value,bool dark=false)
    {
        Button button=GameplayUIFactory.Button(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,value);
        if(dark){button.GetComponent<Image>().color=new Color(0.6f,0.9f,0.94f);button.GetComponentInChildren<Text>().color=Ink;}
        return button;
    }
    private static void BuildUI()
    {
        PlayerHudPanel old=Object.FindFirstObjectByType<PlayerHudPanel>(FindObjectsInactive.Include);
        if(old!=null)Object.DestroyImmediate(old.GetComponentInParent<Canvas>().gameObject);
        GameObject canvasObj=new("Gameplay Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        Canvas canvas=canvasObj.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200;
        CanvasScaler scaler=canvasObj.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600f,900f); scaler.matchWidthOrHeight=0.5f;
        EventSystem system=Object.FindFirstObjectByType<EventSystem>(); if(system==null)system=new GameObject("Gameplay EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)).GetComponent<EventSystem>();
        GameObject hudObj=GameplayUIFactory.Rect("Player HUD",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        PlayerHudPanel hud=hudObj.AddComponent<PlayerHudPanel>();
        Image("Health Back",hud.transform,.02f,.91f,.225f,.95f,new Color(.06f,.1f,.12f,.92f));
        hud.healthFill=Image("Health Fill",hud.transform,.023f,.914f,.222f,.946f,new Color(.23f,.9f,.62f));
        hud.healthFill.sprite=whiteUI;
        hud.healthFill.type=UnityEngine.UI.Image.Type.Filled; hud.healthFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        hud.healthText=Text("Health",hud.transform,.025f,.95f,.23f,.99f,24,"生命");
        hud.dashText=Text("Dash",hud.transform,.025f,.87f,.25f,.91f,20,"");
        Image("Gene Chain Back",hud.transform,.02f,.02f,.47f,.14f,new Color(.08f,.12f,.15f,.95f),theme.chainBackground);
        hud.slotTexts=new Text[6]; hud.slotIcons=new Image[6];
        for(int i=0;i<6;i++)
        {
            float x=.035f+i*.068f;
            hud.slotIcons[i]=Image("Slot "+i,hud.transform,x,.03f,x+.057f,.115f,Color.white,theme.slot);
            hud.slotTexts[i]=Text("Gene "+i,hud.transform,x+.005f,.04f,x+.052f,.105f,17,"");
            hud.slotTexts[i].alignment=TextAnchor.MiddleCenter;
        }
        hud.attributeText=Text("Attribute",hud.transform,.025f,.155f,.5f,.2f,21,"");
        Image("Stage Back",hud.transform,.025f,.142f,.45f,.151f,new Color(.12f,.2f,.24f));
        hud.stageFill=Image("Stage Fill",hud.transform,.025f,.142f,.45f,.151f,Color.cyan);
        hud.stageFill.sprite=whiteUI;
        hud.stageFill.type=UnityEngine.UI.Image.Type.Filled; hud.stageFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
        hud.interactionText=Text("Interaction",hud.transform,.55f,.045f,.97f,.18f,22,"");
        hud.interactionText.alignment=TextAnchor.LowerRight;
        hud.toastText=Text("Toast",hud.transform,.3f,.85f,.7f,.93f,25,"");hud.toastText.alignment=TextAnchor.MiddleCenter;
        GameObject editorObj=GameplayUIFactory.Rect("Gene Editor",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        Image("Scrim",editorObj.transform,0,0,1,1,new Color(.02f,.04f,.06f,.8f),null,true);
        Image("Gene Book",editorObj.transform,.045f,.06f,.955f,.94f,Color.white,theme.book);
        GeneEditPanel editor=editorObj.AddComponent<GeneEditPanel>(); editor.font=font;
        editor.inventoryCount=Text("Inventory Title",editor.transform,.215f,.766f,.45f,.815f,27,"基因库存",true);
        Text("Details Title",editor.transform,.55f,.766f,.87f,.815f,27,"装配方案",true);
        editor.filterButtons=new Button[3];
        for(int i=0;i<3;i++)editor.filterButtons[i]=Button("Filter "+i,editor.transform,.12f+i*.105f,.705f,.218f+i*.105f,.753f,new[]{"全部","改造","属性"}[i],true);
        RectTransform viewport=GameplayUIFactory.Rect("Inventory Scroll",editor.transform,new Vector2(.12f,.365f),new Vector2(.445f,.69f));
        Image mask=viewport.gameObject.AddComponent<Image>();mask.color=new Color(1f,1f,1f,.01f);viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content=GameplayUIFactory.Rect("Content",viewport,Vector2.zero,Vector2.one);content.pivot=new Vector2(.5f,1f);
        VerticalLayoutGroup layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8f;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        layout.childControlWidth=true;layout.childForceExpandWidth=true;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        editor.inventoryContent=content;
        editor.detailIcon=Image("Detail Icon",editor.transform,.55f,.66f,.62f,.76f,Color.white,theme.element);editor.detailIcon.preserveAspect=true;
        Image("Icon Border",editor.transform,.54f,.65f,.63f,.77f,Color.white,theme.iconBorder).preserveAspect=true;
        editor.detailLevel=Text("Evolution",editor.transform,.64f,.66f,.87f,.76f,23,"",true);
        editor.evolutionSegments=new Image[2];
        for(int i=0;i<2;i++)editor.evolutionSegments[i]=Image("Evolution Segment "+i,editor.transform,.645f+i*.095f,.662f,.73f+i*.095f,.674f,Color.cyan);
        Image("Description Background",editor.transform,.535f,.435f,.885f,.68f,Color.white,theme.description);
        editor.detail=Text("Description",editor.transform,.56f,.465f,.855f,.627f,22,"");
        editor.rotation=Text("Attribute Order",editor.transform,.55f,.35f,.87f,.46f,19,"",true);
        editor.slotButtons=new Button[6];
        for(int i=0;i<6;i++)
        {
            float x=.12f+i*.125f;
            editor.slotButtons[i]=Button("Slot "+i,editor.transform,x,.205f,x+.112f,.335f,"",true);
            editor.slotButtons[i].GetComponent<Image>().sprite=theme.slot;
            Image("Gene Face",editor.slotButtons[i].transform,.21f,.29f,.79f,.7f,new Color(.08f,.15f,.19f));
            editor.slotButtons[i].GetComponentInChildren<Text>().transform.SetAsLastSibling();
            editor.slotButtons[i].GetComponentInChildren<Text>().fontSize=19;
        }
        editor.leftButton=Button("Move Left",editor.transform,.12f,.13f,.18f,.19f,"<",true);
        editor.rightButton=Button("Move Right",editor.transform,.19f,.13f,.25f,.19f,">",true);
        editor.removeButton=Button("Remove",editor.transform,.265f,.13f,.395f,.19f,"移除",true);
        editor.confirmButton=Button("Confirm",editor.transform,.61f,.13f,.75f,.19f,"确认装配",true);
        editor.cancelButton=Button("Cancel",editor.transform,.77f,.13f,.87f,.19f,"取消",true);
        editorObj.SetActive(false);
        GameObject menuObj=GameplayUIFactory.Rect("Run Menu",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        Image("Menu Shade",menuObj.transform,0,0,1,1,new Color(.025f,.05f,.07f,.9f),null,true);
        RunMenuPanel menu=menuObj.AddComponent<RunMenuPanel>();
        menu.title=Text("Title",menu.transform,.32f,.65f,.68f,.76f,40,"");
        menu.title.alignment=TextAnchor.MiddleCenter;
        menu.resumeButton=Button("Resume",menu.transform,.38f,.52f,.62f,.6f,"继续探索");
        menu.retryButton=Button("Restart",menu.transform,.38f,.41f,.62f,.49f,"重新开始");
        menu.settingsButton=Button("Settings",menu.transform,.38f,.3f,.62f,.38f,"设置");
        menu.menuButton=Button("Main Menu",menu.transform,.38f,.19f,.62f,.27f,"返回主界面");
        hud.runMenu=menu;menuObj.SetActive(false);
        BuildSettings(canvas.transform);
        EditorUtility.SetDirty(editor);EditorUtility.SetDirty(hud);EditorUtility.SetDirty(menu);
    }
    private static void BuildSettings(Transform parent)
    {
        GameObject obj=GameplayUIFactory.Rect("Settings",parent,Vector2.zero,Vector2.one).gameObject;
        Image("Shade",obj.transform,0,0,1,1,new Color(.025f,.05f,.07f,1f),null,true);
        SettingPanel panel=obj.AddComponent<SettingPanel>();
        Text("Title",obj.transform,.3f,.7f,.7f,.8f,38,"设置").alignment=TextAnchor.MiddleCenter;
        Text("Effects",obj.transform,.3f,.58f,.46f,.64f,24,"音效");
        Text("Music",obj.transform,.3f,.45f,.46f,.51f,24,"音乐");
        panel.sliderGame=Slider(obj.transform,"Effects Volume",.46f,.59f,.7f,.63f);
        panel.sliderBk=Slider(obj.transform,"Music Volume",.46f,.46f,.7f,.5f);
        Text("Fullscreen",obj.transform,.3f,.32f,.57f,.38f,24,"全屏");
        Image toggle=Image("Fullscreen Toggle",obj.transform,.65f,.32f,.69f,.38f,new Color(.2f,.3f,.35f),null,true);
        panel.togFull=toggle.gameObject.AddComponent<Toggle>();panel.togFull.targetGraphic=toggle;
        panel.togFull.graphic=Image("Check",toggle.transform,.15f,.15f,.85f,.85f,new Color(.2f,1f,.8f));
        panel.btnClose=Button("Close",obj.transform,.4f,.18f,.6f,.26f,"返回");
        obj.SetActive(false);EditorUtility.SetDirty(panel);
    }
    private static Slider Slider(Transform parent,string name,float x0,float y0,float x1,float y1)
    {
        Image root=Image(name,parent,x0,y0,x1,y1,new Color(.13f,.2f,.25f),null,true);
        Slider slider=root.gameObject.AddComponent<Slider>();
        Image fill=Image("Fill",root.transform,0,0,1,1,new Color(.2f,.8f,.85f));
        Image handle=Image("Handle",root.transform,0,0,0,1,Color.white,null,true);
        RectTransform handleRect=handle.rectTransform;handleRect.sizeDelta=new Vector2(16f,0);
        slider.fillRect=fill.rectTransform;slider.handleRect=handleRect;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;return slider;
    }
    private static void ConfigureWorld()
    {
        Modifier[] genes=Resources.LoadAll<Modifier>("SO/Genes");
        foreach(GeneChest chest in Object.FindObjectsByType<GeneChest>(FindObjectsInactive.Include,FindObjectsSortMode.None))Set(chest,"genePool",genes);
        foreach(GeneEditStation station in Object.FindObjectsByType<GeneEditStation>(FindObjectsSortMode.None))
            StyleWorldItem(station.gameObject, theme.chain, Color.white, new Vector2(.65f,.65f));
        foreach(GeneChest chest in Object.FindObjectsByType<GeneChest>(FindObjectsSortMode.None))
            StyleWorldItem(chest.gameObject, theme.book, Color.white, new Vector2(.85f,.6f));
        foreach(PlayerSupply supply in Object.FindObjectsByType<PlayerSupply>(FindObjectsSortMode.None))
            StyleWorldItem(supply.gameObject, theme.star, supply.kind==PlayerSupply.SupplyKind.Health?new Color(.5f,1f,.65f):Color.cyan, new Vector2(.4f,.4f));
        PlayerController player=Object.FindFirstObjectByType<PlayerController>();
        foreach(GenePickup pickup in Object.FindObjectsByType<GenePickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(pickup.gene==null)pickup.gene=genes.FirstOrDefault(g=>pickup.name.Contains(g.name)) ??
                (pickup.name.Contains("diffusion")?genes.FirstOrDefault(g=>g.name=="water"):null);
        }
        GameObject electricity=GameObject.Find("Electric Leak");
        if(electricity==null)
        {
            electricity=new GameObject("Electric Leak");electricity.transform.position=new Vector3(30f,-1.1f,0);
            PrototypeVisual.Add(electricity,new Color(.5f,.8f,1f),new Vector2(.5f,.2f));
            electricity.AddComponent<EnvironmentElementSource>();
        }
        foreach(EnemyAIController legacy in Object.FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None))
        {
            if(legacy.name.StartsWith("Monster"))continue;
            if(legacy.GetComponent<ActorElementEffects>()==null)legacy.gameObject.AddComponent<ActorElementEffects>();
            if(legacy.Kind==EnemyAIController.AttackType.Melee)Set(legacy,"contactDamage",false);
        }
    }
    private static void StyleWorldItem(GameObject obj, Sprite sprite, Color color, Vector2 size)
    {
        SpriteRenderer old=obj.GetComponent<SpriteRenderer>(); if(old!=null)Object.DestroyImmediate(old);
        Transform found=obj.transform.Find("World Visual");
        GameObject visual=found!=null?found.gameObject:new GameObject("World Visual");
        visual.transform.SetParent(obj.transform,false);
        SpriteRenderer renderer=Component<SpriteRenderer>(visual);renderer.sprite=sprite;renderer.color=color;renderer.sortingOrder=8;
        Vector3 scale=obj.transform.lossyScale;
        visual.transform.localScale=new Vector3(size.x/(sprite.bounds.size.x*scale.x),size.y/(sprite.bounds.size.y*scale.y),1f);
    }
}
