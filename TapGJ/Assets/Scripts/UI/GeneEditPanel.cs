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
        EnsureSlotCount(draft.Length); RebuildInventory(); Refresh();
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
        foreach (Transform child in inventoryContent) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        int shown = 0;
        for (int i = 0; i < inventory.GenInventory.Count; i++)
        {
            int index = i; Modifier gene = inventory.GenInventory[i];
            if (gene == null || (filter == 1 && gene.IsAttribute) || (filter == 2 && !gene.IsAttribute)) continue;
            shown++;
            Button row = GameplayUIFactory.Button("Gene " + gene.InstanceId, inventoryContent,
                Vector2.zero, Vector2.one, font, gene.Name + "    " + (gene.IsAttribute ? "属性" : "改造") +
                "\n" + (gene.Level > 1 ? "已进化" : gene.Copies + " / " + inventory.EvolutionCopies));
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
            Text label = row.GetComponentInChildren<Text>(); label.alignment = TextAnchor.MiddleLeft; label.color = new Color(0.12f,0.16f,0.2f);
            label.rectTransform.anchorMin = new Vector2(0.2f,0f); label.fontSize = 20;
            Image background = row.GetComponent<Image>(); background.color = new Color(0.9f,0.97f,1f,0.8f);
            Image icon = GameplayUIFactory.Image("Type", row.transform, new Vector2(0.025f,0.18f), new Vector2(0.16f,0.82f), gene.Color);
            if (theme != null) { icon.sprite = theme.element; icon.preserveAspect = true; }
            if (gene.Level > 1 && theme != null)
            {
                Image star = GameplayUIFactory.Image("Evolution", row.transform, new Vector2(0.85f,0.25f), new Vector2(0.98f,0.75f), Color.white);
                star.sprite = theme.star; star.preserveAspect = true;
            }
            row.onClick.AddListener(() => Equip(index));
        }
        inventoryCount.text = "基因库存  " + shown + " / " + inventory.GenInventory.Count;
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
            Text label = slotButtons[i].GetComponentInChildren<Text>();
            label.text = gene == null ? (i + 1).ToString() : (i + 1) + "\n" + gene.Name;
            label.color = Color.white;
            label.rectTransform.anchorMin = gene == null ? new Vector2(.1f,.75f) : new Vector2(.1f,.28f);
            label.rectTransform.anchorMax = gene == null ? new Vector2(.9f,.98f) : new Vector2(.9f,.72f);
            Transform face = slotButtons[i].transform.Find("Gene Face");
            if (face != null) face.gameObject.SetActive(gene != null);
            slotButtons[i].GetComponent<Image>().color = i == selected ? new Color(0.6f,1f,0.91f) : Color.white;
            if (gene != null && gene.IsAttribute) sequence.Add(gene.Name);
        }
        Modifier selectedGene = draft[selected];
        detail.text = selectedGene == null ? "槽位 " + (selected + 1) + "\n\n未装配" :
            selectedGene.Name + "\n" + (selectedGene.IsAttribute ? "属性 · 附魔 " + selectedGene.EnchantDuration + " 秒" : "改造 · 常驻生效") +
            "\n" + selectedGene.Desc;
        if (detailIcon != null) { detailIcon.enabled = selectedGene != null; detailIcon.color = selectedGene != null ? selectedGene.Color : Color.white; }
        if (detailLevel != null) detailLevel.text = selectedGene == null ? "" : selectedGene.Level > 1 ? "高级基因" : "进化  " + selectedGene.Copies + " / " + inventory.EvolutionCopies;
        if (evolutionSegments != null) for (int i = 0; i < evolutionSegments.Length; i++)
        {
            evolutionSegments[i].enabled = selectedGene != null;
            evolutionSegments[i].color = selectedGene != null && (selectedGene.Level > 1 || selectedGene.Copies > i + 1) ?
                new Color(.1f,.8f,.6f) : new Color(.3f,.35f,.4f,.35f);
        }
        rotation.text = "属性顺位  " + (sequence.Count == 0 ? "无" : string.Join("  >  ", sequence)) + "\n重载  " + player.GetComponent<GeneChainController>().reloadDuration + " 秒";
        leftButton.interactable = selected > 0; rightButton.interactable = selected + 1 < draft.Length;
        removeButton.interactable = selectedGene != null;
        if (filterButtons != null) for (int i = 0; i < filterButtons.Length; i++)
            filterButtons[i].GetComponent<Image>().color = i == filter ? new Color(0.45f,0.86f,0.9f) : new Color(0.87f,0.94f,0.96f);
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
