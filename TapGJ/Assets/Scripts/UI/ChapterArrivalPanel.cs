using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Non-modal district announcement. Never captures input; its entire object is destroyed.</summary>
public sealed class ChapterArrivalPanel:MonoBehaviour
{
    public static ChapterArrivalPanel Current { get; private set; }
    public static void Show(string title,string detail)
    {
        if(Current!=null)Destroy(Current.gameObject);
        var canvas=StoryUIFactory.FindGameplayCanvas();
        var rect=GameplayUIFactory.Rect("Chapter Arrival",canvas.transform,new Vector2(.26f,.58f),new Vector2(.74f,.77f));
        var panel=rect.gameObject.AddComponent<ChapterArrivalPanel>();Current=panel;
        var group=rect.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var bg=GameplayUIFactory.Image("Background",rect,Vector2.zero,Vector2.one,new Color(.12f,.09f,.23f,.94f));
        bg.sprite=PlayerUIStyle.Rounded;bg.type=Image.Type.Sliced;bg.raycastTarget=false;
        var font=Object.FindFirstObjectByType<PlayerHudPanel>().healthText.font;
        var heading=GameplayUIFactory.Label("Title",rect,new Vector2(.04f,.52f),new Vector2(.96f,.94f),font,30,title,TextAnchor.MiddleCenter);heading.color=PlayerUIStyle.Mint;heading.raycastTarget=false;
        var text=GameplayUIFactory.Label("Detail",rect,new Vector2(.05f,.07f),new Vector2(.95f,.50f),font,18,detail,TextAnchor.MiddleCenter);text.color=PlayerUIStyle.Cream;text.raycastTarget=false;
        panel.StartCoroutine(panel.Fade(group));
    }
    private IEnumerator Fade(CanvasGroup group)
    {
        float elapsed=0;
        while(elapsed<5)
        {elapsed+=Time.unscaledDeltaTime;group.alpha=Mathf.Min(Mathf.Clamp01(elapsed/.25f),Mathf.Clamp01((5-elapsed)/.8f));yield return null;}
        Destroy(gameObject);
    }
    private void OnDestroy(){if(Current==this)Current=null;}
}
