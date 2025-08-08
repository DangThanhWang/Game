using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Game.Services
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        [System.Serializable]
        public class PoolConfig
        {
            public string poolName;
            public GameObject prefab;
            public int initialSize = 10;
            public int maxSize = 50;
            public bool autoExpand = true;
        }

        [Header("Pool Configuration")]
        [SerializeField] private PoolConfig[] poolConfigs;

        private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void InitializePools()
        {
            foreach (var config in poolConfigs)
            {
                CreatePool(config.poolName, config.prefab, config.initialSize, config.maxSize, config.autoExpand);
            }
        }

        public void CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 50, bool autoExpand = true)
        {
            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool '{poolName}' already exists!");
                return;
            }

            GameObject poolContainer = new GameObject($"Pool_{poolName}");
            poolContainer.transform.SetParent(transform);

            ObjectPool pool = new ObjectPool(poolName, prefab, poolContainer.transform, initialSize, maxSize, autoExpand);
            pools[poolName] = pool;

            Debug.Log($"Created pool '{poolName}' with {initialSize} initial objects");
        }

        public GameObject Spawn(string poolName, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return null;
            }

            return pools[poolName].Get(position, rotation, parent);
        }

        public void Despawn(string poolName, GameObject obj)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"Pool '{poolName}' not found!");
                return;
            }

            pools[poolName].Return(obj);
        }

        public void DespawnAfterDelay(string poolName, GameObject obj, float delay)
        {
            StartCoroutine(DespawnDelayedCoroutine(poolName, obj, delay));
        }

        private IEnumerator DespawnDelayedCoroutine(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                Despawn(poolName, obj);
            }
        }

        public void ClearPool(string poolName)
        {
            if (pools.ContainsKey(poolName))
            {
                pools[poolName].Clear();
            }
        }

        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
        }

        // Get pool stats for debugging
        public string GetPoolStats(string poolName)
        {
            if (pools.ContainsKey(poolName))
            {
                return pools[poolName].GetStats();
            }
            return $"Pool '{poolName}' not found";
        }
    }

    public class ObjectPool
    {
        private string poolName;
        private GameObject prefab;
        private Transform container;
        private Queue<GameObject> availableObjects;
        private HashSet<GameObject> allObjects;
        private int maxSize;
        private bool autoExpand;

        public ObjectPool(string name, GameObject prefab, Transform container, int initialSize, int maxSize, bool autoExpand)
        {
            this.poolName = name;
            this.prefab = prefab;
            this.container = container;
            this.maxSize = maxSize;
            this.autoExpand = autoExpand;

            availableObjects = new Queue<GameObject>();
            allObjects = new HashSet<GameObject>();

            // Pre-populate pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private GameObject CreateNewObject()
        {
            if (allObjects.Count >= maxSize && !autoExpand)
            {
                return null;
            }

            GameObject newObj = Object.Instantiate(prefab, container);
            newObj.SetActive(false);
            
            // Add IPoolable component handling if needed
            IPoolable poolable = newObj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnCreated();
            }

            allObjects.Add(newObj);
            availableObjects.Enqueue(newObj);
            
            return newObj;
        }

        public GameObject Get(Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            GameObject obj = null;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (autoExpand || allObjects.Count < maxSize)
            {
                obj = CreateNewObject();
            }

            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                
                if (parent != null)
                    obj.transform.SetParent(parent);
                else
                    obj.transform.SetParent(container);

                obj.SetActive(true);

                // Handle IPoolable
                IPoolable poolable = obj.GetComponent<IPoolable>();
                if (poolable != null)
                {
                    poolable.OnSpawned();
                }
            }

            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null || !allObjects.Contains(obj))
                return;

            // Handle IPoolable
            IPoolable poolable = obj.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnDespawned();
            }

            obj.SetActive(false);
            obj.transform.SetParent(container);
            availableObjects.Enqueue(obj);
        }

        public void Clear()
        {
            while (availableObjects.Count > 0)
            {
                GameObject obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            availableObjects.Clear();
            allObjects.Clear();
        }

        public string GetStats()
        {
            return $"{poolName}: {availableObjects.Count}/{allObjects.Count} available (Max: {maxSize})";
        }
    }

    // Optional interface for pooled objects
    public interface IPoolable
    {
        void OnCreated();   // Called once when object is first created
        void OnSpawned();   // Called every time object is taken from pool
        void OnDespawned(); // Called every time object is returned to pool
    }
}