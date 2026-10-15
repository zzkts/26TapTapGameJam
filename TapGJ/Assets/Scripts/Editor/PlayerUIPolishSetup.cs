using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Updates player UI only; preserves world placement, combat data and menu callbacks.</summary>
public static class PlayerUIPolishSetup
{
    private static Font font;
    private static Sprite rounded, circle, panel, portrait;
    private static readonly Color Muted = new(.74f, .7f, .84f);
    [MenuItem("Tools/TapGJ/Polish Player UI")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "GameScene") throw new InvalidOperationException("Open GameScene first.");
        PlayerHudPanel hud = Object.FindFirstObjectByType<PlayerHudPanel>(FindObjectsInactive.Include);
        if (hud == null) throw new InvalidOperationException("Player HUD missing.");
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        if (font == null) throw new InvalidOperationException("Noto Sans SC font missing.");
        rounded = PersistShape("Rounded", PlayerUIStyle.Rounded);
        circle = PersistShape("Circle", PlayerUIStyle.Circle);
        panel = Import("Assets/ArtRes/UI/Player/StatusPanel.png", new Vector4(240, 70, 240, 70));
        portrait = Import("Assets/ArtRes/UI/Player/Portrait.png");
        if (panel == null || portrait == null) throw new InvalidOperationException("Generated player UI assets missing.");
        // The old HUD contains only visuals; menus are sibling panels and keep their bindings.
        for (int i = hud.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(hud.transform.GetChild(i).gameObject);
        RectTransform visuals = GameplayUIFactory.Rect("Player Visuals", hud.transform, Vector2.zero, Vector2.one);
        hud.visualGroup = visuals.gameObject.AddComponent<CanvasGroup>();
        hud.visualGroup.blocksRaycasts = false; hud.visualGroup.interactable = false;
        BuildVitals(hud, visuals); BuildChain(hud, visuals); BuildPrompts(hud, visuals);
        RestyleEditor(Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include));
        RestyleMenus(hud.GetComponentInParent<Canvas>());
        foreach (Text label in hud.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>(true))
        {
            label.font = font;
            label.resizeTextForBestFit = false;
        }
        GeneEditPanel geneEditor = Object.FindFirstObjectByType<GeneEditPanel>(FindObjectsInactive.Include);
        if (geneEditor != null) GeneEditorLayoutPolish.Rebuild(geneEditor);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Player UI polish applied: portrait, vitality, dash cooldown, gene chain and prompts.");
    }
    private static void BuildVitals(PlayerHudPanel hud, Transform root)
    {
        hud.vitality = Card("Vitality", root, new Vector2(0, 1), new Vector2(22, -22), new Vector2(432, 124));
        Transform card = hud.vitality;
        Draw("Portrait Halo", card, 24, 13, 93, 93, new Color(.5f, .43f, .7f, .7f), circle);
        hud.portrait = Draw("Drain Portrait", card, 15, 12, 112, 112, Color.white, portrait);
        hud.portrait.preserveAspect = true;
        Label("Identity", card, 140, 86, 170, 24, 19, "地漏  /  生命", PlayerUIStyle.Cream);
        hud.healthText = Label("Health", card, 316, 86, 86, 24, 18, "10 / 10", PlayerUIStyle.Cream, TextAnchor.MiddleRight);
        Draw("Health Well", card, 138, 55, 266, 24, new Color(.1f, .1f, .19f), rounded);
        hud.healthTrail = Fill("Health Loss", card, 142, 59, 258, 16, new Color(1f, .48f, .68f));
        hud.healthFill = Fill("Health Liquid", card, 142, 59, 258, 16, PlayerUIStyle.Mint);
        for (int i = 1; i < 10; i++) Draw("Health Mark " + i, card, 142 + i * 25.8f, 58, 1.5f, 18, new Color(.13f, .16f, .23f, .32f), null);
        Draw("Dash Ring Back", card, 139, 17, 25, 25, new Color(.19f, .17f, .3f), circle);
        hud.dashFill = Draw("Dash Ring", card, 139, 17, 25, 25, PlayerUIStyle.Mint, circle);
        hud.dashFill.type = Image.Type.Filled; hud.dashFill.fillMethod = Image.FillMethod.Radial360;
        Draw("Dash Ring Center", card, 144, 22, 15, 15, new Color(.17f, .15f, .28f), circle);
        Draw("Shift Key", card, 174, 17, 54, 25, new Color(.43f, .36f, .59f), rounded);
        Label("Shift Label", card, 174, 17, 54, 25, 12, "SHIFT", PlayerUIStyle.Cream, TextAnchor.MiddleCenter);
        hud.dashText = Label("Dash", card, 239, 17, 165, 25, 15, "冲刺 · 寻找模块", Muted);
    }
    private static void BuildChain(PlayerHudPanel hud, Transform root)
    {
        RectTransform card = Card("Gene Chain", root, Vector2.zero, new Vector2(22, 22), new Vector2(500, 149));
        hud.attributeText = Label("Attribute", card, 80, 112, 174, 25, 19, "基础水弹", PlayerUIStyle.Cream);
        hud.phaseText = Label("Phase", card, 278, 112, 187, 25, 16, "左键射击", PlayerUIStyle.Mint, TextAnchor.MiddleRight);
        Draw("Phase Track", card, 33, 98, 434, 6, new Color(.15f, .13f, .26f), rounded);
        hud.stageFill = Fill("Stage Fill", card, 33, 98, 434, 6, PlayerUIStyle.Mint);
        hud.slotTexts = new Text[6]; hud.slotLevels = new Text[6]; hud.slotIcons = new Image[6];
        hud.slotFrames = new Image[6]; hud.slotHighlights = new Image[6]; hud.slotWells = new Image[6];
        Sprite slot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Genes/GenSlot.png");
        for (int i = 0; i < 6; i++)
        {
            float x = 34 + i * 74;
            hud.slotHighlights[i] = Draw("Current Gene " + i, card, x - 4, 34, 67, 67, PlayerUIStyle.Mint, circle);
            hud.slotHighlights[i].enabled = false;
            hud.slotFrames[i] = Draw("Slot " + i, card, x, 38, 59, 59, Color.white, slot);
            hud.slotFrames[i].preserveAspect = true;
            Outline rarity = hud.slotFrames[i].gameObject.AddComponent<Outline>();
            rarity.effectColor = PlayerUIStyle.Gold; rarity.effectDistance = new Vector2(2,-2); rarity.enabled = false;
            hud.slotWells[i] = Draw("Gene Well " + i, card, x + 13, 51, 33, 33, PlayerUIStyle.Ink, circle);
            hud.slotWells[i].enabled = false;
            hud.slotIcons[i] = Draw("Gene Icon " + i, card, x + 15, 53, 29, 29, PlayerUIStyle.Mint, null);
            hud.slotIcons[i].enabled = false;
            hud.slotLevels[i] = Label("Gene Level " + i, card, x + 43, 79, 16, 18, 11, (i + 1).ToString(), Muted, TextAnchor.MiddleCenter);
            hud.slotTexts[i] = Label("Gene " + i, card, x - 7, 17, 73, 19, 12, "空槽", Muted, TextAnchor.MiddleCenter);
        }
        hud.nextText = Label("Next Stage", root, 0, 0, 0, 0, 13, "在终端装配基因", Muted);
        hud.nextText.rectTransform.anchorMin = hud.nextText.rectTransform.anchorMax = Vector2.zero;
        hud.nextText.rectTransform.pivot = Vector2.zero;
        hud.nextText.rectTransform.anchoredPosition = new Vector2(39, 178);
        hud.nextText.rectTransform.sizeDelta = new Vector2(470, 24);
    }
    private static void BuildPrompts(PlayerHudPanel hud, Transform root)
    {
        hud.interactionCard = Card("Interaction Card", root, new Vector2(.5f, .5f), new Vector2(0, -150), new Vector2(366, 70));
        hud.interactionCard.pivot = new Vector2(.5f, 0);
        Draw("E Key", hud.interactionCard, 22, 20, 36, 32, PlayerUIStyle.Mint, rounded);
        Label("E", hud.interactionCard, 22, 20, 36, 32, 19, "E", PlayerUIStyle.Ink, TextAnchor.MiddleCenter);
        hud.interactionText = Label("Interaction", hud.interactionCard, 76, 13, 262, 48, 17, "", PlayerUIStyle.Cream);
        hud.interactionCard.gameObject.SetActive(false);
        hud.toastCard = Card("Pickup Toast", root, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(448, 68));
        hud.toastIcon = Draw("Toast Icon", hud.toastCard, 32, 18, 30, 30, PlayerUIStyle.Mint, circle);
        hud.toastText = Label("Toast", hud.toastCard, 80, 16, 328, 36, 18, "", PlayerUIStyle.Cream);
        hud.toastCard.gameObject.SetActive(false);
        RectTransform guide = GameplayUIFactory.Rect("Control Guide", root, new Vector2(1, 0), new Vector2(1, 0));
        guide.pivot = new Vector2(1, 0); guide.anchoredPosition = new Vector2(-24, 24); guide.sizeDelta = new Vector2(255, 53);
        Draw("Guide Back", guide, 0, 0, 255, 53, new Color(.16f, .13f, .27f, .92f), rounded);
        Label("Controls", guide, 17, 8, 221, 38, 12, "A / D  移动    空格  跳跃\n左键  射击    右键  附魔    ESC  暂停", Muted);
    }
    private static void RestyleEditor(GeneEditPanel editor)
    {
        if (editor == null) return;
        editor.font = font;
        foreach (Button button in editor.GetComponentsInChildren<Button>(true))
        {
            Image image = button.GetComponent<Image>();
            bool isSlot = Array.IndexOf(editor.slotButtons, button) >= 0;
            if (!isSlot) { image.sprite = rounded; image.type = Image.Type.Sliced; image.color = new Color(.47f, .36f, .66f); }
            ColorBlock colors = button.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(.78f, 1f, .92f); colors.pressedColor = new Color(.5f, .8f, .71f);
            colors.disabledColor = new Color(.48f, .45f, .55f, .65f); button.colors = colors;
            Text text = button.GetComponentInChildren<Text>(); if (text != null) { text.color = PlayerUIStyle.Cream; text.fontSize = isSlot ? 16 : 18; }
        }
        foreach (Text label in editor.GetComponentsInChildren<Text>(true)) label.font = font;
        ScrollRect scroll = editor.inventoryContent.GetComponentInParent<ScrollRect>();
        if (scroll != null && scroll.verticalScrollbar == null)
        {
            Image track = GameplayUIFactory.Image("Inventory Scrollbar", editor.transform,
                new Vector2(.45f, .365f), new Vector2(.455f, .69f), new Color(.28f, .2f, .4f, .28f), true);
            track.sprite = rounded; track.type = Image.Type.Sliced;
            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            Image handle = GameplayUIFactory.Image("Handle", track.transform, Vector2.zero, Vector2.one, PlayerUIStyle.Mint, true);
            handle.sprite = rounded; handle.type = Image.Type.Sliced;
            scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        editor.detail.color = PlayerUIStyle.Cream;
        editor.rotation.color = new Color(.28f, .19f, .4f);
        EditorUtility.SetDirty(editor);
    }
    private static RectTransform Card(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = GameplayUIFactory.Rect(name, parent, anchor, anchor);
        rect.pivot = new Vector2(anchor.x, anchor.y); rect.anchoredPosition = position; rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>(); image.sprite = panel; image.type = Image.Type.Simple;
        image.raycastTarget = false;
        return rect;
    }
    private static void RestyleMenus(Canvas canvas)
    {
        RunMenuPanel menu = canvas.GetComponentInChildren<RunMenuPanel>(true);
        if (menu != null)
        {
            ModalSurface(menu.transform, "Menu Shade", new Vector2(.3f,.12f), new Vector2(.7f,.9f));
            Transform old = menu.transform.Find("Menu Portrait"); if (old != null) Object.DestroyImmediate(old.gameObject);
            menu.portrait = GameplayUIFactory.Image("Menu Portrait", menu.transform,
                new Vector2(.446f,.766f), new Vector2(.554f,.895f), Color.white);
            menu.portrait.preserveAspect = true; menu.alivePortrait = portrait;
            menu.deadPortrait = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerAnimationPolish.Artwork + "/Dead/Dead_00.png");
            menu.portrait.sprite = portrait;
            foreach (Button button in new[] {menu.resumeButton, menu.retryButton, menu.settingsButton, menu.menuButton})
                StyleModalButton(button, button == menu.resumeButton ? PlayerUIStyle.Mint : button == menu.retryButton ? PlayerUIStyle.Cream : new Color(.42f,.33f,.58f));
            menu.title.color = PlayerUIStyle.Cream;
            EditorUtility.SetDirty(menu);
        }
        foreach (SettingPanel settings in canvas.GetComponentsInChildren<SettingPanel>(true))
        {
            ModalSurface(settings.transform, "Shade", new Vector2(.26f,.12f), new Vector2(.74f,.88f));
            StyleModalButton(settings.btnClose, PlayerUIStyle.Mint);
            Image toggle = settings.togFull.GetComponent<Image>();
            toggle.sprite = rounded; toggle.type = Image.Type.Sliced; toggle.color = new Color(.4f,.32f,.57f);
            Image check = settings.togFull.graphic as Image;
            if (check != null) {check.sprite = circle; check.color = PlayerUIStyle.Mint; check.preserveAspect = true;}
            foreach (Slider slider in settings.GetComponentsInChildren<Slider>(true))
            {
                Image back = slider.GetComponent<Image>(); if (back != null) {back.sprite = rounded; back.type = Image.Type.Sliced; back.color = PlayerUIStyle.Ink;}
                Image fill = slider.fillRect.GetComponent<Image>(); fill.sprite = rounded; fill.type = Image.Type.Sliced; fill.color = PlayerUIStyle.Mint;
                Image handle = slider.handleRect.GetComponent<Image>(); handle.sprite = rounded; handle.color = PlayerUIStyle.Cream;
            }
        }
    }
    private static void ModalSurface(Transform root, string shadeName, Vector2 min, Vector2 max)
    {
        Transform shade = root.Find(shadeName); if (shade != null) shade.GetComponent<Image>().color = new Color(.05f,.035f,.1f,.88f);
        Transform old = root.Find("Polished Modal Surface"); if (old != null) Object.DestroyImmediate(old.gameObject);
        Image border = GameplayUIFactory.Image("Polished Modal Surface", root, min, max, new Color(.56f,.44f,.73f), true);
        border.sprite = rounded; border.type = Image.Type.Sliced; border.transform.SetSiblingIndex(1);
        Image well = GameplayUIFactory.Image("Surface", border.transform, Vector2.zero, Vector2.one, new Color(.18f,.14f,.29f));
        well.sprite = rounded; well.type = Image.Type.Sliced; well.rectTransform.offsetMin = new Vector2(5,5); well.rectTransform.offsetMax = new Vector2(-5,-5);
    }
    private static void StyleModalButton(Button button, Color color)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
        Text label = button.GetComponentInChildren<Text>(); label.color = color == PlayerUIStyle.Mint || color == PlayerUIStyle.Cream ? PlayerUIStyle.Ink : PlayerUIStyle.Cream;
        label.verticalOverflow = VerticalWrapMode.Overflow; label.fontSize = 20;
        ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.selectedColor = Color.white;
        colors.highlightedColor = new Color(.8f,1f,.94f); colors.pressedColor = new Color(.57f,.78f,.69f); button.colors = colors;
    }
    private static Image Draw(string name, Transform parent, float x, float y, float width, float height, Color color, Sprite sprite)
    {
        Image image = GameplayUIFactory.Image(name, parent, Vector2.zero, Vector2.zero, color);
        RectTransform rect = image.rectTransform; rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        image.sprite = sprite; if (sprite == rounded) image.type = Image.Type.Sliced;
        return image;
    }
    private static Image Fill(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        Image image = Draw(name, parent, x, y, width, height, color, rounded);
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; return image;
    }
    private static Text Label(string name, Transform parent, float x, float y, float width, float height, int size, string value, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        Text label = GameplayUIFactory.Label(name, parent, Vector2.zero, Vector2.zero, font, size, value, alignment);
        label.rectTransform.pivot = Vector2.zero; label.rectTransform.anchoredPosition = new Vector2(x, y);
        label.rectTransform.sizeDelta = new Vector2(width, height); label.color = color;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    private static Sprite PersistShape(string name, Sprite source)
    {
        string path = "Assets/ArtRes/UI/Player/" + name + ".png";
        if (!File.Exists(path)) File.WriteAllBytes(path, source.texture.EncodeToPNG());
        return Import(path, source.border);
    }
    private static Sprite Import(string path, Vector4 border = default)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.spriteBorder = border; importer.spritePixelsPerUnit = path.EndsWith("StatusPanel.png") ? 400 : 100;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
