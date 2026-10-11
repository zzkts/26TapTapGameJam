/// <summary>
/// 子弹的直线运动轨迹。
/// 按当前方向和速度持续向前移动。
/// </summary>
public sealed class StraightMove : ProjectileMove
{
    public override void Move(Projectile projectile, float deltaTime)
    {
        projectile.MoveForward(deltaTime);
    }
}
