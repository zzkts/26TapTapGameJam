using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-360)]
public sealed class EmergenceUI:MonoBehaviour
{
    private EmergenceProgress progress;
    private Text feedback,trial,journalText;
    private GameObject journal;
    private PlayerController owner;
    private float nextRefresh;
    public static void Ensure(EmergenceProgress progress)
    {
        Canvas canvas=StoryUIFactory.FindGameplayCanvas();if(canvas==null)return;
        var obj=GameplayUIFactory.Rect("涌现观察",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        var ui=obj.AddComponent<EmergenceUI>();ui.progress=progress;
        PlayerHudPanel hud=Object.FindFirstObjectByType<PlayerHudPanel>();Font font=hud!=null?hud.healthText.font:null;
        ui.feedback=GameplayUIFactory.Label("Reaction Trace",obj.transform,new Vector2(.33f,.025f),new Vector2(.80f,.105f),font,16,"",TextAnchor.MiddleCenter);
        ui.feedback.color=PlayerUIStyle.Mint;ui.feedback.raycastTarget=false;
        ui.trial=GameplayUIFactory.Label("Trial Rule",obj.transform,new Vector2(.36f,.94f),new Vector2(.70f,.99f),font,16,"",TextAnchor.MiddleCenter);
        ui.trial.color=PlayerUIStyle.Gold;ui.trial.raycastTarget=false;
        ui.journal=GameplayUIFactory.Rect("涌现笔记",obj.transform,Vector2.zero,Vector2.one).gameObject;
        GameplayUIFactory.Image("Shade",ui.journal.transform,Vector2.zero,Vector2.one,new Color(.06f,.04f,.12f,.92f),true);
        Image back=GameplayUIFactory.Image("Surface",ui.journal.transform,new Vector2(.18f,.14f),new Vector2(.82f,.87f),PlayerUIStyle.Ink,true);
        back.sprite=PlayerUIStyle.Rounded;back.type=Image.Type.Sliced;
        Text title=GameplayUIFactory.Label("Title",ui.journal.transform,new Vector2(.23f,.75f),new Vector2(.77f,.83f),font,36,"涌现笔记",TextAnchor.MiddleCenter);title.color=PlayerUIStyle.Mint;
        ui.journalText=GameplayUIFactory.Label("Discoveries",ui.journal.transform,new Vector2(.235f,.27f),new Vector2(.765f,.74f),font,20,"",TextAnchor.UpperLeft);ui.journalText.color=PlayerUIStyle.Cream;
        Button close=GameplayUIFactory.Button("Continue Exploring",ui.journal.transform,new Vector2(.37f,.17f),new Vector2(.63f,.25f),font,"继续探索 · TAB / ESC");
        close.GetComponent<Image>().color=PlayerUIStyle.Mint;close.onClick.AddListener(ui.Close);
        ui.journal.SetActive(false);
    }
    private void Update()
    {
        if(progress==null)return;
        Keyboard keyboard=Keyboard.current;
        if(journal.activeSelf&&keyboard!=null&&(keyboard.tabKey.wasPressedThisFrame||keyboard.escapeKey.wasPressedThisFrame)){Close();return;}
        if(keyboard!=null&&keyboard.tabKey.wasPressedThisFrame)
        {
            PlayerController player=StorySession.Current!=null?StorySession.Current.player:null;
            if(player!=null&&!player.MenuOpen&&!player.IsDead)
            {
                owner=player;owner.SetMenuOpen(true);journal.SetActive(true);journal.transform.SetAsLastSibling();
                var lines=new List<string>();foreach(string kind in new[]{"conduction","steam","lift","growth","explosion","fracture","inertia"})
                    lines.Add(progress.Discoveries.TryGetValue(kind,out string text)?"● "+text:"○ 尚未观察到 · 试着让不同规则相遇");
                journalText.text=string.Join("\n\n",lines);
            }
        }
        if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.2f;
        bool modal=StorySession.Current!=null&&StorySession.Current.player.MenuOpen;
        feedback.gameObject.SetActive(!modal);trial.gameObject.SetActive(!modal);
        feedback.text=progress.LastTrace+"\nTAB · 涌现笔记";
        trial.text=RunProgress.ExperimentMode?"回流试验 #"+RunProgress.RunSeed+" · "+EmergenceProgress.TrialName(RunProgress.RunSeed%3):"";
    }
    private void Close()
    {
        journal.SetActive(false);owner?.SetMenuOpen(false);owner=null;GameplayUIFactory.ModalInputFrame=Time.frameCount;
    }
}
