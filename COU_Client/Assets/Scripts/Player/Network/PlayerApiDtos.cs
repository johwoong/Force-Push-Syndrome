using Newtonsoft.Json;

// 플레이어 스탯 서버 API 응답 형식 (필드 이름은 서버 응답과 동일하게 유지)

// GET /api/players/me/stats
public class FinalStatsResponse
{
    public int finalAttack;
    public int finalHp;
    public int finalDefense;
    public int finalPotionRecovery;
    public int criticalDamage;
    public int criticalChance;
    public int skillDamage;
    public float moveSpeed;
    public float maxMoveSpeed;
    public float lootRadius;
    public StatBreakdown breakdown;
}

public class StatBreakdown
{
    [JsonProperty("base")]            // base는 C# 예약어라 이름을 따로 지정
    public StatParts baseStats;
    public StatParts equipment;
    public StatParts evolution;
}

public class StatParts
{
    public int attack;
    public int hp;
    public int defense;
    public int potionRecovery;
}