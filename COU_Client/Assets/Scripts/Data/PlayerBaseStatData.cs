using System;
using UnityEngine;

// 플레이어 기본 스탯. PlayerBaseStat.json의 유일한 행. 필드 이름이 JSON 키와 같아야 한다.
[Serializable]
public class PlayerBaseStatData
{
    // JSON에 없는 항목은 아래 기본값이 사용된다.
    public int playerBaseAttack = 100;
    public int playerBaseHp = 1000;
    public int playerBaseCriticalDamage = 200;
    public int playerBaseCriticalChance = 5;
    public int playerBaseSkillDamage = 100;
    public float playerBaseMoveSpeed = 9f;
    public float playerBaseMaxMoveSpeed = 16f;
    public float playerBaseLootRadius = 2f;

    // 파일 값은 Inspector의 [Min], [Range]가 적용되지 않으므로 직접 보정
    public void Sanitize()
    {
        playerBaseAttack = Mathf.Max(1, playerBaseAttack);
        playerBaseHp = Mathf.Max(1, playerBaseHp);
        playerBaseCriticalDamage = Mathf.Max(1, playerBaseCriticalDamage);
        playerBaseSkillDamage = Mathf.Max(1, playerBaseSkillDamage);
        playerBaseMoveSpeed = Mathf.Max(0.1f, playerBaseMoveSpeed);
        playerBaseMaxMoveSpeed = Mathf.Max(playerBaseMoveSpeed, playerBaseMaxMoveSpeed);
        playerBaseLootRadius = Mathf.Max(0f, playerBaseLootRadius);

        int clampedChance = Mathf.Clamp(playerBaseCriticalChance, 0, 100);
        if (clampedChance != playerBaseCriticalChance)
        {
            Debug.LogWarning("[PlayerBaseStatData] playerBaseCriticalChance는 0~100이어야 합니다. " + playerBaseCriticalChance + " → " + clampedChance);
            playerBaseCriticalChance = clampedChance;
        }
    }
}
