using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>A compact experiment workbench with a clear inventory, ordered draft, and reaction preview.</summary>
public static class GeneEditorLayoutPolish
{
    private static Font font;
    private static Sprite rounded, circle;
    private static readonly Color Border = new Color(.47f,.38f,.63f);
    private static readonly Color Muted = new Color(.72f,.69f,.81f);

    public static void ApplyControls()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="GameScene")throw new InvalidOperationException("Open GameScene first.");
        var editor=Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include);
        editor.ConfigureKeyboardHints();
        editor.confirmButton.GetComponentInChildren<Text>(true).text="确认装配";
        EditorUtility.SetDirty(editor);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/TapGJ/Polish Gene Workbench")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "GameScene") throw new InvalidOperationException("Open GameScene first.");
        GeneEditPanel editor = Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include);
        if (editor == null) throw new InvalidOperationException("GeneEditPanel is missing.");
        Rebuild(editor);
        EditorUtility.SetDirty(editor);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Gene workbench rebuilt: inventory, 3x2 ordered slots, selected gene and reaction preview.");
    }

    public static void Rebuild(GeneEditPanel editor)
    {
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Circle.png");
        if (font == null || rounded == null || circle == null)
            throw new InvalidOperationException("Player UI font and saved shape sprites are required.");
        for (int i = editor.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(editor.transform.GetChild(i).gameObject);
        editor.font = font;
        Image shade = GameplayUIFactory.Image("Workbench Scrim", editor.transform, Vector2.zero, Vector2.one,
            new Color(.035f,.025f,.07f,.92f), true);
        Image border = Surface("Workbench Border", editor.transform, .055f,.065f,.945f,.935f, Border, true);
        Image workbench = Surface("Workbench Surface", border.transform, 0f,0f,1f,1f,
            new Color(.16f,.13f,.25f));
        workbench.rectTransform.offsetMin = Vector2.one * 4f;
        workbench.rectTransform.offsetMax = -Vector2.one * 4f;
        Transform root = workbench.transform;

        Text title = Label("Workbench Title", root, .035f,.892f,.45f,.954f, 32, "基因实验台", PlayerUIStyle.Cream);
        Label("Workbench Subtitle", root, .035f,.839f,.69f,.889f, 15,
            "选择槽位  →  装入基因  →  观察组合  →  确认应用", Muted);
        Surface("Theme Pill", root, .74f,.904f,.962f,.953f, PlayerUIStyle.Mint);
        Label("Theme", root, .747f,.905f,.956f,.952f, 15, WaterDrainUI.Signature, PlayerUIStyle.Ink, TextAnchor.MiddleCenter);
        Surface("Header Line", root, .035f,.823f,.965f,.826f, Border);

        BuildInventory(editor, root);
        BuildDraft(editor, root);
        BuildFooter(editor, root);
        editor.gameObject.SetActive(false);
    }

    private static void BuildInventory(GeneEditPanel editor, Transform root)
    {
        Transform inventory = Surface("Inventory Surface", root, .035f,.193f,.452f,.803f,
            new Color(.97f,.94f,.86f)).transform;
        editor.inventoryCount = Label("Inventory Title", inventory, .05f,.88f,.94f,.972f,
            22, "基因库存", PlayerUIStyle.Ink);
        editor.filterButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float x = .05f + i * .303f;
            editor.filterButtons[i] = Button("Filter " + i, inventory, x,.768f,x+.287f,.85f,
                new[] { "全部", "改造", "属性" }[i], PlayerUIStyle.Cream, 17);
        }

        RectTransform viewport = GameplayUIFactory.Rect("Inventory Scroll", inventory,
            new Vector2(.05f,.258f),new Vector2(.934f,.736f));
        Image mask = viewport.gameObject.AddComponent<Image>();
        mask.color = new Color(1f,1f,1f,.01f); mask.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = GameplayUIFactory.Rect("Content", viewport, Vector2.zero, Vector2.one);
        content.pivot = new Vector2(.5f,1f);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        layout.childControlWidth = true; layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        editor.inventoryContent = content;

        Image track = Surface("Inventory Scrollbar", inventory, .945f,.258f,.958f,.736f,
            new Color(.28f,.22f,.4f,.20f), true);
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        Image handle = Surface("Handle", track.transform, 0f,0f,1f,1f, Border, true);
        scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        Surface("Inventory Hint Surface", inventory, .05f,.038f,.95f,.229f,
            new Color(.87f,.84f,.82f));
        editor.inventoryHint = Label("Inventory Hint", inventory, .08f,.05f,.925f,.218f, 13,
            "已装配  0 / 6\n同一实例只能占据一格；同名的独立实例可分别装配。\n探索冷库与敌人掉落，扩展你的组合。", PlayerUIStyle.Violet);
    }

    private static void BuildDraft(GeneEditPanel editor, Transform root)
    {
        Label("Draft Title", root, .49f,.755f,.95f,.802f, 22, "装配方案", PlayerUIStyle.Cream);
        Label("Draft Rule", root, .49f,.719f,.95f,.757f, 13,
            "A/D 左右选槽 · W/S 上下选槽 · 点击库存装入", Muted);
        editor.slotButtons = new Button[6];
        for (int i = 0; i < 6; i++)
        {
            float x = .49f + i % 3 * (.46f / 3f);
            float y = .719f - (i / 3 + 1) * .126f;
            Button button = Button("Slot " + i, root, x,y,x+.1413f,y+.108f, "空槽", PlayerUIStyle.Ink, 18);
            editor.slotButtons[i] = button;
            Text label = button.transform.Find("Label").GetComponent<Text>();
            label.rectTransform.anchorMin = new Vector2(.34f,.34f);
            label.rectTransform.anchorMax = new Vector2(.95f,.76f);
            label.alignment = TextAnchor.MiddleLeft; label.color = PlayerUIStyle.Cream;
            Image well = Surface("Slot Well", button.transform, .055f,.20f,.285f,.80f, PlayerUIStyle.Violet);
            well.sprite = circle; well.type = Image.Type.Simple; well.preserveAspect = true;
            Text number = Label("Slot Number", button.transform, .035f,.77f,.20f,.98f, 12,
                (i + 1).ToString("00"), Muted);
            Label("Level Mark", button.transform, .81f,.78f,.96f,.98f, 12, "", PlayerUIStyle.Gold, TextAnchor.MiddleRight);
            Label("Empty Glyph", button.transform, .055f,.20f,.285f,.80f, 26, "+", Muted, TextAnchor.MiddleCenter);
            Label("Slot State", button.transform, .34f,.09f,.95f,.36f, 11, "点击选择此槽", Muted);
            Image face = GameplayUIFactory.Image("Gene Face", button.transform,
                new Vector2(.075f,.25f), new Vector2(.265f,.75f), PlayerUIStyle.Mint);
            face.preserveAspect = true; face.gameObject.SetActive(false);
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2,-2); outline.effectColor = PlayerUIStyle.Mint; outline.enabled = i == 0;
            label.transform.SetAsLastSibling();
        }

        Surface("Gene Detail Surface", root, .49f,.193f,.95f,.435f, new Color(.22f,.18f,.34f));
        editor.selectedSlotLabel = Label("Selected Slot", root, .505f,.378f,.93f,.422f,
            20, "槽位 01 · 等待装配", PlayerUIStyle.Cream);
        Image detailWell = Surface("Detail Icon Well", root, .505f,.264f,.568f,.361f, PlayerUIStyle.Ink);
        detailWell.sprite = circle; detailWell.type = Image.Type.Simple; detailWell.preserveAspect = true;
        editor.detailIcon = GameplayUIFactory.Image("Detail Icon", root,
            new Vector2(.515f,.278f),new Vector2(.558f,.346f), PlayerUIStyle.Mint);
        editor.detailIcon.preserveAspect = true; editor.detailIcon.enabled = false;
        editor.detail = Label("Description", root, .582f,.253f,.93f,.367f, 16,
            "点击左侧库存，将基因装入此槽。\n属性会按槽位顺序附魔，改造同时生效。", PlayerUIStyle.Cream);
        editor.detail.alignment = TextAnchor.UpperLeft;
        editor.detailLevel = Label("Evolution", root, .505f,.212f,.88f,.247f, 12,
            "选择与装配仍是预览；确认后才应用", Muted);
        editor.evolutionSegments = new Image[2];
        for (int i = 0; i < 2; i++) editor.evolutionSegments[i] = Surface("Evolution Segment " + i,
            root,.886f+i*.026f,.221f,.906f+i*.026f,.231f,Border);
    }

    private static void BuildFooter(GeneEditPanel editor, Transform root)
    {
        Surface("Build Preview", root, .035f,.105f,.965f,.180f, new Color(.10f,.10f,.18f));
        editor.rotation = Label("Attribute Order", root, .05f,.115f,.472f,.170f, 15,
            "附魔顺序  基础水弹 · 未装配属性", PlayerUIStyle.Cream);
        editor.rotation.resizeTextForBestFit = true;
        editor.rotation.resizeTextMinSize = 11; editor.rotation.resizeTextMaxSize = 17;
        editor.buildHint = Label("Reaction Preview", root, .49f,.115f,.95f,.170f, 14,
            "改造同时生效 · 属性按槽位顺序右键触发", PlayerUIStyle.Mint);
        editor.buildHint.resizeTextForBestFit = true;
        editor.buildHint.resizeTextMinSize = 12; editor.buildHint.resizeTextMaxSize = 14;

        editor.leftButton = Button("Move Left", root, .035f,.022f,.136f,.077f,"← 前移",PlayerUIStyle.Cream,15);
        editor.rightButton = Button("Move Right", root, .148f,.022f,.249f,.077f,"后移 →",PlayerUIStyle.Cream,15);
        editor.removeButton = Button("Remove", root, .261f,.022f,.362f,.077f,"移除此格",PlayerUIStyle.Cream,15);
        Label("Draft Reminder",root,.378f,.022f,.65f,.077f,12,"确认后应用 · 取消保留原构筑",Muted);
        editor.confirmButton = Button("Confirm",root,.674f,.022f,.843f,.077f,"确认装配",PlayerUIStyle.Mint,19);
        Label("Confirm Shortcut",editor.confirmButton.transform,0,1.02f,1,1.48f,13,"Space确认装配",PlayerUIStyle.Mint,TextAnchor.MiddleCenter);
        editor.cancelButton = Button("Cancel",root,.855f,.022f,.965f,.077f,"取消 / ESC",PlayerUIStyle.Cream,16);
    }

    private static Image Surface(string name, Transform parent, float x0,float y0,float x1,float y1,Color color,bool raycast=false)
    {
        Image image = GameplayUIFactory.Image(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),color,raycast);
        image.sprite = rounded; image.type = Image.Type.Sliced; return image;
    }
    private static Text Label(string name, Transform parent,float x0,float y0,float x1,float y1,int size,string value,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        Text text = GameplayUIFactory.Label(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,size,value,alignment);
        text.color = color; return text;
    }
    private static Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string value,Color color,int size)
    {
        Button button = GameplayUIFactory.Button(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,value);
        Image image = button.GetComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
        Text text = button.transform.Find("Label").GetComponent<Text>(); text.fontSize = size;
        text.color = color == PlayerUIStyle.Ink ? PlayerUIStyle.Cream : PlayerUIStyle.Ink;
        ColorBlock colors = button.colors; colors.normalColor=Color.white; colors.selectedColor=Color.white;
        colors.highlightedColor=new Color(.85f,1f,.94f); colors.pressedColor=new Color(.65f,.86f,.78f);
        colors.disabledColor=new Color(.59f,.55f,.69f,.55f); colors.fadeDuration=.08f; button.colors=colors;
        return button;
    }
}
