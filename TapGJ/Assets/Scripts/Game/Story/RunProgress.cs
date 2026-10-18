using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class SavedGene
{
    public uint geneId;
    public string definitionName, instanceId;
    public int level = 1, copies = 1;
}

[Serializable]
public sealed class RunSaveData
{
    public int version = 3;
    public bool campaignLayout;
    public string scene = "GameScene";
    public float checkpointX = 3f, checkpointY = 1f, elapsedSeconds;
    public int storyFlags, region;
    public bool dashUnlocked, guardianCleared, completed;
    public string ending;
    public List<SavedGene> genes = new List<SavedGene>();
    public int[] equipped = Array.Empty<int>();
    public List<string> completedTriggers = new List<string>();
}

/// <summary>A local chapter checkpoint. Starting a new run explicitly replaces it.</summary>
public static class RunProgress
{
    public const string SaveKey = "TapGJ.ChapterCheckpoint.v1";
    public const string EndingKey = "TapGJ.DiscoveredEndings.v1";
    private static bool resumeRequested, sessionStarted;
    public static bool IsolatedValidation { get; private set; }
    private static string validationSave;
    private static int validationEndings;
    public static void BeginIsolatedValidation(){IsolatedValidation=true;validationSave=null;validationEndings=0;resumeRequested=false;sessionStarted=false;}
    public static void EndIsolatedValidation(){IsolatedValidation=false;validationSave=null;resumeRequested=false;sessionStarted=false;}
    public static bool CanContinue => Read() is RunSaveData data && !data.completed;
    public static int DiscoveredEndings => IsolatedValidation ? validationEndings : PlayerPrefs.GetInt(EndingKey, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { resumeRequested = false; sessionStarted = false; }

    public static void BeginNewRun()
    {

        if(IsolatedValidation)validationSave=null;
        else {PlayerPrefs.DeleteKey(SaveKey); PlayerPrefs.Save();}
        resumeRequested = false; sessionStarted = false;
        Time.timeScale = 1f; SceneManager.LoadSceneAsync("GameScene");
    }

    public static bool ContinueRun()
    {
        RunSaveData data = Read();
        if (data == null || data.completed) return false;
        resumeRequested = true; Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(data.scene); return true;
    }

    public static RunSaveData EnterScene()
    {
        RunSaveData data = resumeRequested || sessionStarted ? Read() : null;
        resumeRequested = false; sessionStarted = true;
        return data != null && !data.completed && data.scene == SceneManager.GetActiveScene().name ? data : null;
    }

    public static RunSaveData Read()
    {
        if (IsolatedValidation ? validationSave==null : !PlayerPrefs.HasKey(SaveKey)) return null;
        return ParseSnapshot(IsolatedValidation?validationSave:PlayerPrefs.GetString(SaveKey));
    }

    public static RunSaveData ParseSnapshot(string json)
    {
        if(string.IsNullOrWhiteSpace(json))return null;
        try
        {
            RunSaveData data = JsonUtility.FromJson<RunSaveData>(json);
            if (data == null || (data.version < 1 || data.version > 3) || (data.scene != "GameScene"&&data.scene!="SporeScene"&&data.scene!="CoreScene") ||
                float.IsNaN(data.checkpointX) || float.IsInfinity(data.checkpointX) ||
                float.IsNaN(data.checkpointY) || float.IsInfinity(data.checkpointY) ||
                float.IsNaN(data.elapsedSeconds) || float.IsInfinity(data.elapsedSeconds)) return null;
            if(data.version<3)
            {
                if(data.campaignLayout)
                {
                    int district=data.scene=="SporeScene"?2:data.scene=="CoreScene"?3:1;
                    data.checkpointX=Mathf.Clamp(data.checkpointX,1,43)+(district-1)*48;
                    if(data.completedTriggers!=null)
                        for(int i=0;i<data.completedTriggers.Count;i++)
                        {
                            string id=data.completedTriggers[i];
                            foreach(var pair in new[]{new[]{"GameScene","1"},new[]{"SporeScene","2"},new[]{"CoreScene","3"}})
                                if(id.StartsWith("campaign:"+pair[0]+":"))id=id.Replace("campaign:"+pair[0]+":","campaign:GameScene:区域"+pair[1]+" · ");
                            data.completedTriggers[i]=id;
                        }
                }
                else data.checkpointX=3;
                data.scene="GameScene";data.version=3;
            }
            data.checkpointX=Mathf.Clamp(data.checkpointX,1,142);
            data.genes ??= new List<SavedGene>(); data.equipped ??= Array.Empty<int>();
            data.completedTriggers ??= new List<string>();
            data.elapsedSeconds = Mathf.Max(0f, data.elapsedSeconds);
            data.region = Mathf.Clamp(data.region, 0, 2);
            return data;
        }
        catch (ArgumentException) { return null; }
    }

    public static void Write(RunSaveData data)
    {
        if (data == null) return;
        if(IsolatedValidation){validationSave=JsonUtility.ToJson(data);return;}
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save();
    }

    public static RunSaveData Capture(PlayerController player, Vector2 checkpoint, int storyFlags,
        int region, bool guardianCleared, float elapsedSeconds, IEnumerable<string> completedTriggers)
    {
        RunSaveData data = new RunSaveData
        {
            checkpointX = checkpoint.x, checkpointY = checkpoint.y, storyFlags = storyFlags,
            campaignLayout=CampaignFlow.Current!=null,
            region = region, guardianCleared = guardianCleared, elapsedSeconds = elapsedSeconds,
            dashUnlocked = player != null && player.DashUnlocked,
            scene = SceneManager.GetActiveScene().name
        };
        if (completedTriggers != null) data.completedTriggers.AddRange(completedTriggers);
        GeneInventory inventory = player != null ? player.Genes : null;
        if (inventory == null) return data;
        foreach (Modifier gene in inventory.GenInventory)
            if (gene != null)
                data.genes.Add(new SavedGene
                {
                    geneId = gene.GeneId, definitionName = gene.Definition.name,
                    instanceId = gene.InstanceId, level = gene.Level, copies = gene.Copies
                });
        data.equipped = new int[inventory.GenChain.Count];
        for (int i = 0; i < data.equipped.Length; i++)
        {
            data.equipped[i] = -1;
            Modifier equipped = inventory.GenChain[i];
            if (equipped == null) continue;
            for (int j = 0; j < data.genes.Count; j++)
                if (data.genes[j].instanceId == equipped.InstanceId) { data.equipped[i] = j; break; }
        }
        return data;
    }

    public static void RestorePlayer(PlayerController player, RunSaveData data)
    {
        if (player == null || data == null) return;
        Vector2 position = new Vector2(data.checkpointX, data.checkpointY);
        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
        player.Heal(player.maxHp); if (data.dashUnlocked) player.UnlockDash();
        GeneInventory inventory = player.Genes;
        if (inventory == null) return;
        Modifier[] definitions = Resources.LoadAll<Modifier>("SO/Genes");
        List<Modifier> restored = new List<Modifier>(); HashSet<Modifier> claimed = new HashSet<Modifier>();
        foreach (SavedGene saved in data.genes)
        {
            if(saved==null){restored.Add(null);continue;}
            Modifier definition = Array.Find(definitions, candidate => candidate.name == saved.definitionName &&
                (saved.geneId == 0 || candidate.GeneId == saved.geneId));
            if (definition == null) { restored.Add(null); continue; }
            Modifier instance = null;
            foreach (Modifier existing in inventory.GenInventory)
                if (existing != null && !claimed.Contains(existing) && existing.Definition == definition)
                { instance = existing; break; }
            if (instance == null)
            {
                // Acquisition normally accumulates into a low-level instance. Temporarily finish
                // matching entries so each saved record is rebuilt as its own instance.
                List<Modifier> protectedGenes = new List<Modifier>();
                foreach (Modifier existing in inventory.GenInventory)
                    if (existing != null && existing.Definition == definition && existing.Level == 1)
                    { protectedGenes.Add(existing); existing.Level = 2; }
                int count = inventory.GenInventory.Count; inventory.Acquire(definition);
                if (inventory.GenInventory.Count > count) instance = inventory.GenInventory[inventory.GenInventory.Count - 1];
                foreach (Modifier protectedGene in protectedGenes) protectedGene.Level = 1;
            }
            if (instance != null)
            {
                instance.Level = Mathf.Clamp(saved.level, 1, 2);
                instance.Copies = Mathf.Clamp(saved.copies, 1, inventory.GetEvolutionCopies(instance));
                if (!string.IsNullOrEmpty(saved.instanceId)) instance.InstanceId = saved.instanceId;
                claimed.Add(instance);
            }
            restored.Add(instance);
        }
        Modifier[] chain = new Modifier[inventory.SlotCount]; HashSet<Modifier> used = new HashSet<Modifier>();
        for (int i = 0; i < chain.Length && i < data.equipped.Length; i++)
        {
            int index = data.equipped[i];
            Modifier gene = index >= 0 && index < restored.Count ? restored[index] : null;
            if (gene != null && used.Add(gene)) chain[i] = gene;
        }
        GameObject terminalObject = new GameObject("Story Checkpoint Loadout") { hideFlags = HideFlags.HideAndDontSave };
        terminalObject.transform.position = player.transform.position;
        GeneEditStation terminal = terminalObject.AddComponent<GeneEditStation>();
        inventory.Commit(terminal, player, chain);
        terminalObject.SetActive(false);
        if(Application.isPlaying)UnityEngine.Object.Destroy(terminalObject);
        else UnityEngine.Object.DestroyImmediate(terminalObject);
        Physics2D.SyncTransforms();
    }

    public static void Finish(RunSaveData data, bool coexistence)
    {
        if (data == null) return;
        data.completed = true; data.ending = coexistence ? "水的归处" : "最后的回声"; Write(data);
        if(IsolatedValidation){validationEndings|=coexistence?1:2;return;}
        PlayerPrefs.SetInt(EndingKey, DiscoveredEndings | (coexistence ? 1 : 2)); PlayerPrefs.Save();
    }
}
