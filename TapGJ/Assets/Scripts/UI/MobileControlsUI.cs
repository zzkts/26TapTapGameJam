using UnityEngine;
using UnityEngine.UI;

/// <summary>A separate phone HUD and touch surface, sharing inventory, health and combat.</summary>
[DefaultExecutionOrder(-450)]
public sealed class MobileControlsUI : MonoBehaviour
{
    public static MobileControlsUI Current { get; private set; }
    public PlayerController Player { get; private set; }
    public MobileStick MoveStick { get; private set; }
    public MobileScreenAim ScreenAim { get; private set; }
    public MobileActionButton JumpButton { get; private set; }
    public MobileActionButton DashButton { get; private set; }
    public MobileActionButton EnchantButton { get; private set; }
    public MobileActionButton InteractButton { get; private set; }
    public MobileActionButton DropButton { get; private set; }
    public MobileActionButton PauseButton { get; private set; }
    private CanvasGroup group;
    private PlayerHudPanel desktopHud;
    private GeneInventory inventory;
    private GeneChainController chain;
    private PlayerInteraction interaction;
    private Font font;
    private Text health, attribute, phase, dashLabel, enchantLabel, interactLabel, nearLabel, toastLabel;
    private Image healthFill, dashFill, enchantFill;
    private readonly Image[] slots = new Image[6], icons = new Image[6];
    private RectTransform nearCard, toastCard, reticle;
    private bool suspended;
    private float nextRefresh, toastUntil;
    private Camera gameplayCamera;

