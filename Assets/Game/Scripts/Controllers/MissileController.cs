using UnityEngine;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class MissileController : MonoBehaviour, IPoolable
    {
        private Rigidbody2D rb;
        private string poolName;
        private float speed;
        private bool isActive;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            gameObject.tag = "Missile";
        }

        public void OnCreated()
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void OnSpawned()
        {
            isActive = true;
        }

        public void OnDespawned()
        {
            isActive = false;
            rb.velocity = Vector2.zero;
        }

        public void Initialize(float missileSpeed, string parentPool)
        {
            speed = missileSpeed;
            poolName = parentPool;
        }

        public void SetVelocity(Vector2 velocity)
        {
            rb.velocity = velocity;
        }

        public void DestroyMissile(MissileDestroyReason reason)
        {
            if (!isActive) return;
            EventManager.Trigger(new MissileDestroyedEvent(transform.position, reason));
            PoolManager.Instance.Despawn(poolName, gameObject);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!isActive) return;

            if (collision.CompareTag("Wall"))
            {
                DestroyMissile(MissileDestroyReason.OutOfBounds);
            }
        }
    }
}
