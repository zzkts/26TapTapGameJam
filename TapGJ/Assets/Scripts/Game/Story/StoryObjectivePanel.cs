using UnityEngine;
using UnityEngine.UI;

public sealed class StoryObjectivePanel : MonoBehaviour
{
    public Text chapter,target,progress;
    public void Set(string chapterText,string targetText,string progressText)
    {
        if(chapter!=null&&chapter.text!=chapterText)chapter.text=chapterText;
        if(target!=null&&target.text!=targetText)target.text=targetText;
        if(progress!=null&&progress.text!=progressText)progress.text=progressText;
    }
}
