using TMPro;
using UnityEngine;

namespace Tool.ComponentTool
{
    public static class ComponentTool
    {
        public static void Change_Text(GameObject obj, string text)
        {
            if (!obj.TryGetComponent(out TMP_Text textMesh))
            {
                Debug.LogWarning($"{obj.name} doesn't have a TMP_Text");
            }
            textMesh.text = text;
        }
    }
}