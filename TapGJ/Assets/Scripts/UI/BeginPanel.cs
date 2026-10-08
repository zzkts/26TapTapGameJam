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
            //����
        });
        btnBegin.onClick.AddListener(() =>
        {
            SceneManager.LoadSceneAsync("GameScene");
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
        //�˳�
        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
}
