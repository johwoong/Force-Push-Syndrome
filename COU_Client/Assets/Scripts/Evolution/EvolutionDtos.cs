// 진화 서버 API 응답 형식 (필드 이름은 서버 응답과 동일하게 유지)

// POST /api/players/me/evolution/upgrade
public class EvolutionUpgradeResponse
{
    public string upgradedStatType;   // PlayerStatType 값
    public int evolutionStep;
    public string nextStatType;       // PlayerStatType 값
    public int goldSpent;
    public PlayerStatData playerStat; // 계정 데이터 형식 그대로
    public CurrencyData currency;     // 차감 후 재화
}