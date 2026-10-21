using UnityEngine;

public class SkillManager : MonoSingleton<SkillManager>
{
    [SerializeField] private SkillPrefabTable _prefabTable;

    public GameObject GetPrefab(long skillId)
    {
        if(_prefabTable == null)
        {
            Debug.LogWarning("[SkillManager] SkillPrefabTable이 연결되지 않았습니다. 씬의 SkillManager 오브젝트를 확인하세요.");
            return null;
        }

        SkillObjectBase prefab = _prefabTable.Get(skillId);

        return prefab != null ? prefab.gameObject : null;
    }

    // 진화용 프리팹. 지정하지 않은 스킬은 null
    public GameObject GetEvolvedPrefab(long skillId)
    {
        if(_prefabTable == null)
        {
            return null;
        }

        SkillObjectBase prefab = _prefabTable.GetEvolved(skillId);

        return prefab != null ? prefab.gameObject : null;
    }
}
