using UnityEngine;
using UnityEngine.UI;

public static class GameplayUIFactory
{
    public static int ModalInputFrame = -1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetModalFrame() => ModalInputFrame = -1;
    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 insetMin = default, Vector2 insetMax = default)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = 5;
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = insetMin;
        rect.offsetMax = insetMax;
        return rect;
    }
    public static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Color color, bool raycast = false)
    {
        var rect = Rect(name, parent, min, max);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }
    public static Text Label(string name, Transform parent, Vector2 min, Vector2 max, Font font, int size, string value, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        var rect = Rect(name, parent, min, max);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = size;
        label.color = new Color(0.92f, 0.95f, 0.94f);
        label.alignment = alignment;
        label.text = value;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }
    public static Button Button(string name, Transform parent, Vector2 min, Vector2 max, Font font, string value)
    {
        Image image = Image(name, parent, min, max, new Color(0.19f, 0.22f, 0.22f), true);
        var button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.65f, 0.9f, 0.83f);
        colors.pressedColor = new Color(0.38f, 0.7f, 0.63f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        Label("Label", image.transform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f), font, 21, value, TextAnchor.MiddleCenter);
        return button;
    }
}
