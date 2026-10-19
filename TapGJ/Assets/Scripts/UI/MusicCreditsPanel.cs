using UnityEngine;
using UnityEngine.UI;

/// <summary>Music attribution remains accessible in both menu and gameplay settings.</summary>
public sealed class MusicCreditsPanel : MonoBehaviour
{
    private GameObject overlay;
    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void Initialize(Font font)
    {
        if (overlay != null) return;
        var entry = MakeButton("Music Credits", transform, .43f, .268f, .57f, .305f, font, "音乐署名");
        entry.onClick.AddListener(Open);
        var shade = GameplayUIFactory.Image("Music Credits Overlay", transform, Vector2.zero, Vector2.one,
            new Color(.035f, .025f, .07f, .94f), true);
        overlay = shade.gameObject;
        var card = GameplayUIFactory.Image("Credits Card", shade.transform, new Vector2(.20f, .13f),
            new Vector2(.80f, .87f), WaterDrainUI.Surface, true);
        card.sprite = PlayerUIStyle.Rounded; card.type = Image.Type.Sliced;
        Label(card.transform, "Heading", .07f, .84f, .93f, .94f, font, 30, "音乐署名", PlayerUIStyle.Mint);
        Label(card.transform, "Menu Track", .07f, .68f, .93f, .81f, font, 22,
            "主菜单 · Baby Black Magi (2026 loop)\nBobjt  /  CC0 1.0", PlayerUIStyle.Cream);
        Label(card.transform, "Menu Source", .07f, .58f, .93f, .68f, font, 16,
            "opengameart.org/content/quirky-mageville\ncreativecommons.org/publicdomain/zero/1.0/", WaterDrainUI.Muted);
        Label(card.transform, "Game Track", .07f, .39f, .93f, .54f, font, 22,
            "游戏场景 · Chee Zee Lab\nKevin MacLeod (incompetech.com)\nLicensed under CC BY 4.0", PlayerUIStyle.Cream);
        Label(card.transform, "Game Source", .07f, .29f, .93f, .39f, font, 16,
            "incompetech.com  /  ISRC USUAN1100683\ncreativecommons.org/licenses/by/4.0/", WaterDrainUI.Muted);
        Label(card.transform, "Usage", .07f, .18f, .93f, .26f, font, 17,
            "原曲用于循环播放，并应用音量调整与淡入淡出。", WaterDrainUI.Muted);
        MakeButton("Close Credits", card.transform, .34f, .045f, .66f, .135f, font, "返回设置").onClick.AddListener(Close);
        overlay.SetActive(false);
    }

    public void Open()
    {
        if (overlay == null) return;
        overlay.transform.SetAsLastSibling(); overlay.SetActive(true);
        GameplayUIFactory.ModalInputFrame = Time.frameCount;
    }
    public void Close()
    {
        if (overlay != null) overlay.SetActive(false);
        GameplayUIFactory.ModalInputFrame = Time.frameCount;
    }
    private void OnDisable() { if (overlay != null) overlay.SetActive(false); }

    private static void Label(Transform parent, string name, float x0, float y0, float x1, float y1,
        Font font, int size, string value, Color color)
    {
        var label = GameplayUIFactory.Label(name, parent, new Vector2(x0, y0), new Vector2(x1, y1),
            font, size, value);
        label.color = color; WaterDrainUI.Readable(label);
    }
    private static Button MakeButton(string name, Transform parent, float x0, float y0, float x1, float y1,
        Font font, string value)
    {
        var button = GameplayUIFactory.Button(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), font, value);
        var image = button.GetComponent<Image>();
        image.sprite = PlayerUIStyle.Rounded; image.type = Image.Type.Sliced; image.color = PlayerUIStyle.Violet;
        var text = button.GetComponentInChildren<Text>(); text.color = PlayerUIStyle.Cream;
        text.fontSize = 18; WaterDrainUI.Readable(text);
        return button;
    }
}
