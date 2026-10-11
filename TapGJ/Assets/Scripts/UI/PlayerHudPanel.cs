using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHudPanel : BasePanel
{
    public Image healthFill, stageFill;
    public Text healthText, dashText, attributeText, interactionText, toastText;
    public Text[] slotTexts;
    public RunMenuPanel runMenu;
    private PlayerController player;
    private GeneInventory inventory;
    private PlayerInteraction interaction;
    private float toastUntil, deathElapsed;
    public override void Init()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        inventory = player.Genes;
        interaction = player.GetComponent<PlayerInteraction>();
        if (inventory != null) inventory.Acquired += OnAcquired;
        if (inventory != null && slotTexts.Length != inventory.SlotCount)
        {
            int count = inventory.SlotCount;
            Text[] resized = new Text[count];
            for (int i = 0; i < count; i++)
            {
                resized[i] = i < slotTexts.Length ? slotTexts[i] : Instantiate(slotTexts[0], slotTexts[0].transform.parent);
                resized[i].rectTransform.anchorMin = new Vector2(0.027f + i * 0.405f / count, 0.035f);
                resized[i].rectTransform.anchorMax = new Vector2(0.025f + (i + 1) * 0.405f / count, 0.13f);
                resized[i].rectTransform.offsetMin = resized[i].rectTransform.offsetMax = Vector2.zero;
            }
            for (int i = count; i < slotTexts.Length; i++) slotTexts[i].gameObject.SetActive(false);
            slotTexts = resized;
        }
    }
    private void OnAcquired(Modifier gene)
    {
        toastText.text = "获得 " + gene.Name;
        toastUntil = Time.unscaledTime + 3f;
    }
    protected override void Update()
    {
        base.Update();
        if (player == null) return;
        healthFill.fillAmount = Mathf.Clamp01(player.hp / player.maxHp);
        healthText.text = "生命  " + player.hp.ToString("0.#") + " / " + player.maxHp.ToString("0.#");
        dashText.text = !player.DashUnlocked ? "冲刺 · 未解锁" : player.DashCooldownRemaining > 0f ?
            "冲刺  " + player.DashCooldownRemaining.ToString("0.0") + "s" : "冲刺 · 就绪";
        if (inventory != null)
        {
            Modifier[] slots = inventory.CopyChain();
            for (int i = 0; i < slotTexts.Length; i++)
            {
                Modifier gene = i < slots.Length ? slots[i] : null;
                slotTexts[i].text = gene == null ? (i + 1) + "\n-" : (i + 1) + "\n" + gene.Name;
                slotTexts[i].color = gene == null ? new Color(0.5f, 0.55f, 0.54f) : Color.white;
            }
            attributeText.text = "基因链";
            stageFill.fillAmount = 1f;
        }
        WorldInteractable near = interaction != null ? interaction.Nearby : null;
        interactionText.text = near != null && !player.MenuOpen && !player.IsDead ? "[E]  " + near.Title + "\n" + near.Detail : "";
        if (Time.unscaledTime >= toastUntil) toastText.text = "";
        if (player.IsDead)
        {
            deathElapsed += Time.deltaTime;
            if (deathElapsed > 0.7f && !runMenu.gameObject.activeSelf) runMenu.Open(player, true);
        }
        else if (!player.MenuOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            runMenu.Open(player, false);
    }
    private void OnDestroy() { if (inventory != null) inventory.Acquired -= OnAcquired; }
}
