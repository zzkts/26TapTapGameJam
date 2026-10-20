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
        targetText=MobileGameInput.AdaptHint(targetText);progressText=MobileGameInput.AdaptHint(progressText);
        if(chapter!=null&&chapter.text!=chapterText)chapter.text=chapterText;
        if(target!=null&&target.text!=targetText)target.text=targetText;
        if(progress!=null&&progress.text!=progressText)progress.text=progressText;
    }
    private void Start()
    {
        if(!MobileGameInput.Enabled)return;
        var rect=(RectTransform)transform;rect.anchoredPosition=new Vector2(-96,-16);rect.sizeDelta=new Vector2(300,112);
        if(chapter!=null)chapter.fontSize=18;
        if(target!=null)
        {
            target.rectTransform.anchorMin=new Vector2(.055f,.08f);target.rectTransform.anchorMax=new Vector2(.945f,.70f);
            target.rectTransform.offsetMin=target.rectTransform.offsetMax=Vector2.zero;
            target.fontSize=20;WaterDrainUI.Readable(target,18);
        }
        if(progress!=null)progress.gameObject.SetActive(false);
    }
}
