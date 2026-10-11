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
    private Modifier[] draft;
    private int selected;
    public Modifier[] CopyDraft() => draft == null ? Array.Empty<Modifier>() : (Modifier[])draft.Clone();
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
        inventory = owner.Genes;
        if (inventory == null) return;
        draft = inventory.CopyChain();
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
        for (int i = 0; i < inventory.GenInventory.Count; i++)
        {
            int index = i;
            Modifier gene = inventory.GenInventory[index];
            Button row = GameplayUIFactory.Button("Gene " + gene.GeneId, inventoryContent,
                Vector2.zero, Vector2.one, font, gene.Name + "\n" + gene.Desc);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 68f;
            row.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            row.onClick.AddListener(() => Equip(index));
        }
        inventoryCount.text = "基因库存  " + inventory.GenInventory.Count;
    }
    public void Equip(int inventoryIndex)
    {
        if (inventory == null || draft == null) return;
        draft[selected] = inventory.GenInventory[inventoryIndex];
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
        for (int i = 0; i < slotButtons.Length; i++)
        {
            bool valid = i < draft.Length;
            slotButtons[i].gameObject.SetActive(valid);
            if (!valid) continue;
            Modifier gene = draft[i];
            slotButtons[i].GetComponentInChildren<Text>().text = gene == null ? (i + 1) + "\n空槽" :
                (i + 1) + "\n" + gene.Name;
            slotButtons[i].GetComponent<Image>().color = i == selected ? new Color(0.18f, 0.45f, 0.38f) : new Color(0.18f, 0.2f, 0.21f);
        }
        Modifier selectedGene = draft[selected];
        detail.text = selectedGene == null ? "槽位 " + (selected + 1) + "\n\n未装配" :
            selectedGene.Name + "\n\n" + selectedGene.Desc;
        rotation.text = "基因按槽位从左到右结算";
        leftButton.interactable = selected > 0;
        rightButton.interactable = selected + 1 < draft.Length;
        removeButton.interactable = selectedGene != null;
    }
    public void Confirm()
    {
        if (inventory != null && inventory.Commit(station, player, draft)) Close();
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
