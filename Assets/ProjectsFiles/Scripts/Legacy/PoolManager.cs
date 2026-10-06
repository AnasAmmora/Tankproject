using System.Collections.Generic;
using UnityEngine;

public static class PoolManager
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private static Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        pools.Clear();
        poolRoot = null;
    }

    private static Transform PoolRoot
    {
        get
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject("=== Pooled Objects ===").transform;
                Object.DontDestroyOnLoad(poolRoot.gameObject);
            }
            return poolRoot;
        }
    }


    public static void Prewarm(GameObject prefab, int count)
    {
        Queue<GameObject> queue = GetOrCreateQueue(prefab);
        for (int i = 0; i < count; i++)
        {
            GameObject instance = CreateNew(prefab);
            instance.SetActive(false);
            queue.Enqueue(instance);
        }
    }

    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> queue = GetOrCreateQueue(prefab);
        GameObject instance = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab);

        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public static void Release(GameObject instance)
    {
        if (instance == null) return;

        PooledInstance pooled = instance.GetComponent<PooledInstance>();
        if (pooled == null || pooled.SourcePrefab == null)
        {
            Object.Destroy(instance); 
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(PoolRoot);
        GetOrCreateQueue(pooled.SourcePrefab).Enqueue(instance);
    }

    private static Queue<GameObject> GetOrCreateQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }
        return queue;
    }

    private static GameObject CreateNew(GameObject prefab)
    {
        GameObject instance = Object.Instantiate(prefab, PoolRoot);
        PooledInstance pooled = instance.AddComponent<PooledInstance>();
        pooled.SourcePrefab = prefab;
        return instance;
    }
}