using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基因修改的结算中心。
/// 按装备顺序将基因应用到本次攻击快照。
/// </summary>
public class ModifierSystem
{
    private const string MoveDataPath = "SO/Weapon/MoveStrategyData";
    private static ModifierSystem instance;
    private readonly MoveStrategyData moveData;
    public static ModifierSystem Instance
    {
        get
        {
            if (instance == null)
                instance = new ModifierSystem();
            return instance;
        }
    }

    private ModifierSystem()
    {
        moveData = Resources.Load<MoveStrategyData>(MoveDataPath);
    }

    public BulletSnapshot Apply(WeaponData src, IReadOnlyList<Modifier> modifiers, Modifier activeAttribute = null)
    {
        BulletSnapshot result = new(src);

        // Attribute genes and empty slots do not contribute to the split gene's build scaling.
        // Split is itself a modification gene and participates in this count.
        int modificationCount = 0;
        foreach (Modifier gene in modifiers)
            if (gene != null && !gene.IsAttribute && gene.ModifierType != E_ModifierType.None)
                modificationCount++;

        for (int i = 0; i < modifiers.Count; i++)
        {
            Modifier modifier = modifiers[i];
            if (modifier == null)
                continue;

            switch (modifier.ModifierType)
            {
                case E_ModifierType.Atk:
                    result.damage += modifier.ModifierValue;
                    break;
                case E_ModifierType.DamageMultiplier:
                    result.damageMultiplier += modifier.ModifierValue;
                    break;
                case E_ModifierType.ProjectileSpeed:
                    result.speed += modifier.ModifierValue;
                    break;
                case E_ModifierType.ProjectileCount:
                    result.projectileCount += Mathf.RoundToInt(modifier.ModifierValue);
                    break;
                case E_ModifierType.ProjectileSize:
                    result.size += modifier.ModifierValue;
                    break;
                case E_ModifierType.ProjectileLifetime:
                    result.lifeTime += modifier.ModifierValue;
                    break;
                case E_ModifierType.Pierce:
                    result.pierceCount += Mathf.RoundToInt(modifier.ModifierValue);
                    break;
                case E_ModifierType.Rebound:
                    result.reboundCount += Mathf.RoundToInt(modifier.ModifierValue);
                    break;
                case E_ModifierType.ReboundCoefficient:
                    result.reboundCoefficient += modifier.ModifierValue;
                    break;
                case E_ModifierType.ProjectileSpeedMultiplier:
                    result.speed *= modifier.ModifierValue;
                    break;
                case E_ModifierType.Element:
                    break;
                case E_ModifierType.Split:
                    // One extra launch bullet makes a base shot two; each equipped modification
                    // then contributes +1 (normal) or +2 (evolved). Never enable impact splitting.
                    result.projectileCount += 1 + modificationCount * Mathf.RoundToInt(modifier.ModifierValue);
                    break;
                case E_ModifierType.ProjectileMove:
                    ApplyMove(result, modifier.ModifierValue);
                    break;
            }
        }

        if (activeAttribute != null)
        {
            ApplyElement(result, (float)activeAttribute.Element);
            result.element = activeAttribute.Element;
            result.effectRadius = activeAttribute.Radius;
            result.vortex = activeAttribute.IsVortex;
            result.movingVortex = activeAttribute.IsVortex && activeAttribute.Level > 1;
        }
        result.Clamp();
        return result;
    }

    private void ApplyElement(BulletSnapshot result, float value)
    {
        E_ElementType elementType = ElementSystem.GetElementType(value);
        GameObject prefab = ElementSystem.GetProjectilePrefab(elementType);
        if (prefab != null) result.projectilePrefab = prefab;
    }

    private void ApplyMove(BulletSnapshot result, float value)
    {
        result.moveType = (E_ProjectileMoveType)Mathf.RoundToInt(value);

        switch (result.moveType)
        {
            case E_ProjectileMoveType.Orbit:
                result.orbitRadius = moveData != null ? moveData.orbit.radius : 2f;
                result.orbitAngularSpeed = moveData != null ? moveData.orbit.angularSpeed : 180f;
                break;
        }
    }
}
