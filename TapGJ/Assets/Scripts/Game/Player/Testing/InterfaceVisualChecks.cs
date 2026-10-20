#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class InterfaceVisualChecks : MonoBehaviour
{
    [Serializable] private sealed class Report {public bool success;public string[] checks;}
    private readonly List<string> checks=new();
    private const string Folder="Docs/UIAlignment/2026-10-20/";
    private bool previousBackground;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if(!SessionState.GetBool("UIAlign.Run",false))return;
        SessionState.SetBool("UIAlign.Run",false);
        var host=new GameObject("Interface visual checks");DontDestroyOnLoad(host);
        host.AddComponent<InterfaceVisualChecks>().StartCoroutine("Run");
    }
    private IEnumerator Run()
    {
        Directory.CreateDirectory(Folder);previousBackground=Application.runInBackground;Application.runInBackground=true;
        var routines=new Stack<IEnumerator>();routines.Push(Cases());
        while(true)
        {
            object wait;
            try {var routine=routines.Peek();if(!routine.MoveNext()){routines.Pop();if(routines.Count==0)break;continue;}wait=routine.Current;}
            catch(Exception e){checks.Add("FAIL: "+e);break;}
            if(wait is IEnumerator nested){routines.Push(nested);continue;}
            yield return wait;
        }
        GlobalManager.Instance.SetBackgroundVolume(SessionState.GetFloat("UIAlign.Music",1));
        GlobalManager.Instance.SetGameVolume(SessionState.GetFloat("UIAlign.Effects",1));
        Check(PlayerPrefs.GetString(RunProgress.SaveKey,"")==SessionState.GetString("UIAlign.Save","missing"),"Original save unchanged");
        Check(PlayerPrefs.GetInt(RunProgress.EndingKey,0)==SessionState.GetInt("UIAlign.Endings",-1),"Ending history unchanged");
        Check(PlayerPrefs.GetFloat("Audio.BackgroundVolume",1)==SessionState.GetFloat("UIAlign.Music",-1)&&
            PlayerPrefs.GetFloat("Audio.GameVolume",1)==SessionState.GetFloat("UIAlign.Effects",-1),"Saved volume preferences unchanged");
        var report=new Report{success=checks.All(s=>s.StartsWith("PASS:")),checks=checks.ToArray()};
        File.WriteAllText(Folder+"Validation.json",JsonUtility.ToJson(report,true));
        Application.runInBackground=previousBackground;SessionState.SetBool("UIAlign.Finished",true);
        EditorApplication.delayCall+=()=>EditorApplication.ExitPlaymode();
    }
    private IEnumerator Cases()
    {
        yield return null;yield return null;
        Check(RunProgress.IsolatedValidation,"UI checks use isolated in-memory progress");if(!RunProgress.IsolatedValidation)yield break;
        var begin=FindAnyObjectByType<BeginPanel>();begin.btnSetting.onClick.Invoke();
        yield return new WaitForSecondsRealtime(.2f);
        var settings=UIManager.Instance.GetPanel<SettingPanel>();
        CheckSliders(settings,"BeginScene");settings.sliderGame.value=settings.sliderBk.value=.67f;
        yield return Capture("Settings-67");
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(1024,768)})
            CheckCardFit(settings,size.x,size.y);
        settings.sliderGame.value=0;settings.sliderBk.value=1;yield return null;
        yield return Capture("Settings-Endpoints");
        settings.btnCredits.onClick.Invoke();Check(settings.GetComponent<MusicCreditsPanel>().IsOpen,"Authored music credits button opens attribution");
        yield return Capture("Music-Credits");settings.GetComponent<MusicCreditsPanel>().Close();
        GlobalManager.Instance.SetBackgroundVolume(SessionState.GetFloat("UIAlign.Music",1));GlobalManager.Instance.SetGameVolume(SessionState.GetFloat("UIAlign.Effects",1));
        UIManager.Instance.HidePanel<SettingPanel>(false);begin.btnBegin.onClick.Invoke();
        float deadline=Time.realtimeSinceStartup+15;
        while(Time.realtimeSinceStartup<deadline&&(SceneManager.GetActiveScene().name!="GameScene"||StorySession.Current==null||!StorySession.Current.Ready))yield return null;
        StorySession.Current.SetValidationMode(true);var player=FindAnyObjectByType<PlayerController>();player.SetInvincible(true);
        foreach(var ai in FindObjectsByType<EnemyAIController>(FindObjectsSortMode.None))ai.enabled=false;
        var menu=UIManager.Instance.GetPanel<RunMenuPanel>();menu.Open(player,false);yield return new WaitForSecondsRealtime(.2f);
        yield return Capture("Pause");
        var buttons=new[]{menu.resumeButton,menu.retryButton,menu.settingsButton,menu.menuButton}.Select(b=>b.GetComponent<RectTransform>()).ToArray();
        Check(buttons.All(r=>Mathf.Abs(r.anchoredPosition.x-54)<.01f&&Mathf.Abs(r.rect.width-512)<.01f),"Pause buttons share a left edge and width");
        menu.settingsButton.onClick.Invoke();yield return new WaitForSecondsRealtime(.2f);
        settings=UIManager.Instance.GetPanel<SettingPanel>();CheckSliders(settings,"GameScene");
        UIManager.Instance.HidePanel<SettingPanel>(false);menu.resumeButton.onClick.Invoke();
        foreach(string name in new[]{"water","ember","split"})player.Genes.Acquire(Resources.Load<Modifier>("SO/Genes/"+name));
        var station=FindAnyObjectByType<GeneEditStation>();player.transform.position=station.transform.position+Vector3.left*.5f;
        var gene=UIManager.Instance.GetPanel<GeneEditPanel>();gene.Open(player,station);yield return new WaitForSecondsRealtime(.2f);
        Check(gene.gameObject.activeSelf,"Gene workbench still opens at a terminal");
        Check(gene.confirmButton.transform.Find("Confirm Shortcut")==null||!gene.confirmButton.transform.Find("Confirm Shortcut").gameObject.activeSelf,"Gene keyboard hint stays inside footer rather than above the button");
        yield return Capture("Gene-Workbench");gene.Cancel();
        var story=UIManager.Instance.GetPanel<StoryDialogPanel>();
        Check(story.skip.transform.parent==story.dialogueBody.transform,"Dialogue skip button remains inside dialogue card");
        story.Open(new[]{new StoryLine("旧设施广播","相遇让变化发生。试着把不同的基因放在一起，看看这片地下世界会出现怎样的新可能。")},null);
        yield return new WaitForSecondsRealtime(2.8f);yield return Capture("Story-Dialogue");story.CloseForValidation();
        player.SetMenuOpen(false);menu.Open(player,false);menu.menuButton.onClick.Invoke();
        deadline=Time.realtimeSinceStartup+15;while(Time.realtimeSinceStartup<deadline&&SceneManager.GetActiveScene().name!="BeginScene")yield return null;
        Check(SceneManager.GetActiveScene().name=="BeginScene","Return to menu after UI checks");yield return null;yield return null;
        yield return Capture("Main-Menu");
    }
    private void CheckSliders(SettingPanel panel,string scene)
    {
        foreach(var slider in new[]{panel.sliderGame,panel.sliderBk})
        {
            var track=slider.transform.Find("Track").GetComponent<RectTransform>();var fillArea=slider.fillRect.parent.GetComponent<RectTransform>();
            foreach(float value in new[]{0f,.01f,.67f,1f})
            {
                slider.value=value;Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(slider.handleRect.rect.width-slider.handleRect.rect.height)<.05f,scene+" "+slider.name+" thumb remains circular at "+value.ToString("P0"));
                Check(Vector3.Distance(track.TransformPoint(track.rect.min),fillArea.TransformPoint(fillArea.rect.min))<.05f&&
                    Vector3.Distance(track.TransformPoint(track.rect.max),fillArea.TransformPoint(fillArea.rect.max))<.05f,
                    scene+" "+slider.name+" track/fill geometry matches at "+value.ToString("P0"));
            }
        }
        Check(panel.effectsValue.rectTransform.anchoredPosition.y==panel.transform.Find("Settings Frame/Effects").GetComponent<RectTransform>().anchoredPosition.y,
            scene+" effect label and percentage share their baseline");
        Check(panel.musicValue.rectTransform.anchoredPosition.y==panel.transform.Find("Settings Frame/Music").GetComponent<RectTransform>().anchoredPosition.y,
            scene+" music label and percentage share their baseline");
    }
    private void CheckCardFit(SettingPanel panel,int width,int height)
    {
        var card=(RectTransform)panel.transform.Find("Settings Frame");
        float scale=Mathf.Min(width/1600f,height/900f);
        Check(card.rect.width*scale<=width&&card.rect.height*scale<=height,"Settings card fits "+width+"x"+height+" with Expand scaling");
    }
    private IEnumerator Capture(string name)
    {
        string path=Folder+name+".png";
        ScreenCapture.CaptureScreenshot(path);yield return null;yield return null;
        Check(File.Exists(path)&&new FileInfo(path).Length>50000,name+" actual Game view captured");
    }
    private void Check(bool value,string message){checks.Add((value?"PASS: ":"FAIL: ")+message);}
}
#endif
