using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StoryUIFactory
{
    /// <summary>World signs and enemy health bars must never host fullscreen story UI.</summary>
    public static Canvas FindGameplayCanvas(Canvas preferred = null)
    {
        if(preferred!=null&&preferred.rootCanvas.renderMode!=RenderMode.WorldSpace)return preferred.rootCanvas;
        Scene scene=preferred!=null?preferred.gameObject.scene:SceneManager.GetActiveScene();
        if(!scene.IsValid()||!scene.isLoaded)return null;
        foreach(GameObject root in scene.GetRootGameObjects())
        foreach(PlayerHudPanel hud in root.GetComponentsInChildren<PlayerHudPanel>(true))
        {
            Canvas canvas=hud.GetComponentInParent<Canvas>();
            if(canvas!=null&&canvas.rootCanvas.renderMode!=RenderMode.WorldSpace)return canvas.rootCanvas;
        }
        Canvas cameraCanvas=null;
        foreach(GameObject root in scene.GetRootGameObjects())
        foreach(Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            if(canvas.rootCanvas.renderMode==RenderMode.ScreenSpaceOverlay)return canvas.rootCanvas;
            if(canvas.rootCanvas.renderMode==RenderMode.ScreenSpaceCamera)cameraCanvas=canvas.rootCanvas;
        }
        return cameraCanvas;
    }

    public static StoryDialogPanel Ensure(Canvas canvas, Font font = null)
    {
        Scene requestedScene=canvas!=null?canvas.gameObject.scene:SceneManager.GetActiveScene();
        canvas=FindGameplayCanvas(canvas);
        if(canvas==null)
        {
            GameObject canvasRoot=new GameObject("Story Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            if(requestedScene.IsValid()&&requestedScene.isLoaded)SceneManager.MoveGameObjectToScene(canvasRoot,requestedScene);
            canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            CanvasScaler scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600f,900f);scaler.matchWidthOrHeight=.5f;
        }
        StoryDialogPanel existing = canvas.GetComponentInChildren<StoryDialogPanel>(true);
        if (existing != null) return existing;
#if UNITY_EDITOR
        if (font == null) font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
#endif
        if (font == null)
        {
            Text any = canvas.GetComponentInChildren<Text>(true);
            if (any != null) font = any.font;
        }
        Sprite rounded = null, portrait = null;
#if UNITY_EDITOR
        rounded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        portrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Portrait.png");
#endif
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (rounded == null) rounded = PlayerUIStyle.Rounded;
        if (portrait == null)
        {
            PlayerHudPanel hud = Object.FindFirstObjectByType<PlayerHudPanel>(FindObjectsInactive.Include);
            if (hud != null && hud.portrait != null) portrait = hud.portrait.sprite;
        }
        RectTransform objective = WaterDrainUI.Dock("Story Objective",canvas.transform,new Vector2(1,1),new Vector2(-24,-24),new Vector2(402,146));
        Image objectiveBack = objective.gameObject.AddComponent<Image>(); objectiveBack.sprite = rounded;
        objectiveBack.type = Image.Type.Sliced; objectiveBack.color = new Color(.16f,.13f,.27f,.94f); objectiveBack.raycastTarget = false;
        StoryObjectivePanel objectivePanel = objective.gameObject.AddComponent<StoryObjectivePanel>();
        objectivePanel.chapter = Label("Chapter", objective, .055f,.69f,.945f,.93f,font,18,"第一章 · 异常回流",PlayerUIStyle.Mint);
        objectivePanel.target = Label("Objective", objective, .055f,.28f,.945f,.69f,font,21,"寻找维修日志",PlayerUIStyle.Cream);
        objectivePanel.progress = Label("Progress", objective, .055f,.055f,.945f,.26f,font,13,"安全区会自动记录检查点",WaterDrainUI.Muted);

        GameObject root = GameplayUIFactory.Rect("Story Dialogue", canvas.transform, Vector2.zero, Vector2.one).gameObject;
        StoryDialogPanel panel = root.AddComponent<StoryDialogPanel>();
        Image shade = GameplayUIFactory.Image("Story Shade", root.transform, Vector2.zero, Vector2.one, new Color(.055f,.035f,.095f,.66f), true);
        Button advanceClick = shade.gameObject.AddComponent<Button>(); advanceClick.transition = Selectable.Transition.None;
        panel.advanceClick = advanceClick;
        GameObject body = GameplayUIFactory.Rect("Dialogue Body", root.transform, new Vector2(.055f,.075f), new Vector2(.945f,.43f)).gameObject;
        Back("Dialogue Frame", body.transform, 0,0,1,1,rounded,PlayerUIStyle.Violet);
        Back("Dialogue Paper", body.transform, .005f,.014f,.995f,.986f,rounded,PlayerUIStyle.Ink);
        Back("Speaker Tab", body.transform, .215f,.76f,.74f,.96f,rounded,new Color(.23f,.18f,.35f));
        panel.speaker = Label("Speaker",body.transform,.235f,.77f,.74f,.94f,font,24,"",PlayerUIStyle.Mint);
        Image face = GameplayUIFactory.Image("Character Portrait",body.transform,new Vector2(.025f,.13f),new Vector2(.195f,.86f),Color.white);
        face.sprite=portrait;face.preserveAspect=true;panel.portrait=face;
        Image record = GameplayUIFactory.Image("Record Emblem",body.transform,new Vector2(.060f,.29f),new Vector2(.155f,.71f),PlayerUIStyle.Mint);
        // Use the saved frame asset here. Procedurally generated HideAndDontSave sprites
        // disappear when this authored UI is saved to GameScene or included in a build.
        record.sprite=rounded;record.type=Image.Type.Sliced;panel.recordEmblem=record;
        Label("Record Mark",record.transform,.06f,.06f,.94f,.94f,font,18,"档案\nNODE-03",PlayerUIStyle.Ink,TextAnchor.MiddleCenter);
        panel.body=Label("Dialogue Text",body.transform,.225f,.27f,.955f,.72f,font,26,"",PlayerUIStyle.Cream);
        panel.body.lineSpacing=1.15f;
        panel.body.alignment=TextAnchor.UpperLeft;
        panel.hint=Label("Advance Hint",body.transform,.235f,.055f,.69f,.20f,font,16,"空格 / 点击继续",new Color(.69f,.65f,.78f));
        panel.next=Button("Continue",body.transform,.79f,.055f,.965f,.225f,font,"继续  ›",rounded,PlayerUIStyle.Mint);
        panel.skip=Button("Skip",root.transform,.845f,.90f,.945f,.953f,font,"跳过本段",rounded,new Color(.40f,.33f,.52f));
        panel.skip.GetComponentInChildren<Text>().color=PlayerUIStyle.Cream;
        panel.dialogueBody=body;

        GameObject choices=GameplayUIFactory.Rect("Ending Choice",root.transform,new Vector2(.19f,.28f),new Vector2(.81f,.76f)).gameObject;
        Back("Choice Frame",choices.transform,0,0,1,1,rounded,PlayerUIStyle.Violet);
        Back("Choice Surface",choices.transform,.006f,.015f,.994f,.985f,rounded,PlayerUIStyle.Ink);
        Label("Choice Title",choices.transform,.07f,.80f,.93f,.94f,font,33,"让水去往何处？",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        Label("Choice Description",choices.transform,.08f,.62f,.92f,.80f,font,21,"这里没有被写好的答案。\n你可以替自己，也替地下的声音作出选择。",new Color(.78f,.74f,.87f),TextAnchor.MiddleCenter);
        panel.coexist=Button("Open Return Valve",choices.transform,.075f,.33f,.925f,.55f,font,"打开回流阀 · 让生命继续相遇",rounded,PlayerUIStyle.Mint);
        panel.seal=Button("Start Purification",choices.transform,.075f,.075f,.925f,.295f,font,"启动净化装置 · 暂时封存地下",rounded,PlayerUIStyle.Cream);
        panel.choiceBody=choices;choices.SetActive(false);

        GameObject result=GameplayUIFactory.Rect("Ending Result",root.transform,new Vector2(.20f,.23f),new Vector2(.80f,.79f)).gameObject;
        Back("Result Frame",result.transform,0,0,1,1,rounded,PlayerUIStyle.Violet);
        Back("Result Surface",result.transform,.006f,.014f,.994f,.986f,rounded,PlayerUIStyle.Ink);
        Label("Result Brand",result.transform,.08f,.88f,.92f,.96f,font,15,WaterDrainUI.Signature,WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        panel.endingTitle=Label("Ending Title",result.transform,.07f,.70f,.93f,.87f,font,38,"",PlayerUIStyle.Mint,TextAnchor.MiddleCenter);
        panel.endingDescription=Label("Ending Description",result.transform,.09f,.39f,.91f,.71f,font,23,"",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        panel.endingStats=Label("Ending Statistics",result.transform,.09f,.26f,.91f,.40f,font,18,"",new Color(.77f,.73f,.85f),TextAnchor.MiddleCenter);
        panel.newRun=Button("New Exploration",result.transform,.09f,.08f,.48f,.24f,font,"再次探索",rounded,PlayerUIStyle.Mint);
        panel.mainMenu=Button("Return Main Menu",result.transform,.52f,.08f,.91f,.24f,font,"返回主界面",rounded,PlayerUIStyle.Cream);
        panel.resultBody=result;result.SetActive(false);root.SetActive(false);
        return panel;
    }

    private static Image Back(string name,Transform parent,float x0,float y0,float x1,float y1,Sprite sprite,Color color)
    {
        Image image=GameplayUIFactory.Image(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),color);
        image.sprite=sprite;image.type=Image.Type.Sliced;return image;
    }
    private static Text Label(string name,Transform parent,float x0,float y0,float x1,float y1,Font font,int size,string value,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {
        Text label=GameplayUIFactory.Label(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,size,value,alignment);
        label.color=color;WaterDrainUI.Readable(label,Mathf.Max(12,size-3));return label;
    }
    private static Button Button(string name,Transform parent,float x0,float y0,float x1,float y1,Font font,string text,Sprite sprite,Color color)
    {
        Button button=GameplayUIFactory.Button(name,parent,new Vector2(x0,y0),new Vector2(x1,y1),font,text);
        button.GetComponent<Image>().sprite=sprite;button.GetComponent<Image>().type=Image.Type.Sliced;button.GetComponent<Image>().color=color;
        Text label=button.GetComponentInChildren<Text>();label.fontSize=22;label.color=PlayerUIStyle.Ink;
        ColorBlock colors=button.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;
        colors.highlightedColor=new Color(.88f,1f,.94f);colors.pressedColor=new Color(.63f,.82f,.75f);button.colors=colors;return button;
    }
}
