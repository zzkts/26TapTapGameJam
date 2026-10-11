using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家当前使用的武器。
/// 攻击时读取装备栏，生成本次子弹快照并发射子弹。
/// </summary>
public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData baseData;
    [SerializeField] private LayerMask targetLM;

    private float nextAttackTime;
    private BulletSnapshot currentShot;

    public WeaponData BaseData => baseData;
    public BulletSnapshot CurrentShot => currentShot;

    private void Awake()
    {
        currentShot = new BulletSnapshot(baseData);
    }

    public bool CanAttack()
    {
        return currentShot.projectilePrefab != null &&
               Time.time >= nextAttackTime;
    }

    public void Apply(IReadOnlyList<Modifier> modifiers)
    {
        currentShot = ModifierSystem.Instance.Apply(baseData, modifiers);
    }

    public void Attack(Vector2 pos, Vector2 dir)
    {
        if (!CanAttack()) return;

        BulletSnapshot shot = currentShot;
        nextAttackTime = Time.time + shot.attackInterval;

        for (int i = 0; i < shot.projectileCount; i++)
        {
            GameObject projectileObject = Instantiate(shot.projectilePrefab, pos, Quaternion.identity);
            Projectile projectile = projectileObject.GetComponent<Projectile>();
            float angle = shot.projectileCount <= 1 ? 0f : Mathf.Lerp(-shot.spreadAngle,
                shot.spreadAngle, i / (float)(shot.projectileCount - 1));
            Vector2 heading = Quaternion.Euler(0f, 0f, angle) * dir;
            projectile.SetOwner(transform);
            projectile.SetUp(pos, heading, targetLM, shot.speed, shot.damage,
                shot.damageMultiplier, shot.size, shot.lifeTime, shot.pierceCount,
                shot.reboundCount, shot.reboundCoefficient, shot.moveType,
                shot.orbitRadius, shot.orbitAngularSpeed);
        }
    }
}

