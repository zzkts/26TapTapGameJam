using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-350)]
public sealed class StoryDialogPanel : BasePanel
{
    public Text speaker,body,hint,endingTitle,endingDescription,endingStats;
    public Image portrait,recordEmblem;
    public Button advanceClick,next,skip,coexist,seal,newRun,mainMenu;
    public GameObject dialogueBody,choiceBody,resultBody;
    [Min(10)] public float charactersPerSecond=48f;
    private StoryLine[] lines;
    private Action finished;
    private Action<bool> choice;
    private int lineIndex,revealed,openedFrame,lastAdvanceFrame=-1;
    private float typing;
    private string fullText="";
    public bool IsDialogue => lines!=null;
    public bool IsChoosing => choiceBody!=null&&choiceBody.activeSelf;
    public bool IsResult => resultBody!=null&&resultBody.activeSelf;
    public bool IsTyping => IsDialogue&&revealed<fullText.Length;
    public int LineIndex => lineIndex;
    public int LineCount => lines!=null?lines.Length:0;

    public override void Init()
    {
        advanceClick.onClick.AddListener(Advance);next.onClick.AddListener(Advance);skip.onClick.AddListener(Skip);
        coexist.onClick.AddListener(()=>Choose(true));seal.onClick.AddListener(()=>Choose(false));
        newRun.onClick.AddListener(()=>{RunProgress.BeginNewRun();});
        mainMenu.onClick.AddListener(()=>{Time.timeScale=1f;SceneManager.LoadSceneAsync("BeginScene");});
    }

    public void Open(StoryLine[] script,Action onFinished)
    {
        lines=script??Array.Empty<StoryLine>();finished=onFinished;choice=null;
        dialogueBody.SetActive(true);choiceBody.SetActive(false);resultBody.SetActive(false);skip.gameObject.SetActive(true);
        openedFrame=Time.frameCount;lastAdvanceFrame=-1;lineIndex=0;UIManager.Instance.ShowPanel(gameObject);
        transform.SetAsLastSibling();
        if(lines.Length==0){FinishDialogue();return;}
        PresentLine();
    }

    private void PresentLine()
    {
        StoryLine line=lines[lineIndex];speaker.text=line.speaker;fullText=MobileGameInput.AdaptHint(line.text);typing=0f;revealed=0;body.text="";
        portrait.gameObject.SetActive(line.playerPortrait&&portrait.sprite!=null);
        recordEmblem.gameObject.SetActive(!line.playerPortrait||portrait.sprite==null);
        speaker.color=line.playerPortrait?PlayerUIStyle.Mint:PlayerUIStyle.Gold;
        hint.text=(MobileGameInput.Enabled?"点击补全文字":"空格 / 点击补全文字")+"   ·   "+(lineIndex+1)+" / "+lines.Length;
        next.GetComponentInChildren<Text>().text="继续  ›";
    }

    protected override void Update()
    {
        base.Update();
        if(IsTyping)
        {
            typing+=Time.unscaledDeltaTime*charactersPerSecond;
            int count=Mathf.Min(fullText.Length,Mathf.FloorToInt(typing));
            if(count!=revealed){revealed=count;body.text=fullText.Substring(0,revealed);}
            if(!IsTyping)hint.text=AdvanceHint;
        }
        if(Time.frameCount<=openedFrame)return;
        Keyboard keyboard=Keyboard.current;
        if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)
        {
            GameplayUIFactory.ModalInputFrame=Time.frameCount;
            if(IsDialogue)Skip();
        }
        else if(keyboard!=null&&keyboard.spaceKey.wasPressedThisFrame&&IsDialogue)Advance();
    }

    public void Advance()
    {
        if(!IsDialogue||Time.frameCount<=openedFrame||lastAdvanceFrame==Time.frameCount)return;
        // Space can reach both this handler and the selected UGUI button in one frame.
        // One press must never reveal a line and immediately discard it.
        lastAdvanceFrame=Time.frameCount;
        if(IsTyping){revealed=fullText.Length;typing=revealed;body.text=fullText;hint.text=AdvanceHint;return;}
        lineIndex++;
        if(lineIndex>=lines.Length)FinishDialogue();else PresentLine();
    }
    public void Skip(){if(IsDialogue)FinishDialogue();}
    private string AdvanceHint=>(MobileGameInput.Enabled?"点击继续":"空格 / 点击继续")+"   ·   "+(lineIndex+1)+" / "+lines.Length;
    private void FinishDialogue()
    {
        Action callback=finished;finished=null;lines=null;
        GameplayUIFactory.ModalInputFrame=Time.frameCount;UIManager.Instance.HidePanel(gameObject,false);callback?.Invoke();
    }

    public void ShowChoices(Action<bool> onChoice)
    {
        lines=null;finished=null;choice=onChoice;
        dialogueBody.SetActive(false);choiceBody.SetActive(true);resultBody.SetActive(false);skip.gameObject.SetActive(false);
        openedFrame=Time.frameCount;UIManager.Instance.ShowPanel(gameObject);transform.SetAsLastSibling();
    }
    public void Choose(bool coexistence)
    {
        if(!IsChoosing||choice==null)return;
        Action<bool> callback=choice;choice=null;choiceBody.SetActive(false);
        GameplayUIFactory.ModalInputFrame=Time.frameCount;UIManager.Instance.HidePanel(gameObject,false);callback(coexistence);
    }
    public void ShowResult(bool coexistence,float seconds,int collectedGenes)
    {
        GameAudioPresentation.PlayCue("victory",.9f);
        lines=null;finished=null;choice=null;dialogueBody.SetActive(false);choiceBody.SetActive(false);resultBody.SetActive(true);skip.gameObject.SetActive(false);
        endingTitle.text=coexistence?"水的归处":"最后的回声";
        endingDescription.text=coexistence?"你打开了回流阀。\n未知的生命，将与城市一同学习如何共存。":"你启动了净化装置。\n城市暂时安静，最后一滴水仍保存着地下的回声。";
        int minutes=Mathf.FloorToInt(seconds/60f),remaining=Mathf.FloorToInt(seconds%60f);
        endingStats.text="探索完成  ·  "+minutes+"分"+remaining.ToString("00")+"秒  ·  收集 "+collectedGenes+" 个基因实例";
        if(CampaignFlow.Current!=null)endingStats.text+="\n三名核心守卫已击败 · 基因构筑挑战完成";
        newRun.GetComponentInChildren<Text>().text="再次探索";
        openedFrame=Time.frameCount;UIManager.Instance.ShowPanel(gameObject);transform.SetAsLastSibling();
    }
    public void CloseForValidation()
    {
        lines=null;finished=null;choice=null;GameplayUIFactory.ModalInputFrame=Time.frameCount;
        UIManager.Instance.HidePanel(gameObject,false);
    }
}
