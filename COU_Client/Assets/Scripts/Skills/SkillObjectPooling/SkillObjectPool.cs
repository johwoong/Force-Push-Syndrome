using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillObjectPool : MonoSingleton<SkillObjectPool>
{
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new();
    private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Action<GameObject> initialize = null)
    {
        if(prefab == null)
        {
            return null;
        }

        if(!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        GameObject instance = null;

        while(pool.Count > 0)
        {
            instance = pool.Dequeue();

            if(instance != null && !instance.activeInHierarchy)
            {
                break;
            }

            instance = null;
        }

        if(instance == null)
        {
            instance = Instantiate(prefab);
        }

        _instanceToPrefab.TryAdd(instance, prefab);

        instance.transform.SetPositionAndRotation(position, rotation);
        initialize?.Invoke(instance);
        instance.SetActive(true);
        instance.GetComponent<ISkillPoolable>()?.OnSpawn();

        return instance;
    }

    // 한 번에 많이 꺼내는 스킬이 처음 발동할 때 Instantiate가 몰리지 않도록 미리 만들어 둠
    // 풀에 이미 대기 중인 수가 count보다 적을 때만 부족한 만큼 추가
    public void Prewarm(GameObject prefab, int count)
    {
        if(prefab == null || count <= 0)
        {
            return;
        }

        if(!_pools.TryGetValue(prefab, out Queue<GameObject> pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        for(int i = pool.Count; i < count; i++)
        {
            GameObject instance = Instantiate(prefab);
            instance.SetActive(false);

            _instanceToPrefab.TryAdd(instance, prefab);
            pool.Enqueue(instance);
        }
    }

    public void Return(GameObject instance)
    {
        if(instance == null)
        {
            return;
        }

        if(!_instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        _pools[prefab].Enqueue(instance);
    }
}
