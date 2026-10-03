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