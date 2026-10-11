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
    private GeneChainController chain;
    private PlayerInteraction interaction;
    private float toastUntil, deathElapsed;
    public override void Init()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        inventory = player.GetComponent<GeneInventory>();
        chain = player.GetComponent<GeneChainController>();
        interaction = player.GetComponent<PlayerInteraction>();
        if (inventory != null) inventory.Acquired += OnAcquired;
        if (chain != null && slotTexts.Length != chain.CopySlots().Length)
        {
            int count = chain.CopySlots().Length;
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
    private void OnAcquired(GeneRecord record, bool evolved)
    {
        GeneData gene = inventory.Definition(record);
        toastText.text = evolved ? gene.displayName + " 已进化为高级基因" :
            "获得 " + gene.displayName + "  " + record.copies + "/" + inventory.Rules.evolutionCopies;
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
        if (chain != null)
        {
            string[] slots = chain.CopySlots();
            for (int i = 0; i < slotTexts.Length; i++)
            {
                GeneRecord record = i < slots.Length ? inventory.Find(slots[i]) : null;
                GeneData gene = inventory.Definition(record);
                slotTexts[i].text = gene == null ? (i + 1) + "\n-" : (gene.kind == GeneKind.Modification ? "改" : "属") + "\n" + gene.displayName;
                slotTexts[i].color = gene == null ? new Color(0.5f, 0.55f, 0.54f) : gene.color;
            }
            string name = chain.CurrentAttribute != null ? chain.CurrentAttribute.displayName : "无属性";
            attributeText.text = chain.State == GeneChainController.Phase.Reloading ? "基因重载  " + chain.Remaining.ToString("0.0") + "s" :
                chain.State == GeneChainController.Phase.Active ? name + "  " + chain.Remaining.ToString("0.0") + "s" : name + " · 就绪";
            stageFill.fillAmount = chain.State == GeneChainController.Phase.Ready ? 1f :
                chain.Remaining / (chain.State == GeneChainController.Phase.Reloading ? inventory.Rules.reloadDuration : chain.CurrentAttribute.enchantDuration);
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
