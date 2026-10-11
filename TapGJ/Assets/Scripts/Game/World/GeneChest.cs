using UnityEngine;

public class GeneChest : WorldInteractable
{
    public GeneRules rules;
    public AudioClip openingSound;
    public bool Opened { get; private set; }
    public override string Title => "基因冷库";
    public override string Detail => "开启后获得两份基因";
    public override bool CanUse(PlayerController player) => !Opened && base.CanUse(player);
    public override bool Interact(PlayerController player)
    {
        if (!CanUse(player) || rules == null || rules.genes == null || rules.genes.Length == 0) return false;
        Opened = true;
        DropService.Spawn(rules.genes, 2, transform.position, rules.interactionRange);
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(transform.position, Color.white, 1f, Vector2.up);
        PlayOpeningSound();
        SpriteRenderer visual = GetComponent<SpriteRenderer>();
        if (visual != null) visual.color = new Color(0.4f, 0.45f, 0.45f);
        return true;
    }
    private void PlayOpeningSound()
    {
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
