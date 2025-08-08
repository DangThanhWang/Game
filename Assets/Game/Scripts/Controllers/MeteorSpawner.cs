using UnityEngine;
using System.Collections;
using Game.Core;
using Game.Services;
using Game.Events;

namespace Game.Controllers
{
    public class MeteorSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject[] meteorPrefabs;
        [SerializeField] private int meteorsPerWave = 12;
        [SerializeField] private float spawnDelay = 4f;
        [SerializeField] private float waveDelay = 10f;

        [Header("Spawn Area")]
        [SerializeField] private float spawnHeight = 6f;
        [SerializeField] private Vector2 spawnRangeX = new Vector2(-3f, 3f);
        [SerializeField] private Vector2 spawnVelocityRange = new Vector2(-2f, 2f);

        [Header("Wave Settings")]
        [SerializeField] private bool autoStartWaves = true;
        [SerializeField] private int maxWaves = -1; // -1 = infinite
        [SerializeField] private float difficultyIncrease = 1.1f;

        // Pool names
        private const string METEOR_POOL_PREFIX = "Meteors_";

        // State
        private int currentWave = 0;
        private int meteorsSpawned = 0;
        private int meteorsDestroyed = 0;
        private bool isSpawning = false;
        private Coroutine spawnCoroutine;

        // Dynamic difficulty
        private float currentSpawnDelay;
        private int currentMeteorsPerWave;

        public static MeteorSpawner Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Initialize();
            SubscribeToEvents();

