#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Sprite Editor 内的 Pivot 对齐模块。
/// 使用方式：Sprite Editor 左上角模块下拉框 -> Pivot Baseline。
/// 适用于使用 2D Sprite package 的 Unity 2022.3 / Unity 6（建议先备份 .meta）。
/// </summary>
[RequireSpriteDataProvider(typeof(ITextureDataProvider))]
public class SpritePivotBaselineModule : SpriteEditorModuleBase
{
    public override string moduleName => "Pivot Baseline";

    private SpriteRect[] _working;
    private SpriteRect[] _original;
    private int _textureWidth;
    private int _textureHeight;
    private float _baselineY;
    private bool _centerPivotX = true;
    private bool _hasChanges;
    private VisualElement _panel;
    private Slider _baselineSlider;
    private Slider _pivotXSlider;
    private Slider _pivotYSlider;
    private Toggle _centerToggle;
    private Button _alignButton;
    private Button _saveButton;

    public override bool CanBeActivated()
    {
        return spriteEditor != null &&
               spriteEditor.GetDataProvider<ISpriteEditorDataProvider>() != null &&
               spriteEditor.GetDataProvider<ITextureDataProvider>() != null;
    }

    public override void OnModuleActivate()
    {
        LoadSpriteRects();
        CreatePanel();
        spriteEditor.enableMouseMoveEvent = true;
    }

    public override void OnModuleDeactivate()
    {
        _panel?.RemoveFromHierarchy();
        _panel = null;
        if (spriteEditor != null)
            spriteEditor.enableMouseMoveEvent = false;
    }

    private void LoadSpriteRects()
    {
        var provider = spriteEditor.GetDataProvider<ISpriteEditorDataProvider>();
        var textureProvider = spriteEditor.GetDataProvider<ITextureDataProvider>();
        if (provider == null || textureProvider == null)
            return;

        _original = CloneRects(provider.GetSpriteRects());
        _working = CloneRects(_original);
        textureProvider.GetTextureActualWidthAndHeight(out _textureWidth, out _textureHeight);
        spriteEditor.spriteRects = _working.ToList();
        _hasChanges = false;

        if (_working.Length > 0)
        {
            var first = _working[0];
            // First frame's current Pivot Y is a sensible starting position.
            _baselineY = Mathf.Clamp(
                first.rect.yMin + first.pivot.y * first.rect.height,
                0, _textureHeight);
        }
        else
        {
            _baselineY = _textureHeight * 0.5f;
        }
        spriteEditor.RequestRepaint();
        RefreshPanel();
    }

    private static SpriteRect[] CloneRects(SpriteRect[] source)
    {
        if (source == null) return Array.Empty<SpriteRect>();
        var output = new SpriteRect[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            SpriteRect src = source[i];
            output[i] = new SpriteRect
            {
                spriteID = src.spriteID,
                name = src.name,
                rect = src.rect,
                pivot = src.pivot,
                alignment = src.alignment,
                border = src.border
            };
        }
        return output;
    }

    public override void DoToolbarGUI(Rect drawArea)
    {
    }

