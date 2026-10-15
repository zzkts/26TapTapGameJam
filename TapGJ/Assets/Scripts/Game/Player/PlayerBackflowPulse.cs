using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BackflowPulseReport
{
    public readonly bool pulling;
    public readonly int movedBodies, shiftedWaterFields;
    public readonly Vector2 direction;
    public BackflowPulseReport(bool pulling, int bodies, int fields, Vector2 direction)
    { this.pulling = pulling; movedBodies = bodies; shiftedWaterFields = fields; this.direction = direction; }
}

/// <summary>A water-control action: wet movable materials carry momentum, existing water fields carry location.</summary>
[DisallowMultipleComponent]
public sealed class PlayerBackflowPulse : MonoBehaviour
{
    public event Action<BackflowPulseReport> Pulsed;
    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public float Cooldown => cooldown;
    public float Radius => radius;
    public int PulseCount { get; private set; }
    public int LastMovedBodies { get; private set; }
    public int LastShiftedWaterFields { get; private set; }
    public bool LastPulling { get; private set; }
    private PlayerController player;
    private float cooldown = 2f, radius = 3.2f, impulse = 2.8f, fieldShift = .75f;
    private float readyAt, effectAt = float.NegativeInfinity;
    private Vector2 effectOrigin;
    private LineRenderer ring;
    private Material ringMaterial;
    private readonly RaycastHit2D[] blockers = new RaycastHit2D[32];
    private ContactFilter2D collisionFilter;
    private bool recordedMomentum;

    public void Initialize(PlayerController owner, PlayerData data)
    {
        player = owner;
        if (data != null)
        {
            cooldown = Mathf.Max(.1f, data.pulseCooldown); radius = Mathf.Clamp(data.pulseRadius, .5f, 5f);
            impulse = Mathf.Clamp(data.pulseImpulse, 0f, 4f); fieldShift = Mathf.Clamp(data.pulseFieldShift, 0f, .75f);
        }
        collisionFilter = new ContactFilter2D { useTriggers = false };
        collisionFilter.SetLayerMask(LayerMask.GetMask("Default", "Ground", "Wall", "OneWayPlatform"));
    }

    public bool TryPulse(bool pulling = false)
    {
        if (player == null) player = GetComponent<PlayerController>();
        if (player == null || !player.enabled || !isActiveAndEnabled || player.MenuOpen || player.IsDead ||
            player.IsHurt || player.IsDashing || CooldownRemaining > 0f) return false;
        Collider2D ownerCollider = player.GetComponent<Collider2D>();
        Vector2 origin = ownerCollider != null ? ownerCollider.bounds.center : player.transform.position;
        Vector2 heading = (player.AimPosition - origin).normalized;
        if (heading.sqrMagnitude < .01f) heading = Vector2.right * player.FaceDir;
        readyAt = Time.time + cooldown; PulseCount++; LastPulling = pulling;
        LastMovedBodies = LastShiftedWaterFields = 0;
        var bodies = new List<Rigidbody2D>(); var positions = new List<Vector2>();
        foreach (EmergenceBody material in new List<EmergenceBody>(EmergenceBody.Active))
        {
            if (material == null || material.Broken || !material.Wet || !material.Has(EmergenceMaterial.Movable)) continue;
            Rigidbody2D body = material.GetComponent<Rigidbody2D>();
            if (body == null || !body.simulated || body.bodyType != RigidbodyType2D.Dynamic) continue;
            Vector2 delta = body.worldCenterOfMass - origin;
            if (delta.magnitude > radius || Vector2.Dot(delta.normalized, heading) < -.15f ||
                !ClearLine(origin, body.worldCenterOfMass, material.transform)) continue;
            Vector2 direction = pulling ? -delta.normalized : heading;
            body.AddForce(direction * impulse, ForceMode2D.Impulse);
            // Continuous collision detection preserves thin walls during a strong horizontal push.
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            bodies.Add(body); positions.Add(body.position);
        }
        foreach (ElementField field in new List<ElementField>(ElementField.Active))
        {
            if (field == null || field.element != E_ElementType.Water) continue;
            Vector2 location = field.transform.position, delta = location - origin;
            if (delta.magnitude > radius || Vector2.Dot(delta.normalized, heading) < -.15f ||
                !ClearLine(origin, location, field.transform)) continue;
            Vector2 direction = pulling ? -delta.normalized : heading;
            // Transport is bounded per use and keeps the field's original expiry and element.
            float distance = Mathf.Min(fieldShift, pulling ? Mathf.Max(0f, delta.magnitude - .25f) : fieldShift);
            float allowed = ClearDistance(location, direction, distance, field.transform);
            if (allowed <= .01f) continue;
            field.transform.position += (Vector3)(direction * allowed); LastShiftedWaterFields++;
        }
        effectAt = Time.time; effectOrigin = origin; EnsureRing();
        player.Presentation?.Shoot(heading);
        TapGJ.WaterProjectiles.WaterVfx.SpawnSplash(origin, new Color(.35f, 1f, .82f), .75f, pulling ? -heading : heading);
        StartCoroutine(ReportAfterPhysics(bodies, positions, LastShiftedWaterFields, heading, pulling));
        return true;
    }

