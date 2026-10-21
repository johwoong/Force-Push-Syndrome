using UnityEngine;

// 지정한 위치에 범위 효과를 생성하는 스킬의 베이스. 발동 조건(쿨타임, 적 탐지)은 CooldownSkillBase를 따름
public abstract class AoeSkillBase : CooldownSkillBase
{
    protected void SpawnAoe(Vector2 position)
    {
        Spawn(position, new AoEData(BuildStats(), position));
    }
}
