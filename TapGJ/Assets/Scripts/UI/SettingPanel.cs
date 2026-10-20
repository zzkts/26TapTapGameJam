using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : BasePanel
{
    public Slider sliderGame;
    public Slider sliderBk;
    public Toggle togFull;
    public Button btnClose;
    public Button btnCredits;
    public Text effectsValue, musicValue;
    public Text fullscreenValue;
    public Image fullscreenTrack, fullscreenThumb;
    private MusicCreditsPanel musicCredits;

    public override void Init()
    {
        ConfigureSlider(sliderGame);
        ConfigureSlider(sliderBk);
        sliderGame.onValueChanged.AddListener(GlobalManager.Instance.SetGameVolume);
        sliderBk.onValueChanged.AddListener(GlobalManager.Instance.SetBackgroundVolume);
        sliderGame.onValueChanged.AddListener(_ => RefreshValues());
        sliderBk.onValueChanged.AddListener(_ => RefreshValues());
        togFull.onValueChanged.AddListener(SetFullScreen);
        btnClose.onClick.AddListener(CloseSettings);
        musicCredits = gameObject.GetComponent<MusicCreditsPanel>();
        if (musicCredits == null) musicCredits = gameObject.AddComponent<MusicCreditsPanel>();
        musicCredits.Initialize(btnClose.GetComponentInChildren<Text>().font);
        RefreshSettings();
    }

    private void OnEnable() => RefreshSettings();

    public override void ShowMe()
    {
        transform.SetAsLastSibling();
        base.ShowMe();
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        sliderGame.SetValueWithoutNotify(GlobalManager.Instance.GameVolume);
        sliderBk.SetValueWithoutNotify(GlobalManager.Instance.BackgroundVolume);
        togFull.SetIsOnWithoutNotify(Screen.fullScreen);
        RefreshFullscreen();
        RefreshValues();
    }

    private void RefreshValues()
    {
        if(effectsValue!=null)effectsValue.text=Mathf.RoundToInt(sliderGame.value*100)+"%";
        if(musicValue!=null)musicValue.text=Mathf.RoundToInt(sliderBk.value*100)+"%";
    }

    private static void ConfigureSlider(Slider slider)
    {
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    private void SetFullScreen(bool isFullScreen)
    {
        if (MobileGameInput.Enabled) return;
        Screen.fullScreenMode = isFullScreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        RefreshFullscreen();
    }

    private void RefreshFullscreen()
    {
        bool on = togFull.isOn;
        if(fullscreenValue!=null)fullscreenValue.text=MobileGameInput.Enabled?"自动":on?"开启":"关闭";
        if(fullscreenTrack!=null)fullscreenTrack.color=on?PlayerUIStyle.Mint:WaterDrainUI.Border;
        if(fullscreenThumb!=null)
        {
            fullscreenThumb.color=on?PlayerUIStyle.Ink:PlayerUIStyle.Cream;
            fullscreenThumb.rectTransform.anchoredPosition=new Vector2(on?46:16,16);
        }
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
        {
            if (musicCredits != null && musicCredits.IsOpen) musicCredits.Close();
            else CloseSettings();
        }
    }

}