    private IEnumerator ReportAfterPhysics(List<Rigidbody2D> bodies, List<Vector2> before, int fields, Vector2 heading, bool pulling)
    {
        yield return new WaitForFixedUpdate();
        int moved = 0;
        for (int i = 0; i < bodies.Count; i++)
            if (bodies[i] != null && (bodies[i].position - before[i]).sqrMagnitude > .0001f) moved++;
        LastMovedBodies = moved;
        if (moved > 0 && !recordedMomentum)
        {
            recordedMomentum = true;
            EmergenceReactions.Emit("inertia", effectOrigin, pulling ? "湿润箱体 + 回流 → 拉回 → 机关重量改变" : "湿润箱体 + 脉冲 → 惯性推动 → 机关重量改变", moved);
        }
        Pulsed?.Invoke(new BackflowPulseReport(pulling, moved, fields, heading));
    }
    private bool ClearLine(Vector2 from, Vector2 to, Transform target)
    { Vector2 delta = to - from; return ClearDistance(from, delta.normalized, delta.magnitude, target) >= delta.magnitude - .01f; }
    private float ClearDistance(Vector2 origin, Vector2 direction, float distance, Transform target)
    {
        if (distance <= 0f) return 0f;
        int count = Physics2D.Raycast(origin, direction, collisionFilter, blockers, distance);
        float allowed = distance;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = blockers[i].collider;
            if (hit == null || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(target) ||
                hit.GetComponentInParent<EnemyAIController>() != null) continue;
            if(hit.GetComponent<OneWayPlatform>()!=null&&origin.y<hit.bounds.max.y)continue;
            EmergenceBody material = hit.GetComponentInParent<EmergenceBody>();
            if (material != null && material.Has(EmergenceMaterial.Movable)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, blockers[i].distance - .06f));
        }
        return allowed;
    }
    private void EnsureRing()
    {
        if (ring != null) return;
        var effect = new GameObject("Backflow Pulse Ring") { hideFlags = HideFlags.DontSave };
        effect.transform.SetParent(transform, false); ring = effect.AddComponent<LineRenderer>();
        ring.positionCount = 49; ring.useWorldSpace = true; ring.numCapVertices = 3;
        ring.startWidth = ring.endWidth = .055f; ring.sortingOrder = 25;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) { ringMaterial = new Material(shader) { hideFlags = HideFlags.DontSave }; ring.sharedMaterial = ringMaterial; }
    }
    private void LateUpdate()
    {
        if (ring == null) return;
        float progress = (Time.time - effectAt) / .35f; ring.enabled = progress >= 0f && progress < 1f;
        if (!ring.enabled) return;
        float wave = Mathf.Lerp(LastPulling ? radius : .3f, LastPulling ? .3f : radius, progress);
        Color color = new Color(.4f, 1f, .87f, (1f - progress) * .7f); ring.startColor = ring.endColor = color;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / (ring.positionCount - 1);
            ring.SetPosition(i, new Vector3(effectOrigin.x + Mathf.Cos(angle) * wave, effectOrigin.y + Mathf.Sin(angle) * wave * .5f, 0f));
        }
    }
    private void OnDisable() { if (ring != null) ring.enabled = false; }
    private void OnDestroy() { if (ringMaterial != null) Destroy(ringMaterial); }
}
