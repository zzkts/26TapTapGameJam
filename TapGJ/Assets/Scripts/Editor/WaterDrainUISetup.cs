using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Presentation-only migration. World objects, gameplay and save identity stay intact.</summary>
public static class WaterDrainUISetup
{
    private static Font font;
    private static Sprite rounded,circle,portrait;
    [MenuItem("Tools/TapGJ/UI/Align And Polish Current Interface")]
    public static void ApplyInterfacePass()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="BeginScene"&&scene.name!="GameScene")throw new InvalidOperationException("Open a game scene.");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        circle=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Circle.png");
        portrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Portrait.png");
        foreach(var root in scene.GetRootGameObjects())
        foreach(var canvas in root.GetComponentsInChildren<Canvas>(true).Where(c=>c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace))
        {
            foreach(var settings in canvas.GetComponentsInChildren<SettingPanel>(true))BuildSettings(settings);
            foreach(var menu in canvas.GetComponentsInChildren<RunMenuPanel>(true))BuildRunMenu(menu);
            foreach(var hud in canvas.GetComponentsInChildren<PlayerHudPanel>(true))BuildHud(hud);
            foreach(var gene in canvas.GetComponentsInChildren<GeneEditPanel>(true))
            {GeneEditorLayoutPolish.Rebuild(gene);EditorUtility.SetDirty(gene);}
            foreach(var story in canvas.GetComponentsInChildren<StoryDialogPanel>(true))PolishStory(story);
            foreach(var objective in canvas.GetComponentsInChildren<StoryObjectivePanel>(true))PolishObjective(objective);
            foreach(var label in canvas.GetComponentsInChildren<Text>(true))
            {
                label.font=font;label.alignByGeometry=label.alignment==TextAnchor.MiddleCenter&& !label.text.Contains("\n");
                WaterDrainUI.Readable(label,Mathf.Max(12,label.fontSize-2));
                if(scene.name=="BeginScene"&&new[]{"Game Title","Game Subtitle","Menu Eyebrow","Chapter","Control Hint","Jam Credit"}.Contains(label.name))
                {
                    var r=label.rectTransform;r.anchorMin=new Vector2(.13f,r.anchorMin.y);r.offsetMin=r.offsetMax=Vector2.zero;
                    if(label.name=="Game Title"){label.fontSize=64;WaterDrainUI.Readable(label,54);}
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/TapGJ/UI/Refresh Current Scene")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.name!="BeginScene"&&scene.name!="GameScene")throw new InvalidOperationException("Open BeginScene or GameScene.");
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf");
        rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        circle=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Circle.png");
        portrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Portrait.png");
        if(font==null||rounded==null||circle==null)throw new InvalidOperationException("UI assets missing.");
        if(scene.name=="BeginScene")CompleteGameMenuBuilder.Apply();
        foreach(var canvas in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Canvas>(true)).Where(c=>c.isRootCanvas&&c.renderMode!=RenderMode.WorldSpace))
        {
            var scaler=canvas.GetComponent<CanvasScaler>();
            if(scaler!=null){scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;}
            foreach(var settings in canvas.GetComponentsInChildren<SettingPanel>(true))BuildSettings(settings);
            var hud=canvas.GetComponentInChildren<PlayerHudPanel>(true);
            if(hud!=null)BuildHud(hud);
            var menu=canvas.GetComponentInChildren<RunMenuPanel>(true);
            if(menu!=null)BuildRunMenu(menu);
            var gene=canvas.GetComponentInChildren<GeneEditPanel>(true);
            if(gene!=null){GeneEditorLayoutPolish.Rebuild(gene);EditorUtility.SetDirty(gene);}
            var story=canvas.GetComponentInChildren<StoryDialogPanel>(true);
            if(story!=null)PolishStory(story);
            var objective=canvas.GetComponentInChildren<StoryObjectivePanel>(true);
            if(objective!=null)PolishObjective(objective);
            foreach(var text in canvas.GetComponentsInChildren<Text>(true))
            {text.font=font;WaterDrainUI.Readable(text,Mathf.Max(11,text.fontSize-3));}
        }
        foreach(var sign in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WorldSign>(true)))
        {
            var text=sign.GetComponentInChildren<Text>(true);if(text==null)continue;
            text.text=text.text.Split('·')[0].Trim();text.font=font;text.fontSize=18;text.color=PlayerUIStyle.Cream;
            var rect=(RectTransform)sign.transform;rect.sizeDelta=new Vector2(190,52);
            var back=sign.GetComponent<Image>();if(back==null)back=sign.gameObject.AddComponent<Image>();
            back.sprite=rounded;back.type=Image.Type.Sliced;back.color=new Color(.13f,.105f,.22f,.86f);back.raycastTarget=false;
            Place(text.rectTransform,.05f,.08f,.95f,.92f);WaterDrainUI.Readable(text,16);
            EditorUtility.SetDirty(sign);EditorUtility.SetDirty(text);EditorUtility.SetDirty(back);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }
    private static void Clear(Transform root)
    {for(int i=root.childCount-1;i>=0;i--)Object.DestroyImmediate(root.GetChild(i).gameObject);}
    private static RectTransform R(Transform root,string name,float x0,float y0,float x1,float y1)
    {return GameplayUIFactory.Rect(name,root,new Vector2(x0,y0),new Vector2(x1,y1));}
    private static Image Box(Transform root,string name,float x0,float y0,float x1,float y1,Color color,bool raycast=false)
    {var image=GameplayUIFactory.Image(name,root,new Vector2(x0,y0),new Vector2(x1,y1),color,raycast);image.sprite=rounded;image.type=Image.Type.Sliced;return image;}
    private static Image ImageAt(Transform root,string name,float x,float y,float w,float h,Color color,Sprite sprite=null)
    {var image=GameplayUIFactory.Image(name,root,Vector2.zero,Vector2.zero,color);var rect=image.rectTransform;rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);image.sprite=sprite;image.type=sprite==rounded?Image.Type.Sliced:Image.Type.Simple;return image;}
    private static Text Label(Transform root,string name,float x0,float y0,float x1,float y1,int size,string value,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {var text=GameplayUIFactory.Label(name,root,new Vector2(x0,y0),new Vector2(x1,y1),font,size,value,alignment);text.color=color;WaterDrainUI.Readable(text,Mathf.Max(12,size-2));return text;}
    private static Text TextAt(Transform root,string name,float x,float y,float w,float h,int size,string value,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
    {var text=Label(root,name,0,0,0,0,size,value,color,alignment);var rect=text.rectTransform;rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);return text;}
    private static RectTransform Card(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
    {var rect=WaterDrainUI.Dock(name,parent,anchor,position,size);var border=rect.gameObject.AddComponent<Image>();border.sprite=rounded;border.type=Image.Type.Sliced;border.color=WaterDrainUI.Border;border.raycastTarget=false;var well=Box(rect,"Inner Surface",0,0,1,1,WaterDrainUI.Surface);well.rectTransform.offsetMin=new Vector2(2,2);well.rectTransform.offsetMax=new Vector2(-2,-2);return rect;}
    private static Button Button(Transform root,string name,float x0,float y0,float x1,float y1,string value,Color color,int size=22)
    {var image=Box(root,name,x0,y0,x1,y1,color,true);var button=image.gameObject.AddComponent<Button>();Label(image.transform,"Label",.035f,.08f,.965f,.92f,size,value,color==PlayerUIStyle.Mint||color==PlayerUIStyle.Cream?PlayerUIStyle.Ink:PlayerUIStyle.Cream,TextAnchor.MiddleCenter);var colors=button.colors;colors.normalColor=colors.selectedColor=Color.white;colors.highlightedColor=new Color(.88f,1,.96f);colors.pressedColor=new Color(.62f,.85f,.77f);colors.disabledColor=new Color(.6f,.58f,.68f,.55f);colors.fadeDuration=.12f;button.colors=colors;return button;}
    private static void Modal(Transform root,string name)
    {
        Box(root,"Shade",0,0,1,1,new Color(.035f,.025f,.075f,.87f),true).sprite=null;
        var border=Box(root,name,.285f,.09f,.715f,.91f,WaterDrainUI.Border,true);
        var inner=Box(border.transform,"Inner Surface",0,0,1,1,WaterDrainUI.Surface,true);inner.rectTransform.offsetMin=new Vector2(2,2);inner.rectTransform.offsetMax=new Vector2(-2,-2);
        Label(root,"Brand",.32f,.837f,.68f,.88f,16,WaterDrainUI.Signature,WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        Box(root,"Accent",.47f,.82f,.53f,.824f,PlayerUIStyle.Mint);
    }
    private static RectTransform FixedModal(Transform root,string name)
    {
        Box(root,"Shade",0,0,1,1,new Color(.035f,.025f,.075f,.87f),true).sprite=null;
        var frame=Card(root,name,new Vector2(.5f,.5f),Vector2.zero,new Vector2(620,680));
        TextAt(frame,"Brand",54,628,512,24,15,WaterDrainUI.Signature,WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        ImageAt(frame,"Accent",278,610,64,3,PlayerUIStyle.Mint,rounded);
        return frame;
    }
    private static Button ButtonAt(Transform root,string name,float y,string value,Color color,int size=21)
    {
        var button=Button(root,name,0,0,0,0,value,color,size);var rect=button.GetComponent<RectTransform>();
        rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(54,y);rect.sizeDelta=new Vector2(512,52);return button;
    }
    private static void BuildSettings(SettingPanel panel)
    {
        Clear(panel.transform);var root=FixedModal(panel.transform,"Settings Frame");
        TextAt(root,"Title",54,550,512,48,34,"设置",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        TextAt(root,"Subtitle",54,520,512,24,15,"声音与显示",WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        TextAt(root,"Effects",54,448,360,30,21,"音效音量",PlayerUIStyle.Cream);
        panel.effectsValue=TextAt(root,"Effects Value",446,448,120,30,19,"100%",PlayerUIStyle.Mint,TextAnchor.MiddleRight);
        panel.sliderGame=VolumeSlider(root,"Effects Volume",403);
        TextAt(root,"Music",54,344,360,30,21,"音乐音量",PlayerUIStyle.Cream);
        panel.musicValue=TextAt(root,"Music Value",446,344,120,30,19,"100%",PlayerUIStyle.Mint,TextAnchor.MiddleRight);
        panel.sliderBk=VolumeSlider(root,"Music Volume",299);
        ImageAt(root,"Display Divider",54,276,512,1,WaterDrainUI.Border);
        TextAt(root,"Fullscreen",54,220,300,30,21,"全屏显示",PlayerUIStyle.Cream);
        TextAt(root,"Display Hint",54,197,360,23,13,"切换全屏或窗口显示",WaterDrainUI.Muted);
        panel.fullscreenValue=TextAt(root,"Fullscreen Value",434,220,54,30,15,"关闭",WaterDrainUI.Muted,TextAnchor.MiddleRight);
        var toggle=ImageAt(root,"Fullscreen Toggle",504,220,62,32,WaterDrainUI.Border,rounded);toggle.raycastTarget=true;
        panel.togFull=toggle.gameObject.AddComponent<Toggle>();panel.togFull.targetGraphic=toggle;panel.fullscreenTrack=toggle;
        var thumb=ImageAt(toggle.transform,"Thumb",0,0,24,24,PlayerUIStyle.Cream,circle);
        thumb.rectTransform.pivot=new Vector2(.5f,.5f);thumb.rectTransform.anchoredPosition=new Vector2(16,16);panel.fullscreenThumb=thumb;
        panel.togFull.transition=Selectable.Transition.None;
        panel.btnCredits=ButtonAt(root,"Music Credits",146,"音乐署名",PlayerUIStyle.Violet,16);
        panel.btnCredits.GetComponent<RectTransform>().sizeDelta=new Vector2(512,38);
        panel.btnClose=ButtonAt(root,"Close Settings",78,"保存并返回",PlayerUIStyle.Mint);
        TextAt(root,"Settings Hint",54,28,512,27,13,"Esc 返回 · 设置在返回时保存",WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        panel.gameObject.SetActive(false);EditorUtility.SetDirty(panel);
    }
    private static Slider VolumeSlider(Transform root,string name,float y)
    {
        var holder=R(root,name,0,0,0,0);holder.pivot=Vector2.zero;holder.anchoredPosition=new Vector2(54,y);holder.sizeDelta=new Vector2(512,44);
        var hit=holder.gameObject.AddComponent<Image>();hit.color=Color.clear;
        var slider=holder.gameObject.AddComponent<Slider>();Box(holder,"Track",0,.363636f,1,.636364f,WaterDrainUI.Border);
        // Track and fill share the exact same rectangle; no normalized side inset.
        var fillArea=R(holder,"Fill Area",0,.363636f,1,.636364f);
        var fill=Box(fillArea,"Fill",0,0,1,1,PlayerUIStyle.Mint);slider.fillRect=fill.rectTransform;
        var handleArea=R(holder,"Handle Area",0,.5f,1,.5f);handleArea.offsetMin=new Vector2(12,0);handleArea.offsetMax=new Vector2(-12,0);
        var handle=ImageAt(handleArea,"Handle",0,0,24,24,PlayerUIStyle.Cream,circle);handle.raycastTarget=true;
        handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
        slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.minValue=0;slider.maxValue=1;
        var colors=slider.colors;colors.normalColor=colors.selectedColor=Color.white;colors.highlightedColor=new Color(.8f,1,.94f);colors.pressedColor=PlayerUIStyle.Mint;slider.colors=colors;return slider;
    }
    private static void BuildRunMenu(RunMenuPanel menu)
    {
        var alive=menu.alivePortrait;var dead=menu.deadPortrait;Clear(menu.transform);var root=FixedModal(menu.transform,"Pause Frame");menu.cardLayout=true;
        menu.portrait=ImageAt(root,"Menu Portrait",254,498,112,108,Color.white,alive!=null?alive:portrait);menu.portrait.preserveAspect=true;
        menu.title=TextAt(root,"Title",54,444,512,48,34,"已暂停",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        menu.hint=TextAt(root,"Pause Hint",54,392,512,42,14,"Esc 继续 · 已记录的检查点会保留",WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        menu.resumeButton=ButtonAt(root,"Resume",324,"继续探索",PlayerUIStyle.Mint);
        menu.retryButton=ButtonAt(root,"Restart",256,"从检查点重试",PlayerUIStyle.Cream);
        menu.settingsButton=ButtonAt(root,"Settings",188,"设置",PlayerUIStyle.Violet);
        menu.menuButton=ButtonAt(root,"Main Menu",120,"返回主菜单",PlayerUIStyle.Violet);
        TextAt(root,"Footer",54,48,512,28,13,"随时暂停，重新组合你的下一次尝试。",WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        menu.alivePortrait=alive;menu.deadPortrait=dead;menu.gameObject.SetActive(false);EditorUtility.SetDirty(menu);
    }
    private static void BuildHud(PlayerHudPanel hud)
    {
        Clear(hud.transform);var root=R(hud.transform,"Player Visuals",0,0,1,1);
        hud.visualGroup=root.gameObject.AddComponent<CanvasGroup>();hud.visualGroup.blocksRaycasts=false;hud.visualGroup.interactable=false;
        var card=Card(root,"Vitality",new Vector2(0,1),new Vector2(24,-24),new Vector2(402,120));hud.vitality=card;
        ImageAt(card,"Portrait Halo",14,13,94,94,PlayerUIStyle.Violet,circle);
        hud.portrait=ImageAt(card,"Drain Portrait",10,12,104,104,Color.white,portrait);hud.portrait.preserveAspect=true;
        TextAt(card,"Identity",126,84,174,24,18,"生命",PlayerUIStyle.Cream);
        hud.healthText=TextAt(card,"Health",296,84,84,24,18,"10 / 10",PlayerUIStyle.Cream,TextAnchor.MiddleRight);
        ImageAt(card,"Health Well",126,57,254,18,PlayerUIStyle.Violet,rounded);
        hud.healthTrail=ImageAt(card,"Health Loss",129,60,248,12,new Color(1,.48f,.62f),rounded);hud.healthTrail.type=Image.Type.Filled;hud.healthTrail.fillMethod=Image.FillMethod.Horizontal;
        hud.healthFill=ImageAt(card,"Health Liquid",129,60,248,12,PlayerUIStyle.Mint,rounded);hud.healthFill.type=Image.Type.Filled;hud.healthFill.fillMethod=Image.FillMethod.Horizontal;
        ImageAt(card,"Dash Disc",126,19,24,24,PlayerUIStyle.Violet,circle);
        hud.dashFill=ImageAt(card,"Dash Ring",126,19,24,24,PlayerUIStyle.Mint,circle);hud.dashFill.type=Image.Type.Filled;hud.dashFill.fillMethod=Image.FillMethod.Radial360;
        ImageAt(card,"Dash Center",132,25,12,12,WaterDrainUI.Surface,circle);
        TextAt(card,"Shift Key",160,19,53,24,13,"SHIFT",PlayerUIStyle.Cream,TextAnchor.MiddleCenter);
        hud.dashText=TextAt(card,"Dash",220,19,160,24,15,"寻找冲刺模块",WaterDrainUI.Muted);
        card=Card(root,"Gene Chain",Vector2.zero,new Vector2(24,24),new Vector2(526,174));
        hud.attributeText=TextAt(card,"Attribute",18,137,266,25,19,"基础水弹",PlayerUIStyle.Cream);
        hud.phaseText=TextAt(card,"Phase",300,137,208,25,16,"左键射击",PlayerUIStyle.Mint,TextAnchor.MiddleRight);
        ImageAt(card,"Phase Track",18,124,490,5,PlayerUIStyle.Violet,rounded);
        hud.stageFill=ImageAt(card,"Stage Fill",18,124,490,5,PlayerUIStyle.Mint,rounded);hud.stageFill.type=Image.Type.Filled;hud.stageFill.fillMethod=Image.FillMethod.Horizontal;
        hud.slotTexts=new Text[6];hud.slotLevels=new Text[6];hud.slotIcons=new Image[6];hud.slotFrames=new Image[6];hud.slotHighlights=new Image[6];hud.slotWells=new Image[6];
        for(int i=0;i<6;i++)
        {
            float x=18+i*82;
            hud.slotFrames[i]=ImageAt(card,"Slot "+i,x,49,74,64,Color.white,rounded);
            hud.slotHighlights[i]=ImageAt(card,"Current Gene "+i,x,49,74,64,PlayerUIStyle.Mint,rounded);hud.slotHighlights[i].enabled=false;
            ImageAt(card,"Slot Surface "+i,x+2,51,70,60,PlayerUIStyle.Ink,rounded);
            hud.slotWells[i]=ImageAt(card,"Gene Well "+i,x+21,65,32,32,PlayerUIStyle.Violet,circle);
            hud.slotIcons[i]=ImageAt(card,"Gene Icon "+i,x+23,67,28,28,PlayerUIStyle.Mint);hud.slotIcons[i].enabled=false;
            hud.slotLevels[i]=TextAt(card,"Gene Level "+i,x+3,92,18,18,12,(i+1).ToString(),WaterDrainUI.Muted,TextAnchor.MiddleCenter);
            hud.slotTexts[i]=TextAt(card,"Gene "+i,x-2,28,78,21,14,"空槽",WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        }
        hud.nextText=TextAt(card,"Next Stage",18,5,490,22,14,"在终端装配基因",WaterDrainUI.Muted);
        hud.interactionCard=Card(root,"Interaction Card",new Vector2(.5f,.5f),new Vector2(0,-100),new Vector2(380,94));hud.interactionCard.pivot=new Vector2(.5f,0);
        ImageAt(hud.interactionCard,"E Key",22,31,36,32,PlayerUIStyle.Mint,rounded);
        TextAt(hud.interactionCard,"E",22,31,36,32,19,"E",PlayerUIStyle.Ink,TextAnchor.MiddleCenter);
        hud.interactionText=TextAt(hud.interactionCard,"Interaction",76,18,276,58,18,"",PlayerUIStyle.Cream);hud.interactionCard.gameObject.SetActive(false);
        hud.toastCard=Card(root,"Pickup Toast",new Vector2(.5f,1),new Vector2(0,-24),new Vector2(440,70));
        hud.toastIcon=ImageAt(hud.toastCard,"Icon",20,17,36,36,PlayerUIStyle.Mint);
        hud.toastText=TextAt(hud.toastCard,"Toast",72,14,348,42,19,"",PlayerUIStyle.Cream);hud.toastCard.gameObject.SetActive(false);
        var controls=Card(root,"Control Hints",new Vector2(1,0),new Vector2(-24,24),new Vector2(316,74));
        TextAt(controls,"Controls",16,10,284,54,15,"A / D 移动 · 空格跳跃 · Shift 冲刺\n左键射击 · 右键附魔 · Esc 暂停",WaterDrainUI.Muted);
        EditorUtility.SetDirty(hud);
    }
    private static void Place(RectTransform rect,float x0,float y0,float x1,float y1)
    {rect.anchorMin=new Vector2(x0,y0);rect.anchorMax=new Vector2(x1,y1);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    public static void PolishObjective(StoryObjectivePanel panel)
    {
        var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,1);rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-24,-24);rect.sizeDelta=new Vector2(402,146);
        var back=rect.GetComponent<Image>();back.sprite=rounded;back.type=Image.Type.Sliced;back.color=WaterDrainUI.Surface;
        Place(panel.chapter.rectTransform,.055f,.69f,.945f,.93f);panel.chapter.fontSize=18;panel.chapter.color=PlayerUIStyle.Mint;
        Place(panel.target.rectTransform,.055f,.28f,.945f,.69f);panel.target.fontSize=21;
        Place(panel.progress.rectTransform,.055f,.055f,.945f,.26f);panel.progress.fontSize=13;panel.progress.color=WaterDrainUI.Muted;
        EditorUtility.SetDirty(panel);
    }
    private static void PolishStory(StoryDialogPanel panel)
    {
        Place((RectTransform)panel.dialogueBody.transform,.055f,.075f,.945f,.43f);
        panel.skip.transform.SetParent(panel.dialogueBody.transform,false);
        Place(panel.skip.GetComponent<RectTransform>(),.79f,.78f,.965f,.94f);
        panel.skip.GetComponentInChildren<Text>().fontSize=17;
        Place(panel.speaker.rectTransform,.225f,.77f,.74f,.94f);
        Place(panel.hint.rectTransform,.225f,.055f,.69f,.20f);
        Place(panel.body.rectTransform,.225f,.27f,.955f,.72f);panel.body.fontSize=26;panel.body.lineSpacing=1.15f;
        WaterDrainUI.Readable(panel.body,22);
        foreach(var text in panel.GetComponentsInChildren<Text>(true))WaterDrainUI.Readable(text,Mathf.Max(14,text.fontSize-3));
        var brand=panel.resultBody.transform.Find("Result Brand");
        if(brand==null)Label(panel.resultBody.transform,"Result Brand",.08f,.88f,.92f,.96f,15,WaterDrainUI.Signature,WaterDrainUI.Muted,TextAnchor.MiddleCenter);
        Place(panel.endingTitle.rectTransform,.07f,.70f,.93f,.87f);
        Place(panel.endingDescription.rectTransform,.09f,.40f,.91f,.69f);
        Place(panel.endingStats.rectTransform,.09f,.26f,.91f,.40f);
        EditorUtility.SetDirty(panel);
    }
}
