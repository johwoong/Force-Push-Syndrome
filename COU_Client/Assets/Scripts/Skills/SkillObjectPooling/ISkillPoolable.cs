using System;
using UnityEngine;

public interface ISkillPoolable
{
    event Action<GameObject> OnBeforeReturn;

    void OnSpawn();

    void OnDespawn();
}
