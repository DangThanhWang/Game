using UnityEngine;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private float fireRate = 0.12f;
        [SerializeField] private float missileSpeed = 8f;
        [SerializeField] private float missileLifetime = 15f;

        [Header("Spawn Settings")]
        [SerializeField] private Transform firePoint;

        private float lastFireTime;
        private const string MISSILE_POOL = "Missiles";

        private void Start()
        {
            if (firePoint == null)
                Debug.LogError("[WeaponController] FirePoint chưa gán!");

            if (PoolManager.Instance != null && missilePrefab != null)
            {
                PoolManager.Instance.CreatePool(MISSILE_POOL, missilePrefab, 10, 50, true);
            }

            EventManager.Subscribe<MissileDestroyedEvent>(OnMissileDestroyed);
        }

        private void OnDestroy()
        {
            EventManager.Unsubscribe<MissileDestroyedEvent>(OnMissileDestroyed);
        }

        private void Update()
        {
            // Auto fire (test)
            if (Time.time >= lastFireTime + fireRate)
            {
                FireMissile();
            }
        }

        private void FireMissile()
        {
            if (firePoint == null) firePoint = transform;

            GameObject missile = PoolManager.Instance.Spawn(MISSILE_POOL, firePoint.position);
            if (missile != null)
            {
                var mc = missile.GetComponent<MissileController>();
                mc.Initialize(missileSpeed, MISSILE_POOL);
                mc.SetVelocity(Vector2.up * missileSpeed);

                // PoolManager.Instance.DespawnAfterDelay(MISSILE_POOL, missile, missileLifetime);
                EventManager.Trigger(new MissileFiredEvent(firePoint.position, missileSpeed));
            }

            lastFireTime = Time.time;
        }

        private void OnMissileDestroyed(MissileDestroyedEvent e)
        {
            // Xử lý khi missile bị hủy (nếu cần)
        }
    }
}
