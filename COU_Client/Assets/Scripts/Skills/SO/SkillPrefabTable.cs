using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillPrefabEntry
{
    public long SkillId;
    public SkillObjectBase Prefab;

    // 스킬이 진화했을 때 Prefab 대신 소환할 프리팹. 비워 두면 진화해도 Prefab을 씀
    // Prefab과 같은 종류의 초기화 데이터(투사체면 ProjectileData 등)를 받는 SkillObject여야 함
    public SkillObjectBase EvolvedPrefab;
}

[CreateAssetMenu(fileName = "SkillPrefabTable", menuName = "Skill/SkillPrefabTable")]
public class SkillPrefabTable : ScriptableObject
{
    [SerializeField] private List<SkillPrefabEntry> _entries = new();

    private Dictionary<long, SkillObjectBase> _prefabById;
    private Dictionary<long, SkillObjectBase> _evolvedPrefabById;

    // 진화용 프리팹. 지정하지 않은 스킬은 null (진화해도 기본 프리팹을 쓰는 것이 정상이라 경고하지 않음)
    public SkillObjectBase GetEvolved(long skillId)
    {
        if(_prefabById == null)
        {
            Build();
        }

        return _evolvedPrefabById.TryGetValue(skillId, out SkillObjectBase prefab) ? prefab : null;
    }

    public SkillObjectBase Get(long skillId)
    {
        if(_prefabById == null)
        {
            Build();
        }

        if(!_prefabById.TryGetValue(skillId, out SkillObjectBase prefab))
        {
            Debug.LogWarning($"[SkillPrefabTable] 등록되지 않은 스킬 프리팹입니다: {skillId}");
            return null;
        }

        return prefab;
    }

    private void Build()
    {
        _prefabById = new Dictionary<long, SkillObjectBase>();
        _evolvedPrefabById = new Dictionary<long, SkillObjectBase>();

        foreach(SkillPrefabEntry entry in _entries)
        {
            if(!_prefabById.TryAdd(entry.SkillId, entry.Prefab))
            {
                Debug.LogWarning($"[SkillPrefabTable] 중복된 스킬 ID입니다: {entry.SkillId}");
                continue;
            }

            if(entry.EvolvedPrefab != null)
            {
                _evolvedPrefabById.Add(entry.SkillId, entry.EvolvedPrefab);
            }
        }
    }

    private void OnValidate()
    {
        _prefabById = null;
        _evolvedPrefabById = null;
    }
}
