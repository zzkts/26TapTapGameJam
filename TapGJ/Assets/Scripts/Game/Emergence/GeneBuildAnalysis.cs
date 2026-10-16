using System.Collections.Generic;
public static class GeneBuildAnalysis
{
    public static string Describe(IReadOnlyList<Modifier> genes)
    {
        bool water=false,fire=false,poison=false,split=false,bounce=false,vortex=false;
        foreach(Modifier gene in genes)
        {
            if(gene==null)continue;water|=gene.Element==E_ElementType.Water;fire|=gene.Element==E_ElementType.Fire;poison|=gene.Element==E_ElementType.Poison;
            split|=gene.ModifierType==E_ModifierType.Split; bounce|=gene.ModifierType==E_ModifierType.Rebound;vortex|=gene.IsVortex;
        }
        if(fire&&poison)return "组合观察：先施加毒，再用火焰触发爆破。";
        if(fire&&water)return "组合观察：按顺序切换水与火，观察敌人的元素状态。";
        if(vortex)return "组合观察：旋涡能聚拢敌人，提高范围攻击的命中。";
        if(split&&bounce)return "组合观察：分裂和反弹会扩大碰撞覆盖。";
        return "组合观察：改造持续生效；右键按槽位顺序启动属性。";
    }
}
