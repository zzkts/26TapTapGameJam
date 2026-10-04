using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData baseData;
    [SerializeField] private LayerMask targetLM;
    [SerializeField] private List<Modifier> modifiers = new();

    private WeaponData currentData;
    private float nextAttackTime;

    public WeaponData BaseData => baseData;
    public WeaponData CurrentData => currentData;
    public IReadOnlyList<Modifier> Modifiers => modifiers;

    private void Awake()
    {
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

        for (int i = 0; i < currentData.projectileCount; i++)
        {
            GameObject projectileObject = Instantiate(currentData.projectilePrefab, pos, Quaternion.identity);
            Projectile projectile = projectileObject.GetComponent<Projectile>();
            if (projectile == null)
            {
                Destroy(projectileObject);
                return;
            }

            projectile.SetUp(
                pos,
                dir,
                targetLM,
                currentData.projectileSpeed,
                currentData.damage,
                currentData.damageMultiplier,
                currentData.projectileSizeMultiplier,
                5f);
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
}

