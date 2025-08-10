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

        [SerializeField] private PoolConfig[] poolConfigs;
        private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePools();
            }
            else Destroy(gameObject);
        }

        private void InitializePools()
        {
            foreach (var cfg in poolConfigs)
            {
                CreatePool(cfg.poolName, cfg.prefab, cfg.initialSize, cfg.maxSize, cfg.autoExpand);
            }
        }

        public void CreatePool(string poolName, GameObject prefab, int initialSize, int maxSize, bool autoExpand)
        {
            if (pools.ContainsKey(poolName)) return;
            GameObject container = new GameObject($"Pool_{poolName}");
            container.transform.SetParent(transform);
            pools[poolName] = new ObjectPool(poolName, prefab, container.transform, initialSize, maxSize, autoExpand);
        }

        public GameObject Spawn(string poolName, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"Pool {poolName} not found!");
                return null;
            }
            return pools[poolName].Get(position, rotation, parent);
        }

        public void Despawn(string poolName, GameObject obj)
        {
            if (!pools.ContainsKey(poolName)) return;
            pools[poolName].Return(obj);
        }

        public void DespawnAfterDelay(string poolName, GameObject obj, float delay)
        {
            StartCoroutine(DespawnRoutine(poolName, obj, delay));
        }

        private IEnumerator DespawnRoutine(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy) Despawn(poolName, obj);
        }
    }

    public class ObjectPool
    {
        private readonly string poolName;
        private readonly GameObject prefab;
        private readonly Transform container;
        private readonly Queue<GameObject> available = new();
        private readonly HashSet<GameObject> all = new();
        private readonly int maxSize;
        private readonly bool autoExpand;

        public ObjectPool(string name, GameObject prefab, Transform container, int initialSize, int maxSize, bool autoExpand)
        {
            poolName = name;
            this.prefab = prefab;
            this.container = container;
            this.maxSize = maxSize;
            this.autoExpand = autoExpand;

            for (int i = 0; i < initialSize; i++) CreateNew();
        }

        private GameObject CreateNew()
        {
            if (all.Count >= maxSize && !autoExpand) return null;
            GameObject obj = Object.Instantiate(prefab, container);
            obj.SetActive(false);
            obj.GetComponent<IPoolable>()?.OnCreated();
            all.Add(obj);
            available.Enqueue(obj);
            return obj;
        }

        public GameObject Get(Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj = available.Count > 0 ? available.Dequeue() : CreateNew();
            if (obj == null) return null;

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.SetParent(parent ?? container);
            obj.SetActive(true);
            obj.GetComponent<IPoolable>()?.OnSpawned();
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (!all.Contains(obj)) return;
            obj.GetComponent<IPoolable>()?.OnDespawned();
            obj.SetActive(false);
            obj.transform.SetParent(container);
            available.Enqueue(obj);
        }
    }

    public interface IPoolable
    {
        void OnCreated();
        void OnSpawned();
        void OnDespawned();
    }
}
