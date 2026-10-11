using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GeneInventory))]
public class GeneInventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("应用当前基因链"))
            ((GeneInventory)target).Apply();
        GUI.enabled = true;
    }
}
