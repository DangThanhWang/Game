using UnityEngine;
using Game.Events;
using Game.Core;

namespace Game.Services
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Input Settings")]
        [SerializeField] private bool enableInput = true;
        [SerializeField] private float touchSensitivity = 1f;

        // Input State
        public bool IsPressed { get; private set; }
        public Vector2 MouseWorldPosition { get; private set; }
        public Vector2 TouchPosition { get; private set; }

        // Events
        public System.Action<Vector2> OnTouchStart;
        public System.Action<Vector2> OnTouchMove;
        public System.Action OnTouchEnd;

        private Camera mainCamera;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("InputManager: No main camera found!");
            }
        }

        void Update()
        {
            if (!enableInput) return;

            HandleInput();
        }

        void Initialize()
        {
            // Subscribe to game state changes
            EventManager.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
        }

        void OnDestroy()
        {
            EventManager.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
        }

        private void HandleInput()
        {
            // Handle mouse/touch input
            bool wasPressed = IsPressed;
            IsPressed = Input.GetMouseButton(0);

            if (mainCamera == null) return;

            // Update mouse world position
            Vector3 mousePos = Input.mousePosition;
            MouseWorldPosition = mainCamera.ScreenToWorldPoint(mousePos);
            TouchPosition = MouseWorldPosition;

            // Trigger events
            if (IsPressed && !wasPressed)
            {
                // Touch/Click started
                OnTouchStart?.Invoke(TouchPosition);
                EventManager.Trigger(new PlayerInputEvent(InputType.TouchStart, TouchPosition));
            }
            else if (IsPressed && wasPressed)
            {
                // Touch/Click held
                OnTouchMove?.Invoke(TouchPosition);
                EventManager.Trigger(new PlayerInputEvent(InputType.TouchMove, TouchPosition));
            }
            else if (!IsPressed && wasPressed)
            {
                // Touch/Click ended
                OnTouchEnd?.Invoke();
                EventManager.Trigger(new PlayerInputEvent(InputType.TouchEnd, TouchPosition));
            }

            // Handle keyboard input for testing
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            // Space to fire (for testing)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                EventManager.Trigger(new PlayerInputEvent(InputType.Fire, Vector2.zero));
            }

            // ESC to pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                EventManager.Trigger(new PlayerInputEvent(InputType.Pause, Vector2.zero));
            }
        }

        private void OnGameStateChanged(GameStateChangeEvent eventData)
        {
            // Disable input during game over/pause
            enableInput = eventData.NewState == GameState.Playing;
        }

        // Public API
        public void EnableInput(bool enable)
        {
            enableInput = enable;
        }

        public Vector2 GetWorldPosition(Vector2 screenPosition)
        {
            if (mainCamera == null) return Vector2.zero;
            return mainCamera.ScreenToWorldPoint(screenPosition);
        }
    }

    // Input Events (add to Events folder)
    public struct PlayerInputEvent : IGameEvent
    {
        public InputType Type { get; }
        public Vector2 Position { get; }

        public PlayerInputEvent(InputType type, Vector2 position)
        {
            Type = type;
            Position = position;
        }
    }

    public enum InputType
    {
        TouchStart,
        TouchMove,
        TouchEnd,
        Fire,
        Pause
    }
}