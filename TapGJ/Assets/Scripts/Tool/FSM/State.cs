public abstract class State
{
    protected string stateName;    
    
    public State(string stateName)
    {
        this.stateName = stateName;
    }

    public virtual void Enter() { }
    public virtual void Update(float deltaTime) { }
    public virtual void Exit() { }

}