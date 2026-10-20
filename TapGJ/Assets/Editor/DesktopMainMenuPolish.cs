using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Authors the desktop composition without rebuilding settings or the world.</summary>
public static class DesktopMainMenuPolish
{
    [MenuItem("Tools/TapGJ/UI/Polish PC Main Menu")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scene/BeginScene.unity")throw new InvalidOperationException("Open BeginScene first.");
        var menu=UnityEngine.Object.FindFirstObjectByType<BeginPanel>();
        if(menu==null)throw new InvalidOperationException("Main menu not found.");
        Undo.RegisterFullObjectHierarchyUndo(menu.gameObject,"Polish PC main menu");
        ApplyLayout(menu);
        EditorUtility.SetDirty(menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    public static void ApplyLayout(BeginPanel menu)
    {
        var rounded=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ArtRes/UI/Player/Rounded.png");
        var frame=menu.transform.Find("Menu Surface") as RectTransform;
        if(frame==null||rounded==null)throw new InvalidOperationException("Menu frame or rounded sprite missing.");
        frame.anchorMin=frame.anchorMax=new Vector2(.08f,.5f);frame.pivot=new Vector2(0,.5f);
        frame.anchoredPosition=Vector2.zero;frame.sizeDelta=new Vector2(640,740);
        var surface=frame.GetComponent<Image>();surface.sprite=rounded;surface.type=Image.Type.Sliced;surface.color=new Color(.14f,.10f,.24f,.94f);
        var border=frame.GetComponent<Outline>();if(border==null)border=frame.gameObject.AddComponent<Outline>();
        border.effectColor=WaterDrainUI.Border;border.effectDistance=new Vector2(1,-1);
        var font=menu.btnBegin.GetComponentInChildren<Text>().font;
        var accent=menu.transform.Find("Menu Accent") as RectTransform;
        if(accent==null)accent=GameplayUIFactory.Rect("Menu Accent",menu.transform,Vector2.zero,Vector2.zero);
        Place(accent,frame,new Vector2(40,-18),new Vector2(96,3));
        var line=accent.GetComponent<Image>();if(line==null)line=accent.gameObject.AddComponent<Image>();line.color=PlayerUIStyle.Mint;line.raycastTarget=false;
        Label(menu,frame,"Menu Eyebrow",new Vector2(40,-34),new Vector2(560,30),18,WaterDrainUI.Muted);
        Label(menu,frame,"Game Title",new Vector2(36,-72),new Vector2(568,112),72,PlayerUIStyle.Cream);
        Label(menu,frame,"Game Subtitle",new Vector2(40,-190),new Vector2(560,50),32,PlayerUIStyle.Mint);
        Label(menu,frame,"Chapter",new Vector2(40,-256),new Vector2(560,50),23,PlayerUIStyle.Cream);
        StyleButton(menu.btnBegin,frame,new Vector2(40,-326),new Vector2(560,76),29,PlayerUIStyle.Mint,PlayerUIStyle.Ink,rounded);
        StyleButton(menu.btnContinue,frame,new Vector2(40,-418),new Vector2(560,68),25,new Color(.39f,.31f,.55f),PlayerUIStyle.Cream,rounded);
        var divider=menu.transform.Find("Menu Divider") as RectTransform;if(divider!=null)Place(divider,frame,new Vector2(40,-508),new Vector2(560,1));
        StyleButton(menu.btnSetting,frame,new Vector2(40,-530),new Vector2(176,58),21,PlayerUIStyle.Cream,PlayerUIStyle.Ink,rounded);
        if(menu.btnGuide==null)menu.btnGuide=GameplayUIFactory.Button("Desktop Guide Button",menu.transform,Vector2.zero,Vector2.zero,font,"操作指南");
        StyleButton(menu.btnGuide,frame,new Vector2(232,-530),new Vector2(176,58),21,PlayerUIStyle.Violet,PlayerUIStyle.Cream,rounded);
        StyleButton(menu.btnQuit,frame,new Vector2(424,-530),new Vector2(176,58),21,PlayerUIStyle.Violet,PlayerUIStyle.Cream,rounded);
        var hint=Label(menu,frame,"Control Hint",new Vector2(40,-650),new Vector2(560,34),16,WaterDrainUI.Muted);
        if(hint!=null)hint.text=ControlsGuidePanel.MenuHint;
        var credit=menu.transform.Find("Jam Credit") as RectTransform;
        if(credit!=null)
        {
            credit.anchorMin=credit.anchorMax=new Vector2(.79f,0);credit.pivot=new Vector2(.5f,0);
            credit.anchoredPosition=new Vector2(0,32);credit.sizeDelta=new Vector2(500,30);
            var text=credit.GetComponent<Text>();text.fontSize=14;text.alignment=TextAnchor.MiddleCenter;WaterDrainUI.Readable(text,13);
        }
        var hero=menu.transform.Find("Drain Hero") as RectTransform;
        if(hero!=null){hero.anchorMin=new Vector2(.62f,.14f);hero.anchorMax=new Vector2(.96f,.81f);hero.offsetMin=hero.offsetMax=Vector2.zero;}
    }
    private static void Place(RectTransform rect,RectTransform frame,Vector2 at,Vector2 size)
    {
        // Keep direct children so mobile can independently rearrange the same scene.
        rect.anchorMin=rect.anchorMax=frame.anchorMin;rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=frame.anchoredPosition+new Vector2(at.x,frame.sizeDelta.y*.5f+at.y);rect.sizeDelta=size;
    }
    private static Text Label(BeginPanel menu,RectTransform frame,string name,Vector2 at,Vector2 size,int fontSize,Color color)
    {
        var child=menu.transform.Find(name);if(child==null)return null;
        var text=child.GetComponent<Text>();Place(text.rectTransform,frame,at,size);text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleLeft;WaterDrainUI.Readable(text,fontSize-2);return text;
    }
    private static void StyleButton(Button button,RectTransform frame,Vector2 at,Vector2 size,int fontSize,Color face,Color text,Sprite rounded)
    {
        Place((RectTransform)button.transform,frame,at,size);button.targetGraphic=button.GetComponent<Image>();button.image.sprite=rounded;button.image.type=Image.Type.Sliced;button.image.color=face;
        var colors=button.colors;colors.normalColor=colors.selectedColor=colors.highlightedColor=Color.white;colors.pressedColor=new Color(.8f,.9f,.88f);colors.disabledColor=new Color(.8f,.8f,.86f,.85f);button.colors=colors;
        var label=button.GetComponentInChildren<Text>();label.fontSize=fontSize;label.color=text;WaterDrainUI.Readable(label,fontSize-2);
    }
}
