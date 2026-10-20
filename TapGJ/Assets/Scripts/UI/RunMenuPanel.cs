using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunMenuPanel : BasePanel
{
    public Text title;
    public Text hint;
    public Button resumeButton, retryButton;
    public Button settingsButton, menuButton;
    public Image portrait;
    public Sprite alivePortrait, deadPortrait;
    [HideInInspector] public bool cardLayout;
    private PlayerController player;
    public override void Init()
    {
        resumeButton.onClick.AddListener(() =>
        {
            Resume();
        });
        retryButton.onClick.AddListener(() =>
        {
            player.SetMenuOpen(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().path);
        });
        if (settingsButton != null) settingsButton.onClick.AddListener(() => UIManager.Instance.ShowPanel<SettingPanel>());
        if (menuButton != null) menuButton.onClick.AddListener(() => { player.SetMenuOpen(false); Time.timeScale = 1f; SceneManager.LoadScene("BeginScene"); });
    }
    public void Open(PlayerController owner, bool dead)
    {
        player = owner;
        title.text = dead ? "本次探索结束" : "已暂停";
        if(hint!=null)hint.text=dead?"从最近的检查点继续，重新组合基因再出发":"Esc 继续  ·  已记录的检查点会保留";
        if (portrait != null) portrait.sprite = dead ? deadPortrait : alivePortrait;
        resumeButton.gameObject.SetActive(!dead);
        if(cardLayout)
        {
            PlaceCard(retryButton,dead?324:256);
            PlaceCard(settingsButton,dead?256:188);
            PlaceCard(menuButton,dead?188:120);
        }
        else
        {
            Place(retryButton, dead ? .52f : .41f);
            Place(settingsButton, dead ? .39f : .30f);
            Place(menuButton, dead ? .26f : .19f);
        }
        player.SetMenuOpen(true);
        transform.SetAsLastSibling();
        UIManager.Instance.ShowPanel(gameObject);
    }
    private static void Place(Button button, float bottom)
    {
        if (button == null) return;
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(rect.anchorMin.x, bottom);
        rect.anchorMax = new Vector2(rect.anchorMax.x, bottom + .08f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void PlaceCard(Button button,float bottom)
    {
        if(button==null)return;
        var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
        rect.anchoredPosition=new Vector2(54,bottom);rect.sizeDelta=new Vector2(512,52);
    }
    private void Resume()
    {
        if (player == null || player.IsDead) return;
        player.SetMenuOpen(false); UIManager.Instance.HidePanel(gameObject, false);
    }
    protected override void Update()
    {
        base.Update();
        SettingPanel settings = UIManager.Instance.GetPanel<SettingPanel>();
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            Time.frameCount > GameplayUIFactory.ModalInputFrame &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame &&
            (settings == null || !settings.gameObject.activeSelf)) Resume();
    }
}
