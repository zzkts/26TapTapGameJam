using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHudPanel : BasePanel
{
    public Image healthFill, stageFill;
    public Text healthText, dashText, attributeText, interactionText, toastText;
    public Text[] slotTexts;
    public Image[] slotIcons;
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
        inventory = player.Genes; chain = player.GetComponent<GeneChainController>();
        interaction = player.GetComponent<PlayerInteraction>();
        if (inventory != null) inventory.Acquired += OnAcquired;
    }
    private void OnAcquired(Modifier gene)
    {
        toastText.text = gene.Level > 1 ? gene.Name + " · 进化完成" : "获得 " + gene.Name + "  " + gene.Copies + " / " + inventory.EvolutionCopies;
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
            for (int i = 0; i < slotTexts.Length; i++)
            {
                Modifier gene = i < inventory.GenChain.Count ? inventory.GenChain[i] : null;
                slotTexts[i].text = gene == null ? (i + 1) + "\n-" : (i + 1) + "\n" + gene.Name;
                slotTexts[i].color = gene == null ? new Color(0.62f,0.7f,0.75f) : Color.white;
                if (slotIcons != null && i < slotIcons.Length) slotIcons[i].color = gene != null ? gene.Color : new Color(0.2f,0.25f,0.3f);
            }
        }
        if (chain != null)
        {
            string name = chain.CurrentAttribute != null ? chain.CurrentAttribute.Name : "基础射击";
            attributeText.text = chain.State == GeneChainController.Phase.Reloading ? "基因链重载  " + chain.Remaining.ToString("0.0") + "s" :
                chain.State == GeneChainController.Phase.Active ? name + "  " + chain.Remaining.ToString("0.0") + "s" : name + " · 就绪";
            stageFill.fillAmount = chain.Progress;
            stageFill.color = chain.State == GeneChainController.Phase.Reloading ? new Color(0.8f,0.82f,0.85f) : chain.CurrentAttribute != null ? chain.CurrentAttribute.Color : Color.cyan;
        }
        WorldInteractable near = interaction != null ? interaction.Nearby : null;
        interactionText.text = near != null && !player.MenuOpen && !player.IsDead ? "[E]  " + near.Title + "\n" + near.Detail : "";
        if (Time.unscaledTime >= toastUntil) toastText.text = "";
        if (player.IsDead)
        {
            deathElapsed += Time.unscaledDeltaTime;
            if (deathElapsed > 0.7f && runMenu != null && !runMenu.gameObject.activeSelf) runMenu.Open(player, true);
        }
        else if (!player.MenuOpen && Time.frameCount > GameplayUIFactory.ModalInputFrame &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            runMenu?.Open(player, false);
    }
    private void OnDestroy() { if (inventory != null) inventory.Acquired -= OnAcquired; }
}
