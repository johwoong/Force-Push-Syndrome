using System;

// 스테이지 로스터 한 행: "이 스테이지의 이 슬롯은 이 몬스터이고, 이 배율을 받는다".
// Wave.json의 monsterSlot이 같은 stageId 안의 행을 가리킨다.
[Serializable]
public class StageRosterData
{
    public int stageRosterId;
    public int stageId;               // Stage.json 참조
    public string monsterSlot;        // 슬롯 이름. 같은 stageId 안에서 중복 없음
    public int monsterId;             // Monster.json 참조
    public float monsterHpScale;      // 최대 체력 배율 (0 초과)
    public float monsterDamageScale;  // 접촉 피해와 MonsterAttack 피해 배율 (0 초과)
    public int patternId;             // SpawnPattern.json 참조. 0이면 웨이브 행의 patternId
}
