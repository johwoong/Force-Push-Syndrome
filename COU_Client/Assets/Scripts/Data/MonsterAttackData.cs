using System;
using UnityEngine;

// 몬스터 공격 종류. MonsterAttack.json의 "monsterAttackType" 문자열을 enum으로 바꿔서 쓴다.
// - Melee  : 근접 공격(돌진)
// - Ranged : 투사체. 발 수와 퍼짐 각도로 단발/산탄/원형탄(360)
// - Area   : 범위 공격(경고 장판 → 예고 후 폭발)
// - Trap   : 독 장판. 유지 시간 동안 남아 있고, 밟고 있으면 일정 간격마다 피해
public enum MonsterAttackType { Melee, Ranged, Area, Trap }

// 몬스터 공격 패턴(한 몬스터가 여러 패턴을 가짐)
[Serializable]
public class MonsterAttackData
{
    public int monsterAttackId;
    public int monsterId;         // 이 패턴을 쓰는 몬스터
    public string monsterAttackType;     // "Melee" / "Ranged" / "Area" / "Trap"
    public float monsterAttackCooldown;  // 재사용 대기시간(초)
    public float monsterAttackTriggerRange;  // 이 거리 안에 플레이어가 있어야 사용
    public int monsterAttackDamage;
    public int monsterAttackCount;           // Ranged 발 수 / Area·Trap 장판 수. 0이면 인스펙터 기본값
    public float monsterAttackAngle;         // Ranged 퍼짐 전체 각도(360=원형). 0이면 인스펙터 기본값
    public float monsterAttackDuration;      // Trap 유지 시간(초). 0이면 인스펙터 기본값

    [NonSerialized] private MonsterAttackType parsedAttackType;
    public MonsterAttackType AttackType => parsedAttackType;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출
    public bool OnLoaded()
    {
        return Enum.TryParse(monsterAttackType, out parsedAttackType);
    }
}