using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData baseData;
    [SerializeField] private LayerMask targetLM;
    private float nextAttackTime;
    private BulletSnapshot currentShot;
    private GeneInventory inventory;
    private GeneChainController chain;
    public WeaponData BaseData => baseData;
    public BulletSnapshot CurrentShot => currentShot;
    private void Awake()
    {
        if (baseData == null) baseData = Resources.Load<WeaponData>("SO/Weapon/DefaultWaterBallWeaponData");
        inventory = GetComponent<GeneInventory>(); chain = GetComponent<GeneChainController>();
        if (baseData != null) currentShot = new BulletSnapshot(baseData);
    }
    public bool CanAttack() => currentShot != null && currentShot.projectilePrefab != null && Time.time >= nextAttackTime;
    public void Apply(IReadOnlyList<Modifier> modifiers)
    {
        if (baseData == null) return;
        currentShot = ModifierSystem.Instance.Apply(baseData, modifiers, chain != null ? chain.ActiveAttribute : null);
    }
    public void Attack(Vector2 position, Vector2 direction)
    {
        if (!CanAttack()) return;
        if (inventory != null) Apply(inventory.GenChain);
        BulletSnapshot shot = currentShot.Copy();
        nextAttackTime = Time.time + Mathf.Max(0.04f, shot.attackInterval);
        if(GetComponent<PlayerController>()!=null)GameAudioPresentation.PlayWorld(shot.element==E_ElementType.Fire?"fire_shot":shot.element==E_ElementType.Poison?"poison_shot":"water_shot",position);
        ShotSpawnBudget budget = new(shot.maxSpawn);
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        for (int i = 0; i < shot.projectileCount && budget.Take(); i++)
        {
            float angle = shot.projectileCount <= 1 ? 0f : Mathf.Lerp(-shot.spreadAngle, shot.spreadAngle, i / (float)(shot.projectileCount - 1));
            Projectile projectile = Instantiate(shot.projectilePrefab, position, Quaternion.identity).GetComponent<Projectile>();
            if (projectile == null) continue;
            projectile.LaunchSnapshot(position, Quaternion.Euler(0f, 0f, angle) * direction, targetLM, transform, shot, budget);
        }
    }
}