    public void Build(PlayerHudPanel hud)
    {
        Current = this; desktopHud = hud; font = hud.healthText.font;
        group = gameObject.AddComponent<CanvasGroup>();
        var worldTouch = GameplayUIFactory.Rect("World Touch Aim", transform, Vector2.zero, Vector2.one);
        var touchTarget = worldTouch.gameObject.AddComponent<Image>(); touchTarget.color = Color.clear;
        ScreenAim = worldTouch.gameObject.AddComponent<MobileScreenAim>();
        MoveStick = Stick("Move Stick", new Vector2(128,128),190,"移动");
        JumpButton = Action("Jump",new Vector2(-86,104),106,"跳跃",MobileGameInput.Command.Jump,PlayerUIStyle.Mint);
        DashButton = Action("Dash",new Vector2(-104,236),88,"冲刺",MobileGameInput.Command.Dash,PlayerUIStyle.Violet);
        EnchantButton = Action("Enchant",new Vector2(-214,186),84,"附魔",MobileGameInput.Command.Enchant,PlayerUIStyle.Violet);
        InteractButton = Action("Interact",new Vector2(-316,108),84,"交互",MobileGameInput.Command.Interact,PlayerUIStyle.Mint);
        DropButton = Action("Drop",new Vector2(-218,77),68,"下落",MobileGameInput.Command.Drop,WaterDrainUI.Border);
        PauseButton = Action("Pause", new Vector2(-18, -18), 62, "Ⅱ", MobileGameInput.Command.Pause, WaterDrainUI.Surface, true);
        dashLabel = DashButton.GetComponentInChildren<Text>(); enchantLabel = EnchantButton.GetComponentInChildren<Text>(); interactLabel = InteractButton.GetComponentInChildren<Text>();
        dashFill = Ring(DashButton.transform); enchantFill = Ring(EnchantButton.transform);
        BuildHealth(hud); BuildGenes();
        nearCard = Card("Nearby Action", new Vector2(.5f, 0), new Vector2(0, 126), new Vector2(350, 68));
        nearLabel = Label(nearCard, "Context", 22, "", TextAnchor.MiddleCenter);
        toastCard = Card("Mobile Notice", new Vector2(.5f, 1), new Vector2(0, -116), new Vector2(430, 48));
        toastLabel = Label(toastCard, "Notice",18,"点击场景瞄准射击 · 按住连续射击",TextAnchor.MiddleCenter);toastUntil=Time.unscaledTime+5;
        reticle = WaterDrainUI.Dock("Aim Reticle", transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(38, 38));
        Label(reticle, "Reticle", 34, "+", TextAnchor.MiddleCenter).color = PlayerUIStyle.Mint;
        BindPlayer();
    }
    private RectTransform Card(string name, Vector2 anchor, Vector2 at, Vector2 size)
    {
        var rect = WaterDrainUI.Dock(name, transform, anchor, at, size);
        var back = rect.gameObject.AddComponent<Image>(); back.sprite = PlayerUIStyle.Rounded;
        back.type = Image.Type.Sliced; back.color = WaterDrainUI.Surface; back.raycastTarget = false; return rect;
    }
    private Text Label(Transform root, string name, int size, string value, TextAnchor alignment)
    {
        var label = GameplayUIFactory.Label(name, root, new Vector2(.05f, .06f), new Vector2(.95f, .94f), font, size, value, alignment);
        label.color = PlayerUIStyle.Cream; WaterDrainUI.Readable(label, size - 2); return label;
    }
    private MobileStick Stick(string name, Vector2 at, float size, string title)
    {
        var anchor = Vector2.zero;
        var rect = WaterDrainUI.Dock(name, transform, anchor, at, Vector2.one * size); rect.pivot = Vector2.one * .5f;
        var back = rect.gameObject.AddComponent<Image>(); back.sprite = PlayerUIStyle.Circle; back.color = new Color(.17f, .13f, .29f, .72f);
        var border = rect.gameObject.AddComponent<Outline>(); border.effectColor = new Color(.5f, .43f, .68f, .75f); border.effectDistance = new Vector2(2, -2);
        var well = GameplayUIFactory.Image("Well", rect, Vector2.one * .18f, Vector2.one * .82f, new Color(.38f, .3f, .53f, .62f)); well.sprite = PlayerUIStyle.Circle;
        var knob = WaterDrainUI.Dock("Thumb", rect, Vector2.one * .5f, Vector2.zero, Vector2.one * 66); knob.pivot = Vector2.one * .5f;
        var image = knob.gameObject.AddComponent<Image>(); image.sprite = PlayerUIStyle.Circle; image.color = PlayerUIStyle.Mint; image.raycastTarget = false;
        var stick = rect.gameObject.AddComponent<MobileStick>(); stick.knob = knob;
        var caption = WaterDrainUI.Dock(name + " Caption", transform, anchor, at + Vector2.up * (size * .5f + 20), new Vector2(190, 40)); caption.pivot = Vector2.one * .5f;
        Label(caption, "Label", 20, title, TextAnchor.MiddleCenter);
        return stick;
    }
    private MobileActionButton Action(string name, Vector2 at, float size, string value, MobileGameInput.Command command, Color color, bool top = false)
    {
        var rect = WaterDrainUI.Dock(name, transform, new Vector2(1, top ? 1 : 0), at, Vector2.one * size);
        if (!top) rect.pivot = Vector2.one * .5f;
        var face = rect.gameObject.AddComponent<Image>(); face.sprite = PlayerUIStyle.Circle; face.color = color;
        var border = rect.gameObject.AddComponent<Outline>(); border.effectColor = WaterDrainUI.Border; border.effectDistance = new Vector2(2, -2);
        var label = Label(rect, "Label", 23, value, TextAnchor.MiddleCenter); label.color = color == PlayerUIStyle.Mint ? PlayerUIStyle.Ink : PlayerUIStyle.Cream;
        var control = rect.gameObject.AddComponent<MobileActionButton>(); control.command = command; return control;
    }
    private static Image Ring(Transform parent)
    {
        var image = GameplayUIFactory.Image("Cooldown", parent, new Vector2(.03f, .03f), new Vector2(.97f, .97f), new Color(.1f, .07f, .18f, .55f));
        image.sprite = PlayerUIStyle.Circle; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Radial360;
        image.transform.SetSiblingIndex(0); return image;
    }
    private void BuildHealth(PlayerHudPanel hud)
    {
        var card = Card("Mobile Health",new Vector2(0,1),new Vector2(16,-16),new Vector2(260,76));
        var face = GameplayUIFactory.Image("Portrait", card, new Vector2(.02f, .06f), new Vector2(.25f, .94f), Color.white); face.sprite = hud.portrait.sprite; face.preserveAspect = true;
        health=GameplayUIFactory.Label("Health",card,new Vector2(.29f,.48f),new Vector2(.95f,.99f),font,22,"",TextAnchor.MiddleRight);
        WaterDrainUI.Readable(health,20);
        var well = GameplayUIFactory.Image("Health Well", card, new Vector2(.29f, .23f), new Vector2(.95f, .42f), PlayerUIStyle.Violet); well.sprite = PlayerUIStyle.Rounded;
        healthFill = GameplayUIFactory.Image("Health Fill", card, new Vector2(.29f, .23f), new Vector2(.95f, .42f), PlayerUIStyle.Mint);
        healthFill.sprite = PlayerUIStyle.Rounded; healthFill.type = Image.Type.Filled; healthFill.fillMethod = Image.FillMethod.Horizontal;
    }
    private void BuildGenes()
    {
        var card = Card("Mobile Gene Ribbon",new Vector2(.5f,1),new Vector2(0,-16),new Vector2(300,82));
        attribute=GameplayUIFactory.Label("Attribute",card,new Vector2(.04f,.58f),new Vector2(.58f,.99f),font,20,"基础水弹");
        WaterDrainUI.Readable(attribute,18);
        phase = GameplayUIFactory.Label("Phase", card, new Vector2(.58f, .61f), new Vector2(.96f, .97f), font, 18, "", TextAnchor.MiddleRight); phase.color = PlayerUIStyle.Mint;
        for (int i = 0; i < 6; i++)
        {
            float x = .04f + i * .156f;
            slots[i] = GameplayUIFactory.Image("Slot " + i, card, new Vector2(x, .12f), new Vector2(x + .14f, .56f), WaterDrainUI.Border); slots[i].sprite = PlayerUIStyle.Rounded;
            icons[i] = GameplayUIFactory.Image("Gene " + i, slots[i].transform, Vector2.one * .15f, Vector2.one * .85f, PlayerUIStyle.Mint); icons[i].preserveAspect = true;
        }
    }
    private void BindPlayer()
    {
        var found = FindFirstObjectByType<PlayerController>(); if (found == null || found == Player) return;
        Player = found; interaction = Player.GetComponent<PlayerInteraction>(); chain = Player.GetComponent<GeneChainController>(); inventory = Player.Genes;
        if (inventory != null) inventory.Acquired += Acquired;
        gameplayCamera = Camera.main;
    }
    private void Acquired(Modifier gene)
    { toastLabel.text = "获得 " + gene.Name + (gene.Level > 1 ? " · 进化完成" : ""); toastUntil = Time.unscaledTime + 3; }
    public void SetSuspended(bool value)
    {
        if (group == null) return;
        if (value != suspended)
        {
            suspended = value; MobileGameInput.Reset();
            ScreenAim?.Release();
            foreach (var stick in GetComponentsInChildren<MobileStick>()) stick.Release();
            foreach (var button in GetComponentsInChildren<MobileActionButton>()) button.Release();
        }
        group.alpha = value ? 0 : 1; group.interactable = group.blocksRaycasts = !value;
    }
    private void Update()
    {
        if (group == null) return;
        if (Player == null) BindPlayer();
        SetSuspended(Player == null || Player.MenuOpen || Player.IsDead);
        if (suspended) return;
        reticle.gameObject.SetActive(MobileGameInput.Firing);
        if (MobileGameInput.Firing && gameplayCamera != null)
        {
            var screen = MobileGameInput.AimScreenPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, screen, null, out var local); reticle.anchoredPosition = local;
        }
        if (Time.unscaledTime < nextRefresh) return; nextRefresh = Time.unscaledTime + .1f;
        health.text = Player.Hp.ToString("0.#") + " / " + Player.MaxHp.ToString("0.#"); healthFill.fillAmount = Player.Hp / Mathf.Max(1, Player.MaxHp);
        bool dashReady = Player.DashUnlocked && Player.DashCooldownRemaining <= 0;
        DashButton.Available = dashReady; dashFill.fillAmount = Player.DashCooldownRemaining / Mathf.Max(.01f, Player.rollCooldown);
        dashLabel.text = !Player.DashUnlocked ? "冲刺" : dashReady ? "冲刺" : Player.DashCooldownRemaining.ToString("0.0") + "s";
        bool enchantReady = chain != null && chain.State == GeneChainController.Phase.Ready && chain.CurrentAttribute != null;
        EnchantButton.Available = enchantReady;
        enchantLabel.text = chain == null || chain.CurrentAttribute == null ? "附魔" : enchantReady ? "附魔" : chain.Remaining.ToString("0.0") + "s";
        enchantFill.fillAmount = chain != null && chain.State != GeneChainController.Phase.Ready ? 1 - chain.Progress : 0;
        attribute.text = chain != null && chain.ActiveAttribute != null ? chain.ActiveAttribute.Name : "基础水弹";
        phase.text = chain == null ? "" : chain.State == GeneChainController.Phase.Reloading ? "重载中" : chain.ActiveAttribute != null ? "附魔中" : enchantReady ? "属性就绪" : "收集基因";
        for (int i = 0; i < 6; i++)
        {
            var gene = inventory != null && i < inventory.GenChain.Count ? inventory.GenChain[i] : null;
            slots[i].color = gene != null && chain != null && chain.ActiveAttribute == gene ? PlayerUIStyle.Mint : WaterDrainUI.Border;
            icons[i].enabled = gene != null; icons[i].sprite = gene != null ? PlayerUIStyle.GeneIcon(gene) : null; icons[i].color = PlayerUIStyle.GeneColor(gene);
        }
        var near = interaction != null ? interaction.Nearby : null;
        InteractButton.Available = near != null; interactLabel.color = near != null ? PlayerUIStyle.Ink : WaterDrainUI.Muted;
        nearCard.gameObject.SetActive(near != null); if (near != null) nearLabel.text = near.Title + " · 点击交互";
        toastCard.gameObject.SetActive(Time.unscaledTime < toastUntil);
    }
    private void OnApplicationFocus(bool focus)
    { if (!focus) SuspendForBackground(); }
    private void OnApplicationPause(bool pause) { if (pause) SuspendForBackground(); }
    private void SuspendForBackground()
    { MobileGameInput.Reset(); MoveStick?.Release(); ScreenAim?.Release(); if (Player != null && !Player.MenuOpen && !Player.IsDead) desktopHud.runMenu?.Open(Player, false); }
    private void OnDisable() { MobileGameInput.Reset(); }
    private void OnDestroy() { if (inventory != null) inventory.Acquired -= Acquired; if (Current == this) Current = null; }
}
