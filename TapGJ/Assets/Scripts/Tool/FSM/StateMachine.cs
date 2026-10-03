public class StateMachine
{
    private State currentState;
    public State CurrentState => currentState;

    public void Switch(State state)
    {
        if (currentState == state) return;

        currentState?.Exit();
        currentState = state;
        currentState?.Enter();
    }

    public void FixUpdate()
    {
        currentState?.FixUpdate();
    }
    public void Update()
    {
        currentState?.Update();
    }
}
