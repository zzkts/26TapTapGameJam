/// <summary>
/// 子弹运动轨迹的基础类型。
/// 每种轨迹只负责计算本帧位移。
/// </summary>
public abstract class ProjectileMove
{
    public virtual void SetUp(Projectile projectile)
    {
    }

    public abstract void Move(Projectile projectile, float deltaTime);
}
