using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Targeted migrations; never rebuild the level to change its text or packing settings.</summary>
public static class PerformancePolishSetup
{
    public static void StyleSign(Canvas canvas, string text)
    {
        var rect = (RectTransform)canvas.transform;
        rect.localScale = Vector3.one * .012f;
        rect.sizeDelta = new Vector2(310, 64);
        canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 15;
        var label = canvas.GetComponentInChildren<Text>();
        if (label == null) label = GameplayUIFactory.Label("Text",rect,Vector2.zero,Vector2.one,
            AssetDatabase.LoadAssetAtPath<Font>("Assets/ArtRes/Fonts/PlayerUI/NotoSansCJKsc-Medium.otf"),18,text,TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.rectTransform.anchoredPosition3D = Vector3.zero;
        label.text = text; label.fontSize = 18; label.color = PlayerUIStyle.Mint;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true; label.resizeTextMinSize = 15; label.resizeTextMaxSize = 18;
        label.raycastTarget = false;
        if(canvas.GetComponent<WorldSign>()==null)canvas.gameObject.AddComponent<WorldSign>();
        EditorUtility.SetDirty(label); EditorUtility.SetDirty(canvas);
    }
    [MenuItem("Tools/TapGJ/Polish/Repair Prompt Layouts")]
    public static void RepairSceneText()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (canvas.renderMode != RenderMode.WorldSpace || !canvas.name.StartsWith("路标 · ")) continue;
            var text = canvas.GetComponentInChildren<Text>();
            string value = text.text.Split(new[]{" · "},StringSplitOptions.None)[0];
            if(value=="雨后泵站"||value=="共生菌林"||value=="净化核心")
            {
                float origin = value=="雨后泵站"?0:value=="共生菌林"?48:96;
                canvas.transform.position = new Vector3(origin+1,4.5f,0);
            }
            StyleSign(canvas,value);
        }
        foreach(var hud in Object.FindObjectsByType<PlayerHudPanel>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            InteractionPromptLayout.Apply(hud.interactionCard,hud.interactionText,"附近交互","按 E 操作",448);
            hud.interactionCard.gameObject.SetActive(false);
            EditorUtility.SetDirty(hud);EditorUtility.SetDirty(hud.interactionCard);EditorUtility.SetDirty(hud.interactionText);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    [MenuItem("Tools/TapGJ/Polish/Create Gameplay Atlases")]
    public static void CreateAtlases()
    {
        const string folder="Assets/ArtRes/Atlases";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/ArtRes","Atlases");
        EditorSettings.spritePackerMode=SpritePackerMode.SpriteAtlasV2;
        string[] props={"rock_fill","rock_top_moss","metal_deck","pipe_horizontal","pipe_elbow","crystal","mushroom","gene_terminal","supply_crate","core_gate"};
        Make("World",props.Select(n=>"Assets/ArtRes/WorldPolished/"+n+".png").ToArray(),2048);
        Make("Player",AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/ArtRes/Animation/PlayerPolished"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".png")).ToArray(),2048);
        Make("PlayerHUD",AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/ArtRes/UI/Player"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".png")&&!p.Contains("-source")).ToArray(),2048);
        AssetDatabase.SaveAssets();
    }
    private static void Make(string name,string[] paths,int maxSize)
    {
        string path="Assets/ArtRes/Atlases/"+name+".spriteatlasv2";
        var existing=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        var atlas=existing!=null?SpriteAtlasAsset.Load(path):new SpriteAtlasAsset();
        if(existing!=null)atlas.Remove(existing.GetPackables());
        // Explicit packables keep backgrounds, fonts and shader full-UV quads out of the atlases.
        atlas.Add(paths.Select(p=>AssetDatabase.LoadAssetAtPath<Texture2D>(p)).Where(t=>t!=null).Cast<Object>().ToArray());
        SpriteAtlasAsset.Save(atlas,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        var packing=importer.packingSettings;packing.enableRotation=false;packing.enableTightPacking=false;packing.padding=4;importer.packingSettings=packing;
        var settings=importer.textureSettings;settings.generateMipMaps=false;settings.readable=false;settings.filterMode=FilterMode.Bilinear;importer.textureSettings=settings;
        var platform=importer.GetPlatformSettings("DefaultTexturePlatform");platform.maxTextureSize=maxSize;
        platform.textureCompression=TextureImporterCompression.Compressed;platform.compressionQuality=100;importer.SetPlatformSettings(platform);
        var desktop=importer.GetPlatformSettings("Standalone");desktop.overridden=true;desktop.maxTextureSize=maxSize;
        desktop.format=TextureImporterFormat.BC7;desktop.textureCompression=TextureImporterCompression.Compressed;desktop.compressionQuality=100;
        importer.SetPlatformSettings(desktop);importer.includeInBuild=true;importer.SaveAndReimport();
    }
}
