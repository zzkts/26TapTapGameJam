using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    public Button btnContinue;
    public Button btnBegin;
    public Button btnSetting;
    public Button btnQuit;
    public Button btnGuide;
    public override void Init()
    {
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
        if(!MobileGameInput.Enabled&&btnGuide!=null)
        {
            var guide=ControlsGuidePanel.Create(GetComponentInParent<Canvas>().transform,
                btnBegin.GetComponentInChildren<Text>().font,false);
            btnGuide.onClick.AddListener(guide.Show);
        }
    }
    private void OnEnable()
    {
        if (btnContinue != null) btnContinue.interactable = RunProgress.CanContinue;
    }
}
