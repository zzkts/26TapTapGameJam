using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Editor-only samples and live chain inspection. Acquiring a sample uses normal pickup rules.</summary>
public sealed class GeneDebugWindow:EditorWindow
{
    private PlayerController player;
    private Vector2 scroll;
    private int copies=1;
    [MenuItem("Tools/TapGJ/基因调试")]
    public static void Open(){GetWindow<GeneDebugWindow>("基因调试").minSize=new Vector2(440,400);}
    private void OnInspectorUpdate(){Repaint();}
    public static Modifier[] AttributeDefinitions()=>Resources.LoadAll<Modifier>("SO/Genes")
        .Where(g=>g!=null&&g.IsAttribute&&g.Element!=E_ElementType.Electric).OrderBy(g=>g.GeneId).ToArray();
    public static int SpawnAttributeSamples(PlayerController owner,Modifier definition,int count=1)
    {
        if(!Application.isPlaying||owner==null||owner.IsDead||definition==null||!definition.IsAttribute||definition.Element==E_ElementType.Electric)return 0;
        count=Mathf.Clamp(count,1,6);
        DropService.Spawn(new[]{definition},count,(Vector2)owner.transform.position+Vector2.right*owner.FaceDir*.8f,1.6f);
        return count;
    }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("属性基因调试",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("运行 GameScene → 生成样本 → 回到 Game 窗口按 E 拾取 → 在终端按 E 装配 → Space 确认 → 右键逐次附魔。",MessageType.Info);
        if(!EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("先进入游戏运行模式；这里不修改配置资源，也不会自动更改装配。",MessageType.None);
        if(player==null&&Application.isPlaying)player=Object.FindFirstObjectByType<PlayerController>();
        player=(PlayerController)EditorGUILayout.ObjectField("当前玩家",player,typeof(PlayerController),true);
        copies=EditorGUILayout.IntSlider("每次生成份数",copies,1,6);
        EditorGUILayout.LabelField("拾取样本按正常游戏规则记录库存和进度。",EditorStyles.wordWrappedLabel);
        scroll=EditorGUILayout.BeginScrollView(scroll);
        foreach(var gene in AttributeDefinitions())
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(gene.Name+" · "+gene.Element,EditorStyles.boldLabel);
            using(new EditorGUI.DisabledScope(!Application.isPlaying||player==null||player.IsDead))
                if(GUILayout.Button("生成掉落",GUILayout.Width(80)))SpawnAttributeSamples(player,gene,copies);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(gene.Desc,EditorStyles.wordWrappedLabel);
            EditorGUILayout.ObjectField("配置",gene,typeof(Modifier),false);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前附魔链",EditorStyles.boldLabel);
        var chain=player!=null?player.GetComponent<GeneChainController>():null;
        if(chain!=null)
        {
            EditorGUILayout.LabelField("轮换顺序",string.Join(" → ",chain.Attributes.Select(g=>g.Name)));
            EditorGUILayout.LabelField("当前状态",chain.State+" · 剩余 "+chain.Remaining.ToString("0.0")+" 秒");
            EditorGUILayout.LabelField("就绪属性",chain.CurrentAttribute!=null?chain.CurrentAttribute.Name:"未装配属性");
            EditorGUILayout.LabelField("生效属性",chain.ActiveAttribute!=null?chain.ActiveAttribute.Name:"当前为基础水弹");
        }
        EditorGUILayout.EndScrollView();
    }
}
