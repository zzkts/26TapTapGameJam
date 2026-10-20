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
    private string promptTitle, promptDetail;
    private float promptWidth;
    private float shownHp=float.NaN,shownMaxHp=float.NaN;
    private int shownDashState=-1,shownDashTenths=-1,shownPhase=-1,shownPhaseTenths=-1,shownNextIndex=-1;
    private Modifier shownAttribute,shownNextAttribute;
    private RectTransform canvasRect;
    private Camera gameplayCamera;
    public override void Init()
    {
        if (inventory != null) inventory.Acquired -= OnAcquired;
        player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        inventory = player.Genes; chain = player.GetComponent<GeneChainController>();
        interaction = player.GetComponent<PlayerInteraction>();
        if (inventory != null) inventory.Acquired += OnAcquired;
        lastHp = player.hp;
        shownHp=shownMaxHp=float.NaN;shownDashState=shownPhase=shownNextIndex=-1;
        var canvas=GetComponentInParent<Canvas>();canvasRect=canvas!=null?canvas.transform as RectTransform:null;
        gameplayCamera=Camera.main;
        if (healthFill != null) healthFill.fillAmount = HealthRatio;
        if (healthTrail != null) healthTrail.fillAmount = HealthRatio;
        RefreshSlots();
    }
    private float HealthRatio => player.maxHp > 0f ? Mathf.Clamp01(player.hp / player.maxHp) : 0f;
    private void OnAcquired(Modifier gene)
    {
        if (toastText == null) return;
        toastText.text = gene.Level > 1 ? gene.Name + " · 进化完成！" :
            "获得 " + gene.Name + "   " + gene.Copies + " / " + inventory.GetEvolutionCopies(gene);
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
                slotIcons[i].gameObject.SetActive(gene != null);
                slotIcons[i].color = PlayerUIStyle.GeneColor(gene);
            }
            if (slotLevels != null && i < slotLevels.Length)
            {
                slotLevels[i].text = gene != null && gene.Level > 1 ? "★" : (i + 1).ToString();
                slotLevels[i].color = gene != null && gene.Level > 1 ? PlayerUIStyle.Gold : new Color(.74f, .7f, .88f);
            }
            if (slotFrames != null && i < slotFrames.Length)
            {
                slotFrames[i].color = gene==null?WaterDrainUI.Border:PlayerUIStyle.Violet;
                Outline rarity = slotFrames[i].GetComponent<Outline>();
                if (rarity != null) rarity.enabled = gene != null && gene.Level > 1;
            }
            if (slotWells != null && i < slotWells.Length)
            { slotWells[i].enabled = gene != null; slotWells[i].gameObject.SetActive(gene != null); }
        }
        shownSlotCount = inventory.GenChain.Count;
    }
    protected override void Update()
    {
        base.Update();
        // A scene may create or restore its player after the HUD's Start callback.
        if (player == null) Init();
        if (player == null) return;
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
        if (healthText != null && (shownHp!=player.hp||shownMaxHp!=player.maxHp))
        {
            shownHp=player.hp;shownMaxHp=player.maxHp;
            healthText.text=shownHp.ToString("0.#")+" / "+shownMaxHp.ToString("0.#");
        }
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
            int state=!player.DashUnlocked?0:player.IsDashing?1:ready?2:3;
            int tenths=state==3?Mathf.RoundToInt(player.DashCooldownRemaining*10):-1;
            if(state!=shownDashState||tenths!=shownDashTenths)
            {
                shownDashState=state;shownDashTenths=tenths;
                dashText.text=state==0?"冲刺 · 寻找模块":state==1?"冲刺中":state==2?"冲刺就绪":"冷却 "+(tenths*.1f).ToString("0.0")+"s";
            }
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
        int phase=reload?0:active?1:current!=null?2:3;
        int phaseTenths=reload||active?Mathf.RoundToInt(chain.Remaining*10):-1;
        if(phaseText!=null&&(phase!=shownPhase||phaseTenths!=shownPhaseTenths||current!=shownAttribute))
            phaseText.text=reload?(phaseTenths*.1f).ToString("0.0")+"s":active?"附魔 "+(phaseTenths*.1f).ToString("0.0")+"s":current!=null?"右键激活":"左键射击";
        if (stageFill != null)
        {
            stageFill.fillAmount = current == null && !reload ? 0f : chain.Progress;
            stageFill.color = reload ? new Color(.68f, .58f, .9f) : PlayerUIStyle.GeneColor(current);
        }
        if (nextText != null)
        {
            int next = chain.AttributeIndex + 1;
            Modifier nextAttribute=next<chain.Attributes.Count?chain.Attributes[next]:null;
            if(phase!=shownPhase||current!=shownAttribute||next!=shownNextIndex||nextAttribute!=shownNextAttribute)
                nextText.text=active?nextAttribute!=null?"下一阶段 "+nextAttribute.Name:"结束后重载":reload?"普通射击仍可使用":current!=null?"属性依次激活":"在终端装配基因";
            shownNextIndex=next;shownNextAttribute=nextAttribute;
        }
        shownPhase=phase;shownPhaseTenths=phaseTenths;shownAttribute=current;
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
        if(!show||interactionCard==null||canvasRect==null)return;
        if(gameplayCamera==null)gameplayCamera=Camera.main;
        if(gameplayCamera==null)return;
        float width = Mathf.Min(448, canvasRect.rect.width - 40);
        if (interactionText != null && (promptTitle != near.Title || promptDetail != near.Detail || !Mathf.Approximately(promptWidth, width)))
        {
            promptTitle = near.Title; promptDetail = near.Detail; promptWidth = width;
            InteractionPromptLayout.Apply(interactionCard, interactionText, promptTitle, promptDetail, width);
        }
        Vector2 screen = gameplayCamera.WorldToScreenPoint(player.transform.position + Vector3.up * 1.05f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
        float halfWidth = canvasRect.rect.width * .5f, halfHeight = canvasRect.rect.height * .5f;
        float padding = interactionCard.rect.width * .5f + 20;
        interactionCard.anchoredPosition = new Vector2(Mathf.Clamp(local.x, -halfWidth + padding, halfWidth - padding),
            Mathf.Clamp(local.y + 28, -halfHeight + 222f, halfHeight - interactionCard.rect.height - 24));
    }
    private void OnDestroy() { if (inventory != null) inventory.Acquired -= OnAcquired; }
}
