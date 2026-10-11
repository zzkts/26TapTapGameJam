using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData baseData;
    [SerializeField] private LayerMask targetLM;
    [SerializeField] private List<Modifier> modifiers = new();

    private WeaponData currentData;
    private float nextAttackTime;
    private GeneChainController genes;

    public WeaponData BaseData => baseData;
    public WeaponData CurrentData => currentData;
    public IReadOnlyList<Modifier> Modifiers => modifiers;

    private void Awake()
    {
        genes = GetComponent<GeneChainController>();
        if (baseData == null) baseData = Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        ReapplyModifiers();
    }

    public bool CanAttack()
    {
        return currentData != null &&
               currentData.projectilePrefab != null &&
               Time.time >= nextAttackTime;
    }

    public void Attack(Vector2 pos, Vector2 dir)
    {
        if (!CanAttack()) return;

        nextAttackTime = Time.time + currentData.attackInterval;

        BulletSnapshot shot = genes != null ? genes.Capture(currentData) : new BulletSnapshot(currentData, null);
        var budget = new ShotSpawnBudget(shot.maxSpawn);
        for (int i = 0; i < currentData.projectileCount && budget.Take(); i++)
        {
            GameObject projectileObject = Instantiate(currentData.projectilePrefab, pos, Quaternion.identity);
            Projectile projectile = projectileObject.GetComponent<Projectile>();
            float angle = currentData.projectileCount <= 1 ? 0f : Mathf.Lerp(-currentData.spreadAngle,
                currentData.spreadAngle, i / (float)(currentData.projectileCount - 1));
            Vector2 heading = Quaternion.Euler(0f, 0f, angle) * dir;
            if (projectile is WaterProjectile water)
                water.Launch(shot, pos, heading, targetLM, transform, currentData.projectilePrefab, budget);
            else if (projectile != null)
                projectile.SetUp(pos, heading, targetLM, shot.speed, shot.damage, 1f, shot.size, shot.lifetime);
            else Destroy(projectileObject);
        }
    }

    public void AddModifier(Modifier modifier)
    {
        if (modifier == null)
        {
            return;
        }

        modifiers.Add(modifier);
        ReapplyModifiers();
    }

    public void RemModifier(uint modifierId)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].Id != modifierId)
            {
                continue;
            }

            modifiers.RemoveAt(i);
            ReapplyModifiers();
            return;
        }
    }

    private void ReapplyModifiers()
    {
        if (currentData != null)
        {
            Destroy(currentData);
        }

        currentData = ModifierSystem.Instance.ApplyAllModify(baseData, modifiers);
    }
    private void OnDestroy() { if (currentData != null) Destroy(currentData); }
}

