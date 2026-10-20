using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>Explicit, reversible import migration; retains source images, GUIDs and scene layout.</summary>
[InitializeOnLoad]
public static class ReleaseAssetOptimization
{
    private const string Folder="Temp/ReleasePerformance/";
    private static double nextPoll;
    [Serializable] private sealed class Result
    {
        public bool success;public int actorTextures,backdrops,spritesVerified;
        public string previousCompressionOverride,error;public int previousSizeOverride;public string[] atlases;
    }
    static ReleaseAssetOptimization(){EditorApplication.update+=Poll;}
    private static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        nextPoll=EditorApplication.timeSinceStartup+.5;
        if(!File.Exists(Folder+"apply-request.txt"))return;
        File.Delete(Folder+"apply-request.txt");Apply();
    }
    [MenuItem("Tools/TapGJ/Performance/Optimize Release Assets")]
    public static void Apply()
    {
        var result=new Result();Directory.CreateDirectory(Folder);
        try
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first.");
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save scene edits first.");
            ReleasePerformanceAudit.Export("before-settings");
            var roots=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)
                .Concat(AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/")&&p.Contains("/Resources/")&&!AssetDatabase.IsValidFolder(p))).ToArray();
            var needed=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
            var actors=AssetDatabase.GetAllAssetPaths().Where(p=>p.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&
                (p.StartsWith("Assets/ArtRes/Monster/")||p.StartsWith("Assets/ArtRes/Animation/LegacyMonster/")||
                 p.StartsWith("Assets/ArtRes/Animation/Player/"))).ToArray();
            var backdrops=new[]{"Assets/ArtRes/WorldPolished/MenuBackground.png","Assets/ArtRes/WorldPolished/CavernBackdrop.png"};
            foreach(string path in actors)
            {
                string backup=Folder+"ImportBackup/"+path+".meta";
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if(!File.Exists(backup))File.Copy(path+".meta",backup);
            }
            foreach(string path in backdrops)
            {
                string backup=Folder+"ImportBackup/"+path+".meta";Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if(!File.Exists(backup))File.Copy(path+".meta",backup);
            }
            result.previousCompressionOverride=EditorUserBuildSettings.overrideTextureCompression.ToString();
            result.previousSizeOverride=EditorUserBuildSettings.overrideMaxTextureSize;
            // Fast-import overrides must not silently defeat the authored release compression.
            EditorUserBuildSettings.overrideTextureCompression=OverrideTextureCompression.NoOverride;
            EditorUserBuildSettings.overrideMaxTextureSize=0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach(string path in actors){ConfigureTexture(path,512);result.actorTextures++;}
                foreach(string path in backdrops){ConfigureTexture(path,2048);result.backdrops++;}
            }
            finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in actors)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.GetSourceTextureWidthAndHeight(out int sourceWidth,out int sourceHeight);
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                // Tight mesh bounds follow the resampled alpha contour. Verify the full animation
                // frame and pivot instead, allowing two source pixels of resize rounding.
                var expected=new Vector2(sourceWidth,sourceHeight)/importer.spritePixelsPerUnit;
                var actual=sprite!=null?sprite.rect.size/sprite.pixelsPerUnit:Vector2.zero;
                float tolerance=2f/importer.spritePixelsPerUnit;
                if(sprite==null||Vector2.Distance(actual,expected)>tolerance)
                    throw new InvalidOperationException("Sprite frame size changed: "+path+" expected="+expected+" actual="+actual);
                var pivot=sprite.pivot/sprite.rect.size;
                if(Vector2.Distance(pivot,importer.spritePivot)>.002f)
                    throw new InvalidOperationException("Sprite pivot changed: "+path);
                result.spritesVerified++;
            }
            var enemies=actors.Where(p=>needed.Contains(p)&&!p.StartsWith("Assets/ArtRes/Animation/Player/")).ToArray();
            var player=actors.Where(p=>needed.Contains(p)&&p.StartsWith("Assets/ArtRes/Animation/Player/")).ToArray();
            var atlases=new List<string>();
            if(enemies.Length>0)atlases.Add(CreateAtlas("Enemies",enemies));
            if(player.Length>0)atlases.Add(CreateAtlas("PlayerLegacy",player));
            result.atlases=atlases.ToArray();
            SpriteAtlasUtility.PackAtlases(atlases.Select(AssetDatabase.LoadAssetAtPath<SpriteAtlas>).ToArray(),EditorUserBuildSettings.activeBuildTarget,false);
            BakeRoom();AssetDatabase.SaveAssets();result.success=true;
            ReleasePerformanceAudit.Export("after");
        }
        catch(Exception e){result.error=e.ToString();Debug.LogError("Release optimization: "+e.Message);}
        File.WriteAllText(Folder+"migration.json",JsonUtility.ToJson(result,true));
    }
    private static void ConfigureTexture(string path,int maxSize)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize=maxSize;importer.isReadable=false;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.crunchedCompression=false;
        var desktop=importer.GetPlatformTextureSettings("Standalone");desktop.overridden=true;desktop.maxTextureSize=maxSize;
        desktop.format=TextureImporterFormat.BC7;desktop.textureCompression=TextureImporterCompression.CompressedHQ;
        desktop.compressionQuality=100;desktop.crunchedCompression=false;importer.SetPlatformTextureSettings(desktop);
        AssetDatabase.WriteImportSettingsIfDirty(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    }
    private static string CreateAtlas(string name,string[] paths)
    {
        string path="Assets/ArtRes/Atlases/"+name+".spriteatlasv2";
        var existing=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
        var atlas=existing!=null?SpriteAtlasAsset.Load(path):new SpriteAtlasAsset();
        if(existing!=null)atlas.Remove(existing.GetPackables());
        atlas.Add(paths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).Cast<UnityEngine.Object>().ToArray());
        SpriteAtlasAsset.Save(atlas,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);
        var packing=importer.packingSettings;packing.enableRotation=false;packing.enableTightPacking=true;packing.padding=4;importer.packingSettings=packing;
        var textures=importer.textureSettings;textures.generateMipMaps=false;textures.readable=false;textures.filterMode=FilterMode.Bilinear;importer.textureSettings=textures;
        var defaults=importer.GetPlatformSettings("DefaultTexturePlatform");defaults.maxTextureSize=2048;
        defaults.textureCompression=TextureImporterCompression.CompressedHQ;importer.SetPlatformSettings(defaults);
        var desktop=importer.GetPlatformSettings("Standalone");desktop.overridden=true;desktop.maxTextureSize=2048;
        desktop.format=TextureImporterFormat.BC7;desktop.textureCompression=TextureImporterCompression.CompressedHQ;desktop.compressionQuality=100;
        importer.SetPlatformSettings(desktop);importer.includeInBuild=true;importer.SaveAndReimport();return path;
    }
    private static void BakeRoom()
    {
        const string path="Assets/Resources/Audio/WaterDrain/cavern_ambience.wav";
        if(!File.Exists(path))
        {
            var method=typeof(GameAudioPresentation).GetMethod("CreateRoom",BindingFlags.Static|BindingFlags.NonPublic);
            var clip=(AudioClip)method.Invoke(null,null);
            try
            {
                var samples=new float[clip.samples*clip.channels];clip.GetData(samples,0);
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    int bytes=samples.Length*2;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+bytes);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)clip.channels);
                    writer.Write(clip.frequency);writer.Write(clip.frequency*clip.channels*2);writer.Write((short)(clip.channels*2));writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(bytes);
                    foreach(float sample in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample,-1,1)*32767));
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(clip);}
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.CompressedInMemory;settings.compressionFormat=AudioCompressionFormat.Vorbis;
        settings.quality=.45f;settings.preloadAudioData=true;importer.defaultSampleSettings=settings;
        importer.forceToMono=true;importer.loadInBackground=true;importer.SaveAndReimport();
    }
}
