# Player Movement FSM Design

## Goal

Implement a small prototype-ready 2D player controller using the project's existing FSM and Unity's new Input System. The implementation covers idle, walking, jumping, falling, rolling, hurt, and death while keeping gameplay configuration in `PlayerController`.

## Scope

- Use the existing `State` and `StateMachine` lifecycle.
- Use the existing `Player/Move`, `Player/Jump`, and `Player/Sprint` input actions. `Sprint` triggers the roll state for the prototype.
- Implement movement and state transitions for `Idle`, `Walk`, `Jump`, `Fall`, `Roll`, `Hurt`, and `Death`.
- Keep movement, jump, roll, hurt, health, and ground-check configuration serialized in `PlayerController`.
- Expose configuration and runtime values through read-only properties.
- Keep the Animator optional so a plain square can be used for prototype testing.
- Do not implement camera behavior, animation-event integration, combat reactions beyond the hurt trigger, or a generic transition-table framework.

## Architecture

`PlayerController` owns the Rigidbody, input actions, configuration, state instances, and gameplay APIs. It reads input during `Update`, gives death and hurt transitions global priority, updates the active state, and advances cooldowns. Physics movement remains in state `FixUpdate` methods.

Concrete player states own their local transition rules. A lightweight `PlayerGroundedState` base class shares jump and roll checks between `Idle` and `Walk`; it is not entered as a runtime state.

The generic `StateMachine` remains intentionally small. It only gains a read-only current-state property and ignores requests to switch to the already-active state.

## State Flow

- `Idle -> Walk` when horizontal input is non-zero.
- `Walk -> Idle` when horizontal input becomes zero.
- `Idle/Walk -> Jump` when jump is pressed and the player is grounded.
- `Idle/Walk -> Roll` when roll is pressed, the player is grounded, and the cooldown is ready.
- `Jump -> Fall` when vertical velocity is zero or negative.
- `Fall -> Idle/Walk` after landing, selected from horizontal input.
- `Roll -> Idle/Walk/Fall` when its configurable duration expires.
- `Hurt -> Idle/Walk/Fall` when its configurable duration expires.
- Any non-death state enters `Hurt` after `TakeDamage` requests a hurt reaction.
- Any state enters `Death` when HP reaches zero; death is terminal.

Death has higher priority than hurt. Ground-state jump checks have higher priority than switching between idle and walk.

## Controller API

The controller provides focused mutation methods such as horizontal movement, horizontal stopping, jumping, rolling, taking damage, ground checking, and capability checks. States read input, velocity, health, facing direction, and timing configuration through read-only properties.

Horizontal movement changes only Rigidbody X velocity and preserves Y velocity. Jumping assigns the configured upward velocity once. Rolling assigns a fixed horizontal velocity based on facing direction and starts the cooldown.

## Input

An `InputActionAsset` is assigned in the Inspector. The controller finds the `Player` action map and its `Move`, `Jump`, and `Sprint` actions, enables the map while active, and disables it when the component is disabled. Horizontal input comes from `Move.ReadValue<Vector2>().x`; jump and roll use per-frame pressed checks.

## Prototype Configuration

All tunable values stay in `PlayerController` for now: maximum HP, move speed, jump velocity, roll speed, roll duration, roll cooldown, hurt duration, ground-check distance, and ground layer mask. A future data/config wrapper can replace these serialized fields without changing state responsibilities.

## Error Handling and Scene Setup

The Rigidbody and input action asset are required scene references. Animator and SpriteRenderer are optional for the square prototype. Ground detection uses the configured foot transform, distance, and layer mask. Missing required action names fail with a clear Unity error rather than silently accepting an unusable controller.

## Verification

- Compile the Unity project without C# errors.
- Verify idle and walk switching with horizontal input.
- Verify jump occurs only while grounded and transitions through jump and fall.
- Verify landing selects idle or walk based on current horizontal input.
- Verify roll duration and cooldown.
- Call `TakeDamage` to verify hurt and death priority.
- Verify a player without an Animator can run the prototype logic.
