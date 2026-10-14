using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-350)]
public sealed class GeneDraftPanel:BasePanel
{
    public Button[] options;public Button cancel;public Text[] descriptions;
    private GeneChest chest;private PlayerController player;private Modifier[] choices;
    public override void Init(){cancel.onClick.AddListener(Cancel);for(int i=0;i<options.Length;i++){int index=i;options[i].onClick.AddListener(()=>Choose(index));}}
    public static GeneDraftPanel Ensure()
    {
        Canvas canvas=StoryUIFactory.FindGameplayCanvas();var existing=canvas.GetComponentInChildren<GeneDraftPanel>(true);if(existing!=null)return existing;
        Font font=Object.FindFirstObjectByType<PlayerHudPanel>(FindObjectsInactive.Include).healthText.font;
        GameObject obj=GameplayUIFactory.Rect("基因三选一",canvas.transform,Vector2.zero,Vector2.one).gameObject;
        var panel=obj.AddComponent<GeneDraftPanel>();
        GameplayUIFactory.Image("Shade",obj.transform,Vector2.zero,Vector2.one,new Color(.06f,.04f,.12f,.94f),true);
        Text title=GameplayUIFactory.Label("Title",obj.transform,new Vector2(.14f,.77f),new Vector2(.86f,.88f),font,38,"选择一种变化",TextAnchor.MiddleCenter);title.color=PlayerUIStyle.Mint;
        Text subtitle=GameplayUIFactory.Label("Hint",obj.transform,new Vector2(.17f,.69f),new Vector2(.83f,.77f),font,19,"获得所选基因的两份样本 · 到终端装配后生效",TextAnchor.MiddleCenter);subtitle.color=PlayerUIStyle.Cream;
        panel.options=new Button[3];panel.descriptions=new Text[3];
        for(int i=0;i<3;i++)
        {
            float left=.10f+i*.275f;
            Button card=GameplayUIFactory.Button("Choice "+i,obj.transform,new Vector2(left,.30f),new Vector2(left+.25f,.65f),font,"");
            card.GetComponent<Image>().sprite=PlayerUIStyle.Rounded;card.GetComponent<Image>().type=Image.Type.Sliced;card.GetComponent<Image>().color=PlayerUIStyle.Violet;
            Text label=card.GetComponentInChildren<Text>();label.fontSize=23;label.color=PlayerUIStyle.Cream;panel.options[i]=card;panel.descriptions[i]=label;
        }
        panel.cancel=GameplayUIFactory.Button("Decide Later",obj.transform,new Vector2(.38f,.15f),new Vector2(.62f,.23f),font,"暂不选择");
        panel.cancel.GetComponent<Image>().color=PlayerUIStyle.Mint;obj.SetActive(false);return panel;
    }
    public void Open(PlayerController owner,GeneChest source,Modifier[] pool)
    {
        player=owner;chest=source;choices=pool;owner.SetMenuOpen(true);UIManager.Instance.ShowPanel(gameObject);transform.SetAsLastSibling();
        for(int i=0;i<3;i++)
        {
            Modifier gene=i<pool.Length?pool[i]:null;options[i].gameObject.SetActive(gene!=null);
            if(gene!=null)descriptions[i].text=gene.Name+"\n\n"+gene.Desc+"\n\n两份样本";
        }
    }
    public void Choose(int index)
    {
        if(chest==null||index<0||index>=choices.Length)return;
        if(!chest.ChooseReward(player,choices[index]))return;
        UIManager.Instance.HidePanel(gameObject,false);player.SetMenuOpen(false);GameplayUIFactory.ModalInputFrame=Time.frameCount;
    }
    public void Cancel(){UIManager.Instance.HidePanel(gameObject,false);player?.SetMenuOpen(false);GameplayUIFactory.ModalInputFrame=Time.frameCount;}
    protected override void Update()
    {
        base.Update();
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)Cancel();
    }
}
