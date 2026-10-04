# Battle System Framework Design

## Goal

Create a small reusable battle framework for players and monsters. The framework defines weapon, modifier, attack-strategy, and projectile boundaries without deciding unfinished game-design rules.

## Scope

- `WeaponData`, `ProjectileData`, and modifier definitions use ScriptableObject assets.
- A Weapon owns a list of modifiers and supports adding and removing entries.
- Every list change rebuilds the complete runtime weapon data from the original WeaponData.
- Modifier types are base value, mechanism, and element.
- Mechanism modifiers are combined into one composite attack strategy.
- Element fusion exposes an extension point but does not define any fusion rules.
- Weapon and projectile code must not depend on PlayerController, so monsters can reuse it.
- Projectile initialization, launching, and movement are implemented.
- Damage-on-hit, buffs, skills, concrete modifiers, fusion tables, and object-pool integration remain out of scope.

## Runtime Data Flow

```text
WeaponData + List<WeaponModifier>
                |
                v
       ModifierCenter.Resolve
                |
                v
       WeaponRuntimeData
       - final numeric values
       - ProjectileData
       - IAttackStrategy
                |
                v
       Weapon.CurrentData
                |
                v
            Attack
```

ModifierCenter creates a fresh WeaponRuntimeData for every resolution. It applies all base-value modifiers, collects all mechanism strategies into one CompositeAttackStrategy, and then asks the element-fusion extension point whether the projectile should be replaced.

## Components

### Weapon

Weapon owns the base data, fire point, modifier list, and current runtime data. `AddModifier` and `RemoveModifier` call `ReapplyModifiers`. Attack reads only CurrentData and never traverses modifiers.

### WeaponRuntimeData

This is a normal runtime C# object, not an asset. It contains the final attack values, one ProjectileData reference, and one IAttackStrategy reference.

### ModifierCenter

ModifierCenter is responsible for rebuilding and classifying modifiers. It contains the unresolved element-fusion TODO so the later design can be added without changing Weapon.

### Mechanism Strategy

Weapon stores one IAttackStrategy. ModifierCenter builds a CompositeAttackStrategy containing all mechanism strategy steps in modifier-list order, allowing combinations such as burst plus spread while keeping a single strategy reference.

### Projectile

ProjectileData contains projectile prefab and movement configuration. Projectile receives runtime launch information, launches, moves, and expires. Collision damage and pooling are future integrations.

## Public API

```csharp
Weapon.AddModifier(WeaponModifier modifier);
Weapon.RemoveModifier(WeaponModifier modifier);
Weapon.ReapplyModifiers();
Weapon.Attack(AttackContext context);

ModifierCenter.Resolve(
    WeaponData baseData,
    IReadOnlyList<WeaponModifier> modifiers);
```

## Unresolved Design Rules

- Element compatibility, fusion priority, and fusion chaining.
- Concrete base-value modifier formulas.
- Concrete mechanism modifiers and delayed firing behavior.
- Projectile damage, hit effects, and recycling.

These locations receive short TODO comments only.

## Verification

- Unity scripts compile without errors.
- Resolving always starts from WeaponData rather than previously resolved values.
- Adding and removing modifiers replaces CurrentData.
- Multiple mechanism modifiers appear in one CompositeAttackStrategy.
- Weapon can attack without referencing player-specific code.
