using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Keeps each GeneChest's picture where the artist placed it, and moves the
/// interaction root (the object carrying GeneChest) underneath that picture.
/// Interaction detection is a distance check from the root transform, so the
/// root and the visual must share the same x.
/// </summary>
public static class GeneChestVisualAligner
{
    private const float VisualCenterHeight = 0.5f;

    [MenuItem("Tools/TapGJ/对齐冷库（保留图像位置）")]
    public static void Align()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("请先退出 Play Mode。");

        var report = new StringBuilder();
        int changed = 0;
        foreach (GeneChest chest in Object.FindObjectsByType<GeneChest>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform visual = FindVisual(chest.transform);
            if (visual == null)
            {
                Debug.LogWarning($"[GeneChestAligner] {chest.name} 下没有找到图像子物体，已跳过。", chest);
                continue;
            }

            Vector3 oldLocal = visual.localPosition;
            Vector3 newLocal = new Vector3(0f, VisualCenterHeight, oldLocal.z);
            // Move the root by the world-space difference so the picture does not move.
            Vector3 worldDelta = chest.transform.TransformVector(oldLocal - newLocal);
            if (worldDelta.sqrMagnitude < 1e-8f)
                continue;

            Undo.RecordObject(chest.transform, "Align GeneChest");
            Undo.RecordObject(visual, "Align GeneChest Visual");
            chest.transform.position += worldDelta;
            visual.localPosition = newLocal;
            EditorUtility.SetDirty(chest);
            EditorUtility.SetDirty(visual);
            changed++;

            report.AppendLine($"  {chest.name}: 图像 local {oldLocal} -> {newLocal}，" +
                              $"根物体移动到 {chest.transform.position}");
        }

        if (changed == 0)
        {
            Debug.Log("[GeneChestAligner] 所有冷库的根物体与图像已经对齐，无需修改。");
            return;
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[GeneChestAligner] 已对齐 {changed} 个冷库（图像世界位置未变）：\n{report}");
    }

    private static Transform FindVisual(Transform chest)
    {
        Transform fallback = null;
        for (int i = 0; i < chest.childCount; i++)
        {
            Transform child = chest.GetChild(i);
            bool hasSprite = child.GetComponent<SpriteRenderer>() != null;
            if (!hasSprite) continue;
            if (child.name.Contains("图像") || child.name == "World Visual")
                return child;
            if (fallback == null) fallback = child;
        }
        return fallback;
    }
}
