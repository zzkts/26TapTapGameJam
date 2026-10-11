using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunMenuPanel : BasePanel
{
    public Text title;
    public Button resumeButton, retryButton;
    private PlayerController player;
    public override void Init()
    {
        resumeButton.onClick.AddListener(() =>
        {
            player.SetMenuOpen(false);
            UIManager.Instance.HidePanel(gameObject, false);
        });
        retryButton.onClick.AddListener(() =>
        {
            player.SetMenuOpen(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }
    public void Open(PlayerController owner, bool dead)
    {
        player = owner;
        title.text = dead ? "本次探索结束" : "已暂停";
        resumeButton.gameObject.SetActive(!dead);
        player.SetMenuOpen(true);
        UIManager.Instance.ShowPanel(gameObject);
    }
}
