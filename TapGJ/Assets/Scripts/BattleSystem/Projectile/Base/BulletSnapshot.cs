using UnityEngine;

/// <summary>
/// 一次攻击最终使用的运行时数据。
/// 从武器基础数据复制后再叠加当前装备的基因，不修改原始配置。
/// </summary>
public sealed class BulletSnapshot
{
    public float damage;
    public float damageMultiplier;
    public float attackInterval;
    public float speed;
    public int projectileCount;
    public float size;
    public float lifeTime;
    public int pierceCount;
    public int reboundCount;
    public float reboundCoefficient;
    public float spreadAngle;
    public GameObject projectilePrefab;
    public E_ProjectileMoveType moveType;
    public float orbitRadius;
    public float orbitAngularSpeed;
    public E_ElementType element;
    public float effectRadius;
    public bool vortex, movingVortex;
    public int splitCount, maxGeneration = 1, maxSpawn = 24;

    public BulletSnapshot Copy() => (BulletSnapshot)MemberwiseClone();

    public BulletSnapshot(WeaponData weapon)
    {
        damage = weapon.damage;
        damageMultiplier = weapon.damageMultiplier;
        attackInterval = weapon.attackInterval;
        speed = weapon.projectileSpeed;
        projectileCount = weapon.projectileCount;
        size = weapon.projectileSizeMultiplier;
        lifeTime = weapon.projectileLifetime;
        pierceCount = weapon.pierceCount;
        reboundCount = weapon.reboundCount;
        reboundCoefficient = weapon.reboundCoefficient;
        spreadAngle = weapon.spreadAngle;
        projectilePrefab = weapon.projectilePrefab;
        Projectile template = projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>() : null;
        element = template != null ? template.ElementType : E_ElementType.None;
        moveType = E_ProjectileMoveType.Straight;
    }

    public void Clamp()
    {
        damage = Mathf.Max(0f, damage);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        attackInterval = Mathf.Max(0.04f, attackInterval);
        speed = Mathf.Clamp(speed, 0.1f, 100f);
        size = Mathf.Clamp(size, 0.05f, 3f);
        lifeTime = Mathf.Clamp(lifeTime, 0.1f, 30f);
        projectileCount = Mathf.Clamp(projectileCount, 1, maxSpawn);
        pierceCount = Mathf.Max(0, pierceCount);
        reboundCount = Mathf.Max(0, reboundCount);
        reboundCoefficient = Mathf.Clamp01(reboundCoefficient);
        orbitRadius = Mathf.Max(0.1f, orbitRadius);
        orbitAngularSpeed = Mathf.Max(0f, orbitAngularSpeed);
    }
}