    public override void DoMainGUI()
    {
        if (_working == null || spriteEditor == null) return;

        DrawBaseline();
        DrawSelectedSpritePivot();

        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 &&
            GUIUtility.hotControl == 0)
        {
            if (spriteEditor.HandleSpriteSelection())
            {
                RefreshPanel();
                spriteEditor.RequestRepaint();
            }
        }
    }

    private void DrawBaseline()
    {
        if (_textureWidth <= 0 || _textureHeight <= 0) return;

        using (new Handles.DrawingScope(new Color(1f, 0.23f, 0.25f, 1f)))
        {
            Handles.DrawAAPolyLine(2f,
                new Vector3(0f, _baselineY, 0f),
                new Vector3(_textureWidth, _baselineY, 0f));
        }
    }

    private void DrawSelectedSpritePivot()
    {
        SpriteRect sprite = GetSelectedSprite();
        if (sprite == null || sprite.rect.width <= 0 || sprite.rect.height <= 0)
            return;

        Vector2 pivot = SpritePivotMath.ToTexturePosition(sprite.rect, sprite.pivot);
        float size = HandleUtility.GetHandleSize(pivot) * 0.12f;
        using (new Handles.DrawingScope(Color.yellow))
        {
            Handles.DrawAAPolyLine(2f,
                pivot + Vector2.left * size, pivot + Vector2.right * size);
            Handles.DrawAAPolyLine(2f,
                pivot + Vector2.down * size, pivot + Vector2.up * size);
        }
    }

    private SpriteRect GetSelectedSprite()
    {
        SpriteRect selected = spriteEditor.selectedSpriteRect;
        return selected == null
            ? null
            : _working.FirstOrDefault(x => x.spriteID.Equals(selected.spriteID));
    }

    private void CreatePanel()
    {
        VisualElement root = spriteEditor.GetMainVisualContainer();

        _panel = new VisualElement { name = "pivot-baseline-panel" };
        _panel.style.position = Position.Absolute;
        _panel.style.left = 12f;
        _panel.style.top = 12f;
        _panel.style.width = 420f;
        _panel.style.paddingLeft = 8f;
        _panel.style.paddingRight = 8f;
        _panel.style.paddingBottom = 8f;
        _panel.style.backgroundColor = EditorGUIUtility.isProSkin
            ? new Color(0.16f, 0.16f, 0.16f, 0.96f)
            : new Color(0.82f, 0.82f, 0.82f, 0.96f);
        _panel.style.borderTopLeftRadius = 4f;
        _panel.style.borderTopRightRadius = 4f;
        _panel.style.borderBottomLeftRadius = 4f;
        _panel.style.borderBottomRightRadius = 4f;

        Label header = new("Pivot 脚底对齐");
        header.style.height = 24f;
        header.style.unityTextAlign = TextAnchor.MiddleCenter;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        RegisterPanelDrag(header);
        _panel.Add(header);

        _baselineSlider = CreateSlider("基线 Y");
        _pivotXSlider = CreateSlider("Pivot X");
        _pivotYSlider = CreateSlider("Pivot Y");
        _panel.Add(_baselineSlider);
        _panel.Add(_pivotXSlider);
        _panel.Add(_pivotYSlider);

        _baselineSlider.RegisterValueChangedCallback(evt =>
        {
            _baselineY = Mathf.Round(evt.newValue);
            _baselineSlider.SetValueWithoutNotify(_baselineY);
            spriteEditor.RequestRepaint();
        });
        _pivotXSlider.RegisterValueChangedCallback(evt => SetSelectedPivot(evt.newValue, true));
        _pivotYSlider.RegisterValueChangedCallback(evt => SetSelectedPivot(evt.newValue, false));

        VisualElement buttons = new();
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.style.marginTop = 4f;

        _centerToggle = new Toggle("X 居中") { value = _centerPivotX };
        _centerToggle.style.width = 82f;
        _centerToggle.RegisterValueChangedCallback(evt => _centerPivotX = evt.newValue);
        buttons.Add(_centerToggle);

        _alignButton = new Button(AlignAndSave) { text = "对齐并保存" };
        _alignButton.style.flexGrow = 1f;
        buttons.Add(_alignButton);

        _saveButton = new Button(SaveChanges) { text = "保存微调" };
        _saveButton.style.flexGrow = 1f;
        buttons.Add(_saveButton);
        _panel.Add(buttons);

        root.Add(_panel);
        _panel.BringToFront();
        RefreshPanel();
    }

    private static Slider CreateSlider(string label)
    {
        Slider slider = new(label, 0f, 1f) { showInputField = true };
        slider.style.marginTop = 2f;
        return slider;
    }

    private void RegisterPanelDrag(VisualElement header)
    {
        header.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            header.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        header.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!header.HasPointerCapture(evt.pointerId)) return;

            VisualElement root = _panel.parent;
            float maxX = Mathf.Max(0f, root.resolvedStyle.width - _panel.resolvedStyle.width);
            float maxY = Mathf.Max(0f, root.resolvedStyle.height - _panel.resolvedStyle.height);
            _panel.style.left = Mathf.Clamp(_panel.resolvedStyle.left + evt.deltaPosition.x, 0f, maxX);
            _panel.style.top = Mathf.Clamp(_panel.resolvedStyle.top + evt.deltaPosition.y, 0f, maxY);
            evt.StopPropagation();
        });

        header.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!header.HasPointerCapture(evt.pointerId)) return;
            header.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        });
    }

    private void SetSelectedPivot(float value, bool xAxis)
    {
        SpriteRect selected = GetSelectedSprite();
        if (selected == null) return;

        Vector2 local = SpritePivotMath.ToLocalPixelPosition(selected.rect, selected.pivot);
        if (xAxis)
            local.x = Mathf.Round(value);
        else
            local.y = Mathf.Round(value);

        selected.alignment = SpriteAlignment.Custom;
        selected.pivot = SpritePivotMath.FromLocalPixelPosition(selected.rect, local);
        MarkModified();
    }

    private void RefreshPanel()
    {
        if (_panel == null) return;

        bool canEdit = !spriteEditor.editingDisabled;
        SpriteRect selected = GetSelectedSprite();
        _baselineSlider.highValue = Mathf.Max(1f, _textureHeight);
        _baselineSlider.SetValueWithoutNotify(_baselineY);
        _baselineSlider.SetEnabled(canEdit);

        bool hasSelection = canEdit && selected != null;
        _pivotXSlider.SetEnabled(hasSelection);
        _pivotYSlider.SetEnabled(hasSelection);
        if (selected != null)
        {
            Vector2 local = SpritePivotMath.ToLocalPixelPosition(selected.rect, selected.pivot);
            _pivotXSlider.highValue = Mathf.Max(1f, selected.rect.width);
            _pivotYSlider.highValue = Mathf.Max(1f, selected.rect.height);
            _pivotXSlider.SetValueWithoutNotify(local.x);
            _pivotYSlider.SetValueWithoutNotify(local.y);
        }
        else
        {
            _pivotXSlider.SetValueWithoutNotify(0f);
            _pivotYSlider.SetValueWithoutNotify(0f);
        }

        _centerToggle.SetEnabled(canEdit);
        _alignButton.SetEnabled(canEdit && _working.Length > 0);
        _saveButton.SetEnabled(canEdit && _hasChanges);
    }

    private void AlignAndSave()
    {
        int affected = AlignIntersectingRects();
        if (affected == 0)
        {
            Debug.LogWarning("Pivot Baseline: 红线未经过任何 Sprite 切片。");
            return;
        }

        spriteEditor.ApplyOrRevertModification(true);
    }

    private void SaveChanges()
    {
        spriteEditor.ApplyOrRevertModification(true);
    }

    private int AlignIntersectingRects()
    {
        int count = 0;
        // Only change slices which cross this horizontal guide. Other rows stay untouched.
        foreach (SpriteRect sprite in _working)
        {
            Rect rect = sprite.rect;
            if (rect.height <= 0 || _baselineY < rect.yMin || _baselineY > rect.yMax)
                continue;

            sprite.alignment = SpriteAlignment.Custom;
            sprite.pivot = SpritePivotMath.AlignToBaseline(rect, _baselineY,
                _centerPivotX, sprite.pivot);
            count++;
        }
        if (count > 0) MarkModified();
        return count;
    }

    private void MarkModified()
    {
        _hasChanges = true;
        spriteEditor.spriteRects = _working.ToList();
        spriteEditor.SetDataModified();
        spriteEditor.RequestRepaint();
        RefreshPanel();
    }

    public override bool ApplyRevert(bool apply)
    {
        if (_working == null) return false;

        if (apply)
        {
            ISpriteEditorDataProvider provider =
                spriteEditor.GetDataProvider<ISpriteEditorDataProvider>();
            if (provider == null) return false;

            provider.SetSpriteRects(_working);
            provider.Apply();
            _original = CloneRects(_working);
            _hasChanges = false;
            RefreshPanel();
            return true; // Unity reimports the texture.
        }

        _working = CloneRects(_original);
        spriteEditor.spriteRects = _working.ToList();
        _hasChanges = false;
        spriteEditor.RequestRepaint();
        RefreshPanel();
        return false;
    }

    public override void DoPostGUI() { }
}
#endif
