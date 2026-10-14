using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GeneEditPanel : BasePanel
{
    public Font font;
    public Button[] slotButtons;
    public RectTransform inventoryContent;
    public Text detail, rotation, inventoryCount;
    public Button confirmButton, cancelButton, removeButton, leftButton, rightButton;
    public Button[] filterButtons;
    public Image detailIcon;
    public Text detailLevel;
    public Image[] evolutionSegments;
    private PlayerController player;
    private GeneEditStation station;
    private GeneInventory inventory;
    private Modifier[] draft;
    private int selected, filter;
    private GameplayUITheme theme;
    private readonly List<InventoryRow> rows = new List<InventoryRow>();
    private sealed class InventoryRow
    {
        public Modifier gene;
        public int instanceNumber;
        public Image border, surface;
        public Text state;
    }
    public Modifier[] CopyDraft() => draft == null ? Array.Empty<Modifier>() : (Modifier[])draft.Clone();
    public override void Init()
    {
        theme = Resources.Load<GameplayUITheme>("SO/UI/GameplayUITheme");
        BindSlots();
        confirmButton.onClick.AddListener(Confirm); cancelButton.onClick.AddListener(Cancel);
        removeButton.onClick.AddListener(() => { if (draft == null) return; draft[selected] = null; Refresh(); });
        leftButton.onClick.AddListener(() => MoveSlot(-1)); rightButton.onClick.AddListener(() => MoveSlot(1));
        if (filterButtons != null) for (int i = 0; i < filterButtons.Length; i++)
        {
            int index = i;
            filterButtons[i].onClick.AddListener(() => { filter = index; RebuildInventory(); Refresh(); });
        }
        StyleControls();
    }
    private void BindSlots()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i; slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => SelectSlot(index));
        }
    }
    public void Open(PlayerController owner, GeneEditStation terminal)
    {
        if (owner == null || owner.MenuOpen || terminal == null || !terminal.CanUse(owner)) return;
        player = owner; station = terminal; inventory = owner.Genes;
        if (inventory == null) return;
        draft = inventory.CopyChain(); selected = 0;
        player.SetMenuOpen(true); UIManager.Instance.ShowPanel(gameObject);
        EnsureSlotCount(draft.Length); StyleControls(); RebuildInventory(); Refresh();
        ScrollRect scroll = inventoryContent.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }
    private void EnsureSlotCount(int count)
    {
        if (slotButtons.Length == count) return;
        Button[] resized = new Button[count];
        for (int i = 0; i < count; i++)
        {
            resized[i] = i < slotButtons.Length ? slotButtons[i] : Instantiate(slotButtons[0], slotButtons[0].transform.parent);
            RectTransform rect = resized[i].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.12f + i * 0.75f / count, 0.205f);
            rect.anchorMax = new Vector2(0.12f + (i + 1) * 0.75f / count - 0.012f, 0.335f);
            rect.offsetMin = rect.offsetMax = Vector2.zero; resized[i].gameObject.SetActive(true);
        }
        for (int i = count; i < slotButtons.Length; i++) Destroy(slotButtons[i].gameObject);
        slotButtons = resized; BindSlots();
    }
    private void RebuildInventory()
    {
        rows.Clear();
        foreach (Transform child in inventoryContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        int shown = 0;
        for (int i = 0; i < inventory.GenInventory.Count; i++)
        {
            int index = i; Modifier gene = inventory.GenInventory[i];
            if (gene == null || (filter == 1 && gene.IsAttribute) || (filter == 2 && !gene.IsAttribute)) continue;
            shown++;
            Button row = GameplayUIFactory.Button("Gene " + gene.InstanceId, inventoryContent,
                Vector2.zero, Vector2.one, font, gene.Name);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;
            Image background = row.GetComponent<Image>(); background.sprite = PlayerUIStyle.Rounded;
            background.type = Image.Type.Sliced; background.color = PlayerUIStyle.Violet;
            Image surface = GameplayUIFactory.Image("Surface", row.transform, Vector2.zero, Vector2.one, PlayerUIStyle.Ink);
            surface.sprite = PlayerUIStyle.Rounded; surface.type = Image.Type.Sliced;
            surface.rectTransform.offsetMin = Vector2.one * 3f; surface.rectTransform.offsetMax = -Vector2.one * 3f;
            Text label = row.transform.Find("Label").GetComponent<Text>();
            label.alignment = TextAnchor.MiddleLeft; label.color = PlayerUIStyle.Cream;
            label.rectTransform.anchorMin = new Vector2(0.20f, 0.42f);
            label.rectTransform.anchorMax = new Vector2(0.76f, 0.90f); label.fontSize = 21;
            label.transform.SetAsLastSibling();
            Image iconBack = GameplayUIFactory.Image("Icon Disc", row.transform,
                new Vector2(0.022f,0.12f), new Vector2(0.18f,0.88f), PlayerUIStyle.Violet);
            iconBack.sprite = PlayerUIStyle.Circle; iconBack.preserveAspect = true;
            Image icon = GameplayUIFactory.Image("Type", row.transform,
                new Vector2(0.045f,0.21f), new Vector2(0.157f,0.79f), PlayerUIStyle.GeneColor(gene));
            icon.sprite = PlayerUIStyle.GeneIcon(gene); icon.preserveAspect = true;
            Text type = GameplayUIFactory.Label("Category", row.transform,
                new Vector2(0.20f,0.12f), new Vector2(0.55f,0.44f), font, 15,
                gene.IsAttribute ? "属性 · 右键附魔" : "改造 · 常驻生效");
            type.color = PlayerUIStyle.GeneColor(gene);
            Text state = GameplayUIFactory.Label("Instance State", row.transform,
                new Vector2(0.76f,0.45f), new Vector2(0.965f,0.94f), font, 13, "",
                TextAnchor.MiddleRight);
            Text progress = GameplayUIFactory.Label("Evolution", row.transform,
                new Vector2(0.56f,0.12f), new Vector2(0.965f,0.44f), font, 15,
                gene.Level > 1 ? "高级 · 已进化" : "进化  " + gene.Copies + " / " + inventory.EvolutionCopies,
                TextAnchor.MiddleRight);
            progress.color = gene.Level > 1 ? PlayerUIStyle.Gold : new Color(0.73f,0.72f,0.82f);
            if (gene.Level > 1)
            {
                Image mark = GameplayUIFactory.Image("Advanced Corner", row.transform,
                    new Vector2(0.012f,0.15f), new Vector2(0.022f,0.85f), PlayerUIStyle.Gold);
                mark.sprite = PlayerUIStyle.Rounded; mark.type = Image.Type.Sliced;
            }
            ColorBlock colors = row.colors; colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.83f,1f,0.94f); colors.pressedColor = PlayerUIStyle.Mint;
            colors.selectedColor = Color.white; colors.fadeDuration = 0.08f; row.colors = colors;
            rows.Add(new InventoryRow { gene = gene, instanceNumber = index + 1, border = background, surface = surface, state = state });
            row.onClick.AddListener(() => Equip(index));
        }
        inventoryCount.text = "基因库存  " + shown;
        if (shown == 0)
        {
            Text empty = GameplayUIFactory.Label("Empty Inventory", inventoryContent, Vector2.zero, Vector2.one,
                font, 21, inventory.GenInventory.Count == 0 ? "还没有收集到基因\n击败敌人或开启冷库\n为地漏获得新的能力" : "此分类暂无基因\n切换到「全部」查看库存",
                TextAnchor.MiddleCenter);
            empty.color = PlayerUIStyle.Violet;
            empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
        }
    }
    public void Equip(int index)
    {
        if (inventory == null || draft == null || index < 0 || index >= inventory.GenInventory.Count) return;
        Modifier gene = inventory.GenInventory[index];
        for (int i = 0; i < draft.Length; i++) if (draft[i] == gene) draft[i] = null;
        draft[selected] = gene; Refresh();
    }
    public void SelectSlot(int index) { if (draft != null && index >= 0 && index < draft.Length) { selected = index; Refresh(); } }
    private void MoveSlot(int direction)
    {
        if (draft == null) return;
        int to = selected + direction; if (to < 0 || to >= draft.Length) return;
        (draft[selected], draft[to]) = (draft[to], draft[selected]); selected = to; Refresh();
    }
    private void Refresh()
    {
        if (draft == null) return;
        List<string> sequence = new();
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Modifier gene = draft[i];
            Text label = slotButtons[i].transform.Find("Label").GetComponent<Text>();
            label.text = gene == null ? "空槽" : gene.Name;
            label.color = gene == null ? new Color(.76f,.73f,.87f) : PlayerUIStyle.Cream;
            label.fontSize = gene != null && gene.Name.Length > 5 ? 16 : 18;
            label.rectTransform.anchorMin = new Vector2(.10f,.10f);
            label.rectTransform.anchorMax = new Vector2(.90f,.35f);
            Transform face = slotButtons[i].transform.Find("Gene Face");
            if (face != null)
            {
                face.gameObject.SetActive(gene != null);
                Image icon = face.GetComponent<Image>(); icon.sprite = PlayerUIStyle.GeneIcon(gene);
                icon.color = PlayerUIStyle.GeneColor(gene); icon.preserveAspect = true;
                icon.rectTransform.anchorMin = new Vector2(.34f,.42f);
                icon.rectTransform.anchorMax = new Vector2(.66f,.82f);
            }
            Transform well = slotButtons[i].transform.Find("Slot Well");
            if (well != null) well.gameObject.SetActive(gene != null);
            slotButtons[i].GetComponent<Image>().color = i == selected ? PlayerUIStyle.Mint : new Color(.9f,.85f,1f);
            Outline outline = slotButtons[i].GetComponent<Outline>();
            outline.enabled = i == selected || gene != null && gene.Level > 1;
            outline.effectColor = i == selected ? PlayerUIStyle.Mint : PlayerUIStyle.Gold;
            Text number = slotButtons[i].transform.Find("Slot Number").GetComponent<Text>();
            number.text = (i + 1).ToString("00"); number.color = i == selected ? PlayerUIStyle.Mint : PlayerUIStyle.Cream;
            Text level = slotButtons[i].transform.Find("Level Mark").GetComponent<Text>();
            level.text = gene != null && gene.Level > 1 ? "高" : "";
            if (gene != null && gene.IsAttribute) sequence.Add(gene.Name);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            InventoryRow row = rows[i]; int occupied = Array.IndexOf(draft, row.gene);
            bool current = occupied == selected;
            row.border.color = current ? PlayerUIStyle.Mint : row.gene.Level > 1 ? PlayerUIStyle.Gold : occupied >= 0 ? new Color(.41f,.62f,.63f) : PlayerUIStyle.Violet;
            row.surface.color = current ? new Color(.22f,.19f,.32f) : PlayerUIStyle.Ink;
            row.state.text = "实例 " + row.instanceNumber.ToString("00") + "\n" +
                (occupied >= 0 ? current ? "当前选中槽" : "装入槽位 " + (occupied + 1).ToString("00") : "点击装入");
            row.state.color = occupied >= 0 ? PlayerUIStyle.Mint : new Color(.73f,.72f,.82f);
        }
        Modifier selectedGene = draft[selected];
        detail.text = selectedGene == null ? "槽位 " + (selected + 1).ToString("00") + " · 等待装配\n点击左页基因装入此槽\n确认后才会应用构筑" :
            selectedGene.Name + "\n" + (selectedGene.IsAttribute ? "属性 · 附魔 " + selectedGene.EnchantDuration + " 秒" : "改造 · 常驻生效") +
            "\n" + selectedGene.Desc;
        detail.color = PlayerUIStyle.Cream;
        if (detailIcon != null)
        {
            detailIcon.enabled = selectedGene != null;
            detailIcon.sprite = PlayerUIStyle.GeneIcon(selectedGene);
            detailIcon.color = PlayerUIStyle.GeneColor(selectedGene); detailIcon.preserveAspect = true;
        }
        if (detailLevel != null)
        {
            detailLevel.text = selectedGene == null ? "选择基因开始构筑" : selectedGene.Level > 1 ? "高级基因 · 完成进化" : "初级基因  " + selectedGene.Copies + " / " + inventory.EvolutionCopies;
            detailLevel.color = selectedGene != null && selectedGene.Level > 1 ? new Color(.58f,.33f,.12f) : PlayerUIStyle.Ink;
        }
        if (evolutionSegments != null) for (int i = 0; i < evolutionSegments.Length; i++)
        {
            evolutionSegments[i].enabled = selectedGene != null;
            evolutionSegments[i].color = selectedGene != null && (selectedGene.Level > 1 || selectedGene.Copies > i + 1) ?
                selectedGene.Level > 1 ? PlayerUIStyle.Gold : new Color(.16f,.68f,.51f) : new Color(.3f,.25f,.4f,.22f);
        }
        rotation.fontSize = 18; rotation.color = PlayerUIStyle.Ink;
        rotation.text = "属性轮换  " + (sequence.Count == 0 ? "无 · 改造始终生效" : string.Join("  →  ", sequence)) +
            (sequence.Count == 0 ? "" : "\n右键逐次开启 · 整链重载 " + player.GetComponent<GeneChainController>().reloadDuration + " 秒") +
            "\n" + GeneBuildAnalysis.Describe(draft);
        rotation.fontSize = 15;
        leftButton.interactable = selected > 0; rightButton.interactable = selected + 1 < draft.Length;
        removeButton.interactable = selectedGene != null;
        if (filterButtons != null) for (int i = 0; i < filterButtons.Length; i++)
        {
            filterButtons[i].GetComponent<Image>().color = i == filter ? PlayerUIStyle.Mint : PlayerUIStyle.Cream;
            filterButtons[i].GetComponentInChildren<Text>().color = PlayerUIStyle.Ink;
        }
    }
    private void StyleControls()
    {
        if (filterButtons != null) foreach (Button button in filterButtons) StyleButton(button, PlayerUIStyle.Cream);
        StyleButton(confirmButton, PlayerUIStyle.Mint); StyleButton(cancelButton, PlayerUIStyle.Cream);
        StyleButton(removeButton, PlayerUIStyle.Cream); StyleButton(leftButton, PlayerUIStyle.Cream); StyleButton(rightButton, PlayerUIStyle.Cream);
        if (inventoryCount != null) inventoryCount.color = PlayerUIStyle.Ink;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button button = slotButtons[i];
            Outline outline = button.GetComponent<Outline>();
            if (outline == null) outline = button.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(3f,-3f); outline.useGraphicAlpha = true;
            if (button.transform.Find("Slot Well") == null)
            {
                Image well = GameplayUIFactory.Image("Slot Well", button.transform,
                    new Vector2(.24f,.27f), new Vector2(.76f,.83f), PlayerUIStyle.Ink);
                well.sprite = PlayerUIStyle.Circle;
                well.transform.SetSiblingIndex(1);
            }
            if (button.transform.Find("Slot Number") == null)
                GameplayUIFactory.Label("Slot Number", button.transform, new Vector2(.08f,.76f),
                    new Vector2(.32f,.97f), font, 14, "", TextAnchor.MiddleCenter);
            if (button.transform.Find("Level Mark") == null)
            {
                Text level = GameplayUIFactory.Label("Level Mark", button.transform, new Vector2(.72f,.75f),
                    new Vector2(.94f,.97f), font, 15, "", TextAnchor.MiddleCenter);
                level.color = PlayerUIStyle.Gold;
            }
            Transform face = button.transform.Find("Gene Face");
            if (face == null)
                face = GameplayUIFactory.Image("Gene Face", button.transform, new Vector2(.34f,.42f),
                    new Vector2(.66f,.82f), PlayerUIStyle.Cream).transform;
            face.SetAsLastSibling();
            Transform label = button.transform.Find("Label");
            if (label != null) label.SetAsLastSibling();
        }
        if (evolutionSegments != null) foreach (Image segment in evolutionSegments)
        { segment.sprite = PlayerUIStyle.Rounded; segment.type = Image.Type.Sliced; }
    }
    private static void StyleButton(Button button, Color color)
    {
        if (button == null) return;
        Image image = button.GetComponent<Image>(); image.sprite = PlayerUIStyle.Rounded; image.type = Image.Type.Sliced; image.color = color;
        Text label = button.GetComponentInChildren<Text>(); if (label != null) label.color = PlayerUIStyle.Ink;
        ColorBlock colors = button.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(.92f,1f,.96f); colors.selectedColor = Color.white;
        colors.pressedColor = new Color(.68f,.86f,.79f); colors.disabledColor = new Color(.67f,.63f,.75f,.5f);
        colors.fadeDuration = .08f; button.colors = colors;
    }
    public void Confirm() { if (inventory != null && inventory.Commit(station, player, draft)) Close(); }
    public void Cancel() => Close();
    private void Close()
    {
        if (player != null) player.SetMenuOpen(false);
        draft = null; UIManager.Instance.HidePanel(gameObject, false);
    }
    protected override void Update()
    {
        base.Update();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
        if (player != null && (player.IsDead || station == null || !station.CanUse(player))) Cancel();
    }
    private void OnDisable() { draft = null; if (player != null && player.MenuOpen) player.SetMenuOpen(false); }
}
