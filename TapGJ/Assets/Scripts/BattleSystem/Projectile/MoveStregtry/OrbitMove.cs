using UnityEngine;

/// <summary>
/// 子弹围绕发射者旋转的运动轨迹。
/// 子弹逐渐靠近指定半径，并按设定角速度环绕移动。
/// </summary>
public sealed class OrbitMove : ProjectileMove
{
    private float radius;
    private float angularSpeed;
    public void Configure(float orbitRadius, float speed) { radius = orbitRadius; angularSpeed = speed; }
    private float rotationDirection;

    public OrbitMove(float radius, float angularSpeed)
    {
        this.radius = radius;
        this.angularSpeed = angularSpeed;
    }

    public override void SetUp(Projectile projectile)
    {
        rotationDirection = projectile.Direction.x >= 0f ? 1f : -1f;
    }

    public override void Move(Projectile projectile, float deltaTime)
    {
        if (projectile.Owner == null) { projectile.MoveForward(deltaTime); return; }
        Vector2 center = projectile.Owner.position;
        Vector2 offset = projectile.Position - center;
        if (offset.sqrMagnitude <= 0f)
            offset = projectile.Direction;

        float nextRadius = Mathf.MoveTowards(offset.magnitude, radius,
            projectile.Speed * deltaTime);
        Vector2 nextOffset = Quaternion.Euler(0f, 0f,
            angularSpeed * rotationDirection * deltaTime) * offset.normalized * nextRadius;

        projectile.MoveBy(center + nextOffset - projectile.Position);
    }
}
