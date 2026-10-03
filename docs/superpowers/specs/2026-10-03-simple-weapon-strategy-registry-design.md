# Simple Weapon Strategy Registry Design

## Goal

Reduce the prototype attack path to one strategy interface, one single-shot straight-line implementation, and a string-keyed WeaponMgr registry for strategies and projectile data.

## Boundaries

- `IAttackStrategy` exposes only `Attack(AttackContext, WeaponRuntimeData)`.
- `SingleStraightAttackStrategy` creates exactly one projectile and launches it along the context direction.
- `Weapon` owns cooldown and delegates attacks; it does not instantiate projectiles.
- `WeaponMgr` registers and retrieves attack strategies and projectile data by string ID.
- `WeaponRuntimeData` stores the strategy ID and projectile ID used for lookup.
- `WeaponMgr` registers the built-in straight strategy automatically.
- Projectile assets are registered explicitly through `RegisterProjectile` because no projectile assets exist yet.
- Mechanism modifiers may replace the strategy ID. If several exist, the later modifier wins.

## Out of Scope

- Strategy decorators or composition.
- Burst, spread, and delayed attacks.
- Gene combination rules.
- Weapon appearance generation.
- Element fusion rules.

## Verification

- Registry tests cover built-in strategy lookup and projectile registration.
- Unity compiles in DX11 batch mode.
- The Strategy directory contains only the interface and straight-line implementation.
