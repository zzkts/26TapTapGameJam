#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Builds the playable character from the approved, consistently aligned artwork.</summary>
public static class PlayerAnimationPolish
{
    public const string Artwork = "Assets/ArtRes/Animation/PlayerPolished";
    public const string AnimationFolder = "Assets/ArtRes/Animation/Gameplay/Player";
    private const string PrefabPath = "Assets/Resources/Prefab/Player/Player.prefab";
    private static readonly string[] StateNames = { "Idle", "move", "Jump", "Fall", "Land", "Attack", "Hurt", "Roll", "Dead" };
    private static readonly Dictionary<string, string> SourceFolders = new()
    {
        { "Idle", "Idle" }, { "move", "Run" }, { "Jump", "Jump" }, { "Fall", "Fall" },
        { "Land", "Land" }, { "Attack", "Attack" }, { "Hurt", "Hurt" }, { "Roll", "Roll" }, { "Dead", "Dead" }
    };

    [MenuItem("Tools/TapGJ/Player/Apply Character Animation Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before updating character assets.");
        foreach (string state in StateNames)
            if (!Directory.Exists(Artwork + "/" + SourceFolders[state]) || Frames(state).Length == 0)
                throw new InvalidOperationException("Missing approved character frames: " + state);
        if (!Directory.Exists(AnimationFolder)) Directory.CreateDirectory(AnimationFolder);
        float baseline = ReadFootBaseline(Frames("Idle")[0], out int canvasWidth, out int canvasHeight);
        var sprites = new Dictionary<string, Sprite[]>();
        foreach (string state in StateNames)
            sprites[state] = Frames(state).Select(path => ImportSprite(path, baseline, canvasWidth, canvasHeight)).ToArray();

        string controllerPath = AnimationFolder + "/Player.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        for (int i = 0; i < StateNames.Length; i++)
        {
            string name = StateNames[i];
            AnimationClip clip = BuildClip(name, sprites[name]);
            AnimatorState state = machine.states.Select(child => child.state).FirstOrDefault(value => value.name == name);
            if (state == null) state = machine.AddState(name, new Vector3(240f + i % 3 * 200f, i / 3 * 100f));
            state.motion = clip; state.speed = 1f; state.writeDefaultValues = false;
            // Code owns action priority, so one-shots must not transition back to Idle on their own.
            foreach (AnimatorStateTransition transition in state.transitions) state.RemoveTransition(transition);
            if (name == "Idle") machine.defaultState = state;
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller);
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureVisual(prefab.GetComponent<PlayerController>());
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        Debug.Log("Player animation polish ready: " + string.Join(", ", sprites.Select(pair => pair.Key + " " + pair.Value.Length + " frames")));
    }

    public static void ConfigureVisual(PlayerController player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        Animator animator = player.GetComponentInChildren<Animator>(true);
        SpriteRenderer renderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;
        if (renderer == null) throw new InvalidOperationException("Player artwork must be a child SpriteRenderer with an Animator.");
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Frames("Idle")[0]);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationFolder + "/Player.controller");
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;
        BoxCollider2D collider = player.GetComponent<BoxCollider2D>();
        float feet = collider != null ? collider.offset.y - collider.size.y * .5f : -.9f;
        renderer.transform.localPosition = new Vector3(0f, feet, 0f);
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one;
        renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, 7);
        if (player.GetComponent<PlayerPresentation>() == null) player.gameObject.AddComponent<PlayerPresentation>();
        var serialized = new SerializedObject(player);
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.FindProperty("sr").objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(animator); EditorUtility.SetDirty(player);
    }

    private static string[] Frames(string state) => Directory.GetFiles(Artwork + "/" + SourceFolders[state], "*.png")
        .OrderBy(path => Regex.Replace(Path.GetFileName(path), @"\d+", match => match.Value.PadLeft(6, '0')))
        .Select(path => path.Replace('\\', '/')).ToArray();

    private static float ReadFootBaseline(string path, out int width, out int height)
    {
        Texture2D temporary = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(temporary, File.ReadAllBytes(path))) throw new InvalidOperationException("Invalid character PNG: " + path);
            width = temporary.width; height = temporary.height;
            Color32[] pixels = temporary.GetPixels32();
            // All cells share a foot line; use the first approved Idle frame to retain that fixed pivot.
            for (int y = 0; y < height; y++)
            {
                int solidPixels = 0;
                for (int x = width / 4; x < width * 3 / 4; x++)
                    if (pixels[y * width + x].a > 64) solidPixels++;
                if (solidPixels > width / 32) return (float)y / height;
            }
            throw new InvalidOperationException("Character PNG has no visible feet: " + path);
        }
        finally { Object.DestroyImmediate(temporary); }
    }

    private static Sprite ImportSprite(string path, float footBaseline, int width, int height)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { AssetDatabase.ImportAsset(path); importer = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (importer == null) throw new InvalidOperationException("Unimported character PNG: " + path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        TextureImporterSettings settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, footBaseline);
        settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = width / 2f; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(width, height));
        importer.SaveAndReimport();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null || Mathf.Abs(sprite.rect.width - width) > .1f || Mathf.Abs(sprite.rect.height - height) > .1f)
            throw new InvalidOperationException("Character animation frames need a consistent canvas: " + path);
        return sprite;
    }

    private static AnimationClip BuildClip(string name, Sprite[] frames)
    {
        string path = AnimationFolder + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
        clip.ClearCurves(); clip.name = name; clip.frameRate = 12f;
        float duration = Mathf.Max(frames.Length / 12f,
            name == "Attack" ? .15f : name == "Hurt" ? .22f : name == "Roll" ? .2f : name == "Dead" ? .7f : 0f);
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / 12f, value = frames[i] };
        keys[frames.Length] = new ObjectReferenceKeyframe { time = duration, value = frames[frames.Length - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip,
            new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" }, keys);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = name == "Idle" || name == "move" || name == "Fall";
        settings.startTime = 0f; settings.stopTime = duration;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip); return clip;
    }
}
#endif
