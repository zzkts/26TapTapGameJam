using System.Collections.Generic;
using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    /// <summary>Collision controls gameplay; particles only draw the water.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class WaterProjectile : MonoBehaviour
    {
        [Header("Gameplay")]
        [Min(0f)] public float damage = 1f;
        [Min(0.1f)] public float speed = 12f;
        [Min(0.05f)] public float lifetime = 2f;
        [Min(0f), Tooltip("0 = straight shot; 1 = normal Physics2D gravity.")]
        public float gravityScale;

        [Header("Water look")]
        public Color waterColor = new Color(0.18f, 0.72f, 1f, 1f);
        [Min(0.01f)] public float radius = 0.11f;
        public bool addTrail = true;
        public bool addDroplets = true;
        [Min(0.01f)] public float splashScale = 1f;
        public int sortingOrder = 20;

        private const string PrefabResourcePath = "WaterProjectiles/WaterBullet";
        private static WaterProjectile prefab;
        public GameObject Owner { get; private set; }

        private Rigidbody2D body;
        private CircleCollider2D hitbox;
        private Vector2 velocity;
        private bool launched;
        private bool hasHit;
        private float remainingLifetime;
        private int enemyLayer;
        private int collisionMask;
        private readonly List<RaycastHit2D> castResults = new List<RaycastHit2D>(16);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);

        /// <summary>
        /// 发射水弹
        /// </summary>
        /// <param name="position">发射位置</param>
        /// <param name="direction">发射方向</param>
        /// <param name="damage">扣血量</param>
        /// <param name="speed">速度</param>
        /// <param name="owner">发射者本身用来忽略用</param>
        /// <returns></returns>
        public static WaterProjectile Fire(Vector2 position, Vector2 direction,
            float damage = 1f, float speed = 12f, GameObject owner = null)
        {
            if (direction.sqrMagnitude < 0.0001f || speed <= 0f)
                return null;
            if (LayerMask.NameToLayer("Enemy") < 0 || LayerMask.NameToLayer("Wall") < 0)
            {
                Debug.LogError("水弹需要名为 Enemy 和 Wall 的 Layer。");
                return null;
            }
            if (prefab == null)
                prefab = Resources.Load<WaterProjectile>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError("缺少 Resources/WaterProjectiles/WaterBullet.prefab。");
                return null;
            }
            WaterProjectile bullet = Instantiate(prefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            bullet.damage = Mathf.Max(0f, damage);
            bullet.Launch(direction, owner, speed);
            return bullet;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            // Sweep before every move to catch thin walls and avoid duplicate trigger damage.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            hitbox = GetComponent<CircleCollider2D>();
            hitbox.radius = radius;
            hitbox.isTrigger = true;
            enemyLayer = LayerMask.NameToLayer("Enemy");
            collisionMask = LayerMask.GetMask("Enemy", "Wall");
            BuildVisuals();
        }

        private void OnEnable()
        {
            hasHit = false;
            launched = false;
            remainingLifetime = lifetime;
        }

        /// <summary>Called by a weapon immediately after Instantiate.</summary>
        public void Launch(Vector2 direction, GameObject owner = null, float overrideSpeed = -1f)
        {
            Owner = owner;
            Vector2 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            velocity = normalized * (overrideSpeed > 0f ? overrideSpeed : speed);
            transform.right = normalized;
            launched = true;
            remainingLifetime = lifetime;
        }

        private void FixedUpdate()
        {
            if (!launched || hasHit)
                return;
            float dt = Time.fixedDeltaTime;
            remainingLifetime -= dt;
            if (remainingLifetime <= 0f)
            {
                Finish(false, body.position);
                return;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(collisionMask);
            filter.useTriggers = true;
            Physics2D.OverlapCircle(body.position, WorldRadius, filter, overlaps);
            for (int i = 0; i < overlaps.Count; i++)
            {
                if (CanHit(overlaps[i]))
                {
                    Impact(overlaps[i], overlaps[i].ClosestPoint(body.position));
                    return;
                }
            }

            velocity += Physics2D.gravity * gravityScale * dt;
            Vector2 delta = velocity * dt;
            if (delta.sqrMagnitude <= 0.000001f)
                return;
            Physics2D.CircleCast(body.position, WorldRadius, delta.normalized, filter, castResults, delta.magnitude);
            for (int i = 0; i < castResults.Count; i++)
            {
                RaycastHit2D hit = castResults[i];
                if (!CanHit(hit.collider))
                    continue;
                body.position = hit.centroid;
                Impact(hit.collider, hit.point);
                return;
            }
            transform.right = velocity.normalized;
            body.MovePosition(body.position + delta);
        }

        private float WorldRadius => radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        private bool CanHit(Collider2D other)
        {
            if (other == null || other == hitbox || other.GetComponentInParent<WaterProjectile>() != null)
                return false;
            return Owner == null || (other.gameObject != Owner && !other.transform.IsChildOf(Owner.transform));
        }

        private void Impact(Collider2D other, Vector2 hitPoint)
        {
            hasHit = true;
            hitbox.enabled = false;
            // Wall 只播放水花；只有实际被命中的 Collider2D 位于 Enemy 层时才扣血。
            if (other.gameObject.layer == enemyLayer)
            {
                MonoBehaviour[] behaviours = other.GetComponentsInParent<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IWaterDamageable damageable)
                    {
                        damageable.TakeWaterDamage(damage);
                        break;
                    }
                }
            }
            Finish(true, hitPoint);
        }

        private void Finish(bool splash, Vector2 position)
        {
            hasHit = true;
            if (splash)
                WaterVfx.SpawnSplash(position, waterColor, splashScale, -velocity.normalized, sortingOrder);
            WaterVfx.ReleaseTrail(transform);
            Destroy(gameObject);
        }

        private void BuildVisuals()
        {
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite == null)
                sprite = gameObject.AddComponent<SpriteRenderer>();
            sprite.sprite = WaterVfx.GetDropletSprite();
            sprite.sharedMaterial = WaterVfx.GetSpriteMaterial();
            sprite.color = waterColor;
            sprite.sortingOrder = sortingOrder;
            sprite.drawMode = SpriteDrawMode.Sliced;
            sprite.size = new Vector2(radius * 2.7f, radius * 1.6f);
            if (addTrail)
                WaterVfx.AddRibbon(transform, waterColor, radius, sortingOrder - 1);
            if (addDroplets)
                WaterVfx.AddDropletStream(transform, waterColor, radius, sortingOrder);
        }
    }

}
