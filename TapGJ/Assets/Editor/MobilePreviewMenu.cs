using UnityEditor;
using UnityEngine;

/// <summary>Editor preview only. Android/iOS select touch controls automatically.</summary>
[InitializeOnLoad]
public static class MobilePreviewMenu
{
    private const string Item = "Tools/TapGJ/Mobile/Touch Controls Preview (Next Play)";
    static MobilePreviewMenu()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            // Restore while still playing: enabling URP debugging creates a runtime GameObject.
            if(state!=PlayModeStateChange.ExitingPlayMode||!SessionState.GetBool("MobilePreview.DebugCaptured",false))return;
            UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI=SessionState.GetBool("MobilePreview.PreviousRuntimeDebug",true);
            SessionState.SetBool("MobilePreview.DebugCaptured",false);
        };
    }
    [MenuItem(Item)]
    public static void Toggle()
    {
        SessionState.SetBool("WaterDrain.TouchPreview", !SessionState.GetBool("WaterDrain.TouchPreview", false));
        Debug.Log("Water Drain: next Play uses " + (MobileGameInput.Enabled ? "mobile touch controls" : "desktop keyboard/mouse") + ".");
    }
    [MenuItem(Item, true)]
    private static bool Validate()
    { Menu.SetChecked(Item, SessionState.GetBool("WaterDrain.TouchPreview", false)); return !EditorApplication.isPlayingOrWillChangePlaymode; }
}
