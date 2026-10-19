using UnityEngine;
using UnityEngine.UI;

public sealed class StoryObjectivePanel : MonoBehaviour
{
    public Text chapter,target,progress;
    private CanvasGroup visibility;
    private void Awake()
    {
        visibility=GetComponent<CanvasGroup>();
        if(visibility==null)visibility=gameObject.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts=false;visibility.interactable=false;
    }
    private void Update()
    {
        var player=StorySession.Current!=null?StorySession.Current.player:null;
        visibility.alpha=Mathf.MoveTowards(visibility.alpha,player!=null&&player.MenuOpen?0:1,Time.unscaledDeltaTime*10);
    }
    public void Set(string chapterText,string targetText,string progressText)
    {
        if(chapter!=null&&chapter.text!=chapterText)chapter.text=chapterText;
        if(target!=null&&target.text!=targetText)target.text=targetText;
        if(progress!=null&&progress.text!=progressText)progress.text=progressText;
    }
}
