public sealed class ShotSpawnBudget
{
    public int Remaining { get; private set; }
    public ShotSpawnBudget(int limit) { Remaining = System.Math.Max(0, limit); }
    public bool Take() { if (Remaining <= 0) return false; Remaining--; return true; }
}
