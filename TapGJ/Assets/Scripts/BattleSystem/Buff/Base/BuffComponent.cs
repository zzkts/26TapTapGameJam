using System.Collections.Generic;

/// <summary>
/// 战斗单位的临时状态容器。
/// 负责添加、移除和更新元素附着，元素反应交给 ElementSystem。
/// </summary>
public sealed class BuffComponent
{
    private readonly List<BuffInstance> buffList = new();

    public IReadOnlyList<BuffInstance> BuffList => buffList;

    public BuffInstance Add(E_ElementType elementType, float duration)
    {
        BuffInstance instance = new(elementType, duration);
        buffList.Add(instance);
        return instance;
    }

    public void Remove(BuffInstance instance)
    {
        buffList.Remove(instance);
    }

    public void Remove(E_ElementType elementType)
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            if (buffList[i].ElementType == elementType)
                buffList.RemoveAt(i);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = buffList.Count - 1; i >= 0; i--)
        {
            if (!buffList[i].Update(deltaTime))
                buffList.RemoveAt(i);
        }
    }
}
