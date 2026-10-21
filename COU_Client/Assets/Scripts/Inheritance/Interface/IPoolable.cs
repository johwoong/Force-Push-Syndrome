using System;
using UnityEngine;

public interface IPoolable
{
    event Action<GameObject> OnBeforeReturn;

    void OnSpawn();
    
    void OnDespawn();
}
