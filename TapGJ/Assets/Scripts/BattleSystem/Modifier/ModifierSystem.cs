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

    public BulletSnapshot Apply(WeaponData src, IReadOnlyList<Modifier> modifiers)
    {
        BulletSnapshot result = new(src);

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
                    ApplyElement(result, modifier.ModifierValue);
                    break;
                case E_ModifierType.ProjectileMove:
                    ApplyMove(result, modifier.ModifierValue);
                    break;
            }
        }

        result.Clamp();
        return result;
    }

    private void ApplyElement(BulletSnapshot result, float value)
    {
        E_ElementType elementType = ElementSystem.GetElementType(value);
        result.projectilePrefab = ElementSystem.GetProjectilePrefab(elementType);
    }

    private void ApplyMove(BulletSnapshot result, float value)
    {
        result.moveType = (E_ProjectileMoveType)Mathf.RoundToInt(value);

        switch (result.moveType)
        {
            case E_ProjectileMoveType.Orbit:
                result.orbitRadius = moveData.orbit.radius;
                result.orbitAngularSpeed = moveData.orbit.angularSpeed;
                break;
        }
    }
}
