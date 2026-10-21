using System.Collections.Generic;
using UnityEngine;

// 장착하는 동안 오브젝트가 유지되는 스킬(회전 칼날 등)의 베이스
// 장착/레벨 변경/해제 시점에 오브젝트를 생성하고 정리함
public abstract class PersistentSkillBase : SkillBase
{
    // 오브젝트를 만들 때 수치가 고정되는 보너스. 이 값들이 바뀔 때만 다시 생성함
    // (쿨타임, 치명타 등 오브젝트와 관계없는 보너스가 바뀔 때마다 다시 생성되지 않도록 함)
    // Range는 지속형 오브젝트의 배치(회전 반지름 등)에 쓰이므로 포함
    private static readonly SkillPercentStat[] SpawnPercentStats =
    {
        SkillPercentStat.Damage,
        SkillPercentStat.Speed,
        SkillPercentStat.Area,
        SkillPercentStat.Range
    };

    private static readonly SkillFlatStat[] SpawnFlatStats =
    {
        SkillFlatStat.ProjectileCount
    };

    private readonly List<GameObject> _spawnedObjects = new();

    // 마지막으로 생성할 때의 보너스 합계
    private readonly float[] _spawnedPercents = new float[SpawnPercentStats.Length];
    private readonly int[] _spawnedFlats = new int[SpawnFlatStats.Length];

    // 쿨타임 발동이 없으므로 기본은 비어 있음. 매 프레임 갱신이 필요하면 오버라이드
    public override void Tick(float deltaTime)
    {
    }

    protected override void OnEquip()
    {
        modifiers.OnChanged += HandleModifiersChanged;
        SpawnAll();
    }

    protected override void OnUnequip()
    {
        modifiers.OnChanged -= HandleModifiersChanged;
        DespawnAll();
    }

    // 레벨이 바뀌면 개수/크기 등이 달라질 수 있으므로 전부 정리하고 다시 생성
    protected override void OnLevelChanged()
    {
        Respawn();
    }

    // 진화하면 개수 등이 달라질 수 있으므로 다시 생성
    protected override void OnEvolved()
    {
        Respawn();
    }

    // 오브젝트는 생성 시점의 수치(개수, 대미지 등)를 그대로 쓰므로, 관련 보너스가 바뀌면 다시 만들어야 반영됨
    private void HandleModifiersChanged()
    {
        if(HasSpawnBonusChanged())
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        DespawnAll();
        SpawnAll();
    }

    private void SpawnAll()
    {
        CaptureSpawnBonuses();
        SpawnObjects();
    }

    private void CaptureSpawnBonuses()
    {
        for(int i = 0; i < SpawnPercentStats.Length; i++)
        {
            _spawnedPercents[i] = modifiers.GetPercentTotal(SpawnPercentStats[i]);
        }

        for(int i = 0; i < SpawnFlatStats.Length; i++)
        {
            _spawnedFlats[i] = modifiers.GetFlatTotal(SpawnFlatStats[i]);
        }
    }

    private bool HasSpawnBonusChanged()
    {
        for(int i = 0; i < SpawnPercentStats.Length; i++)
        {
            if(!Mathf.Approximately(_spawnedPercents[i], modifiers.GetPercentTotal(SpawnPercentStats[i])))
            {
                return true;
            }
        }

        for(int i = 0; i < SpawnFlatStats.Length; i++)
        {
            if(_spawnedFlats[i] != modifiers.GetFlatTotal(SpawnFlatStats[i]))
            {
                return true;
            }
        }

        return false;
    }

    // SpawnPersistent로 현재 레벨에 맞는 오브젝트를 생성
    protected abstract void SpawnObjects();

    protected void SpawnPersistent<TInitData>(Vector3 position, TInitData initData)
    {
        GameObject instance = Spawn(position, initData);

        if(instance != null)
        {
            _spawnedObjects.Add(instance);
        }
    }

    private void DespawnAll()
    {
        foreach(GameObject instance in _spawnedObjects)
        {
            // 스스로 풀에 반환된 오브젝트를 다시 반환하면 풀에 중복 등록되므로 활성 상태만 반환
            if(instance != null && instance.activeInHierarchy)
            {
                SkillObjectPool.Instance.Return(instance);
            }
        }

        _spawnedObjects.Clear();
    }
}