            if (autoStartWaves)
            {
                StartWaves();
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Initialize()
        {
            // Create pools for each meteor type
            for (int i = 0; i < meteorPrefabs.Length; i++)
            {
                if (meteorPrefabs[i] != null && PoolManager.Instance != null)
                {
                    string poolName = METEOR_POOL_PREFIX + i;
                    PoolManager.Instance.CreatePool(
                        poolName,
                        meteorPrefabs[i],
                        meteorsPerWave / 2, // Initial pool size
                        meteorsPerWave * 2, // Max pool size
                        true // Auto expand
                    );
                }
            }

            // Initialize difficulty values
            ResetDifficulty();

            Debug.Log($"MeteorSpawner initialized with {meteorPrefabs.Length} meteor types");
        }

        private void SubscribeToEvents()
        {
            EventManager.Subscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Subscribe<MeteorDestroyedEvent>(OnMeteorDestroyed);
        }

        private void UnsubscribeFromEvents()
        {
            EventManager.Unsubscribe<GameStateChangeEvent>(OnGameStateChanged);
            EventManager.Unsubscribe<MeteorDestroyedEvent>(OnMeteorDestroyed);
        }

        #region Wave Management
        public void StartWaves()
        {
            if (isSpawning) return;

            currentWave = 0;
            StartNextWave();
        }

        public void StopWaves()
        {
            isSpawning = false;
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        private void StartNextWave()
        {
            if (maxWaves > 0 && currentWave >= maxWaves) return;

            currentWave++;
            meteorsSpawned = 0;
            meteorsDestroyed = 0;

            // Increase difficulty
            IncreaseDifficulty();

            // Trigger wave start event
            EventManager.Trigger(new WaveStartEvent(currentWave, currentMeteorsPerWave));

            // Start spawning
            spawnCoroutine = StartCoroutine(SpawnWaveCoroutine());

            Debug.Log($"Started wave {currentWave} with {currentMeteorsPerWave} meteors");
        }

        private IEnumerator SpawnWaveCoroutine()
        {
            isSpawning = true;

            // Spawn all meteors in the wave
            for (int i = 0; i < currentMeteorsPerWave; i++)
            {
                if (!isSpawning) yield break; // Exit if stopped

                SpawnRandomMeteor();
                yield return new WaitForSeconds(currentSpawnDelay);
            }

            isSpawning = false;

            // Wait for all meteors to be destroyed before next wave
            StartCoroutine(WaitForWaveComplete());
        }

        private IEnumerator WaitForWaveComplete()
        {
            // Wait until all meteors are destroyed or timeout
            float timeout = 60f; // Max wait time
            float timer = 0f;

            while (meteorsDestroyed < meteorsSpawned && timer < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                timer += 0.5f;
            }

            // Trigger wave complete event
            EventManager.Trigger(new WaveCompleteEvent(currentWave, meteorsDestroyed));

            // Wait before next wave
            yield return new WaitForSeconds(waveDelay);

            // Start next wave if game is still playing
            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                StartNextWave();
            }
        }
        #endregion

        #region Meteor Spawning
        public void SpawnRandomMeteor()
        {
            if (meteorPrefabs.Length == 0) return;

            // Choose random meteor type
            int meteorType = Random.Range(0, meteorPrefabs.Length);
            SpawnMeteor(meteorType);
        }

        public void SpawnMeteor(int meteorType)
        {
            if (meteorType < 0 || meteorType >= meteorPrefabs.Length) return;
            if (PoolManager.Instance == null) return;

            // Calculate spawn position
            Vector3 spawnPos = GetRandomSpawnPosition();

            // Spawn from pool
            string poolName = METEOR_POOL_PREFIX + meteorType;
            GameObject meteor = PoolManager.Instance.Spawn(poolName, spawnPos);

            if (meteor != null)
            {
                SetupMeteor(meteor, poolName);
                meteorsSpawned++;

                // Trigger spawn event
                EventManager.Trigger(new MeteorSpawnedEvent(meteor, spawnPos, currentWave));
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float randomX = Random.Range(spawnRangeX.x, spawnRangeX.y);
            return new Vector3(randomX, spawnHeight, 0);
        }

        private void SetupMeteor(GameObject meteor, string poolName)
        {
            // Setup meteor controller
            MeteorController controller = meteor.GetComponent<MeteorController>();
            if (controller != null)
            {
                controller.Initialize(poolName);
                
                // Set random initial velocity
                Vector2 velocity = new Vector2(
                    Random.Range(spawnVelocityRange.x, spawnVelocityRange.y),
                    0
                );
                controller.SetInitialVelocity(velocity);
            }
        }
        #endregion

        #region Difficulty Scaling
        private void ResetDifficulty()
        {
            currentSpawnDelay = spawnDelay;
            currentMeteorsPerWave = meteorsPerWave;
        }

        private void IncreaseDifficulty()
        {
            if (currentWave > 1)
            {
                // Decrease spawn delay (faster spawning)
                currentSpawnDelay = Mathf.Max(0.5f, currentSpawnDelay / difficultyIncrease);
                
                // Increase meteors per wave
                currentMeteorsPerWave = Mathf.RoundToInt(currentMeteorsPerWave * difficultyIncrease);
                currentMeteorsPerWave = Mathf.Min(currentMeteorsPerWave, 50); // Cap at 50
            }
        }
        #endregion

        #region Event Handlers
        private void OnGameStateChanged(GameStateChangeEvent eventData)
        {
            switch (eventData.NewState)
            {
                case GameState.Playing:
                    if (!isSpawning && autoStartWaves)
                        StartWaves();
                    break;
                    
                case GameState.Paused:
                case GameState.GameOver:
                    StopWaves();
                    break;
            }
        }

        private void OnMeteorDestroyed(MeteorDestroyedEvent eventData)
        {
            meteorsDestroyed++;
        }
        #endregion

        #region Debug
        void OnDrawGizmosSelected()
        {
            // Draw spawn area
            Gizmos.color = Color.yellow;
            Vector3 leftSpawn = new Vector3(spawnRangeX.x, spawnHeight, 0);
            Vector3 rightSpawn = new Vector3(spawnRangeX.y, spawnHeight, 0);
            
            Gizmos.DrawLine(leftSpawn + Vector3.up, leftSpawn - Vector3.up);
            Gizmos.DrawLine(rightSpawn + Vector3.up, rightSpawn - Vector3.up);
            Gizmos.DrawLine(leftSpawn, rightSpawn);
            
            // Draw spawn points
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                Vector3 spawnPoint = Vector3.Lerp(leftSpawn, rightSpawn, t);
                Gizmos.DrawWireSphere(spawnPoint, 0.2f);
            }
        }
        #endregion
    }

    // Wave Events
    public struct WaveStartEvent : IGameEvent
    {
        public int WaveNumber { get; }
        public int MeteorCount { get; }

        public WaveStartEvent(int waveNumber, int meteorCount)
        {
            WaveNumber = waveNumber;
            MeteorCount = meteorCount;
        }
    }

    public struct MeteorSpawnedEvent : IGameEvent
    {
        public GameObject Meteor { get; }
        public Vector3 SpawnPosition { get; }
        public int Wave { get; }

        public MeteorSpawnedEvent(GameObject meteor, Vector3 spawnPosition, int wave)
        {
            Meteor = meteor;
            SpawnPosition = spawnPosition;
            Wave = wave;
        }
    }
}