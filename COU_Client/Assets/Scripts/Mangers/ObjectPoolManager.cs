using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoSingleton<ObjectPoolManager>
{
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new();
            pools[prefab] = pool;
        }

        GameObject obj = null; 
        
        while (pool.Count > 0)
        {
            obj = pool.Dequeue();
            if (obj != null && !obj.activeInHierarchy) break;
        }
        
        if (obj == null) obj = Instantiate(prefab);
        
        instanceToPrefab.TryAdd(obj, prefab);

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        obj.GetComponent<IPoolable>()?.OnSpawn();
        
        return obj;
    }

    public void ReturnObject(GameObject obj)
    {
        if (!instanceToPrefab.TryGetValue(obj, out var prefab))
        {
            if(obj != null)
                Destroy(obj);
            return;
        }
        
        obj.SetActive(false);
        pools[prefab].Enqueue(obj);
    }
}