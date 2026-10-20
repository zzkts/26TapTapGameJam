#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in cleanup smoke test; the existing isolated-save bootstrap protects real progress.</summary>
public sealed class AssetCleanupSmoke : MonoBehaviour
{
    [Serializable] private sealed class Report { public bool success; public string[] checks; }
    private readonly List<string> checks = new();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartRequestedCheck()
    {
        if (!SessionState.GetBool("AssetCleanup.Smoke", false)) return;
        SessionState.SetBool("AssetCleanup.Smoke", false);
        var host = new GameObject("Asset cleanup smoke");
        DontDestroyOnLoad(host); host.AddComponent<AssetCleanupSmoke>().StartCoroutine("Run");
    }
    private IEnumerator Run()
    {
        var routine = Cases();
        while (true)
        {
            object wait;
            try { if (!routine.MoveNext()) break; wait = routine.Current; }
            catch (Exception e) { checks.Add("FAIL: " + e); break; }
            yield return wait;
        }
        Check(PlayerPrefs.GetString(RunProgress.SaveKey, "") == SessionState.GetString("AssetCleanup.OriginalSave", "missing"), "Real save unchanged");
        Check(PlayerPrefs.GetInt(RunProgress.EndingKey, 0) == SessionState.GetInt("AssetCleanup.OriginalEndings", -1), "Ending history unchanged");
        var report = new Report { success = checks.All(c => c.StartsWith("PASS:")), checks = checks.ToArray() };
        Directory.CreateDirectory("Temp/AssetCleanup");
        File.WriteAllText("Temp/AssetCleanup/smoke.json", JsonUtility.ToJson(report, true));
        SessionState.SetBool("AssetCleanup.SmokeFinished", true);
        EditorApplication.isPlaying = false;
    }
    private IEnumerator Cases()
    {
        yield return null; yield return null;
        Check(RunProgress.IsolatedValidation, "In-memory save isolation active");
        if (!RunProgress.IsolatedValidation) yield break;
        var begin = FindAnyObjectByType<BeginPanel>();
        Check(begin != null && begin.btnBegin != null && begin.btnSetting != null, "BeginScene menu controls load");
        Check(GameAudioPresentation.Current != null && GameAudioPresentation.Current.LoadedClipCount == GameAudioPresentation.CueNames.Length, "All gameplay sound cues load");
        var music = FindAnyObjectByType<SceneMusicPlayer>();
        Check(music != null && music.ActiveClip != null && music.ActiveClip.name == "baby_black_magi_loop", "Menu music loads");
        begin.btnBegin.onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && (SceneManager.GetActiveScene().name != "GameScene" || StorySession.Current == null || !StorySession.Current.Ready)) yield return null;
        Check(SceneManager.GetActiveScene().name == "GameScene" && StorySession.Current != null && StorySession.Current.Ready, "Main menu enters ready GameScene");
        StorySession.Current.SetValidationMode(true);
        var player = FindAnyObjectByType<PlayerController>(); player.SetInvincible(true);
        Check(player != null && player.Genes != null && player.Weapon != null, "Player, gene inventory and weapon load");
        Check(UIManager.Instance.GetPanel<PlayerHudPanel>() != null && UIManager.Instance.GetPanel<GeneEditPanel>() != null, "Gameplay HUD and gene editor load");
        var healer = Resources.Load<GameObject>("Prefab/Enemy/Support/Monster4");
        Check(healer != null && healer.GetComponent<EnemySupportAura>() != null, "Monster4 healing support prefab retained");
        Check(Resources.LoadAll<Modifier>("SO/Genes").Length >= 11, "Gene definitions remain available");
        Check(TapGJ.WaterProjectiles.WaterVfx.GetLiquidMaterial(4) != null, "Ground water footprint material loads");
        music = FindAnyObjectByType<SceneMusicPlayer>();
        Check(music != null && music.ActiveClip != null && music.ActiveClip.name == "chee_zee_lab", "GameScene music loads");
        var menu = UIManager.Instance.GetPanel<RunMenuPanel>(); menu.Open(player, false);
        menu.settingsButton.onClick.Invoke();
        var settings = UIManager.Instance.GetPanel<SettingPanel>();
        Check(settings != null && settings.gameObject.activeSelf && settings.sliderBk != null && settings.GetComponent<MusicCreditsPanel>() != null, "Pause, settings and music credits load");
        UIManager.Instance.HidePanel<SettingPanel>(false); menu.menuButton.onClick.Invoke();
        deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "BeginScene") yield return null;
        yield return null;
        Check(FindAnyObjectByType<BeginPanel>() != null && SceneManager.GetActiveScene().name == "BeginScene", "Return to main menu works");
        Check(FindObjectsByType<GameAudioPresentation>(FindObjectsSortMode.None).Length == 1, "Scene transition retains exactly one audio presenter");
    }
    private void Check(bool value, string label) { checks.Add((value ? "PASS: " : "FAIL: ") + label); }
}
#endif
