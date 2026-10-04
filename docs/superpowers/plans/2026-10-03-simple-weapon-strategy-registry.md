# Simple Weapon Strategy Registry Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace attack-plan composition with one registered single-shot straight-line strategy.

**Architecture:** WeaponMgr owns string-keyed strategy and projectile registries. Weapon runtime data stores lookup IDs, Weapon checks cooldown, and the selected strategy performs the concrete attack.

**Tech Stack:** Unity 6, C#, NUnit, ScriptableObject, Rigidbody2D

---

### Task 1: Define registry behavior with tests

**Files:**
- Create: `TapGJ/Assets/Tests/Editor/WeaponMgrTests.cs`
- Create: `TapGJ/Assets/Scripts/BattleSystem/Weapon/WeaponMgr.cs`

- [ ] Add a failing test proving `single_straight` is registered by default.
- [ ] Add a failing test proving a ProjectileData can be registered and retrieved by string ID.
- [ ] Implement the minimal registry API and make both tests pass.

### Task 2: Simplify strategy execution

**Files:**
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Strategy/IAttackStrategy.cs`
- Create: `TapGJ/Assets/Scripts/BattleSystem/Strategy/SingleStraightAttackStrategy.cs`
- Delete: `TapGJ/Assets/Scripts/BattleSystem/Strategy/AttackPlan.cs`
- Delete: `TapGJ/Assets/Scripts/BattleSystem/Strategy/CompositeAttackStrategy.cs`
- Delete: `TapGJ/Assets/Scripts/BattleSystem/Strategy/ProjectileSpawnRequest.cs`

- [ ] Change the interface to one Attack method.
- [ ] Implement exactly one straight projectile spawn per call.
- [ ] Remove the three obsolete planning types.

### Task 3: Migrate weapon data and modifiers

**Files:**
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Weapon/WeaponData.cs`
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Weapon/WeaponRuntimeData.cs`
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Weapon/Weapon.cs`
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Modifier/MechanismModifier.cs`
- Modify: `TapGJ/Assets/Scripts/BattleSystem/Modifier/ModifierCenter.cs`

- [ ] Store strategy and projectile string IDs in runtime data.
- [ ] Delegate Weapon attacks through WeaponMgr.
- [ ] Let later mechanism modifiers replace earlier strategy IDs.
- [ ] Keep unresolved element fusion outside the active attack path.

### Task 4: Verify

- [ ] Run EditMode tests and confirm zero failures.
- [ ] Run Unity DX11 batch compilation and confirm return code zero.
- [ ] Confirm Strategy contains only two C# source files.
- [ ] Check the targeted source files for whitespace errors.
