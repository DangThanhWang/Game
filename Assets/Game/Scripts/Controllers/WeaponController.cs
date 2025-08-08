using UnityEngine;
using System.Collections;
using Game.Core;
using Game.Services;
using Game.Events;
using Game.Controllers;

namespace Game.Controllers
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private int maxMissiles = 10;
        [SerializeField] private float fireRate = 0.12f;
        [SerializeField] private float missileSpeed = 8f;
        [SerializeField] private float missileLifetime = 5f;

        [Header("Spawn Settings")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private Vector2 fireOffset = Vector2.up * 0.5f;

        // Fire control
        private float lastFireTime;
        private bool canFire = true;
        private int activeMissiles = 0;

        // Pool name
        private const string MISSILE_POOL = "Missiles";

        void Start()
        {
            Initialize();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void Update()
        {
            // Auto fire for testing (remove in production)
            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                HandleAutoFire();
            }
        }

        private void Initialize()
        {
            // Create missile pool
            if (PoolManager.Instance != null && missilePrefab != null)
            {
                PoolManager.Instance.CreatePool(
                    MISSILE_POOL, 
                    missilePrefab, 
                    maxMissiles, 
                    maxMissiles * 2, 
                    true
                );
            }

            // If no fire point set, use this transform
            if (firePoint == null)
                firePoint = transform;

            Debug.Log("WeaponController initialized");
        }

        private void SubscribeToEvents()
        {
            EventManager.Subscribe<WeaponFireEvent>(OnWeaponFire);
            EventManager.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Subscribe<MissileDestroyedEvent>(OnMissileDestroyed);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Unsubscribe<WeaponFireEvent>(OnWeaponFire);
            EventManager.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Unsubscribe<MissileDestroyedEvent>(OnMissileDestroyed);
        }

        private void HandleAutoFire()
        {
            // Auto fire every fireRate seconds (for original game behavior)
            if (Time.time >= lastFireTime + fireRate)
            {
                FireMissile();
            }
        }

        #region Event Handlers
        private void OnWeaponFire(WeaponFireEvent eventData)
        {
            FireMissile(eventData.Position);
        }

        private void OnGameStateChanged(GameStateChangeEvent eventData)
        {
            canFire = eventData.NewState == GameState.Playing;
        }

        private void OnMissileDestroyed(MissileDestroyedEvent eventData)
        {
            activeMissiles = Mathf.Max(0, activeMissiles - 1);
        }
        #endregion

        #region Public API
        public bool FireMissile(Vector3? position = null)
        {
            if (!CanFire()) return false;

            // Use provided position or default fire point
            Vector3 spawnPos = position ?? GetFirePosition();

            // Spawn missile from pool
            GameObject missile = PoolManager.Instance.Spawn(MISSILE_POOL, spawnPos);
            
            if (missile == null)
            {
                Debug.LogWarning("Failed to spawn missile from pool");
                return false;
            }

            // Setup missile
            SetupMissile(missile);

            // Update fire tracking
            lastFireTime = Time.time;
            activeMissiles++;

            // Auto-despawn after lifetime
            PoolManager.Instance.DespawnAfterDelay(MISSILE_POOL, missile, missileLifetime);

            // Trigger fire event
            EventManager.Trigger(new MissileFiredEvent(spawnPos, missileSpeed));

            return true;
        }

        public bool CanFire()
        {
            return canFire && 
                   Time.time >= lastFireTime + fireRate && 
                   activeMissiles < maxMissiles &&
                   GameManager.Instance.CurrentState == GameState.Playing;
        }

        public Vector3 GetFirePosition()
        {
            if (firePoint != null)
                return firePoint.position;
            
            return transform.position + (Vector3)fireOffset;
        }
        #endregion

        #region Private Methods
        private void SetupMissile(GameObject missile)
        {
            // Get rigidbody and set velocity
            Rigidbody2D rb = missile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.up * missileSpeed;
            }

            // Setup missile controller if it exists
            MissileController missileController = missile.GetComponent<MissileController>();
            if (missileController != null)
            {
                missileController.Initialize(missileSpeed, MISSILE_POOL);
            }
        }
        #endregion

        #region Collision Detection (Top boundary)
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Missile"))
            {
                // Missile hit top boundary, return to pool
                PoolManager.Instance.Despawn(MISSILE_POOL, other.gameObject);
                EventManager.Trigger(new MissileDestroyedEvent(other.transform.position, MissileDestroyReason.OutOfBounds));
            }
        }
        #endregion

        #region Debug
        void OnDrawGizmosSelected()
        {
            // Draw fire point
            Vector3 firePos = GetFirePosition();
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePos, 0.1f);
            
            // Draw fire direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(firePos, Vector3.up * 2f);
        }
        #endregion
    }

    // Weapon Events
    public struct MissileFiredEvent : IGameEvent
    {
        public Vector3 Position { get; }
        public float Speed { get; }

        public MissileFiredEvent(Vector3 position, float speed)
        {
            Position = position;
            Speed = speed;
        }
    }

    public struct MissileDestroyedEvent : IGameEvent
    {
        public Vector3 Position { get; }
        public MissileDestroyReason Reason { get; }

        public MissileDestroyedEvent(Vector3 position, MissileDestroyReason reason)
        {
            Position = position;
            Reason = reason;
        }
    }

    public enum MissileDestroyReason
    {
        HitTarget,
        OutOfBounds,
        Timeout
    }
}