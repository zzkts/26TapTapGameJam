/// <summary>
/// 状态机中单个状态的基础类。
/// 提供进入、物理更新、普通更新和退出四个生命周期接口。
/// </summary>
public abstract class State
{
    protected string stateName;    
    
    public State(string stateName)
    {
        this.stateName = stateName;
    }

    public virtual void Enter() { }
    public virtual void FixUpdate() { }
    public virtual void Update() { }
    public virtual void Exit() { }

}
