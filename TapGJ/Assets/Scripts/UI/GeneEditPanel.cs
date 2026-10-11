using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GeneEditPanel : BasePanel
{
    public Font font;
    public Button[] slotButtons;
    public RectTransform inventoryContent;
    public Text detail;
    public Text rotation;
    public Text inventoryCount;
    public Button confirmButton, cancelButton, removeButton, leftButton, rightButton;
    private PlayerController player;
    private GeneEditStation station;
    private GeneInventory inventory;
    private GeneChainController chain;
    private string[] draft;
    private int selected;
    public string[] CopyDraft() => draft == null ? Array.Empty<string>() : (string[])draft.Clone();
    public override void Init()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int index = i;
            slotButtons[i].onClick.AddListener(() => { selected = index; Refresh(); });
        }
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
        removeButton.onClick.AddListener(() => { draft[selected] = null; Refresh(); });
        leftButton.onClick.AddListener(() => MoveSlot(-1));
        rightButton.onClick.AddListener(() => MoveSlot(1));
    }
    public void Open(PlayerController owner, GeneEditStation terminal)
    {
        if (terminal == null || !terminal.CanUse(owner)) return;
        player = owner;
        station = terminal;
        inventory = owner.GetComponent<GeneInventory>();
        chain = owner.GetComponent<GeneChainController>();
        if (inventory == null || chain == null) return;
        draft = chain.CopySlots();
        selected = 0;
        player.SetMenuOpen(true);
        UIManager.Instance.ShowPanel(gameObject);
        EnsureSlotCount(draft.Length);
        RebuildInventory();
        Refresh();
    }
    private void EnsureSlotCount(int count)
    {
        if (slotButtons.Length == count) return;
        Button[] resized = new Button[count];
        for (int i = 0; i < count; i++)
        {
            int index = i;
            resized[i] = i < slotButtons.Length ? slotButtons[i] : Instantiate(slotButtons[0], slotButtons[0].transform.parent);
            RectTransform rect = resized[i].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.025f + i * 0.955f / count, 0.76f);
            rect.anchorMax = new Vector2(0.025f + (i + 1) * 0.955f / count - 0.01f, 0.895f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            resized[i].onClick.RemoveAllListeners();
            resized[i].onClick.AddListener(() => { selected = index; Refresh(); });
            resized[i].gameObject.SetActive(true);
        }
        for (int i = count; i < slotButtons.Length; i++) slotButtons[i].gameObject.SetActive(false);
        slotButtons = resized;
    }
    private void RebuildInventory()
    {
        foreach (Transform child in inventoryContent) Destroy(child.gameObject);
        foreach (GeneRecord record in inventory.Records)
        {
            GeneData gene = inventory.Definition(record);
            if (gene == null) continue;
            string id = record.instanceId;
            string type = gene.kind == GeneKind.Modification ? "改造" : "属性";
            string level = record.level > 1 ? "高级" : record.copies + "/" + inventory.Rules.evolutionCopies;
            Button row = GameplayUIFactory.Button("Gene " + id, inventoryContent, Vector2.zero, Vector2.one, font,
                gene.displayName + "    " + type + "\n" + level);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
            row.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            row.onClick.AddListener(() => Equip(id));
        }
        inventoryCount.text = "基因库存  " + inventory.Records.Count;
    }
    public void Equip(string id)
    {
        if (inventory == null || inventory.Find(id) == null || draft == null) return;
        // An instance can move between slots; distinct instances of the same gene can coexist.
        for (int i = 0; i < draft.Length; i++) if (draft[i] == id) draft[i] = null;
        draft[selected] = id;
        Refresh();
    }
    public void SelectSlot(int index) { if (draft != null && index >= 0 && index < draft.Length) { selected = index; Refresh(); } }
    private void MoveSlot(int direction)
    {
        int to = selected + direction;
        if (to < 0 || to >= draft.Length) return;
        (draft[selected], draft[to]) = (draft[to], draft[selected]);
        selected = to;
        Refresh();
    }
    private void Refresh()
    {
        if (draft == null) return;
        var sequence = new System.Collections.Generic.List<string>();
        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool valid = i < draft.Length;
            slotButtons[i].gameObject.SetActive(valid);
            if (!valid) continue;
            GeneRecord record = inventory.Find(draft[i]);
            GeneData gene = inventory.Definition(record);
            slotButtons[i].GetComponentInChildren<Text>().text = gene == null ? (i + 1) + "\n空槽" :
                (i + 1) + "  " + (gene.kind == GeneKind.Modification ? "改造" : "属性") + "\n" +
                (record.level > 1 ? "高级" : "") + gene.displayName;
            slotButtons[i].GetComponent<Image>().color = i == selected ? new Color(0.18f, 0.45f, 0.38f) : new Color(0.18f, 0.2f, 0.21f);
            if (gene != null && gene.kind == GeneKind.Attribute) sequence.Add(gene.displayName);
        }
        GeneRecord selectedRecord = inventory.Find(draft[selected]);
        GeneData selectedGene = inventory.Definition(selectedRecord);
        detail.text = selectedGene == null ? "槽位 " + (selected + 1) + "\n\n未装配" :
            (selectedRecord.level > 1 ? "高级" : "") + selectedGene.displayName + "\n\n" +
            (selectedGene.kind == GeneKind.Modification ? "改造基因 · 持续生效" : "属性基因 · 附魔 " + selectedGene.enchantDuration + " 秒") +
            "\n\n" + (selectedRecord.level > 1 ? selectedGene.evolvedDescription : selectedGene.description);
        rotation.text = "属性轮换  " + (sequence.Count == 0 ? "无" : string.Join("  >  ", sequence)) +
            "\n整链重载  " + inventory.Rules.reloadDuration + " 秒";
        leftButton.interactable = selected > 0;
        rightButton.interactable = selected + 1 < draft.Length;
        removeButton.interactable = selectedGene != null;
    }
    public void Confirm()
    {
        if (chain != null && chain.Commit(station, draft)) Close();
    }
    public void Cancel() => Close();
    private void Close()
    {
        if (player != null) player.SetMenuOpen(false);
        UIManager.Instance.HidePanel(gameObject, false);
    }
    protected override void Update()
    {
        base.Update();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Cancel();
        if (player != null && (station == null || !station.CanUse(player))) Cancel();
    }
    private void OnDisable() { if (player != null && player.MenuOpen) player.SetMenuOpen(false); }
}
