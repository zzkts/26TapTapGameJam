using System;
using UnityEngine;

public readonly struct ResolvedGeneEffect
{
    public readonly GeneEffectKind Kind;
    public readonly GeneTrigger Trigger;
    public readonly float Value;
    public readonly bool Evolved;
    public ResolvedGeneEffect(GeneEffectKind kind, GeneTrigger trigger, float value, bool evolved)
    { Kind = kind; Trigger = trigger; Value = value; Evolved = evolved; }
}

public class ShotSpawnBudget
{
    public int Remaining { get; private set; }
    public ShotSpawnBudget(int capacity) { Remaining = Mathf.Max(1, capacity); }
    public bool Take() { if (Remaining <= 0) return false; Remaining--; return true; }
}

// Each shot owns its values; scene configuration and inventory are never retained here.
public sealed class BulletSnapshot
{
    public float damage, speed, size, lifetime, orbitRadius;
    public int pierce, bounces, maxGeneration, maxSpawn;
    public float reboundRetention;
    public CombatElement element = CombatElement.Water;
    public Color color = new Color(0.3f, 0.8f, 1f);
    public float statusDuration, poisonDuration, poisonRadius, poisonDamage, firePercent, explosionFraction;
    public float electricDamage, contactDistance, waterDuration, waterRadius, vortexDuration, vortexSpeed;
    public ResolvedGeneEffect[] effects = Array.Empty<ResolvedGeneEffect>();
    public BulletSnapshot(WeaponData weapon, GeneRules rules)
    {
        damage = weapon.damage * weapon.damageMultiplier;
        speed = weapon.projectileSpeed;
        size = weapon.projectileSizeMultiplier;
        lifetime = weapon.projectileLifetime;
        pierce = weapon.pierceCount;
        bounces = weapon.bounceCount;
        reboundRetention = weapon.reboundRetention;
        maxGeneration = rules != null ? rules.maxSplitGeneration : 1;
        maxSpawn = rules != null ? rules.maxSpawnPerShot : 24;
        statusDuration = rules != null ? rules.statusDuration : 4f;
        poisonDuration = rules != null ? rules.poisonDuration : 3f;
        poisonRadius = rules != null ? rules.poisonRadius : 2.5f;
        poisonDamage = rules != null ? rules.poisonDamage : 1f;
        firePercent = rules != null ? rules.firePercentPerSecond : 0.05f;
        explosionFraction = rules != null ? rules.explosionHealthFraction : 0.3f;
        electricDamage = rules != null ? rules.electricDamage : 1f;
        contactDistance = rules != null ? rules.contactDistance : 0.8f;
        waterDuration = rules != null ? rules.waterDuration : 4f;
        waterRadius = rules != null ? rules.waterRadius : 0.8f;
        vortexDuration = rules != null ? rules.vortexDuration : 1.5f;
        vortexSpeed = rules != null ? rules.vortexPullSpeed : 2f;
    }
    public BulletSnapshot Copy()
    {
        var copy = (BulletSnapshot)MemberwiseClone();
        copy.effects = (ResolvedGeneEffect[])effects.Clone();
        return copy;
    }
    public void Clamp()
    {
        damage = Mathf.Max(0f, damage);
        speed = Mathf.Clamp(speed, 0.1f, 100f);
        size = Mathf.Clamp(size, 0.05f, 3f);
        lifetime = Mathf.Clamp(lifetime, 0.5f, 10f);
        pierce = Mathf.Clamp(pierce, 0, 32);
        bounces = Mathf.Clamp(bounces, 0, 5);
    }
}
