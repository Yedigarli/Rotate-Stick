using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;

    private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, Pool> poolConfigDictionary = new Dictionary<string, Pool>();

    private void Awake()
    {
        Instance = this;
        poolDictionary.Clear();
        poolConfigDictionary.Clear();

        if (pools == null)
            return;

        for (int i = 0; i < pools.Count; i++)
        {
            Pool pool = pools[i];
            if (pool == null || string.IsNullOrEmpty(pool.tag) || pool.prefab == null)
                continue;

            Queue<GameObject> objectPool = new Queue<GameObject>(Mathf.Max(1, pool.size));
            Transform parent = new GameObject("Pool_" + pool.tag).transform;
            parent.SetParent(transform);

            int initialSize = Mathf.Max(1, pool.size);
            for (int j = 0; j < initialSize; j++)
                objectPool.Enqueue(CreatePooledObject(pool, parent));

            poolDictionary[pool.tag] = objectPool;
            poolConfigDictionary[pool.tag] = pool;
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
            return null;

        int count = objectPool.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject candidate = objectPool.Dequeue();
            objectPool.Enqueue(candidate);

            if (candidate == null)
                continue;

            if (!candidate.activeInHierarchy)
            {
                PrepareSpawn(candidate, position, rotation);
                return candidate;
            }
        }

        if (poolConfigDictionary.TryGetValue(tag, out Pool config) && config.prefab != null)
        {
            GameObject created = CreatePooledObject(config, transform);
            objectPool.Enqueue(created);
            PrepareSpawn(created, position, rotation);
            return created;
        }

        return null;
    }

    public bool HasActiveObject(string tag)
    {
        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
            return false;

        foreach (GameObject obj in objectPool)
        {
            if (obj != null && obj.activeInHierarchy)
                return true;
        }

        return false;
    }

    public void DeactivateAll(string tag)
    {
        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
            return;

        foreach (GameObject obj in objectPool)
        {
            if (obj != null && obj.activeInHierarchy)
                obj.SetActive(false);
        }
    }

    private static void PrepareSpawn(GameObject obj, Vector3 position, Quaternion rotation)
    {
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
    }

    private static GameObject CreatePooledObject(Pool pool, Transform parent)
    {
        GameObject obj = Instantiate(pool.prefab, parent);
        obj.SetActive(false);
        return obj;
    }
}
