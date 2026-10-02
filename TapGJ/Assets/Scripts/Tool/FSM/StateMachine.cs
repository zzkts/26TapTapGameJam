public class StateMachine
{
    private State currentState;

    public void Switch(State state)
    {
        currentState?.Exit();
        currentState = state;
        currentState.Enter();
    }

    public void Update(float deltaTime)
    {
        currentState?.Update(deltaTime);
    }
}
