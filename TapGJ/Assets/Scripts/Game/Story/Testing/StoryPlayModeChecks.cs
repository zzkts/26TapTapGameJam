#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Exercises both real ending buttons and checkpoint reconstruction on isolated player/UI fixtures.</summary>
public sealed class StoryPlayModeChecks:MonoBehaviour
{
    public static readonly List<string> Results=new List<string>();
    public static bool Running{get;private set;}
    public static bool Succeeded{get;private set;}
    private GameObject fixture;
    private StorySession chapter;
    private bool previousValidation,hadSave,hadEndings;
    private string previousSave;
    private int previousEndings;

    public static void Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter GameScene Play Mode first.");
        if(Running)throw new InvalidOperationException("Story checks are already running.");
        new GameObject("Story Verification").AddComponent<StoryPlayModeChecks>().Begin();
    }
    private void Begin()
    {
        Results.Clear();Running=true;Succeeded=false;
        hadSave=PlayerPrefs.HasKey(RunProgress.SaveKey);previousSave=PlayerPrefs.GetString(RunProgress.SaveKey);
        hadEndings=PlayerPrefs.HasKey(RunProgress.EndingKey);previousEndings=PlayerPrefs.GetInt(RunProgress.EndingKey);
        chapter=StorySession.Current;
        if(chapter!=null){previousValidation=chapter.ValidationMode;chapter.SetValidationMode(true);}
        fixture=new GameObject("Story Validation Fixtures");
        StartCoroutine(Execute());
    }
    private IEnumerator Execute()
    {
        var stack=new Stack<IEnumerator>();stack.Push(Cases());
        while(stack.Count>0)
        {
            object wait;
            try
            {
                IEnumerator step=stack.Peek();if(!step.MoveNext()){stack.Pop();continue;}
                wait=step.Current;if(wait is IEnumerator nested){stack.Push(nested);continue;}
            }
            catch(Exception error){Results.Add("FAIL: "+error);break;}
            yield return wait;
        }
        Finish();
    }
    private IEnumerator Cases()
    {
        PlayerPrefs.DeleteKey(RunProgress.EndingKey);
        PlayerController source=Player("Checkpoint Source");yield return null;
        Modifier water=Resources.LoadAll<Modifier>("SO/Genes").First(g=>g.IsAttribute&&g.Element==E_ElementType.Water);
        Modifier fire=Resources.LoadAll<Modifier>("SO/Genes").First(g=>g.IsAttribute&&g.Element==E_ElementType.Fire);
        source.Genes.Acquire(water);source.Genes.Acquire(water);source.Genes.Acquire(water);source.Genes.Acquire(water);
        source.Genes.Acquire(fire);source.Genes.Acquire(fire);source.UnlockDash();
        Modifier evolved=source.Genes.GenInventory.First(g=>g.Definition==water&&g.Level==2);
        Modifier second=source.Genes.GenInventory.First(g=>g.Definition==water&&g.Level==1);
        Modifier burning=source.Genes.GenInventory.First(g=>g.Definition==fire);
        var stationObject=new GameObject("Save Fixture Terminal");stationObject.transform.SetParent(fixture.transform);
        stationObject.transform.position=source.transform.position;GeneEditStation station=stationObject.AddComponent<GeneEditStation>();
        Modifier[] chain=new Modifier[source.Genes.SlotCount];chain[0]=evolved;chain[2]=burning;chain[4]=second;
        Check(source.Genes.Commit(station,source,chain),"The checkpoint source commits an evolved gene and a second independent instance");
        RunSaveData captured=RunProgress.Capture(source,new Vector2(41f,1f),1<<(int)StoryBeat.LabRecord,1,false,123f,new[]{"Checkpoint-41","Water-Sample"});
        RunProgress.Write(captured);RunSaveData saved=RunProgress.Read();
        PlayerController restored=Player("Checkpoint Restored");yield return null;
        RunProgress.RestorePlayer(restored,saved);yield return null;
        Check(Vector2.Distance(restored.transform.position,new Vector2(41f,1f))<.01f&&restored.Hp==restored.MaxHp,"Reload reconstructs the safe checkpoint and restores full health");
        Check(restored.DashUnlocked&&restored.Genes.GenInventory.Count==saved.genes.Count,"Reload keeps the dash unlock and every saved gene instance");
        Check(restored.Genes.GenInventory.All(g=>saved.genes.Any(s=>s.instanceId==g.InstanceId&&s.level==g.Level&&s.copies==g.Copies)),"Reload preserves evolution, copy counts and stable instance identities");
        Check(restored.Genes.GenChain[0].InstanceId==evolved.InstanceId&&restored.Genes.GenChain[2].InstanceId==burning.InstanceId&&restored.Genes.GenChain[4].InstanceId==second.InstanceId&&restored.Genes.GenChain[1]==null,"Reload restores exact slot positions, including empty slots");
        Check(restored.Genes.GenInventory.All(g=>!source.Genes.GenInventory.Contains(g)),"Reload creates independent runtime genes instead of reusing prior scene instances");
        RunProgress.RestorePlayer(restored,saved);yield return null;
        Check(restored.Genes.GenInventory.Count==saved.genes.Count,"Applying a checkpoint twice does not duplicate the saved inventory");

        Canvas canvas=new GameObject("Story Fixture Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
        canvas.transform.SetParent(fixture.transform);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=300;
        StoryDialogPanel panel=StoryUIFactory.Ensure(canvas);
        bool ended=false;panel.Open(new[]{new StoryLine("检查","一次输入只能完成一个动作。"),new StoryLine("检查","第二行应该保留下来。")},()=>ended=true);
        yield return null;
        panel.Advance();panel.Advance();
        Check(panel.LineIndex==0&&!panel.IsTyping,"Keyboard and button submission in one frame reveal a line without skipping it");
        yield return null;panel.Advance();
        Check(panel.LineIndex==1&&!ended,"The next input advances exactly one dialogue line");
        panel.Skip();Check(ended&&!panel.gameObject.activeSelf,"Skip completes only the current conversation and closes its modal");

        foreach(bool coexistence in new[]{true,false})
        {
            GameObject sessionObject=new GameObject("Ending Fixture "+coexistence);sessionObject.transform.SetParent(fixture.transform);sessionObject.SetActive(false);
            StorySession session=sessionObject.AddComponent<StorySession>();session.enabled=false;session.player=restored;session.dialogue=panel;
            Set(session,"<Ready>k__BackingField",true);Set(session,"<GuardianCleared>k__BackingField",true);Set(session,"elapsed",123f);
            restored.SetMenuOpen(false);session.ShowCoreChoice();yield return null;
            Check(panel.IsDialogue&&restored.MenuOpen&&Time.timeScale==0f,"The final record opens a real dialogue and pauses the player");
            panel.skip.onClick.Invoke();yield return null;
            Check(panel.IsChoosing&&session.IsModal&&restored.MenuOpen,"Skipping the record preserves the mandatory final choice");
            panel.Skip();Check(panel.IsChoosing,"Skip cannot silently select or dismiss an ending choice");
            if(coexistence)panel.coexist.onClick.Invoke();else panel.seal.onClick.Invoke();yield return null;
            Check(panel.IsDialogue&&RunProgress.Read().completed&&!RunProgress.CanContinue,"The ending button persists completion and opens its own epilogue");
            panel.skip.onClick.Invoke();yield return null;
            string title=coexistence?"水的归处":"最后的回声";
            Check(panel.IsResult&&panel.endingTitle.text==title&&RunProgress.Read().ending==title,"The selected ending reaches its matching result screen: "+title);
            Check(session.IsModal&&restored.MenuOpen&&Time.timeScale==0f&&panel.newRun!=null&&panel.mainMenu!=null,"The result holds gameplay and offers another exploration or return to menu");
            panel.CloseForValidation();restored.SetMenuOpen(false);
        }
        Check(RunProgress.DiscoveredEndings==3,"Both discovered endings are retained across separate explorations");
    }
    private PlayerController Player(string name)
    {
        GameObject holder=new GameObject(name);holder.transform.SetParent(fixture.transform);holder.transform.position=new Vector2(1000f+fixture.transform.childCount*10f,1000f);holder.SetActive(false);
        PlayerController player=Object.Instantiate(Resources.Load<GameObject>("Prefab/Player/Player"),holder.transform).GetComponent<PlayerController>();
        GeneInventory inventory=player.GetComponent<GeneInventory>();
        Set(inventory,"genInventory",new List<Modifier>());Set(inventory,"genChain",new List<Modifier>());
        holder.SetActive(true);player.enabled=false;
        // Narrative fixtures intentionally have no locomotion; OnDisable restores production gravity.
        Rigidbody2D body=player.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;
        body.constraints=RigidbodyConstraints2D.FreezeAll;body.interpolation=RigidbodyInterpolation2D.None;body.gravityScale=0f;
        foreach(Collider2D collider in player.GetComponentsInChildren<Collider2D>(true))collider.enabled=false;
        PlayerInteraction interaction=player.GetComponent<PlayerInteraction>();if(interaction!=null)interaction.enabled=false;
        return player;
    }
    private static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static void Check(bool value,string message)
    {Results.Add((value?"PASS: ":"FAIL: ")+message);if(!value)throw new InvalidOperationException(message);}
    private void Finish()
    {
        if(!Running)return;
        if(hadSave)PlayerPrefs.SetString(RunProgress.SaveKey,previousSave);else PlayerPrefs.DeleteKey(RunProgress.SaveKey);
        if(hadEndings)PlayerPrefs.SetInt(RunProgress.EndingKey,previousEndings);else PlayerPrefs.DeleteKey(RunProgress.EndingKey);
        PlayerPrefs.Save();
        if(fixture!=null){foreach(PlayerController player in fixture.GetComponentsInChildren<PlayerController>(true))player.SetMenuOpen(false);Object.Destroy(fixture);}
        if(chapter!=null)chapter.SetValidationMode(previousValidation);
        Time.timeScale=1f;Succeeded=Results.Count>0&&Results.All(r=>r.StartsWith("PASS:"));Running=false;
        Debug.Log("Story checks: "+string.Join("\n",Results));Object.Destroy(gameObject);
    }
    private void OnDestroy(){if(Running)Finish();}
}
#endif
