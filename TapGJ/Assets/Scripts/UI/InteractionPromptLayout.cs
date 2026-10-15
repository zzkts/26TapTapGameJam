using UnityEngine;
using UnityEngine.UI;

/// <summary>One bounded prompt for a nearby action; long objectives belong in the map.</summary>
public static class InteractionPromptLayout
{
    public static void Apply(RectTransform card, Text label, string title, string detail, float width)
    {
        width = Mathf.Clamp(width, 290f, 448f);
        label.supportRichText = true;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = false;
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleLeft;
        label.text = "<b>" + title + "</b>" + (string.IsNullOrEmpty(detail) ? "" : "\n" + detail);
        RectTransform textRect = label.rectTransform;
        textRect.anchorMin = textRect.anchorMax = Vector2.zero;
        textRect.pivot = Vector2.zero;
        textRect.anchoredPosition = new Vector2(76, 18);
        textRect.sizeDelta = new Vector2(width - 104, 180);
        float height = Mathf.Clamp(Mathf.Ceil(label.preferredHeight) + 38, 94, 240);
        card.sizeDelta = new Vector2(width, height);
        textRect.sizeDelta = new Vector2(width - 104, height - 36);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 14;
        label.resizeTextMaxSize = 18;
        // Keep the key centered when a three-line instruction expands the card.
        foreach (RectTransform child in card)
            if (child.name == "E" || child.name == "E Key")
                child.anchoredPosition = new Vector2(22, (height - child.rect.height) * .5f);
    }
}
