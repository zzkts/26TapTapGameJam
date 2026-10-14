using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerHudPanel : BasePanel
{
    public Image healthFill, healthTrail, stageFill, dashFill, portrait, toastIcon;
    public Text healthText, dashText, attributeText, interactionText, toastText, phaseText, nextText;
    public Text[] slotTexts, slotLevels;
    public Image[] slotIcons, slotFrames, slotHighlights, slotWells;
    public RectTransform vitality, interactionCard, toastCard;
    public CanvasGroup visualGroup;
    public RunMenuPanel runMenu;
    private PlayerController player;
    private GeneInventory inventory;
    private GeneChainController chain;
    private PlayerInteraction interaction;
    private float toastUntil, deathElapsed, lastHp, trailDelay, hitPulse, dashPulse;
    private bool dashWasReady;
    private Modifier[] shownGenes;
    private int shownSlotCount = -1;
    public override void Init()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        inventory = player.Genes; chain = player.GetComponent<GeneChainController>();
        interaction = player.GetComponent<PlayerInteraction>();
        if (inventory != null) inventory.Acquired += OnAcquired;
        lastHp = player.hp;
        if (healthFill != null) healthFill.fillAmount = HealthRatio;
        if (healthTrail != null) healthTrail.fillAmount = HealthRatio;
        RefreshSlots();
    }
    private float HealthRatio => player.maxHp > 0f ? Mathf.Clamp01(player.hp / player.maxHp) : 0f;
    private void OnAcquired(Modifier gene)
    {
        if (toastText == null) return;
        toastText.text = gene.Level > 1 ? gene.Name + " · 进化完成！" :
            "获得 " + gene.Name + "   " + gene.Copies + " / " + inventory.EvolutionCopies;
        if (toastIcon != null) { toastIcon.sprite = PlayerUIStyle.GeneIcon(gene); toastIcon.color = PlayerUIStyle.GeneColor(gene); }
        toastUntil = Time.unscaledTime + 3.2f; RefreshSlots();
    }
    private void RefreshSlots()
    {
        if (inventory == null || slotTexts == null) return;
        shownGenes = new Modifier[slotTexts.Length];
        for (int i = 0; i < slotTexts.Length; i++)
        {
            Modifier gene = i < inventory.GenChain.Count ? inventory.GenChain[i] : null;
            shownGenes[i] = gene;
            slotTexts[i].text = gene != null ? gene.Name.Replace("高级", "") : "空槽";
            slotTexts[i].color = gene != null ? PlayerUIStyle.Cream : new Color(.67f, .64f, .8f);
            if (slotIcons != null && i < slotIcons.Length)
            {
                slotIcons[i].enabled = gene != null; slotIcons[i].sprite = PlayerUIStyle.GeneIcon(gene);
                slotIcons[i].color = PlayerUIStyle.GeneColor(gene);
            }
            if (slotLevels != null && i < slotLevels.Length)
            {
                slotLevels[i].text = gene != null && gene.Level > 1 ? "★" : (i + 1).ToString();
                slotLevels[i].color = gene != null && gene.Level > 1 ? PlayerUIStyle.Gold : new Color(.74f, .7f, .88f);
            }
            if (slotFrames != null && i < slotFrames.Length)
            {
                slotFrames[i].color = Color.white;
                Outline rarity = slotFrames[i].GetComponent<Outline>();
                if (rarity != null) rarity.enabled = gene != null && gene.Level > 1;
            }
            if (slotWells != null && i < slotWells.Length) slotWells[i].enabled = gene != null;
        }
        shownSlotCount = inventory.GenChain.Count;
    }
    protected override void Update()
    {
        base.Update(); if (player == null) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        if (visualGroup != null) visualGroup.alpha = Mathf.MoveTowards(visualGroup.alpha, player.MenuOpen ? 0f : 1f, dt * 7f);
        float ratio = HealthRatio;
        if (player.hp < lastHp) { trailDelay = Time.unscaledTime + .32f; hitPulse = 1f; }
        if (player.hp > lastHp && healthTrail != null) healthTrail.fillAmount = ratio;
        lastHp = player.hp;
        if (healthFill != null)
        {
            healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, ratio, dt * 3.5f);
            healthFill.color = ratio <= .3f ? new Color(1f, .45f, .59f) : PlayerUIStyle.Mint;
        }
        if (healthTrail != null && Time.unscaledTime >= trailDelay)
            healthTrail.fillAmount = Mathf.MoveTowards(healthTrail.fillAmount, ratio, dt * .65f);
        if (healthText != null) healthText.text = player.hp.ToString("0.#") + " / " + player.maxHp.ToString("0.#");
        hitPulse = Mathf.MoveTowards(hitPulse, 0f, dt * 4f);
        if (vitality != null) vitality.localScale = Vector3.one * (1f + hitPulse * .035f);
        if (portrait != null) portrait.color = Color.Lerp(Color.white, new Color(1f, .4f, .5f), hitPulse);
        bool ready = player.DashUnlocked && player.DashCooldownRemaining <= 0f;
        if (ready && !dashWasReady) dashPulse = 1f;
        dashWasReady = ready; dashPulse = Mathf.MoveTowards(dashPulse, 0f, dt * 2f);
        if (dashFill != null)
        {
            dashFill.fillAmount = !player.DashUnlocked ? 0f : 1f - Mathf.Clamp01(player.DashCooldownRemaining / Mathf.Max(.01f, player.rollCooldown));
            dashFill.color = ready ? PlayerUIStyle.Mint : new Color(.49f, .52f, .76f);
            dashFill.transform.localScale = Vector3.one * (1f + dashPulse * .13f);
        }
        if (dashText != null)
        {
            dashText.text = !player.DashUnlocked ? "冲刺 · 寻找模块" : player.IsDashing ? "冲刺中" :
                ready ? "冲刺就绪" : "冷却 " + player.DashCooldownRemaining.ToString("0.0") + "s";
            dashText.color = ready ? PlayerUIStyle.Mint : new Color(.75f, .73f, .86f);
        }
        UpdateChain(); UpdateInteraction();
        bool toastVisible = Time.unscaledTime < toastUntil && !player.MenuOpen;
        if (toastCard != null)
        {
            toastCard.gameObject.SetActive(toastVisible);
            if (toastVisible) toastCard.localScale = Vector3.one * (1f + .06f * Mathf.Exp(-(3.2f - (toastUntil - Time.unscaledTime)) * 8f));
        }
        else if (!toastVisible && toastText != null) toastText.text = "";
        if (player.IsDead)
        {
            deathElapsed += Time.unscaledDeltaTime;
            if (deathElapsed > .7f && runMenu != null && !runMenu.gameObject.activeSelf) runMenu.Open(player, true);
        }
        else if (!player.MenuOpen && Time.frameCount > GameplayUIFactory.ModalInputFrame &&
            Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) runMenu?.Open(player, false);
    }
    private void UpdateChain()
    {
        if (inventory != null && slotTexts != null)
        {
            bool changed = shownGenes == null || shownSlotCount != inventory.GenChain.Count;
            for (int i = 0; !changed && i < slotTexts.Length; i++)
                changed = shownGenes[i] != (i < inventory.GenChain.Count ? inventory.GenChain[i] : null);
            if (changed) RefreshSlots();
        }
        if (chain == null) return;
        Modifier current = chain.CurrentAttribute;
        bool reload = chain.State == GeneChainController.Phase.Reloading, active = chain.State == GeneChainController.Phase.Active;
        if (attributeText != null) attributeText.text = reload ? "整链重载" : current != null ? current.Name : "基础水弹";
        if (phaseText != null) phaseText.text = reload ? chain.Remaining.ToString("0.0") + "s" :
            active ? "附魔 " + chain.Remaining.ToString("0.0") + "s" : current != null ? "右键激活" : "左键射击";
        if (stageFill != null)
        {
            stageFill.fillAmount = current == null && !reload ? 0f : chain.Progress;
            stageFill.color = reload ? new Color(.68f, .58f, .9f) : PlayerUIStyle.GeneColor(current);
        }
        if (nextText != null)
        {
            int next = chain.AttributeIndex + 1;
            nextText.text = active ? next < chain.Attributes.Count ? "下一阶段 " + chain.Attributes[next].Name : "结束后重载" :
                reload ? "普通射击仍可使用" : current != null ? "属性依次激活" : "在终端装配基因";
        }
        if (slotHighlights != null) for (int i = 0; i < slotHighlights.Length; i++)
        {
            bool selected = !reload && current != null && inventory != null && i < inventory.GenChain.Count && inventory.GenChain[i] == current;
            slotHighlights[i].enabled = selected;
            if (selected) slotHighlights[i].color = new Color(.43f, 1f, .8f, active ? .55f + .2f * Mathf.Sin(Time.unscaledTime * 5f) : .6f);
        }
    }
    private void UpdateInteraction()
    {
        WorldInteractable near = interaction != null ? interaction.Nearby : null;
        bool show = near != null && !player.MenuOpen && !player.IsDead;
        if (interactionCard != null) interactionCard.gameObject.SetActive(show);
        if (interactionText != null) interactionText.text = show ? near.Title + "\n" + near.Detail : "";
        if (!show || interactionCard == null || Camera.main == null) return;
        var canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
        Vector2 screen = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.up * 1.05f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        float halfWidth = canvasRect.rect.width * .5f, halfHeight = canvasRect.rect.height * .5f;
        interactionCard.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -halfWidth + 200f, halfWidth - 200f),
            Mathf.Clamp(local.y, -halfHeight + 170f, halfHeight - 180f));
    }
    private void OnDestroy() { if (inventory != null) inventory.Acquired -= OnAcquired; }
}
