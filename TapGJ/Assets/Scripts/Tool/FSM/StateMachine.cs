/// <summary>
/// 轻量的通用状态机。
/// 负责保存当前状态，并在切换时依次调用退出和进入逻辑。
/// </summary>
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
