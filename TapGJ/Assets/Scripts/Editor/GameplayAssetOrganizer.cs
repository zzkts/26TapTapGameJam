using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GameplayAssetOrganizer
{
    [MenuItem("Tools/TapGJ/Organize Gameplay Art")]
    public static void Organize()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before organizing assets.");
        string[] guids = AssetDatabase.FindAssets("", new[] {"Assets/ArtRes"})
            .Where(g => !AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Move("Assets/ArtRes/999/Animation/Player", "Assets/ArtRes/Animation/Player");
        Move("Assets/ArtRes/999/Animation/Monster", "Assets/ArtRes/Animation/LegacyMonster");
        Move("Assets/ArtRes/999/Front", "Assets/ArtRes/Fonts");
        Move("Assets/ArtRes/999/Sprite/MapSprite", "Assets/ArtRes/Sprite/Map");
        Move("Assets/ArtRes/999/Sprite/BeginSprite", "Assets/ArtRes/UI/Menu");
        Move("Assets/ArtRes/999/GeneratedTiles", "Assets/ArtRes/Tiles");
        Move("Assets/ArtRes/999/Player", "Assets/ArtRes/Reference/Player");
        Move("Assets/ArtRes/GameUi/Image/UI/GenUi", "Assets/ArtRes/UI/Genes");
        Move("Assets/ArtRes/Monster/3", "Assets/ArtRes/Monster/Melee/Monster3");
        Move("Assets/ArtRes/Monster/5", "Assets/ArtRes/Monster/Melee/Monster5");
        Move("Assets/ArtRes/Monster/4", "Assets/ArtRes/Monster/Support/Monster4");
        Move("Assets/ArtRes/Monster/RemoteMonster/6", "Assets/ArtRes/Monster/Ranged/Monster6");
        Move("Assets/ArtRes/Monster/RemoteMonster/7", "Assets/ArtRes/Monster/Ranged/Monster7");
        RemoveEmpty("Assets/ArtRes/999"); RemoveEmpty("Assets/ArtRes/GameUi"); RemoveEmpty("Assets/ArtRes/Monster/RemoteMonster");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        string[] missing = guids.Where(g => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        if (missing.Length != 0) throw new InvalidOperationException("Lost art asset GUIDs: " + string.Join(",", missing));
        Debug.Log("Gameplay art organized; preserved " + guids.Length + " asset GUIDs.");
    }
    private static void Validate(string path)
    {
        string root = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
        string absolute = Path.GetFullPath(path);
        if (!absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Outside Assets: " + path);
    }
    private static void Move(string source, string destination)
    {
        Validate(source); Validate(destination);
        if (!AssetDatabase.IsValidFolder(source)) return;
        if (AssetDatabase.IsValidFolder(destination)) throw new InvalidOperationException("Destination already exists: " + destination);
        GameplayPolishSetup.EnsureFolder(Path.GetDirectoryName(destination).Replace('\\','/'));
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
    }
    private static void RemoveEmpty(string path)
    {
        Validate(path); if (!AssetDatabase.IsValidFolder(path)) return;
        foreach (string child in Directory.GetDirectories(path)) RemoveEmpty(child.Replace('\\','/'));
        if (Directory.GetFileSystemEntries(path).Any()) return;
        if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("Cannot remove empty folder: " + path);
    }
}
