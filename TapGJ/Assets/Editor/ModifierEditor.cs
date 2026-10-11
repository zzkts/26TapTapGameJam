using UnityEditor;
using UnityEngine;

/// <summary>
/// 基因配置的 Inspector。
/// 自动分配基因 ID，并把 ID 作为只读信息显示。
/// </summary>
[CustomEditor(typeof(Modifier))]
public class ModifierEditor : Editor
{
    private void OnEnable()
    {
        ModifierIdUtility.AssignIfNeeded((Modifier)target);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("geneId"), new GUIContent("Gene Id"));

        DrawPropertiesExcluding(serializedObject, "m_Script", "geneId");
        serializedObject.ApplyModifiedProperties();
    }
}

public class ModifierIdPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        for (int i = 0; i < importedAssets.Length; i++)
        {
            Modifier modifier = AssetDatabase.LoadAssetAtPath<Modifier>(importedAssets[i]);
            if (modifier != null)
                ModifierIdUtility.AssignIfNeeded(modifier);
        }
    }
}

public static class ModifierIdUtility
{
    private static bool assigning;

    public static void AssignIfNeeded(Modifier modifier)
    {
        if (assigning)
            return;

        SerializedObject target = new(modifier);
        SerializedProperty id = target.FindProperty("geneId");
        uint nextId = 1;
        bool duplicate = false;
        string[] guids = AssetDatabase.FindAssets("t:Modifier");
        for (int i = 0; i < guids.Length; i++)
        {
            Modifier other = AssetDatabase.LoadAssetAtPath<Modifier>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (other == null || other == modifier)
                continue;

            SerializedObject serialized = new(other);
            uint otherId = (uint)serialized.FindProperty("geneId").longValue;
            if (otherId == id.longValue && id.longValue != 0)
                duplicate = true;
            if (otherId >= nextId)
                nextId = otherId + 1;
        }

        if (id.longValue != 0 && !duplicate)
            return;

        assigning = true;
        id.longValue = nextId;
        target.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(modifier);
        assigning = false;
    }
}
