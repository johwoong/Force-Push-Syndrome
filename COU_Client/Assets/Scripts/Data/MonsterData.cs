using System;
using UnityEngine;

// 몬스터 등급. Monster.json의 "monsterType" 문자열을 enum으로 바꿔서 쓴다.
public enum MonsterType { Normal, Elite, MidBoss, Boss, Box }

// 몬스터 기본 정보
[Serializable]
public class MonsterData
{
    public int monsterId;               // 11001~ 일반, 12001~ 엘리트, 13001~ 보스, 14001~ 상자, 15001~ 중간 보스
    public string monsterName;
    public string monsterType;          // "Normal" / "Elite" / "MidBoss" / "Boss" / "Box"
    public int monsterMaxHealthPoint;
    public float monsterMoveSpeed;
    public int monsterContactDamage;
    public string monsterAsset;         // Resources 기준 스프라이트 경로 (예: "Enemy/goblin")

    [NonSerialized] private MonsterType parsedType;
    public MonsterType Type => parsedType;
    // 보스 전투(등장 정리, 스폰 정지, 결계, HP 바)를 하는 타입
    public bool IsBoss => Type is MonsterType.Boss or MonsterType.MidBoss;

    // JsonDataManager에서 JSON을 읽은 직후 한 번 호출
    public void OnLoaded()
    {
        if (!Enum.TryParse(monsterType, out parsedType))
            Debug.LogWarning($"[MonsterData] {monsterId}의 monsterType \"{monsterType}\"을(를) 알 수 없어 Normal로 처리합니다.");
    }
}