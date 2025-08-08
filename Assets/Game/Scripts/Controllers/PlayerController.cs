using UnityEngine;
using Game.Core;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float smoothing = 0.1f;
        
        [Header("Wheel Settings")]
        [SerializeField] private HingeJoint2D[] wheels;
        [SerializeField] private float wheelMotorSpeed = 150f;
        
        [Header("Boundaries")]
        [SerializeField] private float boundaryOffset = 0.56f;

        // Components
        private Rigidbody2D rb;
        private Camera mainCamera;
        
        // Movement state
        private Vector2 targetPosition;
        private Vector2 currentVelocity;
        private bool isMoving;
        private float screenBounds;

        // Motor
        private JointMotor2D motor;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            mainCamera = Camera.main;
            
            if (wheels.Length > 0)
                motor = wheels[0].motor;
        }

        void Start()
        {
            Initialize();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Initialize()
        {
            // Calculate screen boundaries
            if (mainCamera != null)
            {
                screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, 0f, 0f)).x - boundaryOffset;
            }
            else
            {
                // Fallback to GameManager bounds
                screenBounds = GameManager.Instance.ScreenWidth - boundaryOffset;
            }

            targetPosition = transform.position;
            
            Debug.Log($"Player initialized. Screen bounds: {screenBounds}");
        }

        private void SubscribeToEvents()
        {
            EventManager.Subscribe<PlayerInputEvent>(OnPlayerInput);
            EventManager.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Unsubscribe<PlayerInputEvent>(OnPlayerInput);
            EventManager.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
        }

        void Update()
        {
            // Handle legacy input for backward compatibility
            HandleLegacyInput();
        }

        void FixedUpdate()
        {
            HandleMovement();
            HandleWheelRotation();
        }

        private void HandleLegacyInput()
        {
            // Support old input method for testing
            if (Input.GetMouseButton(0))
            {
                Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                SetTargetPosition(new Vector2(mouseWorldPos.x, transform.position.y));
            }
        }

        private void HandleMovement()
        {
            if (!isMoving)
            {
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, smoothing);
                return;
            }

            // Clamp target position within bounds
            targetPosition.x = Mathf.Clamp(targetPosition.x, -screenBounds, screenBounds);
            targetPosition.y = transform.position.y; // Keep Y position fixed

            // Smooth movement
            Vector2 newPosition = Vector2.SmoothDamp(
                rb.position, 
                targetPosition, 
                ref currentVelocity, 
                smoothing
            );

            rb.MovePosition(newPosition);

            // Check if close enough to stop
            if (Vector2.Distance(rb.position, targetPosition) < 0.1f)
            {
                isMoving = false;
            }
        }

        private void HandleWheelRotation()
        {
            float velocityX = rb.velocity.x;
            bool shouldRotate = Mathf.Abs(velocityX) > 0.01f && Mathf.Abs(rb.position.x) < screenBounds;

            if (shouldRotate)
            {
                motor.motorSpeed = velocityX * wheelMotorSpeed;
                SetWheelMotor(true);
            }
            else
            {
                motor.motorSpeed = 0f;
                SetWheelMotor(false);
            }
        }

        private void SetWheelMotor(bool active)
        {
            foreach (var wheel in wheels)
            {
                wheel.useMotor = active;
                wheel.motor = motor;
            }
        }

        #region Event Handlers
        private void OnPlayerInput(PlayerInputEvent inputEvent)
        {
            switch (inputEvent.Type)
            {
                case InputType.TouchStart:
                case InputType.TouchMove:
                    SetTargetPosition(new Vector2(inputEvent.Position.x, transform.position.y));
                    break;
                    
                case InputType.TouchEnd:
                    isMoving = false;
                    break;
                    
                case InputType.Fire:
                    FireWeapon();
                    break;
            }
        }

        private void OnGameStateChanged(GameStateChangeEvent eventData)
        {
            // Stop movement when game is paused/ended
            if (eventData.NewState != GameState.Playing)
            {
                isMoving = false;
                rb.velocity = Vector2.zero;
            }
        }
        #endregion

        #region Public API
        public void SetTargetPosition(Vector2 position)
        {
            targetPosition = position;
            isMoving = true;
        }

        public void Stop()
        {
            isMoving = false;
            targetPosition = transform.position;
        }

        public void FireWeapon()
        {
            // Trigger weapon fire event
            EventManager.Trigger(new WeaponFireEvent(transform.position + Vector3.up * 0.5f));
        }

        public Vector2 GetFirePosition()
        {
            return transform.position + Vector3.up * 0.5f;
        }
        #endregion

        #region Collision Handling
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Meteor"))
            {
                // Player hit by meteor
                EventManager.Trigger(new PlayerDeathEvent(
                    GameManager.Instance.Lives - 1, 
                    DeathCause.MeteorHit
                ));
            }
        }
        #endregion

        #region Debug
        void OnDrawGizmosSelected()
        {
            // Draw movement bounds
            Gizmos.color = Color.yellow;
            Vector3 leftBound = new Vector3(-screenBounds, transform.position.y, 0);
            Vector3 rightBound = new Vector3(screenBounds, transform.position.y, 0);
            
            Gizmos.DrawLine(leftBound + Vector3.up, leftBound - Vector3.up);
            Gizmos.DrawLine(rightBound + Vector3.up, rightBound - Vector3.up);
            
            // Draw target position
            if (isMoving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
            }
        }
        #endregion
    }

    // Weapon Events (add to Events folder)
    public struct WeaponFireEvent : IGameEvent
    {
        public Vector3 Position { get; }

        public WeaponFireEvent(Vector3 position)
        {
            Position = position;
        }
    }
}