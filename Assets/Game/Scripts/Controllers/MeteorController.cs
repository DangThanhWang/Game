using UnityEngine;
using TMPro;
using Game.Core;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MeteorController : MonoBehaviour, IPoolable
    {
        [Header("Meteor Settings")]
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float bounceForce = 8f;
        [SerializeField] private int scoreValue = 100;

        [Header("UI")]
        [SerializeField] private TMP_Text healthText;

        [Header("Movement")]
        [SerializeField] private Vector2 initialVelocity = Vector2.right;
        [SerializeField] private float maxVelocity = 10f;

        // Components
        private Rigidbody2D rb;
        private Collider2D col;

        // State
        private int currentHealth;
        private bool isActive;
        private string poolName;

        // Properties
        public int Health => currentHealth;
        public bool IsDestroyed => currentHealth <= 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            
            // Set meteor tag
            gameObject.tag = "Meteor";
        }

        #region IPoolable Implementation
        public void OnCreated()
        {
            // Called once when meteor is first created in pool
            SetupPhysics();
            currentHealth = maxHealth;
        }

        public void OnSpawned()
        {
            // Called every time meteor is taken from pool
            isActive = true;
            ResetMeteor();
        }

        public void OnDespawned()
        {
            // Called every time meteor is returned to pool
            isActive = false;
            StopMeteor();
        }
        #endregion

        private void SetupPhysics()
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f; // Meteors affected by gravity
                rb.drag = 0f;
                rb.angularDrag = 0.05f;
            }

            if (col != null)
            {
                col.isTrigger = true; // Use trigger for collision detection
            }
        }

        private void ResetMeteor()
        {
            // Reset health
            currentHealth = maxHealth;
            UpdateHealthUI();

            // Set initial movement
            if (rb != null)
            {
                rb.velocity = initialVelocity;
                rb.angularVelocity = Random.Range(-180f, 180f); // Random spin
            }
        }

        private void StopMeteor()
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        void FixedUpdate()
        {
            if (!isActive) return;

            // Limit velocity to prevent meteors going too fast
            if (rb != null && rb.velocity.magnitude > maxVelocity)
            {
                rb.velocity = rb.velocity.normalized * maxVelocity;
            }
        }

        #region Health System
        public void TakeDamage(int damage)
        {
            if (!isActive || IsDestroyed) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            UpdateHealthUI();

            // Trigger damage event
            EventManager.Trigger(new MeteorDamagedEvent(
                gameObject,
                damage,
                currentHealth,
                transform.position
            ));

            if (IsDestroyed)
            {
                DestroyMeteor();
            }
        }

        public void Heal(int amount)
        {
            if (!isActive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHealthUI();
        }

        private void UpdateHealthUI()
        {
            if (healthText != null)
            {
                healthText.text = currentHealth.ToString();
            }
        }
        #endregion

        #region Meteor Destruction
        private void DestroyMeteor()
        {
            if (!isActive) return;

            // Trigger destruction event
            EventManager.Trigger(new MeteorDestroyedEvent(
                gameObject,
                transform.position,
                scoreValue
            ));

            // Add score
            EventManager.Trigger(new ScoreUpdateEvent(
                scoreValue,
                GameManager.Instance.Score + scoreValue,
                ScoreReason.MeteorDestroyed
            ));

            // Return to pool or destroy
            if (PoolManager.Instance != null && !string.IsNullOrEmpty(poolName))
            {
                PoolManager.Instance.Despawn(poolName, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Public API
        public void Initialize(string parentPool, int health = -1)
        {
            poolName = parentPool;
            
            if (health > 0)
            {
                maxHealth = health;
                currentHealth = health;
            }
        }

        public void SetInitialVelocity(Vector2 velocity)
        {
            initialVelocity = velocity;
            if (rb != null && isActive)
            {
                rb.velocity = velocity;
            }
        }

        public void AddForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
        {
            if (rb != null && isActive)
            {
                rb.AddForce(force, mode);
            }
        }
        #endregion

        #region Collision Detection
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            HandleCollision(other);
        }

        private void HandleCollision(Collider2D other)
        {
            if (other.CompareTag("Player") || other.CompareTag("Cannon"))
            {
                // Meteor hit player
                EventManager.Trigger(new PlayerDeathEvent(
                    GameManager.Instance.Lives - 1,
                    DeathCause.MeteorHit
                ));
            }
            else if (other.CompareTag("Walls"))
            {
                // Bounce off walls
                BounceOffWall(other);
            }
            else if (other.CompareTag("Ground"))
            {
                // Jump when hitting ground
                JumpOffGround();
            }
            else if (other.CompareTag("Missile"))
            {
                // Handle in MissileController
            }
        }

        private void BounceOffWall(Collider2D wall)
        {
            if (rb == null) return;

            float posX = transform.position.x;
            Vector2 bounceDirection = posX > 0 ? Vector2.left : Vector2.right;
            
            AddForce(bounceDirection * bounceForce, ForceMode2D.Impulse);

            // Trigger bounce event
            EventManager.Trigger(new MeteorBouncedEvent(
                transform.position,
                bounceDirection,
                MeteorBounceType.Wall
            ));
        }

        private void JumpOffGround()
        {
            if (rb == null) return;

            // Jump up
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            // Trigger jump event
            EventManager.Trigger(new MeteorBouncedEvent(
                transform.position,
                Vector2.up,
                MeteorBounceType.Ground
            ));
        }
        #endregion

        #region Debug
        void OnDrawGizmosSelected()
        {
            // Draw velocity
            if (rb != null && isActive)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, rb.velocity.normalized);
            }

            // Draw health
            Gizmos.color = Color.green;
            float healthPercent = (float)currentHealth / maxHealth;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, healthPercent * 0.3f);
        }
        #endregion
    }

    // Meteor Events
    public struct MeteorDamagedEvent : IGameEvent
    {
        public GameObject Meteor { get; }
        public int Damage { get; }
        public int RemainingHealth { get; }
        public Vector3 Position { get; }

        public MeteorDamagedEvent(GameObject meteor, int damage, int remainingHealth, Vector3 position)
        {
            Meteor = meteor;
            Damage = damage;
            RemainingHealth = remainingHealth;
            Position = position;
        }
    }

    public struct MeteorDestroyedEvent : IGameEvent
    {
        public GameObject Meteor { get; }
        public Vector3 Position { get; }
        public int ScoreValue { get; }

        public MeteorDestroyedEvent(GameObject meteor, Vector3 position, int scoreValue)
        {
            Meteor = meteor;
            Position = position;
            ScoreValue = scoreValue;
        }
    }

    public struct MeteorBouncedEvent : IGameEvent
    {
        public Vector3 Position { get; }
        public Vector2 Direction { get; }
        public MeteorBounceType BounceType { get; }

        public MeteorBouncedEvent(Vector3 position, Vector2 direction, MeteorBounceType bounceType)
        {
            Position = position;
            Direction = direction;
            BounceType = bounceType;
        }
    }

    public enum MeteorBounceType
    {
        Wall,
        Ground,
        Other
    }
}