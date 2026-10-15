using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CompleteGameMenuBuilder
{
    private static Font font;
    private static Sprite rounded, circle;
    [MenuItem("Tools/TapGJ/Complete Game/Rebuild Main Menu")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "BeginScene") throw new InvalidOperationException("Open BeginScene first.");
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Circle.png");
        Sprite backdrop = Import("Assets/ArtRes/WorldPolished/MenuBackground.png");
        if (font == null || backdrop == null) throw new InvalidOperationException("Menu art or font missing.");
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Canvas old in root.GetComponentsInChildren<Canvas>(true)) Object.DestroyImmediate(old.gameObject);
        var obj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = obj.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
        Image background = Art("Cavern Background", obj.transform, Vector2.zero, Vector2.one, backdrop, Color.white);
        AspectRatioFitter cover = background.gameObject.AddComponent<AspectRatioFitter>(); cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        cover.aspectRatio = backdrop.rect.width / backdrop.rect.height;
        Art("Quiet Menu Veil", obj.transform, Vector2.zero, Vector2.one, null, new Color(.09f,.06f,.18f,.22f));
        var menuObj = GameplayUIFactory.Rect("Begin Panel", obj.transform, Vector2.zero, Vector2.one).gameObject;
        BeginPanel menu = menuObj.AddComponent<BeginPanel>();
        Art("Menu Surface", menu.transform, new Vector2(.075f,.115f), new Vector2(.585f,.91f), rounded, new Color(.16f,.11f,.28f,.82f)).type = Image.Type.Sliced;
        Text title = Label("Game Title", menu.transform, .13f,.71f,.565f,.84f,84,"地漏",PlayerUIStyle.Cream);
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3,-4);
        Label("Game Subtitle",menu.transform,.135f,.65f,.55f,.7f,27,"WATER DRAIN",PlayerUIStyle.Mint);
        Label("Chapter",menu.transform,.135f,.585f,.55f,.64f,27,"涌现 · 一滴水唤醒地下生态",PlayerUIStyle.Cream);
        menu.btnBegin = Button("Start Game",menu.transform,.13f,.46f,.53f,.535f,"开始故事",PlayerUIStyle.Mint,27);
        menu.btnContinue = Button("Continue Game",menu.transform,.13f,.365f,.53f,.435f,"继续探索",new Color(.51f,.41f,.69f),22);
        menu.btnTrial = Button("Backflow Trial",menu.transform,.13f,.275f,.53f,.345f,"回流试验 · 不同规则再来一局",new Color(.34f,.64f,.65f),21);
        menu.btnSetting = Button("Settings Button",menu.transform,.13f,.18f,.32f,.25f,"设置",PlayerUIStyle.Cream,22);
        menu.btnQuit = Button("Quit Button",menu.transform,.34f,.18f,.53f,.25f,"退出游戏",new Color(.51f,.41f,.69f),22);
        Label("Control Hint",menu.transform,.135f,.115f,.55f,.17f,14,"S+F 吸水 · F 放水 · Q / R 播种 · E 交互\n空格跳跃 / 空中踏跃 · Shift 冲刺 · M 水网目标",new Color(.77f,.77f,.85f));
        Label("Jam Credit",menu.transform,.135f,.05f,.7f,.09f,13,"TapTap GameJam 2026  /  探索 · 重组 · 回流",new Color(.74f,.7f,.83f));
        BeginScenePresentation presentation = menuObj.AddComponent<BeginScenePresentation>();
        presentation.hero = Art("Drain Hero",menu.transform,new Vector2(.655f,.145f),new Vector2(.93f,.7f),null,Color.white);
        presentation.hero.preserveAspect = true;
        presentation.idleFrames = AssetDatabase.FindAssets("t:Sprite",new[]{"Assets/ArtRes/Animation/PlayerPolished/Idle"})
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).Select(AssetDatabase.LoadAssetAtPath<Sprite>).ToArray();
        presentation.hero.sprite = presentation.idleFrames.FirstOrDefault();
        presentation.bubbles = new RectTransform[7];
        for(int i=0;i<7;i++)
        {
            float x=.64f+i*.047f, y=.61f+Mathf.Sin(i*1.4f)*.1f;
            Image bubble = Art("Lime Bubble " + i,menu.transform,new Vector2(x,y),new Vector2(x+.015f,y+.025f),circle,new Color(.55f,1f,.66f,.28f));
            bubble.preserveAspect = true; presentation.bubbles[i]=bubble.rectTransform;
        }
        BuildSettings(obj.transform);
        EventSystem events = Object.FindFirstObjectByType<EventSystem>();
        if(events==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        Camera camera = Object.FindFirstObjectByType<Camera>(); if(camera!=null) {camera.backgroundColor=new Color(.15f,.1f,.26f);camera.clearFlags=CameraClearFlags.SolidColor;}
        // Keep the main menu scene independent from the deprecated additive dialogue UI.
        EditorUtility.SetDirty(menu); EditorUtility.SetDirty(presentation);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
    }
    private static void BuildSettings(Transform parent)
    {
        GameObject obj=GameplayUIFactory.Rect("Settings",parent,Vector2.zero,Vector2.one).gameObject;
        Art("Shade",obj.transform,Vector2.zero,Vector2.one,null,new Color(.04f,.025f,.09f,.92f),true);
        Art("Panel",obj.transform,new Vector2(.27f,.15f),new Vector2(.73f,.85f),rounded,new Color(.24f,.17f,.36f),true).type=Image.Type.Sliced;
        SettingPanel settings=obj.AddComponent<SettingPanel>();
        Label("Title",obj.transform,.34f,.69f,.66f,.79f,38,"设置",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        Label("Effects",obj.transform,.34f,.56f,.47f,.63f,24,"音效",PlayerUIStyle.Cream);
        Label("Music",obj.transform,.34f,.43f,.47f,.50f,24,"音乐",PlayerUIStyle.Cream);
        settings.sliderGame=Slider("Effects Volume",obj.transform,.47f,.575f,.66f,.615f);
        settings.sliderBk=Slider("Music Volume",obj.transform,.47f,.445f,.66f,.485f);
        Label("Fullscreen",obj.transform,.34f,.32f,.58f,.39f,24,"全屏",PlayerUIStyle.Cream);
        Image toggle=Art("Fullscreen Toggle",obj.transform,new Vector2(.62f,.325f),new Vector2(.665f,.385f),rounded,new Color(.48f,.37f,.67f),true);toggle.type=Image.Type.Sliced;
        settings.togFull=toggle.gameObject.AddComponent<Toggle>();settings.togFull.targetGraphic=toggle;
        settings.togFull.graphic=Art("Check",toggle.transform,new Vector2(.2f,.2f),new Vector2(.8f,.8f),circle,PlayerUIStyle.Mint);
        settings.btnClose=Button("Close Settings",obj.transform,.39f,.21f,.61f,.28f,"返回",PlayerUIStyle.Mint,22);
        obj.SetActive(false);EditorUtility.SetDirty(settings);
    }
    private static Slider Slider(string name,Transform parent,float x0,float y0,float x1,float y1)
    {
        Image back=Art(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),rounded,PlayerUIStyle.Ink,true);back.type=Image.Type.Sliced;
        Slider slider=back.gameObject.AddComponent<Slider>();
        Image fill=Art("Fill",back.transform,Vector2.zero,Vector2.one,rounded,PlayerUIStyle.Mint);fill.type=Image.Type.Sliced;
        Image handle=Art("Handle",back.transform,Vector2.zero,new Vector2(0,1),rounded,PlayerUIStyle.Cream,true);
        handle.rectTransform.sizeDelta=new Vector2(16,0);slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
        slider.minValue=0;slider.maxValue=1;return slider;
    }
    private static Image Art(string name,Transform parent,Vector2 min,Vector2 max,Sprite sprite,Color color,bool raycast=false)
    { Image image=GameplayUIFactory.Image(name,parent,min,max,color,raycast);image.sprite=sprite;return image; }
    private static Text Label(string name,Transform parent,float x0,float y0,float x1,float y1,int size,string value,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        Text text=GameplayUIFactory.Label(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,size,value,alignment);
        text.color=color;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
    }
    private static Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,string value,Color color,int size)
    {
        Image image=Art(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),rounded,color,true);image.type=Image.Type.Sliced;
        Button button=image.gameObject.AddComponent<Button>();ColorBlock c=button.colors;c.normalColor=Color.white;c.selectedColor=Color.white;
        c.highlightedColor=new Color(.85f,1f,.95f);c.pressedColor=new Color(.58f,.8f,.7f);c.disabledColor=new Color(.65f,.62f,.73f,.5f);button.colors=c;
        Label("Label",image.transform,.03f,.03f,.97f,.97f,size,value,color==PlayerUIStyle.Cream||color==PlayerUIStyle.Mint?PlayerUIStyle.Ink:PlayerUIStyle.Cream,TextAnchor.MiddleCenter);return button;
    }
    private static Sprite Import(string path)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer=AssetImporter.GetAtPath(path)as TextureImporter;if(importer==null)return null;
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
        importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
