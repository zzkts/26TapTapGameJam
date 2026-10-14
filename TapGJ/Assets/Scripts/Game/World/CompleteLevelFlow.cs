using System;
using UnityEngine;

/// <summary>The chapter's progress, guardian encounter and safe fall recovery.</summary>
public sealed class CompleteLevelFlow : MonoBehaviour
{
    public PlayerController player;
    public EnemyAIController[] guardians = Array.Empty<EnemyAIController>();
    public GameObject[] barriers = Array.Empty<GameObject>();
    public Vector2 spawn = new Vector2(3f, 1f);
    public Vector2[] checkpoints = { new Vector2(16f, 1f), new Vector2(41f, 1f), new Vector2(78f, 1f) };
    public float firstRegionEnd = 38f, secondRegionEnd = 78f, recoveryY = -5f;
    public float guardianEntryX=84f;
    public bool CoreDefeated { get; private set; }
    public bool BattleStarted { get; private set; }
    public bool ExitUsed { get; private set; }
    public int Region { get; private set; } = -1;
    public int FallRecoveries { get; private set; }
    public Vector2 SafePosition { get; private set; }
    public event Action CoreCleared;
    public event Action ExitReached;
    public event Action<int> RegionEntered;

    private void Awake() => SafePosition = spawn;
    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        RefreshRegion();
    }
    private void Update()
    {
        if (player == null || player.IsDead) return;
        RefreshRegion();
        if (!player.MenuOpen && player.IsGrounded())
            foreach (Vector2 checkpoint in checkpoints)
                if (Mathf.Abs(player.transform.position.x - checkpoint.x) < 1.3f)
                    SafePosition = checkpoint;
        if (BattleStarted && !CoreDefeated && GuardiansDefeated())
        {
            CoreDefeated = true;
            foreach (GameObject barrier in barriers) if (barrier != null) barrier.SetActive(false);
            CoreCleared?.Invoke();
        }
    }
    private void FixedUpdate()
    {
        if (player == null || player.IsDead || player.MenuOpen || player.transform.position.y >= recoveryY) return;
        RecoverFromFall();
    }
    private void RefreshRegion()
    {
        if (player == null) return;
        int next = player.transform.position.x < firstRegionEnd ? 0 : player.transform.position.x < secondRegionEnd ? 1 : 2;
        if (next == Region) return;
        Region = next; RegionEntered?.Invoke(Region);
    }
    public void SetCheckpoint(Vector2 position) => SafePosition = position;
    public void RecoverFromFall()
    {
        if (player == null || player.IsDead) return;
        if (player.IsDashing) player.Machine.Switch(player.FallState);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        body.position = SafePosition;
        body.linearVelocity = Vector2.zero;
        player.transform.position = new Vector3(SafePosition.x, SafePosition.y, player.transform.position.z);
        Physics2D.SyncTransforms();
        // Fall recovery costs one life point; the last point is kept so it never strands the run.
        if (player.hp > 1f) player.TakeDamageFrom(Mathf.Min(1f, player.hp - 1f), SafePosition + Vector2.down);
        FallRecoveries++;
    }
    public void BeginGuardianBattle()
    {
        if (CoreDefeated || BattleStarted) return;
        BattleStarted = true;
        foreach (EnemyAIController guardian in guardians)
            if (guardian != null) { guardian.gameObject.SetActive(true); guardian.SetTarget(player != null ? player.transform : null); }
        foreach (GameObject barrier in barriers) if (barrier != null) barrier.SetActive(true);
    }
    private bool GuardiansDefeated()
    {
        if (guardians == null || guardians.Length == 0) return false;
        foreach (EnemyAIController guardian in guardians) if (guardian != null && !guardian.IsDead) return false;
        return true;
    }
    public void RestoreCoreCleared(bool cleared)
    {
        CoreDefeated = cleared; BattleStarted = cleared; ExitUsed = false;
        foreach (EnemyAIController guardian in guardians) if (guardian != null) guardian.gameObject.SetActive(false);
        foreach (GameObject barrier in barriers) if (barrier != null) barrier.SetActive(!cleared);
    }
    public bool ReachExit(PlayerController owner)
    {
        if (!CoreDefeated || ExitUsed || owner == null || owner.IsDead || owner.MenuOpen) return false;
        ExitUsed = true; ExitReached?.Invoke(); return true;
    }
}
