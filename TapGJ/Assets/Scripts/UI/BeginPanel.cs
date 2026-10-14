using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    public Button btnContinue;
    public Button btnBegin;
    public Button btnSetting;
    public Button btnQuit;
    public Button btnTrial;
    public override void Init()
    {
        if(btnTrial!=null)btnTrial.onClick.AddListener(RunProgress.BeginExperimentRun);
        btnContinue.onClick.AddListener(() =>
        {
            RunProgress.ContinueRun();
        });
        btnContinue.interactable = RunProgress.CanContinue;
        btnBegin.onClick.AddListener(() =>
        {
            RunProgress.BeginNewRun();
        });
        btnSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.ShowPanel<SettingPanel>();
        });
        btnQuit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }
    private void OnEnable()
    {
        if (btnContinue != null) btnContinue.interactable = RunProgress.CanContinue;
    }
}
