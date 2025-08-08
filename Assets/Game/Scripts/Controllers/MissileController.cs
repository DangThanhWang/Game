using UnityEngine;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MissileController : MonoBehaviour, IPoolable
    {
        [Header("Missile Settings")]
        [SerializeField] private float damage = 1f;
        [SerializeField] private LayerMask targetLayers = -1;

        // Components
        private Rigidbody2D rb;
        private Collider2D col;

        // Pool data
        private string poolName;
        private float speed;
        private bool isActive;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            
            // Set missile tag
            gameObject.tag = "Missile";
        }

        #region IPoolable Implementation
        public void OnCreated()
        {
            // Called once when missile is first created in pool
            SetupPhysics();
        }

        public void OnSpawned()
        {
            // Called every time missile is taken from pool
            isActive = true;
            ResetMissile();
        }

        public void OnDespawned()
        {
            // Called every time missile is returned to pool
            isActive = false;
            StopMissile();
        }
        #endregion

        private void SetupPhysics()
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f; // Missiles ignore gravity
                rb.drag = 0f;
                rb.angularDrag = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (col != null)
            {
                col.isTrigger = true; // Missiles use trigger collisions
            }
        }

        private void ResetMissile()
        {
            if (rb != null)
            {
                rb.velocity = Vector2.up * speed;
                rb.angularVelocity = 0f;
            }

            transform.rotation = Quaternion.identity;
        }

        private void StopMissile()
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        #region Public API
        public void Initialize(float missileSpeed, string parentPool)
        {
            speed = missileSpeed;
            poolName = parentPool;
        }

        public void SetVelocity(Vector2 velocity)
        {
            if (rb != null)
            {
                rb.velocity = velocity;
            }
        }

        public void DestroyMissile(MissileDestroyReason reason = MissileDestroyReason.HitTarget)
        {
            if (!isActive) return;

            // Trigger destruction event
            EventManager.Trigger(new MissileDestroyedEvent(transform.position, reason));

            // Return to pool
            if (PoolManager.Instance != null && !string.IsNullOrEmpty(poolName))
            {
                PoolManager.Instance.Despawn(poolName, gameObject);
            }
            else
            {
                // Fallback if pool system fails
                gameObject.SetActive(false);
            }
        }
        #endregion

        #region Collision Detection
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check if target is in valid layer
            if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

            // Handle different collision types
            if (other.CompareTag("Meteor"))
            {
                HitMeteor(other);
            }
            else if (other.CompareTag("Walls"))
            {
                // Missiles don't collide with walls, pass through
                return;
            }
            else if (other.CompareTag("Ground"))
            {
                // Missiles don't collide with ground, pass through  
                return;
            }
        }

        private void HitMeteor(Collider2D meteorCollider)
        {
            // Get meteor controller
            MeteorController meteor = meteorCollider.GetComponent<MeteorController>();
            if (meteor != null)
            {
                // Damage meteor
                meteor.TakeDamage((int)damage);

                // Trigger hit event
                EventManager.Trigger(new MissileHitEvent(
                    transform.position, 
                    meteorCollider.transform.position,
                    damage
                ));

                // Destroy missile
                DestroyMissile(MissileDestroyReason.HitTarget);
            }
        }
        #endregion

        #region Debug
        void OnDrawGizmosSelected()
        {
            // Draw velocity direction
            if (rb != null && isActive)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, rb.velocity.normalized * 0.5f);
            }

            // Draw damage radius
            Gizmos.color = Color.red;
            if (col != null)
            {
                Gizmos.DrawWireSphere(transform.position, 0.1f);
            }
        }
        #endregion

        #region Validation
        void OnValidate()
        {
            // Ensure damage is positive
            damage = Mathf.Max(0.1f, damage);
        }
        #endregion
    }

    // Missile Hit Event
    public struct MissileHitEvent : IGameEvent
    {
        public Vector3 MissilePosition { get; }
        public Vector3 TargetPosition { get; }
        public float Damage { get; }

        public MissileHitEvent(Vector3 missilePosition, Vector3 targetPosition, float damage)
        {
            MissilePosition = missilePosition;
            TargetPosition = targetPosition;
            Damage = damage;
        }
    }
}