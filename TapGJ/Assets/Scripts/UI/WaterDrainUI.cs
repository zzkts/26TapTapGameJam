using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared naming and readable type rules for authored and runtime UI.</summary>
public static class WaterDrainUI
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeginEditorReview()
    {
        if(!UnityEditor.SessionState.GetBool("WaterDrainUI.Review",false))return;
        UnityEditor.SessionState.SetBool("WaterDrainUI.Review",false);
        RunProgress.BeginIsolatedValidation();
    }
#endif
    public const string EnglishTitle = "Water Drain";
    public const string ChineseTitle = "地底物语";
    public const string Signature = "Water Drain  /  地底物语";
    public static readonly Color Muted = new Color(.74f,.73f,.84f);
    public static readonly Color Surface = new Color(.13f,.105f,.22f,.97f);
    public static readonly Color Border = new Color(.39f,.32f,.53f);

    public static void Readable(Text text, int minimum = 14)
    {
        if(text==null)return;
        text.horizontalOverflow=HorizontalWrapMode.Wrap;
        text.verticalOverflow=VerticalWrapMode.Truncate;
        text.resizeTextForBestFit=true;
        text.resizeTextMaxSize=text.fontSize;
        text.resizeTextMinSize=Mathf.Min(text.fontSize,minimum);
        text.raycastTarget=false;
    }
    public static RectTransform Dock(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
    {
        var rect=GameplayUIFactory.Rect(name,parent,anchor,anchor);
        rect.pivot=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
    }
}
