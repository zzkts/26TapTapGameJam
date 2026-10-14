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
        if(fire&&poison)return "组合观察：先留下毒雾，再用火焰触发连锁。";
        if(fire&&water)return "组合观察：先弄湿目标，再加热；留意蒸汽推力。";
        if(vortex)return "组合观察：旋涡能聚拢敌人，也能移动箱体。";
        if(split&&bounce)return "组合观察：分裂和反弹会扩大碰撞覆盖。";
        return "组合观察：基础水弹能弄湿导体、蓄压和推动箱子。";
    }
}
