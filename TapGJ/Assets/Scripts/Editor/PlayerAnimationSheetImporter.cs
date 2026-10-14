using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Unpacks the generated atlas into equal canvases with a common foot anchor.</summary>
public static class PlayerAnimationSheetImporter
{
    [MenuItem("Tools/TapGJ/Player/Slice Approved Character Sheet")]
    public static void Slice()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        const string source = "Assets/ArtRes/Reference/Player/Player-polished-sheet.png";
        const int canvas = 320, foot = 32;
        Texture2D sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        ImageConversion.LoadImage(sheet, File.ReadAllBytes(source));
        // The generator lays poses in rows with unequal whitespace; boundaries are verified on the source atlas.
        int[] columns = {0, 320, 640, 940, 1254};
        int[] rows = {0, 320, 600, 930, 1254};
        if (sheet.width != 1254 || sheet.height != 1254) throw new InvalidOperationException("Source atlas dimensions changed; review slice boundaries.");
        Color32[] pixels = sheet.GetPixels32();
        string[] states = {"Idle", "Idle", "Idle", "Idle", "Run", "Run", "Run", "Run",
            "Jump", "Jump", "Fall", "Land", "Attack", "Hurt", "Roll", "Dead"};
        string previous = ""; int index = 0;
        try
        {
            for (int cell = 0; cell < 16; cell++)
            {
                int row = cell / 4, col = cell % 4;
                int x0 = columns[col], x1 = columns[col + 1];
                int y0 = sheet.height - rows[row + 1], y1 = sheet.height - rows[row];
                int minX = x1, maxX = x0, minY = y1, maxY = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * sheet.width + x].a > 64)
                    { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                int width = maxX - minX + 1, height = maxY - minY + 1;
                if (width <= 0 || width > canvas - 8 || height > canvas - foot - 4)
                    throw new InvalidOperationException("Pose cannot fit shared canvas: " + cell);
                Color32[] framePixels = new Color32[canvas * canvas];
                int left = (canvas - width) / 2;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    framePixels[(foot + y) * canvas + left + x] = pixels[(minY + y) * sheet.width + minX + x];
                Texture2D frame = new Texture2D(canvas, canvas, TextureFormat.RGBA32, false);
                frame.SetPixels32(framePixels); frame.Apply();
                string state = states[cell]; if (state != previous) { previous = state; index = 0; }
                string folder = PlayerAnimationPolish.Artwork + "/" + state;
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(folder + "/" + state + "_" + index.ToString("00") + ".png", frame.EncodeToPNG());
                index++; UnityEngine.Object.DestroyImmediate(frame);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(sheet); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Approved player atlas sliced: 16 poses, 320px canvases, common 32px foot baseline.");
    }
}
