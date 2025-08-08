using UnityEngine;
using Game.Events;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        [Header("Game Settings")]
        [SerializeField] private GameData gameData;
        
        [Header("Runtime Data")]
        [SerializeField] private GameState currentState = GameState.Menu;
        [SerializeField] private int score = 0;
        [SerializeField] private int lives = 3;
        
        // Screen bounds (from original Game.cs)
        public float ScreenWidth { get; private set; }
        public float ScreenHeight { get; private set; }
        
        // Properties
        public GameData Data => gameData;
        public GameState CurrentState => currentState;
        public int Score => score;
        public int Lives => lives;

        private void Initialize()
        {
            // Calculate screen bounds
            CalculateScreenBounds();
            
            // Initialize services
            InitializeServices();
            
            // Subscribe to events
            SubscribeToEvents();
            
            Debug.Log("GameManager initialized successfully");
        }

        private void CalculateScreenBounds()
        {
            if (Camera.main != null)
            {
                ScreenWidth = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0f, 0f)).x;
                ScreenHeight = Camera.main.ScreenToWorldPoint(new Vector3(0f, Screen.height, 0f)).y;
            }
        }

        private void InitializeServices()
        {
            // Services will auto-initialize themselves
            // This is just a hook for future manual initialization
        }

        private void SubscribeToEvents()
        {
            EventManager.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Subscribe<ScoreUpdateEvent>(OnScoreUpdated);
            EventManager.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Unsubscribe<ScoreUpdateEvent>(OnScoreUpdated);
            EventManager.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        #region Game State Management
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;
            
            GameState previousState = currentState;
            currentState = newState;
            
            EventManager.Trigger(new GameStateChangeEvent(previousState, currentState));
        }

        public void StartGame()
        {
            ResetGameData();
            ChangeGameState(GameState.Playing);
        }

        public void PauseGame()
        {
            ChangeGameState(GameState.Paused);
        }

        public void ResumeGame()
        {
            ChangeGameState(GameState.Playing);
        }

        public void GameOver()
        {
            ChangeGameState(GameState.GameOver);
        }

        private void ResetGameData()
        {
            score = 0;
            lives = gameData.startingLives;
        }
        #endregion

        #region Event Handlers
        private void OnGameStateChanged(GameStateChangeEvent eventData)
        {
            Debug.Log($"Game state changed: {eventData.PreviousState} -> {eventData.NewState}");
            
            // Handle time scale
            switch (eventData.NewState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }

        private void OnScoreUpdated(ScoreUpdateEvent eventData)
        {
            score = Mathf.Max(0, score + eventData.ScoreChange);
        }

        private void OnPlayerDeath(PlayerDeathEvent eventData)
        {
            lives--;
            
            if (lives <= 0)
            {
                GameOver();
            }
        }
        #endregion

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentState == GameState.Playing)
            {
                PauseGame();
            }
        }
    }

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }
}