using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Slider sliderGame;
    public Slider sliderBk;
    public Toggle togFull;
    public Button btnClose;

    public override void Init()
    {
        ConfigureSlider(sliderGame);
        ConfigureSlider(sliderBk);
        sliderGame.onValueChanged.AddListener(GlobalManager.Instance.SetGameVolume);
        sliderBk.onValueChanged.AddListener(GlobalManager.Instance.SetBackgroundVolume);
        togFull.onValueChanged.AddListener(SetFullScreen);
        btnClose.onClick.AddListener(CloseSettings);
        RefreshSettings();
    }

    private void OnEnable() => RefreshSettings();

    public override void ShowMe()
    {
        base.ShowMe();
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        sliderGame.SetValueWithoutNotify(GlobalManager.Instance.GameVolume);
        sliderBk.SetValueWithoutNotify(GlobalManager.Instance.BackgroundVolume);
        togFull.SetIsOnWithoutNotify(Screen.fullScreen);
    }

    private static void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreenMode = isFullScreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void CloseSettings()
    {
        GameplayUIFactory.ModalInputFrame = Time.frameCount;
        GlobalManager.Instance.SaveMusic();
        UIManager.Instance.HidePanel(gameObject, true);
    }

    protected override void Update()
    {
        base.Update();
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            Time.frameCount > GameplayUIFactory.ModalInputFrame &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseSettings();
    }

}
