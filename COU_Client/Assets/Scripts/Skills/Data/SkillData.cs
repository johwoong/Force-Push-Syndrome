using System;
using UnityEngine;

public enum SkillTargetType
{
    None,
    Nearest,
    Forward
}

// 시트의 skillCategory 값과 같아야 함 (0: 액티브, 1: 패시브)
public enum SkillCategory
{
    Active = 0,
    Passive = 1
}

[Serializable]
public class SkillData : ISerializationCallbackReceiver
{
    public long skillId;
    public string skillName;
    public SkillCategory skillCategory;
    public string skillTargetType;
    public float skillCooldown;
    public float skillSpeed;
    public float skillDamage;
    public float skillRange;
    public string skillDescription;
    public string level1SkillDescription;
    public string level2SkillDescription;
    public string level3SkillDescription;
    public string level4SkillDescription;
    public string level5SkillDescription;

    [NonSerialized] public SkillTargetType TargetType;

    public string IconPath => GameConstants.Paths.SkillIcon_Path + skillName;

    // 테이블 원본을 건드리지 않고 값을 바꿔 쓰기 위한 얕은 복사 (필드가 모두 값 타입/문자열이라 충분함)
    //public SkillData Clone()
    //{
    //    return (SkillData)MemberwiseClone();
    //}

    public void OnAfterDeserialize()
    {
        if(string.IsNullOrEmpty(skillTargetType))
        {
            return;
        }

        if(!Enum.TryParse(skillTargetType, true, out TargetType))
        {
            Debug.LogWarning($"[SkillData] 알 수 없는 skillTargetType입니다: {skillTargetType} (ID: {skillId})");
        }
    }

    public void OnBeforeSerialize()
    {

    }
}
