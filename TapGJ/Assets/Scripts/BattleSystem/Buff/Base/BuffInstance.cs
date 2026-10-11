/// <summary>
/// 一个元素附着的运行时状态。
/// 只保存元素类型和剩余时间，不依赖单独的 Buff SO。
/// </summary>
public sealed class BuffInstance
{
    public E_ElementType ElementType { get; }
    public float RemainingTime { get; private set; }

    public BuffInstance(E_ElementType elementType, float duration)
    {
        ElementType = elementType;
        RemainingTime = duration;
    }

    public bool Update(float deltaTime)
    {
        RemainingTime -= deltaTime;
        return RemainingTime > 0f;
    }
}
