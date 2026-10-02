using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    public Button btnContinue;
    public Button btnBegin;
    public Button btnSetting;
    public Button btnQuit;
    public override void Init()
    {
        btnContinue.onClick.AddListener(() =>
        {
            //´ú×ö
        });
        btnBegin.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("GameScene");
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
        //ÍË³ö
        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
