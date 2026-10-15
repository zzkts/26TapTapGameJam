using System;

public enum StoryBeat
{
    Entrance, LabRecord, SampleWater, SampleFire, SamplePoison, Dash, Checkpoint,
    GuardianGate, Core, Ecology, CoreArea, GuardianCleared, ExperimentRecord,
    PhaseDistrict, SporeDistrict, FactoryDistrict
}

[Serializable]
public sealed class StoryLine
{
    public string speaker, text;
    public bool playerPortrait;
    public StoryLine(string speaker, string text, bool playerPortrait = false)
    { this.speaker = speaker; this.text = text; this.playerPortrait = playerPortrait; }
}

/// <summary>A compact adaptation of the narrative treatment, with two playable final decisions.</summary>
public static class StoryContent
{
    private static StoryLine N(string text) => new StoryLine("水落口", text);
    private static StoryLine P(string text) => new StoryLine("地漏 · 心里的声音", text, true);
    private static StoryLine B(string text) => new StoryLine("旧设施广播", text);
    private static StoryLine R(string speaker, string text) => new StoryLine(speaker, text);
    public static StoryLine[] Lines(StoryBeat beat)
    {
        switch (beat)
        {
            case StoryBeat.Entrance:
                return new[] {
                    N("一场暴雨，让废弃多年的地下管网重新连通。水流撞开一枚旧地漏，也叫醒了藏在盖子下面的你。"),
                    P("我……会动？这颗水球，好像也在听我的话。"),
                    B("警告：三号排水区异常回流。检测到未知生命活动。净化核心正在启动。"),
                    P("如果广播说的是我，那我得在被冲走以前，弄明白自己从哪里来。"),
                    R("探索提示", "A / D 移动，空格或 W 跳跃；左键射击，E 交互。水球也能改变环境。观察水、热量、菌落和重量之间的关系，按 TAB 记录发现。") };
            case StoryBeat.LabRecord:
                return new[] {
                    R("维修日志 · 6月17日", "三号区域再次出现异常回流。管道没有堵塞，泵站也没有启动，但压力仍在增加。"),
                    R("后来补上的字", "水在自己移动。请别再把下面的东西当成垃圾冲走。"),
                    P("有人早就发现了它们。地图上这个被划掉的名字……NODE-03？"),
                    R("探索提示", "收集水、火、毒三类基因，在发光的编辑终端组合能力。终端只提交你确认的装配；取消不会改变当前构筑。") };
            case StoryBeat.ExperimentRecord:
                return new[] {
                    R("实验记录 · 17", "地下水能够携带不同样本的特征。毒性、热量和组织形态，在接触中发生变化。"),
                    R("实验记录 · 18 / 19", "初步判断：水只是运输媒介。修正：水并非单纯的媒介。它们正在主动选择。"),
                    R("被撕掉的编号档案", "NODE-01、NODE-02、NODE-04。第三个位置只留下胶带和一张地漏的照片。"),
                    P("我的能力，是它们交给我的。那张缺掉的照片……会不会就是我？") };
            case StoryBeat.SampleWater:
                return new[] {
                    P("这股水流带回了一段新的记忆。我的水球，能把变化传得更远。"),
                    R("基因样本 · 水", "水基因已经进入库存。到终端装配后，按右键开启当前属性阶段；水和环境中的电会连锁传导。") };
            case StoryBeat.SampleFire:
                return new[] {
                    P("管道里藏着一小簇温暖。我也能把它带在身上。"),
                    R("基因样本 · 火", "火基因可以点燃目标。燃烧接触水会形成蒸汽；火与毒雾相遇，会引发范围爆炸。") };
            case StoryBeat.SamplePoison:
                return new[] {
                    P("它们住在污水里，却不只是污水。连危险的东西，也在努力活下去。"),
                    R("基因样本 · 毒", "毒基因会留下影响范围内生物的毒雾。将水、火、毒和常驻改造混装，在环境中寻找组合效果。") };
            case StoryBeat.Dash:
                return new[] {
                    P("这段回流像一股弹簧。我能一下子把自己送出去！"),
                    R("身体机能 · 冲刺解锁", "按左 Shift 朝光标方向冲刺，地面和空中都能使用；冲刺期间短暂无敌，冷却结束后可再次使用。") };
            case StoryBeat.Ecology:
                return new[] {
                    N("生锈的实验罐后面，菌落已经长满管道。褪色的墙画画着一枚地漏，旁边围着许多小怪物。"),
                    R("墙上的儿童涂鸦", "它会把水带回来。妈妈说，它不是怪物。"),
                    P("我不是第一个到这里的。也许，它们一直在等水回来。") };
            case StoryBeat.CoreArea:
                return new[] {
                    B("核心区封锁。异常样本不得进入。执行最终净化程序。"),
                    P("这张封锁令，比实验室建造的日期还早。它们害怕的，真的只是污染吗？"),
                    R("最后一座终端", "前方是核心守卫。先在安全区确认构筑、恢复生命，再进入战斗。常驻改造始终有效；属性阶段结束后，需要再次按右键。") };
            case StoryBeat.PhaseDistrict:
                return new[] {
                    R("泵站记录", "我们切断了导线，却没能切断回流。水把原本孤立的东西重新接在一起。高处的检修节点也仍然能用。"),
                    P("水球不只会击退它们。也许，我能改变这整个地方。"),
                    R("探索提示", "解开三个区域中的任意两条通路，就能进入核心。试试让不同规则相遇；F 推动湿润箱体，S + F 吸回它们。") };
            case StoryBeat.SporeDistrict:
                return new[] {
                    R("菌落观察", "少量的水让一个菌落苏醒。过了一会儿，附近的菌落也开始变化。没有谁在指挥它们。"),
                    P("一件小事，竟然能传到这么远。我可以利用这条连锁，也可以从上方绕过去。") };
            case StoryBeat.FactoryDistrict:
                return new[] {
                    R("工人便笺", "止回阀只认重量。是箱子、是人，还是路过的怪物，它从来分不清。别忘了把那边的控制台锁住。"),
                    P("它们一直遵守同样的规则。换一种排列，结果就不一样了。") };
            case StoryBeat.GuardianGate:
                return new[] {
                    B("三号回流阀禁止开启。核心守卫唤醒。净化目标：所有无法识别的生命。"),
                    P("我只是想知道自己是谁。如果你们不肯让我过去，那就让我自己找到答案。"),
                    R("核心守卫战", "击败前方三名守卫，关闭净化锁。可以移动和射击同时进行，用冲刺躲避攻击，并利用属性与场景形成反应。") };
            case StoryBeat.GuardianCleared:
                return new[] {
                    N("守卫停止了。轰鸣的管道安静下来，中央水库第一次露出它原来的样子。"),
                    R("实验负责人 · 最后记录", "我们以为是实验制造了生命，所以不断清理、消毒。后来才明白：我们只是给了它们一个足够大的地方，彼此接触，彼此改变。"),
                    P("不是谁造出了我。是所有经过这里的水，让我慢慢成为了我。"),
                    R("核心记录", "前往最右侧的回流阀，按 E 查看最后的记录，并决定水流将去向何处。") };
            case StoryBeat.Core:
                return new[] {
                    R("研究员 · 最后录像", "如果有人还能看到这里，那就说明它已经醒了。我们不知道第一个是谁。我们只知道：水到哪里，它们就到哪里。"),
                    N("回流阀的两边，分别接着城市水路和仍在运行的净化装置。你终于有机会，替自己做出选择。"),
                    P("打开回流，它们就能去往更远的地方。封住这里，城市会暂时安全……但下面的声音，也会越来越远。") };
            default: return Array.Empty<StoryLine>();
        }
    }

    public static StoryLine[] Ending(bool coexistence)
    {
        return coexistence ? new[] {
            N("你打开了三号回流阀。第一滴水流向远处，第二滴跟了上来。沉寂的排水节点，一个接一个亮起。"),
            P("如果我能走到这里，它们也一定可以。"),
            N("城市没有立刻变得更好。但从今天起，地上的人和地下的生命，将不得不重新学习怎样生活在一起。"),
            R("结局 · 水的归处", "你没有找到一个制造你的人。你找到的，是让你成为自己的整条水路。\n而水，从来没有停止流动。")
        } : new[] {
            N("你启动了净化装置，关上三号回流阀。城市的警报停了，地下的灯也一盏盏熄灭。"),
            P("我还不知道怎样保护你们。但我会记住这里，记住每一个把能力交给我的生命。"),
            N("你带着最后一颗水滴离开封锁区。水滴中偶尔传来很轻的声音，仿佛整个地下世界都没有真正睡去。"),
            R("结局 · 最后的回声", "你替城市换来了一段安静，也留下了一道没有回答的问题。\n只要还有一滴水，回流就仍有可能。")
        };
    }
}
