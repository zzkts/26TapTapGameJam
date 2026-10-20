using UnityEngine;
using UnityEngine.UI;

/// <summary>Phone-only composition; keeps the scene's existing buttons and their listeners.</summary>
public static class MobileBeginMenuLayout
{
    public static void Apply(BeginPanel menu,Transform safeRoot)
    {
        menu.btnQuit.gameObject.SetActive(false);
        if(menu.btnGuide!=null)menu.btnGuide.gameObject.SetActive(false);
        var font=menu.btnBegin.GetComponentInChildren<Text>().font;
        var guide=ControlsGuidePanel.Create(safeRoot,font);
        var frame=menu.transform.Find("Menu Surface") as RectTransform;
        if(frame==null)return;
        frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(0,.5f);
        frame.anchoredPosition=new Vector2(64,0);frame.sizeDelta=new Vector2(520,600);
        var image=frame.GetComponent<Image>();image.color=new Color(.14f,.10f,.24f,.94f);
        var outline=frame.GetComponent<Outline>();if(outline==null)outline=frame.gameObject.AddComponent<Outline>();outline.effectColor=WaterDrainUI.Border;outline.effectDistance=new Vector2(1,-1);
        var oldAccent=menu.transform.Find("Menu Accent");if(oldAccent!=null)oldAccent.gameObject.SetActive(false);
        var accent=WaterDrainUI.Dock("Mobile Menu Accent",frame,new Vector2(0,1),new Vector2(36,-18),new Vector2(76,3));
        var line=accent.gameObject.AddComponent<Image>();line.color=PlayerUIStyle.Mint;line.raycastTarget=false;
        MoveLabel(menu,frame,"Menu Eyebrow",new Vector2(36,-34),new Vector2(448,26),15,WaterDrainUI.Muted);
        MoveLabel(menu,frame,"Game Title",new Vector2(32,-70),new Vector2(456,88),60,PlayerUIStyle.Cream);
        MoveLabel(menu,frame,"Game Subtitle",new Vector2(36,-164),new Vector2(448,42),27,PlayerUIStyle.Mint);
        MoveLabel(menu,frame,"Chapter",new Vector2(36,-218),new Vector2(448,48),20,PlayerUIStyle.Cream);
        PlaceButton(menu.btnBegin,frame,new Vector2(36,-278),new Vector2(448,68),26,PlayerUIStyle.Mint,PlayerUIStyle.Ink);
        PlaceButton(menu.btnContinue,frame,new Vector2(36,-362),new Vector2(448,60),23,new Color(.39f,.31f,.55f),menu.btnContinue.interactable?PlayerUIStyle.Cream:WaterDrainUI.Muted);
        var divider=menu.transform.Find("Menu Divider") as RectTransform;
        if(divider!=null)Place(divider,frame,new Vector2(36,-438),new Vector2(448,1));
        PlaceButton(menu.btnSetting,frame,new Vector2(36,-452),new Vector2(216,56),21,PlayerUIStyle.Cream,PlayerUIStyle.Ink);
        var help=GameplayUIFactory.Button("Mobile Guide Button",frame,Vector2.zero,Vector2.zero,font,"操作指南");
        PlaceButton(help,frame,new Vector2(268,-452),new Vector2(216,56),21,PlayerUIStyle.Violet,PlayerUIStyle.Cream);
        help.onClick.AddListener(guide.Show);
        var oldHint=menu.transform.Find("Control Hint");if(oldHint!=null)oldHint.gameObject.SetActive(false);
        var footer=GameplayUIFactory.Label("Mobile Menu Footer",frame,Vector2.zero,Vector2.zero,font,15,ControlsGuidePanel.MenuHint);
        Place(footer.rectTransform,frame,new Vector2(36,-536),new Vector2(448,30));footer.color=WaterDrainUI.Muted;WaterDrainUI.Readable(footer,14);
        var credit=menu.transform.Find("Jam Credit");
        if(credit!=null)
        {
            var rect=(RectTransform)credit;rect.anchorMin=rect.anchorMax=new Vector2(.79f,0);rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=new Vector2(0,28);rect.sizeDelta=new Vector2(420,26);
            var label=credit.GetComponent<Text>();label.fontSize=13;label.alignment=TextAnchor.MiddleCenter;WaterDrainUI.Readable(label,12);
        }
        var hero=menu.transform.Find("Drain Hero") as RectTransform;
        if(hero!=null){hero.anchorMin=new Vector2(.62f,.15f);hero.anchorMax=new Vector2(.96f,.80f);hero.offsetMin=hero.offsetMax=Vector2.zero;}
    }
    private static void Place(RectTransform rect,Transform parent,Vector2 at,Vector2 size)
    {rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=at;rect.sizeDelta=size;}
    private static void MoveLabel(BeginPanel menu,Transform parent,string name,Vector2 at,Vector2 size,int fontSize,Color color)
    {
        var child=menu.transform.Find(name);if(child==null)return;
        var label=child.GetComponent<Text>();Place(label.rectTransform,parent,at,size);
        label.fontSize=fontSize;label.color=color;label.alignment=TextAnchor.MiddleLeft;WaterDrainUI.Readable(label,fontSize-2);
    }
    private static void PlaceButton(Button button,Transform parent,Vector2 at,Vector2 size,int fontSize,Color face,Color text)
    {
        Place((RectTransform)button.transform,parent,at,size);button.image.sprite=PlayerUIStyle.Rounded;button.image.type=Image.Type.Sliced;button.image.color=face;
        var colors=button.colors;colors.normalColor=colors.selectedColor=colors.highlightedColor=Color.white;
        colors.pressedColor=new Color(.8f,.9f,.88f);colors.disabledColor=new Color(.8f,.8f,.86f,.85f);button.colors=colors;
        var label=button.GetComponentInChildren<Text>();label.fontSize=fontSize;label.color=text;WaterDrainUI.Readable(label,fontSize-2);
    }
}
