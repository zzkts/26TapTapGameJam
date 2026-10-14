using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunMenuPanel : BasePanel
{
    public Text title;
    public Button resumeButton, retryButton;
    public Button settingsButton, menuButton;
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
        resumeButton.gameObject.SetActive(!dead);
        player.SetMenuOpen(true);
        UIManager.Instance.ShowPanel(gameObject);
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
