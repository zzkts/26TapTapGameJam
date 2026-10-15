using UnityEngine;

public class GeneChest : WorldInteractable
{
    [SerializeField] private Modifier[] genePool;
    [SerializeField] private float pickupRange = 1.6f;
    public AudioClip openingSound;
    public bool offerChoice;
    public bool Opened { get; private set; }
    public override string Title => "基因冷库";
    public override string Detail => offerChoice ? "三种变化中选一种 · 获得两份样本" : "开启后获得两份基因";
    public override bool CanUse(PlayerController player) => !Opened && base.CanUse(player) && CompleteRewardState.IsAvailable(this);
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || genePool == null || genePool.Length == 0) return false;
        if (offerChoice) { GeneDraftPanel.Ensure().Open(player,this,genePool); return true; }
        Opened = true;
        DropService.Spawn(genePool, 2, transform.position, pickupRange);
        CompleteRewardState.Claim(this);
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, Color.white, 1f, Vector2.up);
        PlayOpeningSound();
        SpriteRenderer visual = GetComponentInChildren<SpriteRenderer>();
        if (visual != null) visual.color = new Color(0.4f, 0.45f, 0.45f);
        return true;
    }
    public bool ChooseReward(PlayerController player,Modifier chosen)
    {
        if (!CanUse(player) || chosen == null || System.Array.IndexOf(genePool,chosen) < 0) return false;
        Opened=true;
        player.Genes.Acquire(chosen);player.Genes.Acquire(chosen);
        CompleteRewardState.Claim(this);PlayOpeningSound();
        SpriteRenderer visual=GetComponentInChildren<SpriteRenderer>();if(visual!=null)visual.color=new Color(.4f,.45f,.45f);
        return true;
    }
    private void PlayOpeningSound()
    {
        if(openingSound==null&&GameAudioPresentation.PlayCue("chest"))return;
        AudioClip clip = openingSound;
        bool generated = clip == null;
        if (generated)
        {
            const int samples = 5600;
            var noise = new float[samples];
            var random = new System.Random(17);
            float smooth = 0f;
            for (int i = 0; i < samples; i++)
            {
                smooth = Mathf.Lerp(smooth, (float)random.NextDouble() * 2f - 1f, 0.25f);
                noise[i] = smooth * (1f - i / (float)samples) * 0.3f;
            }
            clip = AudioClip.Create("Cold Storage Steam", samples, 1, 16000, false);
            clip.SetData(noise, 0);
        }
        var audioObject = new GameObject("Cold Storage Audio");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.volume = PlayerPrefs.GetFloat("Audio.GameVolume", 1f);
        source.clip = clip;
        source.Play();
        Destroy(audioObject, clip.length + 0.1f);
        if (generated) Destroy(clip, clip.length + 0.1f);
    }
}
