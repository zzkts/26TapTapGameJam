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

    public GameObject projectilePrefab;
}
