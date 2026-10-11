using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Weapon Data", fileName = "WeaponData")]
public class WeaponData : ScriptableObject
{
    public float damage = 1f;
    public float damageMultiplier = 1f;
    public float attackInterval = 0.2f;

    public float projectileSpeed = 5f;
    public int projectileCount = 1;
    public float projectileSizeMultiplier = 1f;
    [Min(0.5f)] public float projectileLifetime = 3f;
    [Range(0, 3)] public int pierceCount;
    [Range(0, 5)] public int bounceCount;
    [Range(0f, 1f)] public float reboundRetention = 1f;
    [Range(0f, 90f)] public float spreadAngle = 12f;

    public GameObject projectilePrefab;
}
